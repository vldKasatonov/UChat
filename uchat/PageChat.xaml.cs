using System.Windows.Controls;

namespace uchat;

public partial class PageChat : Page
{
    private Client _client;

    public PageChat(Client client)
    {
        InitializeComponent();
        _client = client;
    }
}