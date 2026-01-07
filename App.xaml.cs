using LoadOrderKeeper.ViewModels;
using System;
using System.Threading.Tasks;

namespace LoadOrderKeeper
{
    public partial class App : System.Windows.Application
    {
        private MainViewModel? _mainViewModel;

        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);

            _mainViewModel = new MainViewModel();
            var window = new MainWindow
            {
                DataContext = _mainViewModel
            };

            window.Show();
        }

        protected override void OnExit(System.Windows.ExitEventArgs e)
        {
            // Dispose MainViewModel to trigger cleanup
            _mainViewModel?.Dispose();
            
            base.OnExit(e);
            
            // Force exit after brief grace period to ensure all resources released
            // This is necessary for WPF render thread cleanup
            Task.Delay(500).ContinueWith(_ => Environment.Exit(0));
        }
    }
}
