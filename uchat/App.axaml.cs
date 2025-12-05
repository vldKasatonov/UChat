using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace uchat;

public partial class App : Application
{
    private string _ip = string.Empty;
    private int _port;
    private Client? _client;

    public App() { } //required by the Avalonia
    
    public App(string ip, int port) : this()
    {
        _ip = ip;
        _port = port;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _client = new Client(_ip, _port);
            desktop.MainWindow = new MainWindow(_client);
            Task.Run(() => _client.ConnectToServer());
        }
        base.OnFrameworkInitializationCompleted();
    }
}