using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using WebViewControl;

namespace SampleWebView.Avalonia {

    internal class MainWindow : Window {

        public MainWindow() {
            WebView.Settings.OsrEnabled = false;
            WebView.Settings.LogFile = "ceflog.txt";
            WebView.Settings.EnableErrorLogOnly = true;
            if (Environment.OSVersion.Platform == PlatformID.Unix) {
                WebView.Settings.OsrEnabled = true;
                WebView.Settings.OverrideCefSettings = new[] {
                    new KeyValuePair<string, object>("NoSandbox", true),
                    new KeyValuePair<string, object>("MultiThreadedMessageLoop", true),
                    new KeyValuePair<string, object>("LocalesDirPath", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "locales"))
                };
                WebView.Settings.CustomFlags = new[] {
                    // new KeyValuePair<string, string>("disable-gpu", null),
                    new KeyValuePair<string, string>("no-zygote", null),
                    new KeyValuePair<string, string>("single-process", null),
                    // enable experimental feature flags
                    // new KeyValuePair<string, string>("enable-experimental-web-platform-features", null)
                };
            }

            AvaloniaXamlLoader.Load(this);

            DataContext = new MainWindowViewModel(this.FindControl<WebView>("webview"));
        }
    }
}