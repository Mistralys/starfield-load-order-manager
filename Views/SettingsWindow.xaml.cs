using System;
using System.IO;
using System.Windows;
using LoadOrderKeeper.ViewModels;
using Forms = System.Windows.Forms;

namespace LoadOrderKeeper.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            DataContextChanged += OnSettingsDataContextChanged;
        }

        private void OnSettingsDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SettingsViewModel oldVm)
            {
                oldVm.BrowseAppDataRequested -= OnBrowseAppDataRequested;
                oldVm.BrowseGamePathRequested -= OnBrowseGamePathRequested;
                oldVm.SaveRequested -= OnSaveRequested;
            }

            if (e.NewValue is SettingsViewModel newVm)
            {
                newVm.BrowseAppDataRequested += OnBrowseAppDataRequested;
                newVm.BrowseGamePathRequested += OnBrowseGamePathRequested;
                newVm.SaveRequested += OnSaveRequested;
            }
        }

        private void OnBrowseAppDataRequested(object? sender, EventArgs e)
        {
            if (sender is not SettingsViewModel vm)
            {
                return;
            }

            var selected = ShowFolderDialog("Select the Starfield AppData folder", vm.StarfieldAppDataPath);
            if (!string.IsNullOrWhiteSpace(selected))
            {
                vm.UpdateAppDataPath(selected);
            }
        }

        private void OnBrowseGamePathRequested(object? sender, EventArgs e)
        {
            if (sender is not SettingsViewModel vm)
            {
                return;
            }

            var selected = ShowFolderDialog("Select the Starfield installation folder", vm.StarfieldGamePath);
            if (!string.IsNullOrWhiteSpace(selected))
            {
                vm.UpdateGamePath(selected);
            }
        }

        private static string? ShowFolderDialog(string description, string? initialPath)
        {
            using var dialog = new Forms.FolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
            {
                dialog.SelectedPath = initialPath;
            }

            return dialog.ShowDialog() == Forms.DialogResult.OK ? dialog.SelectedPath : null;
        }

        private void OnSaveRequested(object? sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
