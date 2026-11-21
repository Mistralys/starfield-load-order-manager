using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LoadOrderKeeper.ViewModels;

namespace LoadOrderKeeper.Views
{
    public partial class DiffWindow : Window
    {
        public DiffWindow()
        {
            InitializeComponent();
            Loaded += OnDiffWindowLoaded;
            DataContextChanged += OnDiffDataContextChanged;
            // NOTE: OnWindowClosed fires inside base.OnClosed(e), which is called AFTER
            // the existing OnClosed override calls viewModel.Dispose(). This ordering is
            // currently safe because Dispose() does not null out _mainViewModel or the card
            // bool properties. If Dispose() is ever widened to null those out, persistence
            // will silently break.
            Closed += OnWindowClosed;
        }

        private void OnDiffDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is DiffDialogViewModel oldVm)
            {
                oldVm.CloseRequested -= OnCloseRequested;
                oldVm.ScrollRequested -= OnScrollRequested;
                oldVm.ConfirmationRequested -= OnConfirmationRequested;
            }

            if (e.NewValue is DiffDialogViewModel newVm)
            {
                newVm.CloseRequested += OnCloseRequested;
                newVm.ScrollRequested += OnScrollRequested;
                newVm.ConfirmationRequested += OnConfirmationRequested;
            }
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            CloseDialog();
        }

        private void OnDiffWindowLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnDiffWindowLoaded;
            ScrollToTargetLine();
            RestoreSidebarWidth();
        }

        private void RestoreSidebarWidth()
        {
            if (DataContext is DiffDialogViewModel vm)
            {
                SidebarColumn.Width = new GridLength(vm.GetSidebarWidth());
            }
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            if (DataContext is DiffDialogViewModel vm)
            {
                _ = vm.PersistWindowStateAsync(SidebarColumn.Width.Value);
            }
        }

        private void OnScrollRequested(object? sender, EventArgs e)
        {
            ScrollToTargetLine();
        }

        private void ScrollToTargetLine()
        {
            if (DataContext is not DiffDialogViewModel viewModel)
            {
                return;
            }

            int index = viewModel.ScrollTargetIndex;
            if (index < 0 || index >= DiffListView.Items.Count)
            {
                return;
            }

            DiffListView.SelectedIndex = index;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                DiffListView.UpdateLayout();
                object? item = DiffListView.Items[index];
                DiffListView.ScrollIntoView(item);
            }), DispatcherPriority.Loaded);
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            CloseDialog();
        }

        private void CloseDialog()
        {
            Close();
        }

        private void OnConfirmationRequested(object? sender, ConfirmationRequestedEventArgs e)
        {
            var result = ConfirmationDialog.Show(
                e.Title,
                e.Message,
                e.Icon,
                e.Buttons,
                ConfirmationResult.No,
                this);

            e.Result = result;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is DiffDialogViewModel viewModel)
            {
                viewModel.Dispose();
            }

            base.OnClosed(e);
        }
    }
}
