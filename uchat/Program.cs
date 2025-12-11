using Avalonia;
using Avalonia.ReactiveUI;

namespace uchat;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: uchat <ip> <port>");
            return;
        }
        //TODO: validate program args
        string ip = args[0];

        if (!int.TryParse(args[1], out int port))
        {
            Console.WriteLine("Error: invalid port format");
            return;
        }
        
        BuildAvaloniaApp(ip, port)
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp(string ip, int port)
        => AppBuilder.Configure(() => new App(ip, port))
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}