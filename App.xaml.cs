using LoadOrderKeeper.ViewModels;

namespace LoadOrderKeeper
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);

            var window = new MainWindow
            {
                DataContext = new MainViewModel()
            };

            window.Show();
        }
    }
}
