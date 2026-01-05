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
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ReferenceHistoryViewModel oldViewModel)
            {
                oldViewModel.CloseRequested -= OnCloseRequested;
            }

            if (e.NewValue is ReferenceHistoryViewModel newViewModel)
            {
                newViewModel.CloseRequested += OnCloseRequested;
            }
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnVersionDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ReferenceHistoryViewModel viewModel && viewModel.RollbackCommand.CanExecute(null))
            {
                viewModel.RollbackCommand.Execute(null);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is ReferenceHistoryViewModel viewModel)
            {
                viewModel.CloseRequested -= OnCloseRequested;
            }
            base.OnClosed(e);
        }
    }
}
