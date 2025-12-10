using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Avalonia;
using System.Text.RegularExpressions;
using dto;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace uchat;

public partial class PageRegister : UserControl
{
    private Client _client = null!;
    private Button? _nicknameClearButton;
    private Button? _usernameClearButton;
    private Button? _passwordClearButton;
    private Button? _confirmPasswordClearButton;
    private TextBox? _nicknameTextBox;
    private TextBox? _usernameTextBox;
    private TextBox? _passwordTextBox;
    private TextBox? _confirmPasswordTextBox;
    private Button? _passwordToggleButton;
    private Image? _visiblePasswordImage;
    private Image? _invisiblePasswordImage;
    private Button? _confirmPasswordToggleButton;
    private Image? _visibleConfirmPasswordImage;
    private Image? _invisibleConfirmPasswordImage;
    private bool _isPasswordVisible = false;
    private bool _isConfirmPasswordVisible = false;
    private bool _isLight;

    public PageRegister()
    {
        InitializeComponent();
        ModeLight = this.FindControl<Image>("ModeLight");
        ModeDark = this.FindControl<Image>("ModeDark");
        _isLight = Application.Current?.ActualThemeVariant != ThemeVariant.Dark;
        UpdateModeThemeIcon();
    }
    
    public PageRegister(Client client) : this()
    {
        _client = client;
        AttachTextChangedHandlers();
        InitializeClearButtons();
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
    private void InitializeClearButtons()
    {
        _nicknameTextBox = this.FindControl<TextBox>("NicknameTextBox");
        _usernameTextBox = this.FindControl<TextBox>("UsernameTextBox");
        _passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");
        _confirmPasswordTextBox = this.FindControl<TextBox>("ConfirmPasswordTextBox");
        
        _nicknameClearButton = this.FindControl<Button>("NicknameClearButton");
        _usernameClearButton = this.FindControl<Button>("UsernameClearButton");
        _passwordClearButton = this.FindControl<Button>("PasswordClearButton");
        _confirmPasswordClearButton = this.FindControl<Button>("ConfirmPasswordClearButton");
        
        _passwordToggleButton = this.FindControl<Button>("PasswordToggleButton");
        _visiblePasswordImage = _passwordToggleButton?.FindControl<Image>("VisiblePassword");
        _invisiblePasswordImage = _passwordToggleButton?.FindControl<Image>("InvisiblePassword");
        _confirmPasswordToggleButton = this.FindControl<Button>("ConfirmPasswordToggleButton");
        _visibleConfirmPasswordImage = _confirmPasswordToggleButton?.FindControl<Image>("VisibleConfirmPassword");
        _invisibleConfirmPasswordImage = _confirmPasswordToggleButton?.FindControl<Image>("InvisibleConfirmPassword");
        
        if (_nicknameTextBox != null && _nicknameClearButton != null)
        {
            _nicknameClearButton.Click += (s, e) =>
            {
                _nicknameTextBox.Text = string.Empty;
                _nicknameTextBox.Focus();
            };

            _nicknameTextBox.GetObservable(TextBox.TextProperty)
                .Subscribe(text =>
                {
                    _nicknameClearButton.IsVisible = !string.IsNullOrEmpty(text);
                });
        }
        
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
        
        if (_confirmPasswordTextBox != null && _confirmPasswordClearButton != null && _confirmPasswordToggleButton != null)
        {
            _confirmPasswordClearButton.Click += (s, e) =>
            {
                _confirmPasswordTextBox.Text = string.Empty;
                _confirmPasswordTextBox.Focus();
            };

            _confirmPasswordTextBox.GetObservable(TextBox.TextProperty)
                .Subscribe(text =>
                {
                    _confirmPasswordClearButton.IsVisible = !string.IsNullOrEmpty(text);
                });
            
            _confirmPasswordToggleButton.Click += (_, _) =>
            {
                if (_isConfirmPasswordVisible)
                {
                    _confirmPasswordTextBox.PasswordChar = '•';
                    _confirmPasswordTextBox.FontSize = 30;
                    _visibleConfirmPasswordImage!.IsVisible = false;
                    _invisibleConfirmPasswordImage!.IsVisible = true;
                }
                else
                {
                    _confirmPasswordTextBox.PasswordChar = '\0';
                    _confirmPasswordTextBox.FontSize = 20;
                    _visibleConfirmPasswordImage!.IsVisible = true;
                    _invisibleConfirmPasswordImage!.IsVisible = false;
                }
                _isConfirmPasswordVisible = !_isConfirmPasswordVisible;
            };
                
            _confirmPasswordTextBox.GetObservable(TextBox.TextProperty)
                .Subscribe(text => _confirmPasswordToggleButton.IsVisible = !string.IsNullOrEmpty(text));
        }
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
        
        if (string.IsNullOrWhiteSpace(nicknameBox.Text))
        {
            ShowError(nicknameBox, nicknameError, "Nickname cannot be empty.");
        }
        else if (nicknameBox.Text.Length > 20)
        {
            ShowError(nicknameBox, nicknameError, "Nickname must be 1–20 characters.");
        }
        else
        {
            ShowError(nicknameBox, nicknameError, null);
        }
        
        var username = usernameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(usernameBox.Text))
        {
            ShowError(usernameBox, usernameError, "Username cannot be empty.");
        }
        else if (usernameBox.Text.Length < 3 || usernameBox.Text.Length > 20)
        {
            ShowError(usernameBox, usernameError, "Username must be 3–20 characters.");
        }
        else if (username!.Contains(' '))
        {
            ShowError(usernameBox, usernameError, "Username cannot contain spaces.");
        }
        else if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
        {
            ShowError(usernameBox, usernameError, "Only Latin letters, digits and _ are allowed.");
        }
        else
        {
            ShowError(usernameBox, usernameError, null);
        }

        var password = passwordBox.Text?.Trim() ?? "";
        var confirm = confirmPasswordBox.Text ?? "";

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError(passwordBox, passwordError, "Password cannot be empty.");
        }
        else if (password.Length < 8)
        {
            ShowError(passwordBox, passwordError, "Password must be at least 8 characters.");
        }
        else if (password.Length > 50)
        {
            ShowError(passwordBox, passwordError, "Password must be at most 50 characters.");
        }
        else if (password.Contains(' '))
        {
            ShowError(passwordBox, passwordError, "Password cannot contain spaces.");
        }
        else if (Regex.IsMatch(password, @"\p{IsCyrillic}"))
        {
            ShowError(passwordBox, passwordError, "Password must contain only Latin letters, not Cyrillic.");
        }
        else if (!Regex.IsMatch(password, @"[a-z]"))
        {
            ShowError(passwordBox, passwordError, "Password must contain a lowercase letter.");
        }
        else if (!Regex.IsMatch(password, @"[A-Z]"))
        {
            ShowError(passwordBox, passwordError, "Password must contain an uppercase letter.");
        }
        else if (!Regex.IsMatch(password, @"\d"))
        {
            ShowError(passwordBox, passwordError, "Password must contain a digit.");
        }
        else if (!Regex.IsMatch(password, @"[!@#$%^&*()\-_=+<>?]"))
        {
            ShowError(passwordBox, passwordError, "Password must contain a special character (!@#$%^&*()-_+=<>?).");
        }
        else
        {
            ShowError(passwordBox, passwordError, null);
        }
        
        if (string.IsNullOrWhiteSpace(confirm))
        {
            ShowError(confirmPasswordBox, confirmPasswordError, "Confirm password cannot be empty.");
        }
        else if (password != confirm)
        {
            ShowError(confirmPasswordBox, confirmPasswordError, "Passwords don't match.");
        }
        else
        {
            ShowError(confirmPasswordBox, confirmPasswordError, null);
        }
        
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
                main.Navigate(new PageLogin(_client));
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
            Topmost = false,
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
    
    private void UpdateModeThemeIcon()
    {
        ModeLight.IsVisible = !_isLight;
        ModeDark.IsVisible = _isLight;
    } 
    
    private void SwitchTheme_Click(object? sender, RoutedEventArgs e)
    {
        _isLight = !_isLight;
        App.SetTheme(_isLight ? "Light" : "Dark");
        UpdateModeThemeIcon();
    }
    
}
