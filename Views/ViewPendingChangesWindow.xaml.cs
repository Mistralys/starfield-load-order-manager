using System.Windows;
using LoadOrderKeeper.ViewModels;

namespace LoadOrderKeeper.Views
{
    /// <summary>
    /// Interaction logic for ViewPendingChangesWindow.xaml
    /// </summary>
    public partial class ViewPendingChangesWindow : Window
    {
        public ViewPendingChangesWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ViewPendingChangesViewModel oldViewModel)
            {
                oldViewModel.CloseRequested -= OnCloseRequested;
            }

            if (e.NewValue is ViewPendingChangesViewModel newViewModel)
            {
                newViewModel.CloseRequested += OnCloseRequested;
            }
        }

        private void OnCloseRequested(object? sender, System.EventArgs e)
        {
            Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            if (DataContext is ViewPendingChangesViewModel viewModel)
            {
                viewModel.CloseRequested -= OnCloseRequested;
            }
            base.OnClosed(e);
        }
    }
}
