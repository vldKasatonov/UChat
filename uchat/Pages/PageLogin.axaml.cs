using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Avalonia;
using dto;
using System.Reactive.Linq;

namespace uchat;

public partial class PageLogin : UserControl
{
    private Client _client = null!;
    private Button? _usernameClearButton;
    private Button? _passwordClearButton;
    private TextBox? _usernameTextBox;
    private TextBox? _passwordTextBox;
    private Button? _passwordToggleButton;
    private Image? _visiblePasswordImage;
    private Image? _invisiblePasswordImage;
    private bool _isPasswordVisible = false;

    public PageLogin()
    {
        InitializeComponent();
    }
    
    public PageLogin(Client client) : this()
    {
        _client = client;
        AttachTextChangedHandlers();
        InitializeClearButtons();
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
    
    private void InitializeClearButtons()
    {
        _usernameTextBox = this.FindControl<TextBox>("UsernameTextBox");
        _passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");
        _usernameClearButton = this.FindControl<Button>("UsernameClearButton");
        _passwordClearButton = this.FindControl<Button>("PasswordClearButton");
        
        _passwordToggleButton = this.FindControl<Button>("PasswordToggleButton");
        _visiblePasswordImage = _passwordToggleButton?.FindControl<Image>("VisiblePassword");
        _invisiblePasswordImage = _passwordToggleButton?.FindControl<Image>("InvisiblePassword");
        
        if (_usernameTextBox != null && _usernameClearButton != null)
        {
            _usernameClearButton.Click += (s, e) =>
            {
                _usernameTextBox.Text = string.Empty;
                _usernameTextBox.Focus();
            };
            
            _usernameTextBox.GetObservable(TextBox.TextProperty)
                .Subscribe(text =>
                {
                    _usernameClearButton.IsVisible = !string.IsNullOrEmpty(text);
                });
        }
        
        if (_passwordTextBox != null && _passwordClearButton != null && _passwordToggleButton != null)
        {
            _passwordClearButton.Click += (s, e) =>
            {
                _passwordTextBox.Text = string.Empty;
                _passwordTextBox.Focus();
            };
            
            _passwordTextBox.GetObservable(TextBox.TextProperty)
                .Subscribe(text =>
                {
                    _passwordClearButton.IsVisible = !string.IsNullOrEmpty(text);
                });
            
            _passwordToggleButton.Click += (_, _) =>
            {
                if (_isPasswordVisible)
                {
                    _passwordTextBox.PasswordChar = '•';
                    _passwordTextBox.FontSize = 30;
                    _visiblePasswordImage!.IsVisible = false;
                    _invisiblePasswordImage!.IsVisible = true;
                }
                else
                {
                    _passwordTextBox.PasswordChar = '\0';
                    _passwordTextBox.FontSize = 20;
                    _visiblePasswordImage!.IsVisible = true;
                    _invisiblePasswordImage!.IsVisible = false;
                }
                _isPasswordVisible = !_isPasswordVisible;
            };
                
            _passwordTextBox.GetObservable(TextBox.TextProperty)
                .Subscribe(text => _passwordToggleButton.IsVisible = !string.IsNullOrEmpty(text));
        }
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
        string normalizedUsername = usernameBox.Text!.Trim();
        if (normalizedUsername.StartsWith("@"))
        {
            normalizedUsername = normalizedUsername[1..];
        }
        var response = await _client.Login(normalizedUsername, passwordBox.Text!);

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
            Topmost = false,
            Content = content
        };

        if (okButton != null)
            okButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(owner);
    }
}