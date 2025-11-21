using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace uchat;

public partial class PageLogin : Page
{
    private Client _client;

    public PageLogin(Client client)
    {
        InitializeComponent();
        _client = client;
    }

    private async void LogInButton_Click(object sender, RoutedEventArgs e)
    {
        bool isUsernameValid = NameTextBox.Validate();
        bool isPasswordValid = PasswordTextBox.Validate();

        if (!isUsernameValid || !isPasswordValid)
        {
            return;
        }

        string username = NameTextBox.Text;
        string password = PasswordTextBox.Password;

        bool ok = false;

        try
        {
            ok = await _client.Authorise(username, password);
        }
        catch
        {
            MessageBox.Show("Error connecting to server.");
            return;
        }

        if (ok)
        {
            this.NavigationService?.Navigate(new PageChat(_client));
        }
        else
        {
            MessageBox.Show("Invalid username or password.");
        }
    }

    private void RegisterText_MouseDown(object sender, MouseButtonEventArgs e)
    {
        this.NavigationService?.Navigate(new PageRegister());
    }
}