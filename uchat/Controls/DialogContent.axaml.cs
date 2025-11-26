using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace uchat
{
    public partial class DialogContent : UserControl
    {
        public DialogContent()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
            => AvaloniaXamlLoader.Load(this);
    }
}