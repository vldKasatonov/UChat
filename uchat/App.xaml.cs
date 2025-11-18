using System.Configuration;
using System.Data;
using System.Windows;

namespace uchat;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Length != 2 || !int.TryParse(e.Args[1], out int port))
        {
            //TODO: make msgbox
            return;
        }
        //TODO: validate program args
        string ip = e.Args[0];
        Client client = new Client(ip, port); 
        MainWindow mainWindow = new MainWindow
        {
            DataContext = client
        };
            
        mainWindow.Show();
        //TODO:remove call
        Task.Run(async () =>
        {
            if (await client.Authorise("user", "password"))
            {
                
            }
        });
        //
        base.OnStartup(e);
    }
}