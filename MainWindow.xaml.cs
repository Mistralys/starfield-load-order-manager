using System.ComponentModel;
using System.Windows;

namespace LoadOrderKeeper
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Allow MainViewModel to clean up
            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
            base.OnClosing(e);
        }
    }
}