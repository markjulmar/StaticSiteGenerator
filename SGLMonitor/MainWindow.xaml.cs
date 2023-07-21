using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using SGLMonitor.ViewModels;
using MDPGen.Core.Services;
using System.Windows.Input;

namespace SGLMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var appVersion = typeof(MainViewModel).Assembly.GetName().Version;
            var viewModel = new MainViewModel
            {
                ShowError = OnShowError,
                RefreshBrowser = () =>
                {
                    Dispatcher.BeginInvoke(
                        new Action(() => webBrowser.Refresh()));
                }
            };

            ((INotifyCollectionChanged) viewModel.Status).CollectionChanged += OnLogChanged;
            DataContext = viewModel;
        }

        private void OnShowError(string errorText)
        {
            TraceLog.Write(TraceType.Error, errorText);
            MessageBox.Show(this, errorText);
        }

        private void OnLogChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var scrollViewer = GetDescendantByType(LogList, typeof(ScrollViewer)) as ScrollViewer;
                scrollViewer?.ScrollToEnd();
            }
        }

        public static Visual GetDescendantByType(Visual element, Type type)
        {
            if (element == null)
                return null;
            if (element.GetType() == type)
                return element;

            Visual foundElement = null;
            (element as FrameworkElement)?.ApplyTemplate();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = GetDescendantByType(visual, type);
                if (foundElement != null)
                    break;
            }
            return foundElement;
        }

        private void OnChooseSiteConfigFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "SiteConfiguration files (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true,
                Title = "Locate the SiteConfiguration file"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                ((MainViewModel) DataContext).SiteConfigFile = openFileDialog.FileName;
            }
        }

        private void OnChooseOutputFolder(object sender, RoutedEventArgs e)
        {
            string desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var dlg = new CommonOpenFileDialog
            {
                Title = "Choose output folder",
                IsFolderPicker = true,
                InitialDirectory = desktopFolder,
                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                DefaultDirectory = desktopFolder,
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ((MainViewModel) DataContext).DestinationFolder = dlg.FileName;
            }
        }

        void CopyToClipboardCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        void CopyToClipboardExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var selected = ((ListBox)sender).SelectedItem;
            if (selected != null)
                Clipboard.SetText(selected.ToString());
        }

        void ContextCopyToClipboardExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var selected = ((MenuItem)sender).DataContext;
            if (selected != null)
                Clipboard.SetText(selected.ToString());
        }
    }
}
