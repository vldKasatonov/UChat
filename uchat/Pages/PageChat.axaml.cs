using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace uchat;

public partial class PageChat : UserControl
{
    private Client _client;

    public PageChat(Client client)
    {
        InitializeComponent();
        _client = client;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}