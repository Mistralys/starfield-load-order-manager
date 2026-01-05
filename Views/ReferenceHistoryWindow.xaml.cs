using System.Windows;
using System.Windows.Input;
using LoadOrderKeeper.ViewModels;

namespace LoadOrderKeeper.Views
{
    public partial class ReferenceHistoryWindow : Window
    {
        public ReferenceHistoryWindow()
        {
            InitializeComponent();

            if (DataContext is ReferenceHistoryViewModel viewModel)
            {
                viewModel.CloseRequested += (s, e) => Close();
            }
        }

        private void OnVersionDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ReferenceHistoryViewModel viewModel && viewModel.RollbackCommand.CanExecute(null))
            {
                viewModel.RollbackCommand.Execute(null);
            }
        }
    }
}
