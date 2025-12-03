using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace uchat;

public partial class MainWindow : Window
{
    private Client _client;
    private ContentControl? _control;
    
    public MainWindow(Client client)
    {
        _client = client;
        InitializeComponent();
        _control = this.FindControl<ContentControl>("PageHost");
        Navigate(new PageLogin(_client));
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