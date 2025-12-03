using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace uchat;

public partial class App : Application
{
    private string _ip;
    private int _port;
    private Client? _client;

    public App(string ip, int port)
    {
        _ip = ip;
        _port = port;
    }
    
    public App() : this("127.0.0.1", 8080) {} //required by the Avalonia, values not used in program
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _client = new Client(_ip, _port);
            desktop.MainWindow = new MainWindow();
            Task.Run(() => _client.ConnectToServer());
        }
        base.OnFrameworkInitializationCompleted();
    }
}