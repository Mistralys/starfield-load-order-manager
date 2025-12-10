using StarfieldLoadOrderManager.Classes;
using StarfieldLoadOrderManager.Dialogs;
using System.Windows;

namespace StarfieldLoadOrderManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!LoadOrderManager.Instance.StarfieldFolderExists)
            {
                PathConfigurator configWindow = new PathConfigurator();

                bool? configSucceeded = configWindow.ShowDialog();

                if (configSucceeded != true)
                {
                    MessageBox.Show("Application cannot run without configuration.", "Startup Error");
                    Shutdown(); 
                    return;
                }
            }

            // By default, the application is set to StartupUri="MainWindow.xaml" in App.xaml.
            // If you removed that, you would manually create and show the MainWindow here:
            // MainWindow mainWindow = new MainWindow();
            // mainWindow.Show();
        }
    }
}
