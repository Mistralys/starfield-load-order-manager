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
        }
    }
}
