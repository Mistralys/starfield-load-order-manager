using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using StarfieldLoadOrderManager.Classes;
using StarfieldLoadOrderManager.Resources;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace StarfieldLoadOrderManager.Dialogs
{
    /// <summary>
    /// Interaction logic for PathConfigurator.xaml
    /// </summary>
    public partial class PathConfigurator : Window
    {
        private readonly WpfTextBox _folderPathTextBox;

        public PathConfigurator()
        {
            InitializeComponent();

            _folderPathTextBox = (WpfTextBox)FindName("FolderPathTextBox") ?? throw new InvalidOperationException("Folder path input not found.");
            _folderPathTextBox.Text = LoadOrderManager.Instance.StarfieldFolder ?? string.Empty;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new OpenFolderDialog
            {
                Title = "Select the folder containing your Starfield installation",
                InitialDirectory = GetInitialFolder()
            };

            if (dialog.ShowDialog(this) == true && Directory.Exists(dialog.FolderName))
            {
                _folderPathTextBox.Text = dialog.FolderName;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedFolder = _folderPathTextBox.Text.Trim();

            if (!Directory.Exists(selectedFolder))
            {
                MessageBox.Show(
                    "The selected folder does not exist. Please choose a valid folder.",
                    "Invalid Folder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            ManagerSettings.Default.StarfieldFolder = selectedFolder;
            ManagerSettings.Default.Save();

            DialogResult = true;
            Close();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private string GetInitialFolder()
        {
            string currentText = _folderPathTextBox.Text.Trim();
            if (Directory.Exists(currentText))
            {
                return currentText;
            }

            string savedFolder = LoadOrderManager.Instance.StarfieldFolder;
            if (!string.IsNullOrWhiteSpace(savedFolder) && Directory.Exists(savedFolder))
            {
                return savedFolder;
            }

            // Try the default configuration path
            string defaultPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Starfield"
            );

            if (Directory.Exists(defaultPath))
            {
                return defaultPath;
            }

            // Fallback to the local application data folder
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
    }
}
