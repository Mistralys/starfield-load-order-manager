using System;
using System.Windows;
using LoadOrderKeeper.ViewModels;

namespace LoadOrderKeeper.Views;

public partial class UpdateOptionsDialog : Window
{
    public UpdateOptionsDialog()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is UpdateOptionsViewModel oldVm)
        {
            oldVm.CloseRequested -= OnCloseRequested;
        }

        if (e.NewValue is UpdateOptionsViewModel newVm)
        {
            newVm.CloseRequested += OnCloseRequested;
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }
}
