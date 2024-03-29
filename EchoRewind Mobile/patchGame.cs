﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Maui;
using System.Threading.Tasks;
using BsDiff;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Diagnostics;
using QuestPatcher.Zip;

namespace EchoRelayInstaller
{
    public partial class patchGame : ContentPage
    {

        Label header;

        public class GameConfig
        {
            public string apiservice_host { get; set; }
            public string configservice_host { get; set; }
            public string loginservice_host { get; set; }
            public string matchingservice_host { get; set; }
            public string serverdb_host { get; set; }
            public string transactionservice_host { get; set; }
            public string publisher_lock { get; set; }
        };

        public String apkGlobal;
        public Servers[] serversGlobal;
        public int selectedServerGlobal;

        public patchGame(String apk, Servers[] servers, int selectedServer)
        {
            apkGlobal = apk;
            serversGlobal = servers;
            selectedServerGlobal = selectedServer;

            Title = "Echo Navigator Standalone";
            
            var patchGameMenu = new StackLayout();

            header = new Label
            {
                Text = "Patching APK...",
                FontSize = 24,
                HorizontalOptions = LayoutOptions.Center
            };
            patchGameMenu.Children.Add(header);

            Content = patchGameMenu;


            patchingSystems(servers, selectedServer, apk);
        }

        public bool patching = false;
        string downloadsPath;

        public async Task patchingSystems(Servers[] servers, int selectedServer, String apk)
        {
#if ANDROID
            downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
#endif

#if WINDOWS
            downloadsPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/downloads/";
#endif
            string serverIP;
            if (servers[selectedServer].port == null || servers[selectedServer].port == "")
            {
                serverIP = $"{servers[selectedServer].IP}";
            } else {
                serverIP = $"{servers[selectedServer].IP}:{servers[selectedServer].port}";
            }

            GameConfig config = new GameConfig();
            config.apiservice_host = $"http://{serverIP}/api";
            config.configservice_host = $"ws://{serverIP}/config";
            config.loginservice_host = $"ws://{serverIP}/login?auth={GlobalVariables.passwordBox.Text}&displayname={GlobalVariables.usernameBox.Text}";
            config.matchingservice_host = $"ws://{serverIP}/matching";
            config.serverdb_host = $"ws://{serverIP}/serverdb";
            config.transactionservice_host = $"ws://{serverIP}/transaction";
            config.publisher_lock = servers[selectedServer].publisherLock;

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(config);

            var filePath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Personal), "config.json");
            await File.WriteAllTextAsync(filePath, json);
            string[] apkPath = new string[] { apk };

            if (File.Exists(Path.Join(Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "crashLog.txt")))
                File.Delete(Path.Join(Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "crashLog.txt"));


            Thread thread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                StartPatching(apkPath);
            });
            patching = true;
            thread.Start();
            
            while (thread.IsAlive && File.Exists(Path.Join(Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "crashLog.txt")) == false)
            {
                await Task.Delay(1000);
            }
            patching = false;

            if (File.Exists(Path.Join(Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "crashLog.txt")))
            {
                var errorMessage = await File.ReadAllTextAsync(Path.Join(Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "crashLog.txt"));
                File.Delete(Path.Join(Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "crashLog.txt"));
                await DisplayAlert("Error", errorMessage, "OK");
                await Navigation.PopAsync();
                return;
            }

#if WINDOWS
            header.Text = "APK Ready, it can be found in your downloads folder\nYou can load it onto your headset with SideQuest";
#endif
#if ANDROID
            header.Text = "APK Ready, it can be found in your downloads folder\n\nIf you are using an Android phone you can use Bugjaeger to load it onto your headset.";
#endif

        }

        protected override bool OnBackButtonPressed()
        {
            if (patching)
            {
                return true;
            }
            else
            {
                return base.OnBackButtonPressed();
            }
        }

        private class Hashes
        {
            public const string APK = "c14c0f68adb62a4c5deaef46d046f872"; // Hash of 
        }

        string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public async Task ExitLog(string errorString, bool error = true)
        {
            if (error)
            {
                Console.WriteLine(errorString);
                var crashLog = new StringBuilder();
                crashLog.AppendLine(errorString);
                var crashLogPath = Path.Join(Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "crashLog.txt");
                await File.WriteAllTextAsync(crashLogPath, crashLog.ToString());
                while (true)
                {
                    Thread.Sleep(9999999);
                }
            }
            return;
        }


        static bool CheckJson(JObject jsonObject)
        {
            // Make sure the services exist
            if (!jsonObject.ContainsKey("configservice_host"))
                return false;
            if (!jsonObject.ContainsKey("loginservice_host"))
                return false;
            if (!jsonObject.ContainsKey("matchingservice_host"))
                return false;
            if (!jsonObject.ContainsKey("publisher_lock"))
                return false;

            // Make sure they are all strings
            if (jsonObject.GetValue("configservice_host")!.Type != JTokenType.String)
                return false;
            if (jsonObject.GetValue("loginservice_host")!.Type != JTokenType.String)
                return false;
            if (jsonObject.GetValue("matchingservice_host")!.Type != JTokenType.String)
                return false;
            if (jsonObject.GetValue("publisher_lock")!.Type != JTokenType.String)
                return false;

            // Make sure the hosts are valid URLs
            if (!Uri.IsWellFormedUriString(jsonObject.Value<string>("configservice_host"), UriKind.Absolute))
                return false;
            if (!Uri.IsWellFormedUriString(jsonObject.Value<string>("loginservice_host"), UriKind.Absolute))
                return false;
            if (!Uri.IsWellFormedUriString(jsonObject.Value<string>("matchingservice_host"), UriKind.Absolute))
                return false;

            return true;
        }

        public async Task CheckPrerequisites(string originalApkPath, string configPath)
        {
            if (!File.Exists(originalApkPath))
                await ExitLog("Failed to copy Echo VR APK, please restart the headset and try again");

            if (CalculateMD5(originalApkPath) != Hashes.APK)
                await ExitLog("Invalid EchoVR APK (Hash mismatch) : Please download the correct APK, it is version 4987566, it also has the most downloads on OculusDB");

            if (!File.Exists(configPath))
                await ExitLog("Invalid Config: Failed to write config file, please restart the headset and try again");

            string ConfigString;
            try
            {
                ConfigString = File.ReadAllText(configPath);
            }
            catch (Exception)
            {
                await ExitLog("Invalid Config: Failed to read config file, please restart the headset and try again");
                return; // Just to make the compiler happy
            }

            JObject ConfigJson;
            try
            {
                ConfigJson = JObject.Parse(ConfigString);
            }
            catch (Exception)
            {
                await ExitLog("Invalid Config: Json could not be parsed, please reastart the headset and try again");
                return;
            }

            if (!CheckJson(ConfigJson))
                await ExitLog("Invalid Config: Service endpoints incorrect, please reastart the headset and try again");

            using var libpnsovr_patchStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EchoRelayInstaller.Resources.Raw.libpnsovr_patch.bin");
            if (libpnsovr_patchStream == null)
                await ExitLog("libpnsovr_patch missing!");
    
            using var libr15_patch = Assembly.GetExecutingAssembly().GetManifestResourceStream("EchoRelayInstaller.Resources.Raw.libr15_patch.bin");
            if (libr15_patch == null)
                await ExitLog("libr15_patch missing!");
        }

        public async void StartPatching(string[] args)
        {
            Console.WriteLine("Parsing arguments...");
            if (args.Length == 0)
                await ExitLog("APK path missing, please restart the headset and try again");


            Console.WriteLine("Generating paths...");
            var originalApkPath = args[0];
            var baseDir = Path.GetDirectoryName(args[0]);
            var newApkPath = Path.Join(downloadsPath, $"r15_goldmaster_store_patched.apk");
            //check if apk already exists
            if (File.Exists(newApkPath))
                newApkPath = Path.Join(downloadsPath, $"r15_goldmaster_store_patched_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.apk");
            var configPath = Path.Join(Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "config.json");

            Console.WriteLine("Checking prerequisites...");
            await CheckPrerequisites(originalApkPath, configPath);

            Console.WriteLine("Creating extraction directory...");
            var extractedApkDir = Path.Join(Path.GetTempPath(), "EchoQuestUnzip");
            if (Directory.Exists(extractedApkDir))
                Directory.Delete(extractedApkDir, true);
            Directory.CreateDirectory(extractedApkDir);

            Console.WriteLine("Extracting files...");
            using (var archive = ZipFile.OpenRead(originalApkPath))
            {
                foreach (var entry in archive.Entries)
                {
                    var destinationPath = Path.GetFullPath(Path.Combine(extractedApkDir, entry.FullName));
                    if (!destinationPath.StartsWith(extractedApkDir, StringComparison.Ordinal))
                        throw new InvalidOperationException("Trying to create a file outside of the extraction directory.");
                    if (entry.Name != "")
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, true);
                }
            }
            var extractedLocalPath = Path.Join(extractedApkDir, "assets", "_local");
            var extractedPnsRadOvrPath = Path.Join(extractedApkDir, @"lib", "arm64-v8a", "libpnsovr.so");
            var extractedr15Path = Path.Join(extractedApkDir, @"lib", "arm64-v8a", "libr15.so");

            Console.WriteLine("Copying config.json...");
            Directory.CreateDirectory(extractedLocalPath); // No need to check for existence, as the hash will capture that
            File.Copy(configPath, Path.Join(extractedLocalPath, "config.json"));


            Console.WriteLine("Patching pnsradovr.so...");
            using var oldPnsOvrFile = File.OpenRead(extractedPnsRadOvrPath);
            using var newPnsOvrFile = File.Create(extractedPnsRadOvrPath + "_patched");
            BinaryPatch.Apply(oldPnsOvrFile, () => Assembly.GetExecutingAssembly().GetManifestResourceStream("EchoRelayInstaller.Resources.Raw.libpnsovr_patch.bin"), newPnsOvrFile);
            oldPnsOvrFile.Close();
            newPnsOvrFile.Close();

            Console.WriteLine("Patching libr15.so...");
            using var oldr15File = File.OpenRead(extractedr15Path);
            using var newr15File = File.Create(extractedr15Path + "_patched");
            BinaryPatch.Apply(oldr15File, () => Assembly.GetExecutingAssembly().GetManifestResourceStream("EchoRelayInstaller.Resources.Raw.libr15_patch.bin"), newr15File);
            oldr15File.Close();
            newr15File.Close();

            Console.WriteLine("Swapping pnsradovr.so...");
            File.Delete(extractedPnsRadOvrPath);
            File.Move(extractedPnsRadOvrPath + "_patched", extractedPnsRadOvrPath);

            Console.WriteLine("Swapping libr15.so...");
            File.Delete(extractedr15Path);
            File.Move(extractedr15Path + "_patched", extractedr15Path);

            Console.WriteLine("Creating miscellaneous directory...");
            string miscDir = Path.Join(Path.GetTempPath(), "EchoQuest");
            if (Directory.Exists(miscDir))
                Directory.Delete(miscDir, true);
            Directory.CreateDirectory(miscDir);

            Console.WriteLine("Creating unsigned apk...");
            string unsignedApkPath = Path.Join(miscDir, "unsigned.apk");
            ZipFile.CreateFromDirectory(extractedApkDir, unsignedApkPath);

            Console.WriteLine("Signing unsigned apk...");
            var unsignedApkSteam = File.Open(unsignedApkPath, FileMode.Open);
            //sign APK (this is how you do it with this lib)
            QuestPatcher.Zip.ApkZip.Open(unsignedApkSteam).Dispose();
            unsignedApkSteam.Close();

            Console.WriteLine("Moving signed apk...");
            if (File.Exists(newApkPath))
                File.Delete(newApkPath);
            File.Move(unsignedApkPath, newApkPath);

            Console.WriteLine("Cleaning up temporary files...");
            Directory.Delete(extractedApkDir, true);
            Directory.Delete(miscDir, true);
            await ExitLog("Finished creating patched apk! (r15_goldmaster_store_patched.apk)", false);
        }
    }
}
