using dto;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace uchat_server.Data;

public class DbProvider
{
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
    
    public async Task<Chat> CreateChatAsync(CreateChatRequestPayload payload)
    {
        await using var dbContext = CreateDbContext();
    
        var newChat = new Chat
        {
            IsGroup = payload.IsGroup,
            Name = payload.IsGroup ? payload.Name : null
        };
    
        dbContext.Chats.Add(newChat);

        var chatMembers = new List<ChatMember>();
    
        var usernames = payload.Members.Select(m => m.Username).ToList();
    
        var dbUsers = await dbContext.Users
            .Where(u => usernames.Contains(u.Username))
            .ToDictionaryAsync(u => u.Username, u => u.Id);

        if (dbUsers.Count != payload.Members.Count)
        {
            throw new InvalidOperationException("One or more chat members were not found.");
        }

        foreach (var member in payload.Members)
        {
            if (dbUsers.TryGetValue(member.Username, out var userId))
            {
                chatMembers.Add(new ChatMember
                {
                    Chat = newChat, 
                    UserId = userId,
                    HasPrivileges = member.HasPrivileges
                });
            }
        }

        dbContext.ChatMembers.AddRange(chatMembers);
    
        await dbContext.SaveChangesAsync();
    
        return newChat;
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
            throw new InvalidOperationException("One or both users not found in the system.");
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
}