using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace uchat;

public partial class MainWindow : Window
{
    private ContentControl? _control;

    public MainWindow() //required by the Avalonia
    {
        InitializeComponent();
    }
    
    public MainWindow(Client client) : this()
    {
        _control = this.FindControl<ContentControl>("PageHost");
        Navigate(new PageLogin(client));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    public void Navigate(UserControl page)
    {
        if (_control != null)
        {
            _control.Content = page;
        }
    }
}