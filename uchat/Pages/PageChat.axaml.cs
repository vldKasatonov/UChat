using Avalonia.Controls;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.ComponentModel;
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
            Name = "Vlad", Username = "online", Messages = new ObservableCollection<Message>
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

        Chats.Add(new ChatItem
        {
            Name = "Masha",
            Username = "@2"
        });

        Chats.Add(new ChatItem
        {
            Name = "Uchat",
            Username = "6 members",
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
        
        _allUsers = new List<User>
        {
            new User { Name = "Vlad", Username = "@vlad" },
            new User { Name = "Vika", Username = "@1" },
            new User { Name = "Masha", Username = "@2" },
            new User { Name = "Mariia", Username = "@3" },
            new User { Name = "Roma", Username = "@4" }
        };
        
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
        public string Name { get; set; } = "";
        public ObservableCollection<Message> Messages { get; set; } = new();
        public string Username { get; set; } = "";
        public bool IsGroup { get; set; } = false;
        public string LastMessage => Messages.LastOrDefault()?.Text ?? "";
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

        text = text?.Trim().ToLower() ?? "";
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
            return;
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
        string username = SearchUserBox.Text.Trim();
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
    
    private void SearchUserBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        SearchResultBorder.DataContext = null;
        SearchResultBorder.IsVisible = false;
    }
    
    private void StartChatButton_Click(object? sender, RoutedEventArgs e)
    {
        string username = SearchUserBox.Text.Trim();
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
        SingleChatOverlay.IsVisible = false;
        MessageInputPanel.IsVisible = true;
        MessagesPanel.IsVisible = true;
        var chat = Chats.FirstOrDefault(c => !c.IsGroup && c.Username == user.Username);

        if (chat == null)
        {
            chat = new ChatItem
            {
                Name = user.Name,
                Username = user.Username,
                Messages = new ObservableCollection<Message>()
            };

            Chats.Add(chat);
            FilteredChats.Add(chat);
        }
        _currentChat = chat;
        ChatList.SelectedItem = chat;
        UpdateChatView(chat);
    }
    
    private void GroupSearchButton_Click(object? sender, RoutedEventArgs e)
    {
        string username = GroupSearchBox.Text.Trim();
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
    
    private void CreateGroupChat_Click(object? sender, RoutedEventArgs e)
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

        var group = new ChatItem
        {
            Name = groupName,
            Username = $"{_selectedGroupMembers.Count + 1} members",
            IsGroup = true,
            Messages = new ObservableCollection<Message>()
        };

        Chats.Add(group);
        FilteredChats.Add(group);
        GroupChatOverlay.IsVisible = false;
        ChatList.SelectedItem = group;
        _currentChat = group;
        UpdateChatView(group);
        _selectedGroupMembers.Clear();
        SelectedMembersList.ItemsSource = null;
    }
    
    private void AttachTextChangedHandlers(params (TextBox box, TextBlock errorText)[] pairs)
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
            (this.FindControl<TextBox>("SearchUserBox"), this.FindControl<TextBlock>("SearchErrorText")),
            (this.FindControl<TextBox>("GroupNameBox"), this.FindControl<TextBlock>("GroupNameErrorText")),
            (this.FindControl<TextBox>("GroupSearchBox"), this.FindControl<TextBlock>("GroupSearchErrorText"))
        );
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
            tb.Text = tb.Text.Insert(pos, "\n");
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
}
