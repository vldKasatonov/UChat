using Avalonia.Controls;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.ComponentModel;
using System.Text.Json;
using Avalonia;
using dto;

namespace uchat;

public partial class PageChat : UserControl, INotifyPropertyChanged
{
    private readonly Client _client = null!;
    private List<User> _allUsers = new();
    public ObservableCollection<ChatItem> Chats { get; } = new();
    public ObservableCollection<ChatItem> FilteredChats { get; } = new();
    private ObservableCollection<User> _selectedGroupMembers = new();
    public new event PropertyChangedEventHandler? PropertyChanged;
    private ObservableCollection<Message> _selectedChatMessages = new();
    private bool _isApplyingFilter = false;
    private ChatItem? _selectedChatBeforeSearch = null;
    private bool _isUpdatingFilteredChats = false;
    private ChatItem? _currentChat = null;
    private static long _pinSequence = 0;
    private bool _isLight = true;
    private bool _isMembersPanelOpen = false;
    private bool _showToggleMembersButton;
    private Message? _editingMessage;
    
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
    private bool _needToShutdown;
    public bool NeedToShutdown
    {
        get => _needToShutdown;
        set
        {
            if (_needToShutdown != value)
            {
                _needToShutdown = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NeedToShutdown)));
            }
        }
    }
    public string CurrentUserName { get; set; } = "Mister Crabs";
    public string CurrentUserUsername { get; set; } = "@crabs";
    
    public PageChat()
    {
        InitializeComponent();
        UpdateExitThemeIcon();
        UpdateSingleChatThemeIcon();
        UpdateGroupChatThemeIcon();
        UpdateModeThemeIcon();
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

        Chats.Add(new ChatItem
        {
            Name = "Vlad", Username = "@vlad", Messages = new ObservableCollection<Message>
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
            }
        });

        Chats.Add(new ChatItem
        {
            Name = "Vika",
            Username = "@1",
            Messages = new ObservableCollection<Message>
            {
                new Message { Sender = "Vika", Text = "fngngnbdgngfn ndfg nfgnfg n f nf gng ffg g nf g n dfv df ed  dfdfhggdfhgbfjhbdhfgjdfhgdfbhjbhjfg", IsMine = false },
                new Message { Sender = "Me", Text = "hvjhyvkv", IsMine = true },
                new Message { Sender = "Vika", Text = "dggzhgjyr", IsMine = false }
            }
        });
        
        _allUsers = new List<User>
        {
            new User { Name = "Vlad", Username = "@vlad" },
            new User { Name = "Vika", Username = "@1" },
            new User { Name = "Masha", Username = "@2" },
            new User { Name = "Mariia", Username = "@3" },
            new User { Name = "Vika 2", Username = "@5" },
            new User { Name = "Masha 2", Username = "@6" },
            new User { Name = "Mariia 2", Username = "@7" },
            new User { Name = "Roma", Username = "@4" }
        };
        
        foreach (var chat in Chats)
        {
            if (chat.Messages.Any())
            {
                var lastMessage = chat.Messages.Last();
                chat.NotifyLastMessageChanged(lastMessage.Text, lastMessage.SentTime);
            }
        }
        
        foreach (var chat in Chats)
            FilteredChats.Add(chat);
        
        SortChats();

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

        _client.Shutdown += async () =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                NeedToShutdown = true;
            });
        };

        _client.ResponseReceived += async (response) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateChatsWithResponse(response);
            });
        };
        
        AttachGroupTextChangedHandlers();
    }
    
    public class User
    {
        public string Name { get; set; } = "";
        public string Username { get; set; } = "";
    }

    public class ChatItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public int ChatId { get; set; }
        public string Name { get; set; } = "";
        public ObservableCollection<Message> Messages { get; set; } = new();
        public string Username { get; set; } = "";
        public bool IsGroup { get; set; } = false;
        public ObservableCollection<User> Members { get; set; } = new();
        private string _lastMessage = "";
        public string LastMessage
        {
            get => _lastMessage;
            private set
            {
                if (_lastMessage != value)
                {
                    _lastMessage = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastMessage)));
                }
            }
        }
        private DateTime _lastMessageTime;
        public DateTime LastMessageTime
        {
            get => _lastMessageTime;
            private set
            {
                if (_lastMessageTime != value)
                {
                    _lastMessageTime = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastMessageTime)));
                }
            }
        }
        public void NotifyLastMessageChanged(string messageText, DateTime sentTime)
        {
            LastMessage = messageText;
            LastMessageTime = sentTime;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastMessage)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastMessageTime)));
        }
        
        private string _draft = "";
        private bool _isPinned = false;
        public long PinOrder { get; set; }

        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                if (_isPinned != value)
                {
                    _isPinned = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPinned)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PinMenuText)));
                }
            }
        }
        public string PinMenuText => IsPinned ? "Unpin chat" : "Pin chat";

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

    private void SortChats()
    {
        if (_isApplyingFilter) return;

        _isUpdatingFilteredChats = true;

        var pinnedChats = Chats
            .Where(chat => chat.IsPinned)
            .OrderByDescending(chat => chat.PinOrder)  
            .ToList();

        var unpinnedChats = Chats
            .Where(chat => !chat.IsPinned)
            .ToList();

        FilteredChats.Clear();

        foreach (var chat in pinnedChats)
            FilteredChats.Add(chat);
        foreach (var chat in unpinnedChats)
            FilteredChats.Add(chat);

        _isUpdatingFilteredChats = false;
    }

    private void UpdateChatView(ChatItem contact)
    {
        ChatHeader.Text = contact.Name;
        ChatAvatar.IsVisible = true;
        ChatUsernameTextBlock.Text = contact.Username;
        ChatUsernameTextBlock.IsVisible = true;
        ShowToggleMembersButton = contact.IsGroup;
        if (contact.IsGroup)
        {
            GroupMembersList.ItemsSource = contact.Members;
            GroupMembersPanel.IsVisible = false;
            _isMembersPanelOpen = false;
            UpImage.IsVisible = false;
            DownImage.IsVisible = true;
        }
        else
        {
            GroupMembersList.ItemsSource = null;
            GroupMembersPanel.IsVisible = false;
            _isMembersPanelOpen = false;
        }
        
        ComputeGroupingFlags(contact.Messages);
        
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
        ChatUsernameTextBlock.IsVisible = false;
        SelectedChatMessages.Clear();
        MessageInputPanel.IsVisible = false;
    }

    private void ApplyFilter(string text)
    {
        if (_isApplyingFilter) return;

        text = text.Trim().ToLower();
        _isApplyingFilter = true;
        _isUpdatingFilteredChats = true;

        if (!string.IsNullOrEmpty(text))
            ChatList.SelectedIndex = -1;

        FilteredChats.Clear();

        var pinnedChats = Chats
            .Where(chat => chat.IsPinned &&
                           (string.IsNullOrEmpty(text) || chat.Name.ToLower().Contains(text)))
            .OrderByDescending(chat => chat.PinOrder)
            .ToList();

        var unpinnedChats = Chats
            .Where(chat => !chat.IsPinned &&
                           (string.IsNullOrEmpty(text) || chat.Name.ToLower().Contains(text)))
            .ToList();

        foreach (var chat in pinnedChats)
            FilteredChats.Add(chat);

        foreach (var chat in unpinnedChats)
            FilteredChats.Add(chat);

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

    private void SearchTextBox_GotFocus(object? sender, RoutedEventArgs e)
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

    private void TogglePinChat_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menu || menu.DataContext is not ChatItem chat)
            return;
        
        chat.IsPinned = !chat.IsPinned;

        if (chat.IsPinned)
            chat.PinOrder = ++_pinSequence;  
        else
            chat.PinOrder = 0;

        SortChats();

        if (!string.IsNullOrEmpty(SearchTextBox.Text))
            ApplyFilter(SearchTextBox.Text);
    }
    
    
    private void MessagesPanel_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
                (MessagesPanel.Parent as ScrollViewer)?.ScrollToEnd(),
            DispatcherPriority.Background);
    }
    
    private void ChatItem_ContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (sender is Control ctrl && ctrl.ContextMenu != null)
        {
            ctrl.ContextMenu.Open(ctrl);
        }

        e.Handled = true;
    }

    
    private void ChatItem_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            e.Handled = true; 
            return;           
        }
        
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
        }
    }

    private void ChatList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isApplyingFilter || _isUpdatingFilteredChats)
            return;

        if (ChatList.SelectedItem is ChatItem contact)
        {
            SingleChatOverlay.IsVisible = false;
            GroupChatOverlay.IsVisible = false;
            MessageInputPanel.IsVisible = true;
            MessagesPanel.IsVisible = true;
            
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

    private async void SendMessage_Click(object? sender, RoutedEventArgs e)
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
        
        if (_editingMessage != null)
        {
            _editingMessage.Text = text;
            _editingMessage.IsEdited = true;
            _editingMessage = null;
            MessageTextBox.Text = "";
            return;
        }

        var msgToSend = new Message { Text = text };

        var msgToDisplay = new Message
        {
            Sender = "Me",
            Text = text,
            IsMine = true,
            IsGroup = contact.IsGroup
        };

        var response = await _client.SendTextMessage(contact.ChatId, msgToSend);
        
        if (response != null && response.Status == Status.Success)
        {
            var msgPayload = response.Payload.Deserialize<TextMessageResponsePayload>();
        
            if (msgPayload != null)
            {
                msgToDisplay.Id = msgPayload.MessageId;
                msgToDisplay.IsDeleted = msgPayload.IsDeleted;
            
                contact.Messages.Add(msgToDisplay);
                SelectedChatMessages.Add(msgToDisplay);
                
                int count = contact.Messages.Count;
                if (count > 2)
                {
                    ComputeFlagsAtIndex(contact.Messages, count - 1);
                    ComputeFlagsAtIndex(contact.Messages, count - 2);
                }
                else
                {
                    ComputeGroupingFlags(contact.Messages);
                }
                
                contact.Draft = "";
                MessageTextBox.Text = "";
                contact.NotifyLastMessageChanged(msgToDisplay.Text, msgToDisplay.SentTime);
            }
        }
        else
        {
            // make error message maybe
        }
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
    
    private void OpenSingleChat_Click(object? sender, RoutedEventArgs e)
    {
        ResetChatView();
        ChatHeader.Text = "Start single chat";
        SingleChatOverlay.IsVisible = true;
        ResetUserSearch();
    }
    
    private void OpenGroupChat_Click(object? sender, RoutedEventArgs e)
    {
        ResetChatView();
        ChatHeader.Text = "Create group chat";
        GroupChatOverlay.IsVisible = true;
        GroupSearchBox.Text = "";
        GroupSearchResultBorder.IsVisible = false;
        _selectedGroupMembers.Clear();
        GroupNameBox.Text = "";
    }
    
    private void ResetUserSearch()
    {
        SearchUserBox.Text = "";
        SearchResultBorder.IsVisible = false;
        SearchErrorText.IsVisible = false;
        SearchUserBox.Classes.Remove("error");
        ResultUserName.Text = "";
        ResultUserUsername.Text = "";
    }
    
    private void ResetChatView()
    {
        MessageInputPanel.IsVisible = false;
        MessagesPanel.IsVisible = false;
        ChatList.SelectedIndex = -1;
        ChatAvatar.IsVisible = false;
        ChatUsernameTextBlock.IsVisible = false;
        SingleChatOverlay.IsVisible = false;
        GroupChatOverlay.IsVisible = false;
        ShowToggleMembersButton = false;
    }
    
    private string NormalizeUsername(string username)
    {
        username = username.Trim().ToLower();
        return username.StartsWith("@") ? username[1..] : username;
    }
    
    private User? FindUserByUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }
        var normalized = NormalizeUsername(username);
        return _allUsers.FirstOrDefault(u =>
            NormalizeUsername(u.Username) == normalized);
    }
    
    private void SearchButton_Click(object? sender, RoutedEventArgs e)
    {
        string username = SearchUserBox.Text!.Trim();
        if (string.IsNullOrEmpty(username))
        {
            SearchErrorText.Text = "Enter a username";
            SearchErrorText.IsVisible = true;
            if (!SearchUserBox.Classes.Contains("error"))
            {
                SearchUserBox.Classes.Add("error");
            }
            return;
        }

        var user = FindUserByUsername(username);
        if (user != null)
        {
            SearchResultBorder.IsVisible = true;
            SearchErrorText.IsVisible = false;
            ResultUserName.Text = user.Name;
            ResultUserUsername.Text = user.Username;
            SearchUserBox.Classes.Remove("error");
            SearchResultBorder.DataContext = user;
        }
        else
        {
            SearchResultBorder.IsVisible = false;
            SearchErrorText.Text = "User not found";
            SearchErrorText.IsVisible = true;
            if (!SearchUserBox.Classes.Contains("error"))
            {
                SearchUserBox.Classes.Add("error");
            }
        }
    }
    //search user box
    private void SearchUserBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb) return;
    
        ClearSearchUserBoxButton.IsVisible = !string.IsNullOrEmpty(tb.Text);
        
        SearchResultBorder.IsVisible = false;
        SearchErrorText.IsVisible = false;
        SearchUserBox.Classes.Remove("error");
    }
    
    private void SearchUserBox_GotFocus(object? sender, RoutedEventArgs e)
    {
        ClearSearchUserBoxButton.IsVisible = !string.IsNullOrEmpty(SearchUserBox.Text);
    }
    
    private void SearchUserBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        ClearSearchUserBoxButton.IsVisible = !string.IsNullOrEmpty(SearchUserBox.Text);
    }
    
    private void ClearSearchUserBoxButton_Click(object? sender, RoutedEventArgs e)
    {
        SearchUserBox.Text = "";
        ClearSearchUserBoxButton.IsVisible = false;
        SearchResultBorder.IsVisible = false;
        SearchErrorText.IsVisible = false;
        SearchUserBox.Classes.Remove("error");
        SearchUserBox.Focus();
    }
    
    /*private void PerformUserSearch(string username)
    {
        var user = FindUserByUsername(username);
        if (user != null)
        {
            SearchResultBorder.IsVisible = true;
            SearchErrorText.IsVisible = false;
            ResultUserName.Text = user.Name;
            ResultUserUsername.Text = user.Username;
            SearchUserBox.Classes.Remove("error");
            SearchResultBorder.DataContext = user;
        }
        else
        {
            SearchResultBorder.IsVisible = false;
            SearchErrorText.Text = "User not found";
            SearchErrorText.IsVisible = true;
            if (!SearchUserBox.Classes.Contains("error"))
            {
                SearchUserBox.Classes.Add("error");
            }
        }
    }*/
    
    //group search user box
    private void GroupSearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb) return;
    
        ClearGroupSearchBoxButton.IsVisible = !string.IsNullOrEmpty(tb.Text);
        
        GroupSearchResultBorder.IsVisible = false;
        GroupSearchErrorText.IsVisible = false;
        GroupSearchBox.Classes.Remove("error");
    }
    
    private void GroupSearchBox_GotFocus(object? sender, RoutedEventArgs e)
    {
        ClearGroupSearchBoxButton.IsVisible = !string.IsNullOrEmpty(GroupSearchBox.Text);
    }
    
    private void GroupSearchBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        ClearGroupSearchBoxButton.IsVisible = !string.IsNullOrEmpty(GroupSearchBox.Text);
    }

    private void ClearGroupSearchBoxButton_Click(object? sender, RoutedEventArgs e)
    {
        GroupSearchBox.Text = "";
        ClearGroupSearchBoxButton.IsVisible = false;
        GroupSearchResultBorder.IsVisible = false;
        GroupSearchErrorText.IsVisible = false;
        GroupSearchBox.Classes.Remove("error");
        GroupSearchBox.Focus();
    }
    
    /*private void PerformGroupUserSearch(string username)
    {
        var user = FindUserByUsername(username);
        if (user != null)
        {
            GroupSearchResultBorder.DataContext = user;
            GroupResultName.Text = user.Name;
            GroupResultUsername.Text = user.Username;
            GroupSearchResultBorder.IsVisible = true;
            GroupSearchErrorText.IsVisible = false;
            GroupSearchBox.Classes.Remove("error");
        }
        else
        {
            GroupSearchResultBorder.IsVisible = false;
            GroupSearchErrorText.Text = "User not found";
            GroupSearchErrorText.IsVisible = true;
            if (!GroupSearchBox.Classes.Contains("error"))
                GroupSearchBox.Classes.Add("error");
        }
    }*/
    
    private async void StartChatButton_Click(object? sender, RoutedEventArgs e)
    {
        string username = SearchUserBox.Text!.Trim();
        if (string.IsNullOrEmpty(username))
        {
            SearchErrorText.Text = "Enter a username";
            SearchErrorText.IsVisible = true;
            if (!SearchUserBox.Classes.Contains("error"))
            {
                SearchUserBox.Classes.Add("error");
            }
            return;
        }
        if (SearchResultBorder.DataContext is not User user)
        {
            SearchErrorText.Text = "Find the user first";
            SearchErrorText.IsVisible = true;
            if (!SearchUserBox.Classes.Contains("error"))
            {
                SearchUserBox.Classes.Add("error");
            }
            return;
        }
        
        var response = await _client.CreatePrivateChat(NormalizeUsername(user.Username));
        
        if (response is null)
        {
            SearchErrorText.Text = "Error connecting to server."; //change
            SearchErrorText.IsVisible = true;
            if (!SearchUserBox.Classes.Contains("error"))
            {
                SearchUserBox.Classes.Add("error");
            }
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (response.Status == Status.Success)
            {
                UpdateChatsWithResponse(response);

                var responsePayload = response.Payload.Deserialize<CreateChatResponsePayload>();
                var chat = Chats.FirstOrDefault(c => c.ChatId == responsePayload?.ChatId);

                //var chat = Chats.FirstOrDefault(c => !c.IsGroup && c.Username == user.Username);
                
                if (chat != null)
                {
                    SingleChatOverlay.IsVisible = false;
                    MessageInputPanel.IsVisible = true;
                    MessagesPanel.IsVisible = true;
                    _currentChat = chat;
                    ChatList.SelectedItem = chat;
                    UpdateChatView(chat);
                }
            }
            else
            {
                if (response.Payload != null
                    && response.Payload.TryGetPropertyValue("message", out var message))
                {
                    SearchErrorText.Text = message?.ToString();
                }
                else
                {
                    SearchErrorText.Text = "Unknown error";
                }

                SearchErrorText.IsVisible = true;
                if (!SearchUserBox.Classes.Contains("error"))
                {
                    SearchUserBox.Classes.Add("error");
                }
            }
        });
    }
    
    private void GroupSearchButton_Click(object? sender, RoutedEventArgs e)
    {
        string username = GroupSearchBox.Text!.Trim();
        if (string.IsNullOrEmpty(username))
        {
            GroupSearchErrorText.Text = "Enter a username";
            GroupSearchErrorText.IsVisible = true;
            if (!GroupSearchBox.Classes.Contains("error"))
            {
                GroupSearchBox.Classes.Add("error");
            }
            GroupSearchResultBorder.IsVisible = false;
            return;
        }
        var user = FindUserByUsername(username);
        if (user != null)
        {
            GroupSearchResultBorder.DataContext = user;
            GroupResultName.Text = user.Name;
            GroupResultUsername.Text = user.Username;
            GroupSearchResultBorder.IsVisible = true;
            GroupSearchErrorText.IsVisible = false;
            GroupSearchBox.Classes.Remove("error");
        }
        else
        {
            GroupSearchResultBorder.IsVisible = false;
            GroupSearchErrorText.Text = "User not found";
            GroupSearchErrorText.IsVisible = true;
            if (!GroupSearchBox.Classes.Contains("error"))
                GroupSearchBox.Classes.Add("error");
        }
    }
    
    private void AddMemberButton_Click(object? sender, RoutedEventArgs e)
    {
        if (GroupSearchResultBorder.DataContext is User user)
        {
            if (_selectedGroupMembers.Any(u => u.Username == user.Username))
            {
                GroupSearchErrorText.Text = "This user has already been added";
                GroupSearchErrorText.IsVisible = true;
                if (!GroupSearchBox.Classes.Contains("error"))
                {
                    GroupSearchBox.Classes.Add("error");
                }
                GroupSearchResultBorder.IsVisible = false;
                return;
            }
            GroupSearchErrorText.IsVisible = false;
            GroupSearchBox.Classes.Remove("error");
            _selectedGroupMembers.Add(user);
            SelectedMembersList.ItemsSource = _selectedGroupMembers;
            GroupSearchBox.Text = "";
            GroupSearchResultBorder.IsVisible = false;
            GroupSearchResultBorder.DataContext = null;
        }
    }
    
    private void RemoveMemberButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is User user)
        {
            _selectedGroupMembers.Remove(user);
            SelectedMembersList.ItemsSource = null;
            SelectedMembersList.ItemsSource = _selectedGroupMembers;
        }
    }

    
    private void GroupNameBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (GroupNameBox == null || GroupNameErrorText == null)
        {
            return;
        }

        if (GroupSearchErrorText.IsVisible && !string.IsNullOrWhiteSpace(GroupSearchBox.Text))
        {
            GroupSearchErrorText.IsVisible = false;
            GroupSearchBox.Classes.Remove("error");
        }
    }
    
    private async void CreateGroupChat_Click(object? sender, RoutedEventArgs e)
    {
        if (GroupNameBox == null || GroupNameErrorText == null || GroupChatOverlay == null)
        {
            return;
        }
        if (_selectedGroupMembers.Count < 2)
        {
            GroupSearchErrorText.Text = "Add at least 2 group members";
            GroupSearchErrorText.IsVisible = true;
            if (!GroupSearchBox.Classes.Contains("error"))
            {
                GroupSearchBox.Classes.Add("error");
            }
            return;
        }
        GroupSearchErrorText.IsVisible = false;
        GroupSearchBox.Classes.Remove("error");
        
        string groupName = GroupNameBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(groupName))
        {
            GroupNameErrorText.Text = "Name cannot be empty";
            GroupNameErrorText.IsVisible = true;
            if (!GroupNameBox.Classes.Contains("error"))
            {
                GroupNameBox.Classes.Add("error");
            }
            return;
        }
        GroupNameErrorText.IsVisible = false;
        GroupNameBox.Classes.Remove("error");

        var chatMembers = new List<string>();

        foreach (var member in _selectedGroupMembers)
        {
            chatMembers.Add(NormalizeUsername(member.Username));
        }
        
        var response = await _client.CreateGroupChat(chatMembers, groupName);

        if (response is null)
        {
            GroupSearchErrorText.Text = "Error connecting to server."; //change
            GroupSearchErrorText.IsVisible = true;
            if (!GroupSearchBox.Classes.Contains("error"))
            {
                GroupSearchBox.Classes.Add("error");
            }
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (response.Status == Status.Success)
            {
                UpdateChatsWithResponse(response);

                var responsePayload = response.Payload.Deserialize<CreateChatResponsePayload>();
                var group = Chats.FirstOrDefault(c => c.ChatId == responsePayload?.ChatId);

                if (group != null)
                {
                    GroupChatOverlay.IsVisible = false;
                    MessageInputPanel.IsVisible = true;
                    MessagesPanel.IsVisible = true;
                    ChatList.SelectedItem = group;
                    _currentChat = group;
                    UpdateChatView(group);
                    _selectedGroupMembers.Clear();
                    SelectedMembersList.ItemsSource = null;
                }
            }
            else
            {
                if (response.Payload != null
                    && response.Payload.TryGetPropertyValue("message", out var message))
                {
                    GroupSearchErrorText.Text = message?.ToString();
                }
                else
                {
                    GroupSearchErrorText.Text = "Unknown error";
                }

                GroupSearchErrorText.IsVisible = true;
                if (!GroupSearchBox.Classes.Contains("error"))
                {
                    GroupSearchBox.Classes.Add("error");
                }
            }
        });
    }
    
    private void AttachTextChangedHandlers(params (TextBox?, TextBlock?)[] pairs)
    {
        foreach (var (box, errorText) in pairs)
        {
            if (box != null && errorText != null)
            {
                box.GetObservable(TextBox.TextProperty).Subscribe(_ =>
                {
                    box.Classes.Remove("error");
                    errorText.IsVisible = false;
                    errorText.Text = "";
                });
            }
        }
    }
    
    private void AttachGroupTextChangedHandlers()
    {
        AttachTextChangedHandlers(
            (this.FindControl<TextBox>("GroupNameBox"), this.FindControl<TextBlock>("GroupNameErrorText"))
        );
    }
    
    private void ToggleMembersButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentChat == null || !_currentChat.IsGroup)
        {
            return;
        }

        _isMembersPanelOpen = !_isMembersPanelOpen;
        GroupMembersPanel.IsVisible = _isMembersPanelOpen;
        UpImage.IsVisible = _isMembersPanelOpen;
        DownImage.IsVisible = !_isMembersPanelOpen;

        if (_isMembersPanelOpen)
        {
            var members = new ObservableCollection<User>
            {
                new User
                {
                    Name = CurrentUserName,
                    Username = CurrentUserUsername
                }
            };
            foreach (var member in _currentChat.Members)
            {
                if (member.Username != CurrentUserUsername)
                {
                    members.Add(member);
                }
            }
            GroupMembersList.ItemsSource = members;
        }
    }
    
    public bool ShowToggleMembersButton
    {
        get => _showToggleMembersButton;
        set
        {
            if (_showToggleMembersButton != value)
            {
                _showToggleMembersButton = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowToggleMembersButton)));
            }
        }
    }
    
    private async void CopyMessage_Click(object? sender, RoutedEventArgs e)
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
    
    private async void EditMessage_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menu || menu.DataContext is not Message msg)
        {
            return;
        }

        MessageTextBox.Text = msg.Text;
        MessageTextBox.Focus();
        MessageTextBox.CaretIndex = MessageTextBox.Text.Length;
        _editingMessage = msg;
        
        // if (ChatList.SelectedItem is ChatItem chat)
        // {
        //     await _client.EditMessageAsync(chat.Name, msg.Id, msg.Text);
        // }
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

    private async void DeleteMessageForAll_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menu || menu.DataContext is not Message msg)
            return;

        if (ChatList.SelectedItem is not ChatItem chat)
            return;

        msg.IsDeleted = true; 
    }

    private void MessageTextBox_SendWithEnter(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb)
            return;

        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.Shift)
        {
            var pos = tb.CaretIndex;
            tb.Text = tb.Text!.Insert(pos, "\n");
            tb.CaretIndex = pos + 1;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None)
        {
            SendMessage_Click(null!, null!);
            e.Handled = true;
        }
    }
    
    private void UpdateExitThemeIcon()
    {
        ExitLight.IsVisible = !_isLight;
        ExitDark.IsVisible = _isLight;
    }
    
    private void UpdateSingleChatThemeIcon()
    {
        SingleChatLight.IsVisible = !_isLight;
        SingleChatDark.IsVisible = _isLight;
    }
    
    private void UpdateGroupChatThemeIcon()
    {
        GroupChatLight.IsVisible = !_isLight;
        GroupChatDark.IsVisible = _isLight;
    }
    
    private void UpdateModeThemeIcon()
    {
        ModeLight.IsVisible = !_isLight;
        ModeDark.IsVisible = _isLight;
    }

    private void SwitchTheme_Click(object? sender, RoutedEventArgs e)
    {
        _isLight = !_isLight;

        (Application.Current as App)?.SetTheme(_isLight ? "Light" : "Dark");
        
        UpdateExitThemeIcon();
        UpdateSingleChatThemeIcon();
        UpdateGroupChatThemeIcon();
        UpdateModeThemeIcon();
    }

    //message grouping

    private static readonly TimeSpan GroupThreshold = TimeSpan.FromMinutes(3);

    private void ComputeGroupingFlags(IList<Message> messages)
    {
        if (messages == null || messages.Count == 0)
            return;

        for (int i = 0; i < messages.Count; i++)
            ComputeFlagsAtIndex(messages, i);
    }

    private void ComputeFlagsAtIndex(IList<Message> messages, int index)
    {
        if (messages.Count == 0 || index < 0 || index >= messages.Count)
            return;

        var cur = messages[index];
        Message? prev = index > 0 ? messages[index - 1] : null;
        Message? next = index < messages.Count - 1 ? messages[index + 1] : null;

        bool isFirst = true;
        bool isLast = true;

        if (prev != null &&
            prev.Sender == cur.Sender &&
            (cur.Timestamp - prev.Timestamp) <= GroupThreshold &&
            !prev.IsDeleted && !cur.IsDeleted)
        {
            isFirst = false;
        }

        if (next != null &&
            next.Sender == cur.Sender &&
            (next.Timestamp - cur.Timestamp) <= GroupThreshold &&
            !next.IsDeleted && !cur.IsDeleted)
        {
            isLast = false;
        }

        cur.IsFirstInGroup = isFirst;
        cur.ShowAvatar = isLast;
        cur.ShowTail = isLast;
    }
    
    public async void UpdateChatsWithResponse(Response response)
    {
        switch (response.Type)
        {
            case CommandType.CreateChat:
            {
                var chatPayload = response.Payload.Deserialize<CreateChatResponsePayload>();
                if (chatPayload != null)
                {
                    if (Chats.Any(c => c.ChatId == chatPayload.ChatId))
                    {
                        return;
                    }
                    
                    var chat = new ChatItem();
                    chat.IsGroup = chatPayload.IsGroup;
                    chat.ChatId = chatPayload.ChatId;

                    if (chat.IsGroup)
                    {
                        chat.Name = chatPayload.Name ?? "";
                        chat.Username = $"{chatPayload.Members.Count} members";
                        //chat.Members = ;
                    }
                    else
                    {
                        var firstMember = chatPayload.Members[0];
                        var secondMember = chatPayload.Members[1];

                        chat.Name = firstMember.Username == _client.GetUsername()
                            ? secondMember.Username
                            : firstMember.Username;
                    }

                    Chats.Add(chat);
                    
                    if (!_isApplyingFilter)
                    {
                        FilteredChats.Add(chat);
                    }
                }

                break;
            }
            case CommandType.SendMessage:
            {
                var msgPayload = response.Payload.Deserialize<TextMessageResponsePayload>();
                
                if (msgPayload is null) 
                {
                    break;
                }
                
                var chat = Chats.FirstOrDefault(c => c.ChatId == msgPayload.ChatId);
                
                if (chat != null)
                {
                    var newMessage = new Message
                    {
                        Id = msgPayload.MessageId,
                        Sender = msgPayload.SenderNickname,
                        //SentTime = msgPayload.SentAt,
                        Text = msgPayload.Content,
                        IsMine = false,
                        IsDeleted = msgPayload.IsDeleted,
                        //IsEdited = msgPayload.IsEdited,
                        IsGroup = chat.IsGroup
                    };
        
                    chat.Messages.Add(newMessage);
                    chat.NotifyLastMessageChanged(newMessage.Text, newMessage.SentTime);

                    if (_currentChat == chat)
                    {
                        SelectedChatMessages.Add(newMessage);
                    }
                }

                break;
            }
        }
    }
}
