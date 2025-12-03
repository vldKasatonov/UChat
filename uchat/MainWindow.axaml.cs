using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace uchat;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Client client = new Client("127.0.0.1", 8080);
        var loginPage = new PageLogin(client);
        this.FindControl<ContentControl>("PageHost").Content = loginPage;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    public void Navigate(UserControl page)
    {
        this.FindControl<ContentControl>("PageHost").Content = page;
    }
}