using System.Windows;
using LoadOrderKeeper.ViewModels;

namespace LoadOrderKeeper.Views
{
    public partial class CommentInputDialog : Window
    {
        public CommentInputDialog()
        {
            InitializeComponent();

            var viewModel = new CommentInputViewModel();
            viewModel.OkRequested += (s, e) => DialogResult = true;
            viewModel.CancelRequested += (s, e) => DialogResult = false;
            DataContext = viewModel;
        }

        public string Comment => ((CommentInputViewModel)DataContext).Comment;
    }
}
