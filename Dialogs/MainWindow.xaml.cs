using StarfieldLoadOrderManager.Resources;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StarfieldLoadOrderManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        public string TLTitle => string.Format(Strings.Main_Window_Title, "Starfield"); 
        public string TLMenuFile => Strings.Menu_File;
        public string TLMenuExit => Strings.Menu_Exit;
        public string TLMenuEdit => Strings.Menu_Edit;
        public string TLMenuAbout => Strings.Menu_About;
        public string TLMenuHelp => Strings.Menu_Help;
        public string TLMenuConfiguration => Strings.Menu_Configuration;

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}