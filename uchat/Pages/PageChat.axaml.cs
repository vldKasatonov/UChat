using Avalonia.Controls;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.ComponentModel;
using System.Text.Json;
using Avalonia;
using Avalonia.Styling;
using Avalonia.VisualTree;
using dto;

namespace uchat;

public partial class PageChat : UserControl, INotifyPropertyChanged
{
    private readonly Client _client = null!;
    public ObservableCollection<ChatItem> Chats { get; } = new();
    public ObservableCollection<ChatItem> FilteredChats { get; } = new();
    private ObservableCollection<User> _selectedGroupMembers = new();
    public new event PropertyChangedEventHandler? PropertyChanged;
    private ObservableCollection<Message> _selectedChatMessages = new();
    private bool _isApplyingFilter;
    private ChatItem? _selectedChatBeforeSearch;
    private bool _isUpdatingFilteredChats;
    private ChatItem? _currentChat;
    private bool _isLight;
    private bool _isMembersPanelOpen;
    private bool _showToggleMembersButton;
    private Message? _editingMessage;
    private Window? _currentModalDialog;
    
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
    private string _currentUserName = string.Empty;
    public string CurrentUserName
    {
        get => _currentUserName;
        set
        {
            if (_currentUserName != value)
            {
                _currentUserName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentUserName)));
            }
        }
    }
    
    private string _currentUserUsername = string.Empty;
    public string CurrentUserUsername
    {
        get => _currentUserUsername;
        set
        {
            if (_currentUserUsername != value)
            {
                _currentUserUsername = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentUserUsername)));
            }
        }
    }
    
    public PageChat()
    {
        InitializeComponent();
        _isLight = Application.Current?.ActualThemeVariant != ThemeVariant.Dark;
        UpdateExitThemeIcon();
        UpdateSingleChatThemeIcon();
        UpdateGroupChatThemeIcon();
        UpdateModeThemeIcon();
        UpdateSearchThemeIcon();
        UpdateGroupSearchThemeIcon();
        UpdateSendMessageThemeIcon();
        UpdateChangePfpThemeIcon();
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
        CurrentUserUsername = ToHandleFormat(_client.GetUsername());
        CurrentUserName = _client.GetNickname();
        Chats.Clear();
        FilteredChats.Clear();
        
        Task.Run(LoadAllUserChats);

        ChatList.ItemsSource = FilteredChats;
        
        _client.Disconnected += async () =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var dialogToClose = _currentModalDialog;
                
                if (dialogToClose != null)
                {
                    try
                    {
                        _currentModalDialog = null;
                        dialogToClose.Close();
                    }
                    catch (Exception) { /**/ }
                }

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
        public int Id { get; set; }
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
        public bool IsGroupVisible => IsGroup;
        public bool IsSingleVisible => !IsGroup;
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
        private string _lastMessageSender = "";
        public string LastMessageSender
        {
            get => _lastMessageSender;
            private set
            {
                if (_lastMessageSender != value)
                {
                    _lastMessageSender = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastMessageSender)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastMessageDisplay)));
                }
            }
        }
        public string LastMessageDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(LastMessageSender) || LastMessage == "[No messages]")
                    return LastMessage;

                return IsGroup ? $"{LastMessageSender}: {LastMessage}" : LastMessage;
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
        public void NotifyLastMessageChanged(string messageText, DateTime sentTime, string? senderName = null)
        {
            LastMessage = messageText;
            LastMessageSender = senderName ?? "";
            LastMessageTime = sentTime;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastMessage)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastMessageSender)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastMessageDisplay)));
        }

        public DateTime? PinnedAt { get; set; }
        private bool _isPinned;
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
        
        private bool _hasMoreHistory = true; 
        public bool HasMoreHistory
        {
            get => _hasMoreHistory;
            set
            {
                if (_hasMoreHistory != value)
                {
                    _hasMoreHistory = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasMoreHistory)));
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
        
        var currentlySelectedChat = ChatList.SelectedItem as ChatItem;

        var sortedChats = Chats
            .OrderByDescending(chat => chat.IsPinned)
            .ThenByDescending(chat => chat.PinnedAt)
            .ThenByDescending(chat => chat.LastMessageTime)
            .ToList();

        FilteredChats.Clear();

        foreach (var chat in sortedChats)
            FilteredChats.Add(chat);

        _isUpdatingFilteredChats = false;
        
        if (currentlySelectedChat != null && FilteredChats.Contains(currentlySelectedChat))
        {
            ChatList.SelectedItem = currentlySelectedChat;
            _currentChat = currentlySelectedChat;
        }
    }

    private void UpdateChatView(ChatItem contact)
    {
        ChatHeader.Text = contact.Name;
        ChatAvatar.IsVisible = true;
        contact.IsGroup = contact.IsGroup;
        ChatAvatar.DataContext = contact;
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
        
        var currentlySelectedChat = ChatList.SelectedItem as ChatItem;

        if (!string.IsNullOrEmpty(text))
            ChatList.SelectedIndex = -1;

        FilteredChats.Clear();

        var pinnedChats = Chats
            .Where(chat => chat.IsPinned &&
                           (string.IsNullOrEmpty(text) || chat.Name.ToLower().Contains(text)))
            .OrderByDescending(chat => chat.PinnedAt)
            .ThenByDescending(chat => chat.LastMessageTime)
            .ToList();

        var unpinnedChats = Chats
            .Where(chat => !chat.IsPinned &&
                           (string.IsNullOrEmpty(text) || chat.Name.ToLower().Contains(text)))
            .OrderByDescending(chat => chat.LastMessageTime)
            .ToList();

        foreach (var chat in pinnedChats)
            FilteredChats.Add(chat);

        foreach (var chat in unpinnedChats)
            FilteredChats.Add(chat);

        _isUpdatingFilteredChats = false;
        _isApplyingFilter = false;
        
        if (currentlySelectedChat != null && FilteredChats.Contains(currentlySelectedChat))
        {
            ChatList.SelectedItem = currentlySelectedChat;
        }
        else
        {
            ChatList.SelectedIndex = -1;
            if (currentlySelectedChat != null)
            {
                ClearChatView();
                _currentChat = null;
            }
        }
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

    private async void TogglePinChat_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menu || menu.DataContext is not ChatItem chat)
            return;
        
        var newPinStatus = !chat.IsPinned;

        var response = await _client.UpdateChatPinStatus(chat.ChatId, newPinStatus);

        if (response != null && response.Status == Status.Success)
        {
            var pinPayload = response.Payload.Deserialize<UpdatePinStatusRequestPayload>();

            if (pinPayload != null)
            {
                chat.IsPinned = pinPayload.IsChatPinned;
                chat.PinnedAt = pinPayload.IsChatPinned ? DateTime.UtcNow : null;
            
                SortChats();

                if (!string.IsNullOrEmpty(SearchTextBox.Text))
                    ApplyFilter(SearchTextBox.Text);
            }
        }
    }

    private async void LeaveChat_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menu || menu.DataContext is not ChatItem chat)
            return;

        var confirmation =
            await ShowConfirmationDialog("Are you sure you want to delete chat?", "Confirm Action");

        if (!confirmation) 
        {
            return; 
        }

        var response = await _client.LeaveChat(chat.ChatId);

        if (response != null && response.Status == Status.Success)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Chats.Remove(chat);
                FilteredChats.Remove(chat);

                if (_currentChat == chat)
                {
                    ClearChatView();
                    _currentChat = null;
                }
            });
        }
        else
        {
            await ShowConfirmationDialog("Failed to perform the operation. Please try again later.", "Error");
        }
    }

    private async Task<bool> ShowConfirmationDialog(string message, string title)
    {
        var owner = this.GetVisualRoot() as Window;
        if (owner == null)
            return false;

        var content = new ConfirmationDialog(); 
        content.Message = message; 

        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 150,
            CanResize = false,
            SystemDecorations = SystemDecorations.None,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Topmost = false,
            Content = content
        };
        
        _currentModalDialog = dialog;

        var result = await dialog.ShowDialog<bool>(owner);

        if (_currentModalDialog == dialog)
        {
            _currentModalDialog = null; 
        }

        return result;
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
            ChangePfpOverlay.IsVisible = false;
            MessageInputPanel.IsVisible = true;
            MessagesPanel.IsVisible = true;
            
            if (_currentChat == contact)
                return;
        
            _currentChat = contact;
            UpdateChatView(contact);
            
            _selectedChatBeforeSearch = contact;
            
            Task.Run(() => CheckAndLoadHistory(contact));
        }
        else
        {
            if (!_isUpdatingFilteredChats)
            {
                _currentChat = null;
                ClearChatView();
            }
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
            var editedMessage = _editingMessage;
            var newText = text;

            var editResponse = await _client.EditMessage(contact.ChatId, editedMessage.Id, newText);

            if (editResponse != null && editResponse.Status == Status.Success)
            {
                editedMessage.Text = newText;
                editedMessage.IsEdited = true;
                editedMessage.IsDeleted = false;
                
                int index = contact.Messages.IndexOf(editedMessage);
                if (index >= 0)
                {
                    ComputeFlagsAtIndex(contact.Messages, index);
                    if (index > 0) ComputeFlagsAtIndex(contact.Messages, index - 1);
                    if (index < contact.Messages.Count - 1) ComputeFlagsAtIndex(contact.Messages, index + 1);
                }

                if (contact.Messages.LastOrDefault() == editedMessage)
                {
                    string? senderName = contact.IsGroup ? editedMessage.Sender : null;
                    contact.NotifyLastMessageChanged(editedMessage.DisplayText, editedMessage.SentTime, senderName);
                }
            } 

            contact.Draft = "";
            MessageTextBox.Text = "";
            _editingMessage = null; 

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
                msgToDisplay.IsEdited = msgPayload.IsEdited;
                msgToDisplay.SentTime = msgPayload.SentAt.ToLocalTime();
            
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
                string? senderName = contact.IsGroup ? msgToDisplay.Sender : null;
                contact.NotifyLastMessageChanged(msgToDisplay.Text, msgToDisplay.SentTime, senderName);
                ComputeGroupingFlags(contact.Messages);
                SortChats();
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
    
    private void ChangePfp_Click(object? sender, RoutedEventArgs e)
    {
        ResetChatView();
        ChatHeader.Text = "Please choose your new profile picture";
        ChangePfpOverlay.IsVisible = true;
        ResetUserSearch();
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
        ChangePfpOverlay.IsVisible = false;
        ShowToggleMembersButton = false;
    }
    
    private string NormalizeUsername(string username)
    {
        username = username.Trim().ToLower();
        return username.StartsWith("@") ? username[1..] : username;
    }
    
    private async void SearchButton_Click(object? sender, RoutedEventArgs e)
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

        if (ToHandleFormat(username) == CurrentUserUsername)
        {
            SearchResultBorder.IsVisible = false;
            SearchErrorText.Text = "You cannot start a chat with yourself";
            SearchErrorText.IsVisible = true;
            if (!SearchUserBox.Classes.Contains("error"))
            {
                SearchUserBox.Classes.Add("error");
            }
            return;
        }
        
        var response = await _client.SearchUsers(NormalizeUsername(username));
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (response is null)
            {
                SearchResultBorder.IsVisible = false;
                SearchErrorText.Text = "Error connecting to server.";
                SearchErrorText.IsVisible = true;
                if (!SearchUserBox.Classes.Contains("error"))
                {
                    SearchUserBox.Classes.Add("error");
                }
                return;
            }

            if (response.Status == Status.Success)
            {
                var searchPayload = response.Payload.Deserialize<SearchUserResponsePayload>();

                if (searchPayload != null && searchPayload.UserId > 0)
                {
                    var user = new User
                    {
                        Id = searchPayload.UserId,
                        Name = searchPayload.Nickname,
                        Username = ToHandleFormat(searchPayload.Username)
                    };

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
            else
            {
                string errorMessage = "Unknown server error.";
                if (response.Payload != null
                    && response.Payload.TryGetPropertyValue("message", out var message))
                {
                    errorMessage = message?.ToString() ?? errorMessage;
                }
                
                SearchResultBorder.IsVisible = false;
                SearchErrorText.Text = errorMessage;
                SearchErrorText.IsVisible = true;
                if (!SearchUserBox.Classes.Contains("error"))
                {
                    SearchUserBox.Classes.Add("error");
                }
            }
        });
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
                var errorPayload = response.Payload.Deserialize<ChatErrorPayload>();
                
                if (errorPayload != null && errorPayload.ChatId > 0)
                {
                    int existingChatId = errorPayload.ChatId;
        
                    var existingChat = Chats.FirstOrDefault(c => c.ChatId == existingChatId);

                    if (existingChat != null)
                    {
                        SingleChatOverlay.IsVisible = false;
                        MessageInputPanel.IsVisible = true;
                        MessagesPanel.IsVisible = true;
                        _currentChat = existingChat;
                        ChatList.SelectedItem = existingChat;
                        UpdateChatView(existingChat);
                        return;
                    }
                }
    
                string errorMessage = "Unknown server error.";

                if (errorPayload != null)
                {
                    errorMessage = errorPayload.Message; 
                }
                else if (response.Payload != null
                         && response.Payload.TryGetPropertyValue("message", out var message))
                {
                    errorMessage = message?.ToString() ?? errorMessage;
                }

                SearchErrorText.Text = errorMessage;
                SearchErrorText.IsVisible = true;
                if (!SearchUserBox.Classes.Contains("error"))
                {
                    SearchUserBox.Classes.Add("error");
                }
            }
        });
    }
    
    private async void GroupSearchButton_Click(object? sender, RoutedEventArgs e)
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
        
        if (ToHandleFormat(username) == CurrentUserUsername) 
        {
            GroupSearchResultBorder.IsVisible = false;
            GroupSearchErrorText.Text = "You'll be added to the group chat upon creation";
            GroupSearchErrorText.IsVisible = true;
            if (!GroupSearchBox.Classes.Contains("error"))
            {
                GroupSearchBox.Classes.Add("error");
            }
            return;
        }

        var response = await _client.SearchUsers(NormalizeUsername(username));
        
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            if (response is null)
            {
                GroupSearchResultBorder.IsVisible = false;
                GroupSearchErrorText.Text = "Error connecting to server.";
                GroupSearchErrorText.IsVisible = true;
                if (!GroupSearchBox.Classes.Contains("error"))
                {
                    GroupSearchBox.Classes.Add("error");
                }
                return;
            }

            if (response.Status == Status.Success)
            {
                var searchPayload = response.Payload.Deserialize<SearchUserResponsePayload>();

                if (searchPayload != null && searchPayload.UserId > 0)
                {
                    var user = new User
                    {
                        Id = searchPayload.UserId,
                        Name = searchPayload.Nickname,
                        Username = ToHandleFormat(searchPayload.Username)
                    };
                    
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
            else
            {
                string errorMessage = "Unknown server error.";
                if (response.Payload != null
                    && response.Payload.TryGetPropertyValue("message", out var messageToken))
                {
                    errorMessage = messageToken?.ToString() ?? errorMessage;
                }
                
                GroupSearchResultBorder.IsVisible = false;
                GroupSearchErrorText.Text = errorMessage;
                GroupSearchErrorText.IsVisible = true;
                if (!GroupSearchBox.Classes.Contains("error"))
                    GroupSearchBox.Classes.Add("error");
            }
        });
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

    
    private void ClearGroupNameBoxButton_Click(object? sender, RoutedEventArgs e)
    {
        GroupNameBox.Text = "";
        ClearGroupNameBoxButton.IsVisible = false;
        GroupNameBox.Focus();
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
        
        if (ClearGroupNameBoxButton != null)
            ClearGroupNameBoxButton.IsVisible = !string.IsNullOrEmpty(GroupNameBox.Text);
    }
    
    private void GroupNameBox_GotFocus(object? sender, RoutedEventArgs e)
    {
        if (ClearGroupNameBoxButton != null)
            ClearGroupNameBoxButton.IsVisible = !string.IsNullOrEmpty(GroupNameBox.Text);
    }

    private void GroupNameBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (ClearGroupNameBoxButton != null)
            ClearGroupNameBoxButton.IsVisible = !string.IsNullOrEmpty(GroupNameBox.Text);
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
                    GroupSearchErrorText.Text = "Unknown server error.";
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
            var members = new ObservableCollection<User>();
            foreach (var member in _currentChat.Members)
            {
                members.Add(member);
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
    
    private void EditMessage_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menu || menu.DataContext is not Message msg)
        {
            return;
        }

        MessageTextBox.Text = msg.Text;
        MessageTextBox.Focus();
        MessageTextBox.CaretIndex = MessageTextBox.Text.Length;
        _editingMessage = msg;
    }

    private async void DeleteMessageForAll_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menu || menu.DataContext is not Message msg)
            return;

        if (ChatList.SelectedItem is not ChatItem chat)
            return;
        
        var messageToDelete = msg;
        
        var response = await _client.DeleteMessage(messageToDelete.Id, chat.ChatId);

        if (response != null && response.Status == Status.Success)
        {
            messageToDelete.IsDeleted = true;
            
            int index = chat.Messages.IndexOf(messageToDelete);
            if (index >= 0)
            {
                ComputeFlagsAtIndex(chat.Messages, index);
                if (index > 0) ComputeFlagsAtIndex(chat.Messages, index - 1);
                if (index < chat.Messages.Count - 1) ComputeFlagsAtIndex(chat.Messages, index + 1);
            }

            if (chat.Messages.LastOrDefault() == messageToDelete)
            {
                chat.NotifyLastMessageChanged("[Message deleted]", messageToDelete.SentTime);
            }
        }
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
    
    private void UpdateSearchThemeIcon()
    {
        SearchLight.IsVisible = !_isLight;
        SearchDark.IsVisible = _isLight;
    }
    
    private void UpdateModeThemeIcon()
    {
        ModeLight.IsVisible = !_isLight;
        ModeDark.IsVisible = _isLight;
    } 
    
    private void UpdateGroupSearchThemeIcon()
    {
        GroupSearchLight.IsVisible = !_isLight;
        GroupSearchDark.IsVisible = _isLight;
    }
    
    private void UpdateSendMessageThemeIcon()
    {
        SendLight.IsVisible = !_isLight;
        SendDark.IsVisible = _isLight;
    }
    
    private void UpdateChangePfpThemeIcon()
    {
        ChangePfpLight.IsVisible = !_isLight;
        ChangePfpDark.IsVisible = _isLight;
    }

    private void SwitchTheme_Click(object? sender, RoutedEventArgs e)
    {
        _isLight = !_isLight;
        App.SetTheme(_isLight ? "Light" : "Dark");
        UpdateExitThemeIcon();
        UpdateSingleChatThemeIcon();
        UpdateGroupChatThemeIcon();
        UpdateModeThemeIcon();
        UpdateSearchThemeIcon();
        UpdateGroupSearchThemeIcon();
        UpdateSendMessageThemeIcon();
        UpdateChangePfpThemeIcon();
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

        var current = messages[index];
        Message? previous = index > 0 ? messages[index - 1] : null;
        Message? next = index < messages.Count - 1 ? messages[index + 1] : null;

        bool isFirstInGroup = true;
        if (previous != null &&
            previous.Sender == current.Sender &&
            !previous.IsDeleted && !current.IsDeleted &&
            (current.SentTime - previous.SentTime) <= GroupThreshold)
        {
            isFirstInGroup = false;
        }
        
        bool isLastInGroup = true;
        if (next != null &&
            next.Sender == current.Sender &&
            !next.IsDeleted && !current.IsDeleted &&
            (next.SentTime - current.SentTime) <= GroupThreshold)
        {
            isLastInGroup = false;
        }

        current.IsFirstInGroup = isFirstInGroup;
        current.ShowSenderName = current.IsGroup && !current.IsMine && isFirstInGroup;
        current.ShowTail = isLastInGroup;
        current.ShowAvatar = current.IsGroup && !current.IsMine && isLastInGroup;
        current.AvatarPlaceholderWidth = current.IsGroup && !current.IsMine ? 40 : 0;
        if (current.ShowTail)
        {
            current.MessageMarginLeft = current.IsGroup && !current.IsMine ? 40 : 0;
        }
        else
        {
            current.MessageMarginLeft = current.IsGroup && !current.IsMine ? 46.5 : 6.5;
        }
    }
    
    private void LogOutButton_Click(object? sender, RoutedEventArgs e)
    {
        _client.ClearInfo();
        var main = this.GetVisualRoot() as MainWindow;
        if (main != null)
        {
            main.Navigate(new PageLogin(_client));
        }
    } 

    private static string ToHandleFormat(string username)
    {
        username = username.Trim().ToLower();
        return username.StartsWith("@") ? username : "@" + username;
    }
    
    private ChatItem ChatsToChatItem(Chats chat)
    {
        var chatItem = new ChatItem
        {
            ChatId = chat.ChatId,
            Name = chat.ChatName,
            Username = chat.Username,
            IsGroup = chat.IsGroup,
            Members = new ObservableCollection<User>(),
            Messages = new ObservableCollection<Message>(),
            IsPinned = chat.IsChatPinned,
            PinnedAt = chat.PinnedAt
        };
        
        foreach (var member in chat.Members)
        {
            chatItem.Members.Add(new User 
            {
                Id = member.UserId,
                Name = member.Nickname,
                Username = ToHandleFormat(member.Username)
            });
        }
        
        string? senderName = chat.IsGroup ? chat.LastMessageUsername : null;
        chatItem.NotifyLastMessageChanged(chat.LastMessage, chat.LastMessageTime.ToLocalTime(), senderName);
    
        return chatItem;
    }
    
    private async Task LoadAllUserChats()
    {
        if (Chats.Any())
        {
            return;
        } 

        var response = await _client.GetUserChats();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (response != null && response.Status == Status.Success)
            {
                var chatListPayload = response.Payload.Deserialize<GetUserChatsResponsePayload>();

                if (chatListPayload != null)
                {
                    foreach (var chat in chatListPayload.Chats)
                    {
                        var newChatItem = ChatsToChatItem(chat); 
                    
                        Chats.Add(newChatItem);
                        FilteredChats.Add(newChatItem); 
                    }
                
                    SortChats(); 
                }
            }
        });
    }
    
    private bool _isLoadingHistory;

    private async Task LoadChatHistory(int chatId, int beforeMessageId)
    {
        if (_isLoadingHistory)
        {
            return;
        }

        _isLoadingHistory = true; 
        
        try
        {
            var response = await _client.GetChatHistory(chatId, beforeMessageId);
            
            if (response != null && response.Status == Status.Success)
            {
                var historyPayload = response.Payload.Deserialize<ChatHistoryResponsePayload>();
                
                if (historyPayload != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var chat = Chats.FirstOrDefault(c => c.ChatId == chatId);
                        if (chat == null)
                        {
                            return;
                        }
                        
                        var newMessages = historyPayload.Messages;

                        foreach (var msg in newMessages)
                        {
                            msg.SentTime = msg.SentTime.ToLocalTime(); 
                            msg.Timestamp = msg.Timestamp.ToLocalTime();
                            msg.IsGroup = chat.IsGroup;
                        }

                        for (int i = 0; i < newMessages.Count; i++)
                        {
                            chat.Messages.Insert(0, newMessages[i]);
                        }

                        chat.HasMoreHistory = historyPayload.HasMore;

                        ComputeGroupingFlags(chat.Messages);

                        if (_currentChat == chat)
                        {
                            for (int i = 0; i < newMessages.Count; i++)
                            {
                                SelectedChatMessages.Insert(0, newMessages[i]); 
                            }

                            if (MessagesPanel is { } itemsControl) 
                            {
                                if (beforeMessageId == 0)
                                {
                                    (MessagesPanel.Parent as ScrollViewer)?.ScrollToEnd();
                                }
                                else 
                                {
                                    int addedCount = newMessages.Count;

                                    if (chat.Messages.Count > addedCount)
                                    {
                                        var anchorMessage = chat.Messages[addedCount]; 
                                        itemsControl.ScrollIntoView(anchorMessage); 
                                    }
                                }
                            }
                        }
                    });
                }
            }
            else
            {
                // make error message
            }
        }
        finally
        {
            _isLoadingHistory = false;
        }
    }
    
    private async Task CheckAndLoadHistory(ChatItem chat)
    {
        if (chat.Messages.Any() && !chat.HasMoreHistory)
        {
            return;
        }

        int beforeMessageId = chat.Messages.Any() ? chat.Messages.First().Id : 0;

        await LoadChatHistory(chat.ChatId, beforeMessageId); 
    }
    
    private void UpdateChatsWithResponse(Response response)
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
                    chat.Members = new ObservableCollection<User>();
                    chat.IsPinned = false; 
                    chat.PinnedAt = null;

                    foreach (var member in chatPayload.Members)
                    {
                        chat.Members.Add(new User
                        {
                            Id = member.UserId,
                            Name = member.Nickname,
                            Username = ToHandleFormat(member.Username)
                        });
                    }

                    if (chat.IsGroup)
                    {
                        chat.Name = chatPayload.Name ?? "";
                        chat.Username = $"{chatPayload.Members.Count} members";
                    }
                    else
                    {
                        var firstMember = chatPayload.Members[0];
                        var secondMember = chatPayload.Members[1];

                        var firstUsername = ToHandleFormat(firstMember.Username);
                        var secondUsername = ToHandleFormat(secondMember.Username);

                        if (firstUsername == CurrentUserUsername)
                        {
                            chat.Name = secondMember.Nickname;
                            chat.Username = secondUsername;
                        }
                        else
                        {
                            chat.Name = firstMember.Nickname;
                            chat.Username = firstUsername;
                        }
                    }

                    Chats.Add(chat);
                    chat.NotifyLastMessageChanged("[No messages]",chatPayload.CreatedAt.ToLocalTime());
                    SortChats();
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
                        SentTime = msgPayload.SentAt.ToLocalTime(),
                        Text = msgPayload.Content,
                        IsMine = false,
                        IsDeleted = msgPayload.IsDeleted,
                        IsEdited = msgPayload.IsEdited,
                        IsGroup = chat.IsGroup
                    };

                    chat.Messages.Add(newMessage);
                    ComputeGroupingFlags(chat.Messages);
                    string? senderName = chat.IsGroup ? newMessage.Sender : null;
                    chat.NotifyLastMessageChanged(newMessage.Text, newMessage.SentTime, senderName);
                    SortChats();

                    if (_currentChat == chat)
                    {
                        SelectedChatMessages.Add(newMessage);
                    }
                }

                break;
            }
            case CommandType.DeleteForAll:
            {
                var deletePayload = response.Payload.Deserialize<TextMessageResponsePayload>();
                
                if (deletePayload is null) 
                {
                    break;
                }
                
                var chat = Chats.FirstOrDefault(c => c.ChatId == deletePayload.ChatId);
                
                if (chat != null)
                {
                    var msg = chat.Messages.FirstOrDefault(m => m.Id == deletePayload.MessageId);
                    
                    if (msg != null)
                    {
                        msg.IsDeleted = true; 

                        int index = chat.Messages.IndexOf(msg);
                        if (index >= 0)
                        {
                            ComputeFlagsAtIndex(chat.Messages, index);
                            if (index > 0) ComputeFlagsAtIndex(chat.Messages, index - 1);
                            if (index < chat.Messages.Count - 1) ComputeFlagsAtIndex(chat.Messages, index + 1);
                        }

                        if (chat.Messages.LastOrDefault() == msg)
                        {
                            string? senderName = chat.IsGroup ? msg.Sender : null;
                            chat.NotifyLastMessageChanged("[Message deleted]", msg.SentTime, senderName);
                        }
                    }
                }

                break;
            }
            case CommandType.EditMessage:
            {
                var editPayload = response.Payload.Deserialize<TextMessageResponsePayload>();

                if (editPayload is null) 
                {
                    break;
                }

                var chat = Chats.FirstOrDefault(c => c.ChatId == editPayload.ChatId);
                
                if (chat != null)
                {
                    var messageToEdit = chat.Messages.FirstOrDefault(m => m.Id == editPayload.MessageId);
                    
                    if (messageToEdit != null)
                    {
                        messageToEdit.Text = editPayload.Content;
                        messageToEdit.IsEdited = editPayload.IsEdited;
                        messageToEdit.IsDeleted = editPayload.IsDeleted;

                        int index = chat.Messages.IndexOf(messageToEdit);
                        if (index >= 0)
                        {
                            ComputeFlagsAtIndex(chat.Messages, index);
                            if (index > 0) ComputeFlagsAtIndex(chat.Messages, index - 1);
                            if (index < chat.Messages.Count - 1) ComputeFlagsAtIndex(chat.Messages, index + 1);
                        }

                        if (chat.Messages.LastOrDefault() == messageToEdit)
                        {
                            string? senderName = chat.IsGroup ? messageToEdit.Sender : null;
                            chat.NotifyLastMessageChanged(messageToEdit.DisplayText, messageToEdit.SentTime, senderName);
                        }
                    }
                }
                
                break;
            }
            case CommandType.LeaveChat:
            {
                var deletePayload = response.Payload.Deserialize<LeaveChatResponsePayload>();

                if (deletePayload != null) 
                {
                    var chat = Chats.FirstOrDefault(c => c.ChatId == deletePayload.ChatId);

                    if (chat != null)
                    {
                        if (chat.IsGroup)
                        {
                            if (!deletePayload.ChatDeleted)
                            {
                                var memberToRemove = chat.Members.FirstOrDefault(m => m.Id == deletePayload.UserId);
                                if (memberToRemove != null)
                                {
                                    chat.Members.Remove(memberToRemove);
                                    chat.Username = $"{deletePayload.UsersId.Count} members";
                                }

                                if (_currentChat == chat)
                                {
                                    GroupMembersList.ItemsSource = null;
                                    GroupMembersList.ItemsSource = chat.Members;
                                    ChatUsernameTextBlock.Text = chat.Username;
                                }
                            }
                        }
                        else
                        {
                            Chats.Remove(chat);
                            FilteredChats.Remove(chat);
                            if (_currentChat == chat)
                            {
                                ClearChatView();
                            }
                        }
                    }
                }

                break;
            }
        }
    }

}
