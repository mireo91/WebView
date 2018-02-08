﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace WebViewControl {

    public partial class ReactView : ContentControl, IDisposable {

        private const string PathSeparator = "/";
        private const string RootObject = "__Root__";
        private const string ReadyEventName = "Ready";

        private static readonly string AssemblyName = typeof(ReactView).Assembly.GetName().Name;
        private static readonly string BuiltinResourcesPath = AssemblyName + "/Resources/";
        private static readonly string DefaultUrl = BuiltinResourcesPath + "index.html";

        private readonly WebView webView = new WebView();
        private Assembly userCallingAssembly;

        private bool isSourceSet = false;
        private Listener readyEventListener;

        public ReactView() {
            Initialize(CreateRootPropertiesObject());
        }

        public ReactView(object rootProperties) {
            Initialize(rootProperties);
        }

        private void Initialize(object rootProperties) {
            userCallingAssembly = WebView.GetUserCallingAssembly();

            webView.AllowDeveloperTools = true;
            webView.DisableBuiltinContextMenus = true;
            webView.IgnoreMissingResources = false;
            webView.AttachListener(ReadyEventName, () => IsReady = true, executeInUI: false);

            if (rootProperties != null) {
                webView.RegisterJavascriptObject("__RootProperties__", rootProperties, executeCallsInUI: false);
            }

            Content = webView;
        }

        public event Action Ready {
            add { readyEventListener = webView.AttachListener(ReadyEventName, value); }
            remove {
                if (readyEventListener != null) {
                    webView.DetachListener(readyEventListener);
                }
            }
        }

        public event Action<UnhandledExceptionEventArgs> UnhandledAsyncException {
            add { webView.UnhandledAsyncException += value; }
            remove { webView.UnhandledAsyncException -= value; }
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if (!isSourceSet) {
                // wait for default stylesheet property to be set
                LoadSource();
                isSourceSet = true;
            }
        }

        private void LoadSource() {
            const string JsExtension = ".js";

            var source = NormalizeUrl(Source ?? "");
            if (source.EndsWith(JsExtension)) {
                source = source.Substring(0, source.Length - JsExtension.Length);
            }

            var filenameParts = source.Split(new[] { PathSeparator }, StringSplitOptions.None);

            // eg: example/dist/source.js
            // defaultSource = ./dist/source.js
            // baseUrl = /AssemblyName/example/
            var sourceDepth = filenameParts.Length >= 2 ? 2 : 1; 
            var defaultSource = "./" + string.Join(PathSeparator, filenameParts.Reverse().Take(sourceDepth).Reverse()); // take last 2 parts of the path
            var baseUrl = ToFullUrl(string.Join(PathSeparator, filenameParts.Take(filenameParts.Length - sourceDepth))) + PathSeparator;

            var urlParams = new List<string>() {
                PathSeparator + BuiltinResourcesPath,
                baseUrl,
                defaultSource
            };

            if (DefaultStyleSheet != null) { 
                urlParams.Add(NormalizeUrl(ToFullUrl(DefaultStyleSheet)));
            }

            var url = WebView.BuildEmbeddedResourceUrl(AssemblyName, DefaultUrl + "?" + string.Join("&", urlParams));
            webView.Address = url;
        }

        private static string NormalizeUrl(string url) {
            return url.Replace("\\", PathSeparator);
        }

        private string ToFullUrl(string url) {
            return (url.StartsWith(PathSeparator) || url.Contains(Uri.SchemeDelimiter)) ? url : $"/{userCallingAssembly.GetName().Name}/{url}";
        }

        public void Dispose() {
            webView.Dispose();
        }

        protected void ExecuteMethodOnRoot(string methodCall, params string[] args) {
            webView.ExecuteScriptFunction(RootObject + "." + methodCall, args);
        }

        protected T EvaluateMethodOnRoot<T>(string methodCall, params string[] args) {
            return webView.EvaluateScriptFunction<T>(RootObject + "." + methodCall, args);
        }

        protected virtual object CreateRootPropertiesObject() {
            return null;
        }

        protected virtual string Source => null;

        public static readonly DependencyProperty DefaultStyleSheetProperty = DependencyProperty.Register(
            nameof(DefaultStyleSheet), 
            typeof(string),
            typeof(ReactView));

        public string DefaultStyleSheet {
            get { return (string) GetValue(DefaultStyleSheetProperty); }
            set { SetValue(DefaultStyleSheetProperty, value); }
        }

        public bool IsReady { get; private set; }
    }
}
