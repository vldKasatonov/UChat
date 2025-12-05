using Avalonia.Controls;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.ComponentModel;
using dto;

namespace uchat;

public partial class PageChat : UserControl, INotifyPropertyChanged
{
    private readonly Client _client = null!;
    public ObservableCollection<ChatItem> Chats { get; } = new();
    public ObservableCollection<ChatItem> FilteredChats { get; } = new();
    public new event PropertyChangedEventHandler? PropertyChanged;
    private ObservableCollection<Message> _selectedChatMessages = new();
    private bool _isApplyingFilter = false;
    private ChatItem? _selectedChatBeforeSearch = null;
    private bool _isUpdatingFilteredChats = false;
    private ChatItem? _currentChat = null;
    private bool _isReconnecting;
    public bool IsReconnecting
    {
        get => _isReconnecting;
        set
        {
            if (_isReconnecting != value)
            {
                _isReconnecting = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsReconnecting)));
            }
        }
    }
    
    public PageChat()
    {
        InitializeComponent();
    }
    
    public PageChat(Client client) : this()
    {
        _client = client;
        DataContext = this;
        ChatList.SelectedIndex = -1;
        ChatHeader.Text = "Select a chat";
        ChatList.SelectionChanged += ChatList_SelectionChanged;
        SelectedChatMessages.CollectionChanged += MessagesPanel_CollectionChanged;
        ChatAvatar.IsVisible = false;
        
        Chats.Add(new ChatItem { Name = "Vlad", Status = "online", Messages = new ObservableCollection<Message>
        {
            new Message { Sender = "Vlad", Text = "nrgffgn", IsMine = false },
            new Message { Sender = "Me", Text = "fgnf", IsMine = true },
            new Message { Sender = "Vlad", Text = "nrgffgn", IsMine = false },
            new Message { Sender = "Me", Text = "fgnf", IsMine = true },
            new Message { Sender = "Vlad", Text = "nrgffgn", IsMine = false },
            new Message { Sender = "Me", Text = "fgnf", IsMine = true },
            new Message { Sender = "Vlad", Text = "nrgffgn", IsMine = false },
            new Message { Sender = "Me", Text = "fgnf", IsMine = true },
            new Message { Sender = "Vlad", Text = "nrgffgn", IsMine = false },
            new Message { Sender = "Me", Text = "fgnf", IsMine = true },
            new Message { Sender = "Vlad", Text = "nrgffgn", IsMine = false },
            new Message { Sender = "Me", Text = "fgnf", IsMine = true },
            new Message { Sender = "Vlad", Text = "nrgffgn", IsMine = false },
            new Message { Sender = "Vlad", Text = "nrgffgn", IsMine = false },
            new Message { Sender = "Vlad", Text = "nrgffgn", IsMine = false },
            new Message { Sender = "Me", Text = "fgnf", IsMine = true }
        }});

        Chats.Add(new ChatItem
        {
            Name = "Vika",
            Status = "offline",
            Messages = new ObservableCollection<Message>
            {
                new Message { Sender = "Vika", Text = "fngngnbdgngfn ndfg nfgnfg n f nf gng ffg g nf g n dfv df ed  dfdfhggdfhgbfjhbdhfgjdfhgdfbhjbhjfg", IsMine = false },
                new Message { Sender = "Me", Text = "hvjhyvkv", IsMine = true },
                new Message { Sender = "Vika", Text = "dggzhgjyr", IsMine = false }
            }
        });

        Chats.Add(new ChatItem
        {
            Name = "Masha",
            Status = "offline"
        });
        
        Chats.Add(new ChatItem
        {
            Name = "Uchat",
            Status = "6 members, 2 online",
            IsGroup = true,
            Messages = new ObservableCollection<Message>
            {
                new Message { Sender = "Vika", Text = "fngngnbdgngfn ndfg nfgnfg n f nf gng ffg g nf g n dfv df ed  dfdfhggdfhgbfjhbdhfgjdfhgdfbhjbhjfg", IsMine = false },
                new Message { Sender = "Me", Text = "hvjhyvkv", IsMine = true },
                new Message { Sender = "Mariia", Text = "bdgdfbfb", IsMine = false },
                new Message { Sender = "Roma", Text = "scahbkhdcbs", IsMine = false },
                new Message { Sender = "Masha", Text = "hfbeaskcjabsk", IsMine = false },
                new Message { Sender = "Vlad", Text = "pon", IsMine = false }
            }
        });
        
        foreach (var chat in Chats)
            FilteredChats.Add(chat);
        
        ChatList.ItemsSource = FilteredChats;
        
        _client.Disconnected += async () =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsReconnecting = true;
            });
        };

        _client.Reconnected += async () =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsReconnecting = false;
            });
        };
    }
    
    public class ChatItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string Name { get; set; } = "";
        public ObservableCollection<Message> Messages { get; set; } = new();
        public string Status { get; set; } = "";
        public bool IsGroup { get; set; } = false;
        public string LastMessage => Messages.LastOrDefault()?.Text ?? "";
        private string _draft = "";
        public string Draft
        {
            get => _draft;
            set
            {
                if (_draft != value)
                {
                    _draft = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Draft)));
                }
            }
        }

        public void NotifyLastMessageChanged() =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastMessage)));
    }
    
    public ObservableCollection<Message> SelectedChatMessages
    {
        get => _selectedChatMessages;
        set
        {
            if (_selectedChatMessages != value)
            {
                _selectedChatMessages = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedChatMessages)));
            }
        }
    }
    
    private void UpdateChatView(ChatItem contact)
    {
        ChatHeader.Text = contact.Name;
        ChatAvatar.IsVisible = true;
        ChatStatus.Text = contact.Status;
        ChatStatus.IsVisible = true;
        SelectedChatMessages.Clear();
        foreach (var msg in contact.Messages)
        { 
            msg.IsGroup = contact.IsGroup;
            SelectedChatMessages.Add(msg);
        }
        MessageTextBox.Text = contact.Draft;
        MessageInputPanel.IsVisible = true;
    }

    private void ClearChatView()
    {
        ChatHeader.Text = "Select a chat";
        ChatAvatar.IsVisible = false;
        ChatStatus.IsVisible = false;
        SelectedChatMessages.Clear();
        MessageInputPanel.IsVisible = false;
    }

    private void ApplyFilter(string text)
    {
        if (_isApplyingFilter) return;

        text = text?.Trim().ToLower() ?? "";
        _isApplyingFilter = true;
        _isUpdatingFilteredChats = true;
        
        if (!string.IsNullOrEmpty(text))
            ChatList.SelectedIndex = -1;

        FilteredChats.Clear();

        foreach (var chat in Chats)
        {
            if (string.IsNullOrEmpty(text) || chat.Name.ToLower().Contains(text))
                FilteredChats.Add(chat);
        }

        _isUpdatingFilteredChats = false;
        _isApplyingFilter = false;
    }
    
    private void SelectChatAfterFilterUpdate(ChatItem chatToSelect)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (FilteredChats.Contains(chatToSelect))
            {
                ChatList.SelectedItem = chatToSelect;
            }
            else
            {
                ChatList.SelectedIndex = -1;
                ClearChatView();
            }
        }, DispatcherPriority.Background);
    }

    private void SearchTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            string searchText = tb.Text?.Trim() ?? "";

            // Зберігаємо вибраний чат перед початком пошуку
            if (!string.IsNullOrEmpty(searchText) && _selectedChatBeforeSearch == null)
            {
                if (ChatList.SelectedItem is ChatItem current)
                    _selectedChatBeforeSearch = current;
            }

            ApplyFilter(searchText);
            ClearSearchButton.IsVisible = !string.IsNullOrEmpty(tb.Text);

            if (string.IsNullOrEmpty(searchText))
            {
                ClearSearchButton.IsVisible = false;
                
                if (_selectedChatBeforeSearch != null)
                {
                    SelectChatAfterFilterUpdate(_selectedChatBeforeSearch);
                }
                else
                {
                    ChatList.SelectedIndex = -1;
                    ClearChatView();
                }
            }
        }
    }

    private void SearchTextBox_GotFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ClearSearchButton.IsVisible = !string.IsNullOrEmpty(SearchTextBox.Text);
        
        if (_selectedChatBeforeSearch == null && ChatList.SelectedItem is ChatItem current)
            _selectedChatBeforeSearch = current;
    }

    private void SearchTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        ClearSearchButton.IsVisible = !string.IsNullOrEmpty(SearchTextBox.Text);
    }
    
    private void ClearSearchButton_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ClearSearchButton.IsVisible = true;
    }
    
    private void ClearSearchButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SearchTextBox.Text = "";
        ClearSearchButton.IsVisible = false;
    
        ApplyFilter("");
    
        if (_selectedChatBeforeSearch != null)
        {
            SelectChatAfterFilterUpdate(_selectedChatBeforeSearch);
        }
    
        SearchTextBox.Focus();
    }
    
    
    private void MessagesPanel_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
                (MessagesPanel.Parent as ScrollViewer)?.ScrollToEnd(),
            DispatcherPriority.Background);
    }
    
    private void ChatItem_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control ctrl) return;
        if (ctrl.DataContext is not ChatItem pressedChat) return;

        if (!string.IsNullOrEmpty(SearchTextBox.Text))
        {
            if (_currentChat == pressedChat)
            {
                SearchTextBox.Text = "";
                ClearSearchButton.IsVisible = false;
                ApplyFilter(""); 
            
                Dispatcher.UIThread.Post(() => ChatList.SelectedItem = pressedChat, DispatcherPriority.Background);

                MessageTextBox.Focus();
                return;
            }
            
            _selectedChatBeforeSearch = pressedChat;
        
            SearchTextBox.Text = "";
            ClearSearchButton.IsVisible = false;
            ApplyFilter("");
            SelectChatAfterFilterUpdate(pressedChat);
            MessageTextBox.Focus();
            return;
        }
    }

    private void ChatList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isApplyingFilter || _isUpdatingFilteredChats)
            return;

        if (ChatList.SelectedItem is ChatItem contact)
        {
            if (_currentChat == contact)
                return;
        
            _currentChat = contact;
            UpdateChatView(contact);
            
            _selectedChatBeforeSearch = contact;
        }
        else
        {
            _currentChat = null;
            ClearChatView();
        }
    }

    private async void SendMessage_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ChatList.SelectedItem is not ChatItem contact)
        {
            return;
        }
        string text = MessageTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        var msg = new Message { Id = Guid.NewGuid().ToString(), Sender = "Me", Text = text, IsMine = true };
        // bool success = await _client.SendMessageAsync(contact.Name, msg);
        // if (success)
        // {
            contact.Messages.Add(msg);
            SelectedChatMessages.Add(msg);
            contact.Draft = "";
            MessageTextBox.Text = "";
            contact.NotifyLastMessageChanged();
        // }
    }
    
    private void MessageTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb)
        {
            return;
        }
        var lineCount = tb.Text!.Split('\n').Length;
        var lineHeight = tb.FontSize + 5;
        tb.Height = Math.Min(lineCount * lineHeight, 120);
        if (ChatList.SelectedItem is ChatItem chat)
        {
            chat.Draft = tb.Text;
        }
    }
    
    private async void CopyMessage_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem menu && menu.DataContext is Message msg)
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(msg.Text);
            }
        }
    }
    
    private async void DeleteMessageForMe_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not MenuItem menu || menu.DataContext is not Message msg)
        {
            return;
        }
        if (ChatList.SelectedItem is not ChatItem chat)
        {
            return;
        }
        // bool success = await _client.DeleteMessageForMeAsync(chat.Name, msg.Id.ToString());
        // if (success)
        // {
            SelectedChatMessages.Remove(msg);
            chat.Messages.Remove(msg);
        // }
    }

    private async void DeleteMessageForAll_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not MenuItem menu || menu.DataContext is not Message msg)
        {
            return;
        }
        if (ChatList.SelectedItem is not ChatItem chat)
        {
            return;
        }
       // bool success = await _client.DeleteMessageForAllAsync(chat.Name, msg.Id.ToString());
       // if (success)
        // {
            msg.IsDeleted = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedChatMessages)));
        // }
    }
}
