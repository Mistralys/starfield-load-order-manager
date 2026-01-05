using System.Windows;
using LoadOrderKeeper.ViewModels;

namespace LoadOrderKeeper.Views
{
    public partial class CommentInputDialog : Window
    {
        public CommentInputDialog()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            
            // Set DataContext after subscribing to DataContextChanged
            var viewModel = new CommentInputViewModel();
            DataContext = viewModel;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is CommentInputViewModel oldVm)
            {
                oldVm.OkRequested -= OnOkRequested;
                oldVm.CancelRequested -= OnCancelRequested;
            }

            if (e.NewValue is CommentInputViewModel newVm)
            {
                newVm.OkRequested += OnOkRequested;
                newVm.CancelRequested += OnCancelRequested;
            }
        }

        private void OnOkRequested(object? sender, System.EventArgs e)
        {
            DialogResult = true;
        }

        private void OnCancelRequested(object? sender, System.EventArgs e)
        {
            DialogResult = false;
        }

        public string Comment => ((CommentInputViewModel)DataContext).Comment;

        protected override void OnClosed(System.EventArgs e)
        {
            if (DataContext is CommentInputViewModel viewModel)
            {
                viewModel.OkRequested -= OnOkRequested;
                viewModel.CancelRequested -= OnCancelRequested;
            }
            base.OnClosed(e);
        }
    }
}
