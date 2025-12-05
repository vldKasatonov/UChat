using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Avalonia;
using dto;

namespace uchat;

public partial class PageLogin : UserControl
{
    private Client _client = null!;

    public PageLogin()
    {
        InitializeComponent();
    }
    
    public PageLogin(Client client) : this()
    {
        _client = client;
        AttachTextChangedHandlers();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void AttachHandler(TextBox? box, TextBlock? errorBlock, Button? loginButton, TextBlock? loginErrorText)
    {
        if (box == null) return;

        box.GetObservable(TextBox.TextProperty).Subscribe(_ =>
        {
            box.Classes.Remove("error");

            if (errorBlock != null)
                errorBlock.Text = "";

            if (loginButton != null)
                loginButton.Classes.Remove("error");

            if (loginErrorText != null)
                loginErrorText.Text = "";
        });
    }
    
    private void AttachTextChangedHandlers()
    {
        var loginButton = this.FindControl<Button>("LoginButton");
        var loginErrorText = this.FindControl<TextBlock>("LoginErrorText");

        AttachHandler(this.FindControl<TextBox>("UsernameTextBox"), this.FindControl<TextBlock>("UsernameErrorText"), loginButton, loginErrorText);
        AttachHandler(this.FindControl<TextBox>("PasswordTextBox"), this.FindControl<TextBlock>("PasswordErrorText"), loginButton, loginErrorText);
    }
    
    private bool ShowError(TextBox box, TextBlock errorBlock, string? message)
    {
        if (message != null)
        {
            if (!box.Classes.Contains("error"))
                box.Classes.Add("error");
            errorBlock.Text = message;
            return true;
        }
        else
        {
            box.Classes.Remove("error");
            errorBlock.Text = "";
            return false;
        }
    }
    
    private async void LogInButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var loginButton = this.FindControl<Button>("LoginButton");
        var usernameBox = this.FindControl<TextBox>("UsernameTextBox");
        var passwordBox = this.FindControl<TextBox>("PasswordTextBox");
        var usernameError = this.FindControl<TextBlock>("UsernameErrorText");
        var passwordError = this.FindControl<TextBlock>("PasswordErrorText");
        var loginError = this.FindControl<TextBlock>("LoginErrorText");
        loginError!.Text = "";

        if (usernameBox == null || passwordBox == null || usernameError == null || passwordError == null)
        {
            return;
        }

        bool hasError = false;

        hasError |= ShowError(usernameBox, usernameError, string.IsNullOrWhiteSpace(usernameBox.Text) ? "Username cannot be empty!" : null);
        hasError |= ShowError(passwordBox, passwordError, string.IsNullOrWhiteSpace(passwordBox.Text) ? "Password cannot be empty!" : null);

        if (hasError)
        {
            return;
        }

        var response = await _client.Login(usernameBox.Text!, passwordBox.Text!);

        if (response is null)
        {
            await ShowDialog("Error connecting to server.");
            return;
        }

        if (response.Status == Status.Success)
        {
            loginError.Text = "";
            loginButton!.Classes.Remove("error");
            var main = this.GetVisualRoot() as MainWindow;
            if (main != null)
            {
                main.Navigate(new PageChat(_client));
            }
        }
        else
        {
            if (response.Payload != null
                && response.Payload.TryGetPropertyValue("message", out var message))
            {
               loginError.Text = message?.ToString();
            }
            else
            {
                loginError.Text = "Unknown error";
            }

            if (!loginButton!.Classes.Contains("error"))
            {
                loginButton.Classes.Add("error");
            }
        }
    }
    
    private void SignUpButtonTop_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var main = this.GetVisualRoot() as MainWindow;
        if (main != null)
        {
            main.Navigate(new PageRegister(_client));
        }
    }
    
    private void SignUpText_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var main = this.GetVisualRoot() as MainWindow;
        if (main != null)
        {
            main.Navigate(new PageRegister(_client));
        }
    }
    
    private async Task ShowDialog(string message)
    {
        var owner = this.GetVisualRoot() as Window;
        if (owner == null)
            return;

        var content = new DialogContent();
        var messageText = content.FindControl<TextBlock>("MessageText");
        var okButton = content.FindControl<Button>("OkButton");

        if (messageText != null)
            messageText.Text = message;

        var dialog = new Window
        {
            Width = 400,
            Height = 150,
            CanResize = false,
            SystemDecorations = SystemDecorations.None,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Topmost = true,
            Content = content
        };

        if (okButton != null)
            okButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(owner);
    }
}