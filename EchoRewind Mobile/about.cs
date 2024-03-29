﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoRelayInstaller
{
    public partial class AboutPage : ContentPage
    {
        public AboutPage()
        {
            Title = "Echo Navigator Standalone";

            var menu = new StackLayout();

            var header = new Label
            {
                Text = "About",
                HorizontalOptions = LayoutOptions.Center,
                Padding = new Thickness(0, 0, 0, 20),
                FontSize = 24,
                TranslationY = 10,
            };

            menu.Children.Add(header);

            var aboutText = new Label
            {
                Text = "Echo Navigator Standalone is a tool created by Aldin101 that allows you to patch Echo VR APKs for use with Echo Relay.\n\nNeed help? Contact me on Discord: @aldin101\n\nEcho Navigator Standalone is licensed under the MIT Licence and the source code is available on GitHub"
            };

            menu.Children.Add(aboutText);

            var versionNumber = new Label
            {
                Text = "Version: " + AppInfo.VersionString,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.EndAndExpand,

            };
            menu.Children.Add(versionNumber);

            Content = menu;
        }

    }
}
