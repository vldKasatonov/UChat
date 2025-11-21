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

namespace uchat;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : NavigationWindow
{
        public MainWindow()
        {
            InitializeComponent();
            Client client = new Client("127.0.0.1", 8080);
            this.Navigate(new PageLogin(client));
        }


    /* private void LogInButton_Click(object sender, RoutedEventArgs e)
    {
        // bool isLogIn =
        // call Client Authorise() method
    } */
}