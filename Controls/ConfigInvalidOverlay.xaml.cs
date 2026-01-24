using System.Windows.Controls;
using LoadOrderKeeper.ViewTexts;

namespace LoadOrderKeeper.Controls
{
    /// <summary>
    /// Modal overlay that blocks interaction when configuration is invalid.
    /// </summary>
    public partial class ConfigInvalidOverlay : System.Windows.Controls.UserControl
    {
        public ConfigInvalidOverlayTexts Texts { get; } = new();

        public ConfigInvalidOverlay()
        {
            InitializeComponent();
            // Don't set DataContext - let it inherit from parent window
        }
    }
}
