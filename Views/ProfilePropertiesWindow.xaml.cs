using System;
using System.Windows;
using LoadOrderKeeper.ViewModels;

namespace LoadOrderKeeper.Views;

public partial class ProfilePropertiesWindow : Window
{
    public ProfilePropertiesWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LabelTextBox.Focus();
        LabelTextBox.SelectAll();
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ProfilePropertiesViewModel oldVm)
        {
            oldVm.SaveRequested -= OnSaveRequested;
            oldVm.CancelRequested -= OnCancelRequested;
        }

        if (e.NewValue is ProfilePropertiesViewModel newVm)
        {
            newVm.SaveRequested += OnSaveRequested;
            newVm.CancelRequested += OnCancelRequested;
        }
    }

    private void OnSaveRequested(object? sender, EventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancelRequested(object? sender, EventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
