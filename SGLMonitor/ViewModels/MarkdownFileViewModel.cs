using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using MDPGen.Core.Infrastructure;
using XamarinUniversity.Infrastructure;

namespace SGLMonitor.ViewModels
{
    public class MarkdownFileViewModel : SimpleViewModel
    {
        private readonly MainViewModel owner;
        public IList<MarkdownFileViewModel> Children { get; }
        public string Name => Node.Title;

        public bool IsSelected
        {
            get => owner.SelectedFile == this;
            set
            {
                if (value)
                    owner.SelectedFile = this;
            }
        }

        public void OnSelectionChanged() => RaisePropertyChanged(nameof(IsSelected));
        public string SourceFilename => Node.Filename;
        public ContentPage Node { get; }

        public ICommand Edit { get; }
        public ICommand OpenUrl { get; }

        public MarkdownFileViewModel(MainViewModel owner, ContentPage node)
        {
            this.owner = owner;
            Node = node;
            OpenUrl = new DelegateCommand(() => Process.Start(new ProcessStartInfo(MainViewModel.UrlRoot + Node.Url) { UseShellExecute = true }));
            Edit = new DelegateCommand(() => Process.Start(new ProcessStartInfo(Node.Filename) { UseShellExecute = true }));
            Children = new ObservableCollection<MarkdownFileViewModel>(
                Node.Children.Select(tn => new MarkdownFileViewModel(owner, tn)));
        }
    }
}