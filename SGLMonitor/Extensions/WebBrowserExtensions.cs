using System;
using System.Configuration;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using SGLMonitor.ViewModels;

namespace SGLMonitor.Extensions
{
    /// <summary>
    /// This is a simple attached property to support binding
    /// with a WebBrowser. Note that it only supports ONE WebBrowser
    /// </summary>
    public static class WebBrowserExtensions
    {
        public static readonly DependencyProperty UriSourceProperty =
            DependencyProperty.RegisterAttached(
            "UriSource", typeof(string), typeof(WebBrowserExtensions),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, 
                OnUriSourcePropertyChanged));

        public static string GetUriSource(WebBrowser view)
        {
            return (string)view.GetValue(UriSourceProperty);
        }

        public static void SetUriSource(WebBrowser view, string value)
        {
            view.SetValue(UriSourceProperty, value);
        }

        public static void HideScriptErrors(WebBrowser wb, bool hide)
        {
            FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;
            object objComWebBrowser = fiComWebBrowser.GetValue(wb);
            objComWebBrowser?.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { hide });
        }

        private static bool initialized;
        private static void OnUriSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var webView = sender as WebBrowser;
            if (webView != null)
            {
                if (!initialized)
                {
                    string disableScriptErrors = ConfigurationManager.AppSettings["disableScriptErrors"];
                    if (string.Compare(disableScriptErrors, "true", StringComparison.InvariantCultureIgnoreCase) == 0)
                        HideScriptErrors(webView, true);
                    webView.Navigated += OnUrlChanged;
                    initialized = true;
                }

                string url = e.NewValue?.ToString() ?? "";
                if (string.IsNullOrEmpty(url))
                    webView.Navigate("about:blank");
                else
                {
                    if (url.StartsWith("/"))
                        url = MainViewModel.UrlRoot + url;
                    if (webView.Source.AbsoluteUri != url)
                        webView.Navigate(new Uri(url));
                }
            }
        }

        private static void OnUrlChanged(object sender, NavigationEventArgs e)
        {
            var webView = sender as WebBrowser;
            string url = GetUriSource(webView);
            if (url == "")
                url = "about:blank";

            if (url != e.Uri.AbsoluteUri)
            {
                SetUriSource(webView, e.Uri.AbsoluteUri);
            }
        }
    }
}
