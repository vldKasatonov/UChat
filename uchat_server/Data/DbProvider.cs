using dto;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace uchat_server.Data;

public class DbProvider
{
    public record UserSearchResult(int Id, string Username, string Nickname);
    public record CreateChatResult(Chat Chat, List<ChatMemberResponse> Members);

    private readonly string _dbConnection;

    public DbProvider(string dbConnection)
    {
        _dbConnection = dbConnection;
    }
    
    private UchatDbContext CreateDbContext()
    {
        return new UchatDbContext(_dbConnection); 
    }
    
    public async Task<User> RegisterUserAsync(RegisterRequestPayload payload)
    {
        var newUser = new User
        {
            Username = payload.Username,
            Nickname = payload.Nickname,
            HashPassword = payload.Password,
            IsOnline = false,
            Avatar = null
        };

        await using var dbContext = CreateDbContext();

        try
        {
            dbContext.Users.Add(newUser);
            await dbContext.SaveChangesAsync();
        }
        catch(DbUpdateException e) when ((e.InnerException as PostgresException)?.SqlState == "23505")
        {
            throw new InvalidOperationException("Username already exists");
        }

        return newUser;
    }
    
    public async Task<User> AuthenticateUserAsync(string username, string password)
    {
        await using var dbContext = CreateDbContext();

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(password, user.HashPassword))
        {
            throw new InvalidOperationException("Invalid username or password.");
        }

        return user;
    }
    
    public async Task<CreateChatResult> CreateChatAsync(CreateChatRequestPayload payload)
    {
        await using var dbContext = CreateDbContext();
    
        var newChat = new Chat
        {
            IsGroup = payload.IsGroup,
            Name = payload.IsGroup ? payload.Name : null
        };
    
        dbContext.Chats.Add(newChat);
    
        var usernames = payload.Members.Select(m => m.Username).ToList();
    
        var dbUsers = await dbContext.Users
            .Where(u => usernames.Contains(u.Username))
            .Select(u => new 
            {
                u.Id,
                u.Username,
                u.Nickname
            })
            .ToDictionaryAsync(u => u.Username, u => u);

        if (dbUsers.Count != payload.Members.Count)
        {
            throw new InvalidOperationException("One or more users are unavailable or deleted.");
        }

        var chatMembers = new List<ChatMember>();
        var responseMembers = new List<ChatMemberResponse>();

        foreach (var memberRequest in payload.Members)
        {
            if (dbUsers.TryGetValue(memberRequest.Username, out var dbUser))
            {
                chatMembers.Add(new ChatMember
                {
                    Chat = newChat, 
                    UserId = dbUser.Id,
                    HasPrivileges = memberRequest.HasPrivileges
                });

                responseMembers.Add(new ChatMemberResponse
                {
                    UserId = dbUser.Id,
                    Username = dbUser.Username,
                    Nickname = dbUser.Nickname,
                    HasPrivileges = memberRequest.HasPrivileges
                });
            }
        }

        dbContext.ChatMembers.AddRange(chatMembers);
    
        await dbContext.SaveChangesAsync();
    
        return new CreateChatResult(newChat, responseMembers);
    }
    
    public async Task<int?> FindExistingPrivateChatAsync(string username1, string username2)
    {
        await using var dbContext = CreateDbContext();

        var userIds = await dbContext.Users
            .Where(u => u.Username == username1 || u.Username == username2)
            .Select(u => u.Id)
            .ToListAsync();

        if (userIds.Count < 2)
        {
            throw new InvalidOperationException("Failed to resolve both users for chat creation.");
        }
    
        int id1 = userIds[0];
        int id2 = userIds.Count > 1 ? userIds[1] : 0;

        var chatId = await dbContext.Chats
            .Where(c => c.IsGroup == false)
            .Where(c => c.Members.Count == 2)
            .Where(c => c.Members.Any(cm => cm.UserId == id1) && c.Members.Any(cm => cm.UserId == id2))
            .Select(c => c.Id)
            .FirstOrDefaultAsync();

        if (chatId != 0)
        {
            return chatId;
        }

        return null;
    }
    
    public async Task<Message> SaveTextMessageAsync(SendTextMessageRequestPayload payload)
    {
        var newMessage = new Message
        {
            ChatId = payload.ChatId,
            SenderId = payload.SenderId,
            IsText = true,
            IsEdited = false,
            IsDeleted = false
        };

        var newTextMessage = new TextMessage
        {
            Message = newMessage,
            Content = payload.Content
        };

        await using var dbContext = CreateDbContext();

        var chatExists = await dbContext.Chats.AnyAsync(c => c.Id == payload.ChatId);
        if (!chatExists)
        {
            throw new InvalidOperationException($"Chat with ID {payload.ChatId} not found.");
        }

        dbContext.Messages.Add(newMessage);
        dbContext.TextMessages.Add(newTextMessage);
        
        await dbContext.SaveChangesAsync();

        return newMessage;
    }
    
    public async Task<List<int>> GetChatMemberIdsAsync(int chatId)
    {
        await using var dbContext = CreateDbContext();

        var memberIds = await dbContext.ChatMembers
            .Where(cm => cm.ChatId == chatId)
            .Select(cm => cm.UserId)
            .ToListAsync();

        if (memberIds.Count == 0)
        {
            throw new InvalidOperationException($"Chat with ID {chatId} not found or has no members.");
        }

        return memberIds;
    }

    public async Task<string> GetUserNicknameByIdAsync(int userId)
    {
        await using var dbContext = CreateDbContext();

        var nickname = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Nickname)
            .FirstOrDefaultAsync();

        return nickname ?? "";
    }
    
    public async Task<bool> CheckUserExistenceAsync(int userId, string username)
    {
        await using var dbContext = CreateDbContext();

        return await dbContext.Users
            .AnyAsync(u => u.Id == userId && u.Username == username);
    }
    
    public async Task<UserSearchResult?> FindUserByUsernameAsync(string username)
    {
        await using var dbContext = CreateDbContext();

        var result = await dbContext.Users
            .Where(u => u.Username.ToLower() == username.ToLower())
            .Select(u => new UserSearchResult(
                u.Id,
                u.Username,
                u.Nickname
            ))
            .FirstOrDefaultAsync(); 

        return result;
    }
    
    public async Task<List<Chats>> GetUserChatsAsync(int userId)
    {
        await using var dbContext = CreateDbContext();

        var userChatsQuery = from cm in dbContext.ChatMembers
                             where cm.UserId == userId
                             join chat in dbContext.Chats on cm.ChatId equals chat.Id
                             select new
                             {
                                 ChatId = chat.Id,
                                 chat.IsGroup, chat.Name, chat.CreatedAt,
                                 IsChatPinned = cm.IsChatPinned, PinnedAt = cm.PinnedAt
                             };

        var userChatData = await userChatsQuery.ToListAsync();
        var userChatIds = userChatData.Select(x => x.ChatId).ToList();

        if (!userChatIds.Any())
        {
            return new List<Chats>();
        }

        var chatDetailsQuery = dbContext.Chats
            .Where(c => userChatIds.Contains(c.Id))
            .Select(c => new 
            {
                Chat = c,
                LastMessage = dbContext.Messages
                    .Where(m => m.ChatId == c.Id)
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => new { m.Id, m.SentAt, m.SenderId, m.IsDeleted })
                    .FirstOrDefault()
            });
        
        var chatData = await chatDetailsQuery.ToListAsync();
        
        var senderIds = chatData
            .Where(x => x.LastMessage != null)
            .Select(x => x.LastMessage!.SenderId)
            .Distinct()
            .ToList();

        var senderNicknames = await dbContext.Users
            .Where(u => senderIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Nickname);

        var lastMessageIds = chatData
            .Where(x => x.LastMessage != null && !x.LastMessage.IsDeleted)
            .Select(x => x.LastMessage!.Id)
            .ToList();

        var lastMessagesContent = await dbContext.TextMessages
            .Where(tm => lastMessageIds.Contains(tm.MessageId))
            .ToDictionaryAsync(tm => tm.MessageId, tm => tm.Content);
        
        var allMembers = await dbContext.ChatMembers
            .Where(cm => userChatIds.Contains(cm.ChatId))
            .Join(dbContext.Users, cm => cm.UserId, u => u.Id, (cm, u) => new 
            {
                cm.ChatId,
                Member = new ChatMemberResponse 
                {
                    UserId = u.Id,
                    Nickname = u.Nickname,
                    Username = u.Username,
                    HasPrivileges = cm.HasPrivileges
                }
            })
            .ToListAsync();

        var membersByChatId = allMembers.GroupBy(x => x.ChatId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Member).ToList());
        
        var chatMetadata = userChatData.ToDictionary(x => x.ChatId);

        var chatsList = new List<Chats>();

        foreach (var item in chatData)
        {
            string displayName;
            string displayUsername;
            string lastMessageSenderNickname = "";
            
            if (!membersByChatId.TryGetValue(item.Chat.Id, out List<ChatMemberResponse>? chatMembers))
            {
                chatMembers = new List<ChatMemberResponse>();
            }

            if (!chatMetadata.TryGetValue(item.Chat.Id, out var metadata))
            {
                continue;
            }
            
            if (item.Chat.IsGroup)
            {
                displayName = item.Chat.Name ?? "";
                
                int memberCount = await dbContext.ChatMembers.CountAsync(cm => cm.ChatId == item.Chat.Id);
                displayUsername = $"{memberCount} members";
            }
            else
            {
                var otherMember = chatMembers.FirstOrDefault(u => u.UserId != userId);

                displayName = otherMember?.Nickname ?? "Unknown User";
                displayUsername = $"@{otherMember?.Username}";
            }

            string lastMessageContent = "[No messages]";
            DateTime lastMessageTime = item.Chat.CreatedAt;
            
            if (item.LastMessage != null && item.LastMessage.Id > 0)
            {
                lastMessagesContent.TryGetValue(item.LastMessage.Id, out string? content);
                lastMessageContent = content ?? "[Message deleted]";
                lastMessageTime = item.LastMessage.SentAt.ToLocalTime();
                
                if (senderNicknames.TryGetValue(item.LastMessage.SenderId, out string? nickname))
                {
                    lastMessageSenderNickname = item.LastMessage.SenderId == userId ? "Me" : nickname;
                }
            }

            chatsList.Add(new Chats
            {
                ChatId = item.Chat.Id,
                ChatName = displayName,
                Username = displayUsername,
                IsGroup = item.Chat.IsGroup,
                Members = chatMembers,
                LastMessage = lastMessageContent,
                LastMessageTime = lastMessageTime,
                LastMessageUsername = lastMessageSenderNickname,
                IsChatPinned = metadata.IsChatPinned,
                PinnedAt = metadata.PinnedAt
            });
        }

        return chatsList;
    }
    
    public async Task<List<dto.Message>> GetHistoryAsync(int chatId, int userId, int firstLoadedMessageId, int limit)
    {
        await using var dbContext = CreateDbContext();
        DateTime? referenceMessage = null;

        if (firstLoadedMessageId > 0)
        {
            referenceMessage = await dbContext.Messages
                .Where(m => m.Id == firstLoadedMessageId)
                .Select(m => m.SentAt)
                .FirstOrDefaultAsync();

            if (referenceMessage == default)
            {
                return new List<dto.Message>();
            }
        }

        var query = from message in dbContext.Messages
                join textMessage in dbContext.TextMessages
                    on message.Id equals textMessage.MessageId
                join chat in dbContext.Chats
                    on message.ChatId equals chat.Id
                where message.ChatId == chatId
                where !referenceMessage.HasValue || message.SentAt < referenceMessage.Value
                select new 
                {
                    Message = message,
                    Content = textMessage.Content,
                    IsGroup = chat.IsGroup,
                    message.SenderId
                };

        var historyData = await query
            .OrderByDescending(x => x.Message.SentAt)
            .Take(limit) 
            .ToListAsync();


        var senderIds = historyData.Select(x => x.SenderId).Distinct().ToList();
        var userNicknames = await dbContext.Users
            .Where(u => senderIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Nickname);


        var messages = new List<dto.Message>();

        foreach (var item in historyData)
        {
            userNicknames.TryGetValue(item.SenderId, out string? nickname);

            messages.Add(new dto.Message
            {
                Id = item.Message.Id,
                Sender = nickname ?? $"User {item.SenderId}",
                IsMine = item.SenderId == userId,
                Text = item.Message.IsDeleted ? "Message deleted" : item.Content,
                IsDeleted = item.Message.IsDeleted,
                IsEdited = item.Message.IsEdited,
                IsGroup = item.IsGroup,
                Timestamp = new DateTimeOffset(item.Message.SentAt, TimeSpan.Zero),
                SentTime = item.Message.SentAt.ToLocalTime()
            });
        }

        return messages;
    }

    public async Task<TextMessageResponsePayload?> DeleteMessageAsync(int messageId, int userId)
    {
        await using (var dbContext = CreateDbContext())
        {
            var message = await dbContext.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null || message.IsDeleted || message.SenderId != userId)
            {
                return null;
            }

            message.IsDeleted = true;
            await dbContext.SaveChangesAsync();

            return new TextMessageResponsePayload
            {
                ChatId = message.ChatId,
                MessageId = message.Id,
                SenderId = message.SenderId,
                Content = "[Message deleted]",
                SentAt = message.SentAt,
                IsEdited = message.IsEdited,
                IsDeleted = message.IsDeleted
            };
        }
    }
    
    public async Task<TextMessageResponsePayload?> EditMessageAsync(int userId, int messageId, string newContent)
    {
        await using var dbContext = CreateDbContext(); 

        var message = await dbContext.Messages
            .Include(m => m.TextMessage) 
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null || message.TextMessage == null
            || message.SenderId != userId || message.IsDeleted)
        {
            return null;
        }

        message.TextMessage.Content = newContent;
        message.IsEdited = true;
    
        await dbContext.SaveChangesAsync();

        return new TextMessageResponsePayload
        {
            ChatId = message.ChatId,
            MessageId = message.Id,
            SenderId = message.SenderId,
            Content = newContent,
            SentAt = message.SentAt, 
            IsEdited = message.IsEdited,
            IsDeleted = message.IsDeleted
        };
    }
    
    public async Task<bool> UpdateChatPinStatusAsync(int userId, int chatId, bool isPinned)
    {
        await using var dbContext = CreateDbContext();

        var chatMember = await dbContext.ChatMembers
            .FirstOrDefaultAsync(cm => cm.UserId == userId && cm.ChatId == chatId);

        if (chatMember == null)
        {
            return false;
        }

        chatMember.IsChatPinned = isPinned;
        chatMember.PinnedAt = isPinned ? DateTime.UtcNow : null;

        await dbContext.SaveChangesAsync();

        return true;
    }
    
    public async Task<LeaveChatResponsePayload?> DeleteChatAsync(int userId, int chatId)
    {
        await using var dbContext = CreateDbContext();

        var chatMember = await dbContext.ChatMembers
            .Include(cm => cm.Chat)
            .Include(cm => cm.User)
            .FirstOrDefaultAsync(cm => cm.UserId == userId && cm.ChatId == chatId);

        if (chatMember == null)
        {
            return null;
        }

        var chat = chatMember.Chat;
        var deletedUsername = chatMember.User.Username;
        var deletedNickname = chatMember.User.Nickname;
        var isGroup = chat.IsGroup;

        dbContext.ChatMembers.Remove(chatMember);
        await dbContext.SaveChangesAsync();

        var remainingMemberIds = await dbContext.ChatMembers
            .Where(cm => cm.ChatId == chatId)
            .Select(cm => cm.UserId)
            .ToListAsync();
        
        var remainingMembersCount = remainingMemberIds.Count;
        bool chatWasDeleted = false;

        if (!chat.IsGroup && remainingMembersCount <= 1)
        {
            var messages = await dbContext.Messages.Where(m => m.ChatId == chatId).ToListAsync();
            dbContext.Messages.RemoveRange(messages);

            var allMembers = await dbContext.ChatMembers.Where(cm => cm.ChatId == chatId).ToListAsync();
            dbContext.ChatMembers.RemoveRange(allMembers);

            dbContext.Chats.Remove(chat);

            await dbContext.SaveChangesAsync();
    
            chatWasDeleted = true;

            if (remainingMemberIds.Count != 1)
            {
                remainingMemberIds = new List<int>();
            }
        }
        else if (remainingMembersCount == 0)
        {
            var messages = await dbContext.Messages.Where(m => m.ChatId == chatId).ToListAsync();
            dbContext.Messages.RemoveRange(messages);
    
            dbContext.Chats.Remove(chat);
            await dbContext.SaveChangesAsync();
    
            chatWasDeleted = true;
            remainingMemberIds = new List<int>();
        }

        return new LeaveChatResponsePayload
        {
            ChatId = chatId,
            UserId = userId,
            Username = deletedUsername,
            Nickname = deletedNickname,
            IsGroup = isGroup,
            UsersId = remainingMemberIds,
            ChatDeleted = chatWasDeleted
        };
    }
}