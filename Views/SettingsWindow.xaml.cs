using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Find the text boxes and attach blur event handlers
            var appDataTextBox = FindName("AppDataPathTextBox") as System.Windows.Controls.TextBox;
            var gamePathTextBox = FindName("GamePathTextBox") as System.Windows.Controls.TextBox;

            if (appDataTextBox != null)
            {
                appDataTextBox.LostFocus += OnPathTextBoxLostFocus;
            }

            if (gamePathTextBox != null)
            {
                gamePathTextBox.LostFocus += OnPathTextBoxLostFocus;
            }

            // Validate on window load
            if (DataContext is SettingsViewModel vm)
            {
                vm.ValidateConfiguration();
            }
        }

        private void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            // Auto-save if configuration is valid and DialogResult is not explicitly set
            if (DialogResult != true && DataContext is SettingsViewModel vm)
            {
                vm.ValidateConfiguration();
                
                // Check if configuration is valid by examining the status banner
                if (!vm.StatusBannerIsError)
                {
                    // Configuration is valid, auto-save it
                    DialogResult = true;
                }
            }
        }

        private void OnPathTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            // Validate configuration when user leaves a text box
            if (DataContext is SettingsViewModel vm)
            {
                vm.ValidateConfiguration();
            }
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
                vm.ValidateConfiguration(); // Validate after browse
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
                vm.ValidateConfiguration(); // Validate after browse
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
            // Validate before saving
            if (sender is SettingsViewModel vm)
            {
                vm.ValidateConfiguration();
            }

            DialogResult = true;
            Close();
        }
    }
}
