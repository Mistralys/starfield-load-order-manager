using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using LoadOrderKeeper.ViewModels;
using LoadOrderKeeper.Views;

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

            // Initialize error log (clear previous content)
            ErrorLoggingService.InitializeErrorLog();

            // Register global exception handlers
            RegisterExceptionHandlers();

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

        private void RegisterExceptionHandlers()
        {
            // UI thread exceptions
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            // Non-UI thread exceptions
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // Task exceptions
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private async void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true; // Prevent default crash behavior
            await HandleExceptionAsync(e.Exception);
        }

        private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                await HandleExceptionAsync(exception);
            }

            // If terminating, exit gracefully
            if (e.IsTerminating)
            {
                Environment.Exit(1);
            }
        }

        private async void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved(); // Mark as observed to prevent app termination
            await HandleExceptionAsync(e.Exception);
        }

        private async Task HandleExceptionAsync(Exception exception)
        {
            try
            {
                // Get application state if available
                AppConfigModel? config = null;
                IReadOnlyList<DiffLineModel>? changeList = null;

                if (_mainViewModel != null)
                {
                    config = _mainViewModel.Config;
                    // changeList is not readily available in MainViewModel, will be null
                }

                // Log the exception with available state
                await ErrorLoggingService.LogExceptionAsync(exception, config, changeList);

                // Show error dialog on UI thread
                await Dispatcher.InvokeAsync(() => ShowErrorDialog(exception));
            }
            catch
            {
                // Last resort: if error handling itself fails, just exit
                Environment.Exit(1);
            }
        }

        private void ShowErrorDialog(Exception exception)
        {
            try
            {
                var errorVm = new ErrorDialogViewModel(exception);
                var errorDialog = new ErrorDialog
                {
                    DataContext = errorVm
                };

                errorDialog.ShowDialog();
            }
            catch
            {
                // If dialog fails to show, exit
                Environment.Exit(1);
            }
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
