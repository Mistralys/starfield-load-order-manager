using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using LoadOrderKeeper.ViewModels;
using System;
using System.Threading.Tasks;

namespace LoadOrderKeeper
{
    public partial class App : System.Windows.Application
    {
        private MainViewModel? _mainViewModel;
        private static LocalizationService? _localizationService;

        /// <summary>
        /// Gets the application's localization service instance.
        /// </summary>
        public static LocalizationService LocalizationService => 
            _localizationService ?? throw new InvalidOperationException("LocalizationService not initialized");

        protected override async void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize localization service
            _localizationService = new LocalizationService();
            
            // Load user's language preference from settings
            AppConfigModel config;
            try
            {
                config = await SettingsService.LoadSettingsAsync();
            }
            catch
            {
                // If settings can't be loaded, use default config
                config = new AppConfigModel();
            }
            
            // Apply preferred language
            _localizationService.SetCulture(config.PreferredLanguage);

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
