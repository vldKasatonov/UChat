using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace uchat
{
    public partial class PageRegister : Page
    {
        private Client? _client;
        public PageRegister()
        {
            InitializeComponent();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            bool isNicknnameValid = NicknameTextBox.Validate();
            bool isUsernameValid = UsernameTextBox.Validate();
            bool isPasswordValid = PasswordTextBox.Validate();
            bool isConfimPasswordValid = ConfirmPasswordTextBox.Validate();

            if (!isUsernameValid || !isPasswordValid || !isNicknnameValid || !isConfimPasswordValid)
            {
                return;
            }

            string nickname = NicknameTextBox.Text;
            string username = UsernameTextBox.Text;
            string password = PasswordTextBox.Text;
            string confirmPassword = ConfirmPasswordTextBox.Text;

            if (password != confirmPassword)
            {
                ConfirmPasswordTextBox.ShowTBError("Passwords don't match.");
                return;
            }

            if (password.Length < 8)
            {
                PasswordTextBox.ShowTBError("Password must be at least 8 characters.");
                return;
            }

            /* bool ok = false;

            try
            {
                ok = await _client.Register(username, password, nickname);
            }
            catch
            {
                MessageBox.Show("Error connecting to server.");
                return;
            }

            if (ok)
            {
                NavigationService?.Navigate(new PageChat(_client));
            }
            else
            {
                MessageBox.Show("User already exists.");
            } */
        }
    }
}