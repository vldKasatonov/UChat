using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace uchat;

public class ConfirmationDialogViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    private string _message = string.Empty;
    public string Message
    {
        get => _message;
        set
        {
            if (_message != value)
            {
                _message = value;
                OnPropertyChanged();
            }
        }
    }
    
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public partial class ConfirmationDialog : UserControl
{
    public ConfirmationDialog()
    {
        InitializeComponent();
        this.DataContext = new ConfirmationDialogViewModel();

        var confirmButton = this.FindControl<Button>("ConfirmButton");
        var cancelButton = this.FindControl<Button>("CancelButton");

        if (confirmButton != null)
        {
            confirmButton.Click += (sender, e) => 
            {
                var parentWindow = this.Parent as Window;
                parentWindow?.Close(true);
            };
        }
        if (cancelButton != null)
        {
            cancelButton.Click += (sender, e) => 
            {
                var parentWindow = this.Parent as Window;
                parentWindow?.Close(false);
            };
        }
    }

    private void InitializeComponent()
        => AvaloniaXamlLoader.Load(this);

    public string Message
    {
        get => (this.DataContext as ConfirmationDialogViewModel)?.Message ?? string.Empty;
        set
        {
            if (this.DataContext is ConfirmationDialogViewModel vm)
            {
                vm.Message = value;
            }
        }
    }
}