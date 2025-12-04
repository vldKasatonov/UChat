using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Avalonia;
using dto;

namespace uchat;

public partial class PageRegister : UserControl
{
    private Client _client = null!;

    public PageRegister()
    {
        InitializeComponent();
    }
    
    public PageRegister(Client client) : this()
    {
        _client = client;
        AttachTextChangedHandlers();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AttachHandler(TextBox? box, TextBlock? errorBlock, Button? registerButton, TextBlock? registerErrorText)
    {
        if (box == null)
        {
            return;
        }

        box.GetObservable(TextBox.TextProperty).Subscribe(_ =>
        {
            box.Classes.Remove("error");
            if (errorBlock != null)
                errorBlock.Text = "";

            if (registerButton != null)
                registerButton.Classes.Remove("error");

            if (registerErrorText != null)
                registerErrorText.Text = "";
        });
    }
    
    private void AttachTextChangedHandlers()
    {
        var registerButton = this.FindControl<Button>("RegisterButton");
        var registerErrorText = this.FindControl<TextBlock>("RegisterErrorText");

        AttachHandler(this.FindControl<TextBox>("UsernameTextBox"), this.FindControl<TextBlock>("UsernameErrorText"), registerButton, registerErrorText);
        AttachHandler(this.FindControl<TextBox>("NicknameTextBox"), this.FindControl<TextBlock>("NicknameErrorText"), registerButton, registerErrorText);
        AttachHandler(this.FindControl<TextBox>("PasswordTextBox"), this.FindControl<TextBlock>("PasswordErrorText"), registerButton, registerErrorText);
        AttachHandler(this.FindControl<TextBox>("ConfirmPasswordTextBox"), this.FindControl<TextBlock>("ConfirmPasswordErrorText"), registerButton, registerErrorText);
    }
    
    private void ShowError(TextBox box, TextBlock errorBlock, string? message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            box.Classes.Add("error");
            errorBlock.Text = message;
        }
        else
        {
            box.Classes.Remove("error");
            errorBlock.Text = "";
        }
    }

    private async void RegisterButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var registerButton = this.FindControl<Button>("RegisterButton");
        var nicknameBox = this.FindControl<TextBox>("NicknameTextBox");
        var usernameBox = this.FindControl<TextBox>("UsernameTextBox");
        var passwordBox = this.FindControl<TextBox>("PasswordTextBox");
        var confirmPasswordBox = this.FindControl<TextBox>("ConfirmPasswordTextBox");

        var nicknameError = this.FindControl<TextBlock>("NicknameErrorText");
        var usernameError = this.FindControl<TextBlock>("UsernameErrorText");
        var passwordError = this.FindControl<TextBlock>("PasswordErrorText");
        var confirmPasswordError = this.FindControl<TextBlock>("ConfirmPasswordErrorText");
        var registerError = this.FindControl<TextBlock>("RegisterErrorText");

        if (nicknameBox == null || usernameBox == null || passwordBox == null || confirmPasswordBox == null ||
            nicknameError == null || usernameError == null || passwordError == null || confirmPasswordError == null ||
            registerError == null)
        {
            return;
        }

        registerError.Text = "";
        
        ShowError(nicknameBox, nicknameError, string.IsNullOrWhiteSpace(nicknameBox.Text) ? "Nickname cannot be empty!" : null);
        ShowError(usernameBox, usernameError, string.IsNullOrWhiteSpace(usernameBox.Text) ? "Username cannot be empty!" : null);

        if (!string.IsNullOrWhiteSpace(passwordBox.Text) && passwordBox.Text.Length < 8)
        {
            ShowError(passwordBox, passwordError, "Password must be at least 8 characters");
        }
        else
        {
            ShowError(passwordBox, passwordError, string.IsNullOrWhiteSpace(passwordBox.Text) ? "Password cannot be empty!" : null);
        }
        
        ShowError(confirmPasswordBox, confirmPasswordError, string.IsNullOrWhiteSpace(confirmPasswordBox.Text) ? "Confirm password cannot be empty!" : 
            passwordBox.Text != confirmPasswordBox.Text ? "Passwords don't match" : null);
        
        bool hasError = !string.IsNullOrEmpty(nicknameError.Text) || !string.IsNullOrEmpty(usernameError.Text) ||
                        !string.IsNullOrEmpty(passwordError.Text) || !string.IsNullOrEmpty(confirmPasswordError.Text);

        if (hasError)
        {
            return;
        }
        
        var response = await _client.Register(usernameBox.Text!, passwordBox.Text!, nicknameBox.Text!);

        if (response is null)
        {
            await ShowDialog("Error connecting to server.");
            return;
        }

        if (response.Status == Status.Success)
        {
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
                usernameError.Text = message?.ToString();
            }
            else
            {
                usernameError.Text = "Unknown error";
            }
            
            if (!usernameError.Classes.Contains("error"))
            {
                usernameBox.Classes.Add("error");
            }
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
    
    private void SignInText_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var main = this.GetVisualRoot() as MainWindow;
        if (main != null)
        {
            main.Navigate(new PageLogin(_client));
        }
    }
    
    private void SignInButtonTop_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var main = this.GetVisualRoot() as MainWindow;
        if (main != null)
        {
            main.Navigate(new PageLogin(_client));
        }
    }
    
}
