using System;
using System.Windows;
using LoadOrderKeeper.ViewModels;

namespace LoadOrderKeeper.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            DataContextChanged += OnAboutDataContextChanged;
        }

        private void OnAboutDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is AboutViewModel oldVm)
            {
                oldVm.CloseRequested -= OnCloseRequested;
            }

            if (e.NewValue is AboutViewModel newVm)
            {
                newVm.CloseRequested += OnCloseRequested;
            }
        }

        private void OnCloseRequested(object? sender, EventArgs e)
        {
            Close();
        }
    }
}