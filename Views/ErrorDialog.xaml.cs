using System;
using System.Windows;
using LoadOrderKeeper.ViewModels;

namespace LoadOrderKeeper.Views
{
    public partial class ErrorDialog : Window
    {
        public ErrorDialog()
        {
            InitializeComponent();
            DataContextChanged += OnErrorDialogDataContextChanged;
        }

        private void OnErrorDialogDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ErrorDialogViewModel oldVm)
            {
                oldVm.CloseRequested -= OnCloseRequested;
                oldVm.ExitRequested -= OnExitRequested;
            }

            if (e.NewValue is ErrorDialogViewModel newVm)
            {
                newVm.CloseRequested += OnCloseRequested;
                newVm.ExitRequested += OnExitRequested;
            }
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnExitRequested(object? sender, EventArgs e)
        {
            DialogResult = true;
            Close();
            System.Windows.Application.Current.Shutdown();
        }
    }
}
