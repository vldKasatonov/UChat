using Microsoft.EntityFrameworkCore;

namespace uchat_server.Data;

public class UchatDbContext : DbContext
{
    private readonly string _dbConnectionString;
    
    public UchatDbContext(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(_dbConnectionString);
    }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<ChatMember> ChatMembers => Set<ChatMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<TextMessage> TextMessages => Set<TextMessage>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.Property(u => u.Username)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(u => u.Username)
                .IsUnique();

            entity.Property(u => u.Nickname)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(u => u.HashPassword)
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(u => u.IsOnline)
                .HasDefaultValue(false);

            entity.Property(u => u.Avatar)
                .HasColumnType("bytea")
                .IsRequired(false);
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.ToTable("chats");

            entity.Property(c => c.IsGroup)
                .HasDefaultValue(false);

            entity.Property(c => c.Name)
                .HasMaxLength(50)
                .IsRequired(false);

            entity.Property(c => c.Avatar)
                .HasColumnType("bytea")
                .IsRequired(false);
            
            entity.Property(c => c.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
            
            entity.Property(c => c.IsChatPinned)
                .HasColumnName("is_chat_pinned")
                .HasDefaultValue(false);

            entity.Property(c => c.PinnedAt)
                .HasColumnName("pinned_at")
                .HasDefaultValueSql(null);
        });

        modelBuilder.Entity<ChatMember>(entity =>
        {
            entity.ToTable("chat_members");

            entity.HasKey(cm => new { cm.ChatId, cm.UserId });

            entity.Property(cm => cm.HasPrivileges)
                .HasDefaultValue(false);

            entity.HasOne(cm => cm.Chat)
                .WithMany(c => c.Members)
                .HasForeignKey(cm => cm.ChatId);

            entity.HasOne(cm => cm.User)
                .WithMany(u => u.ChatMembers)
                .HasForeignKey(cm => cm.UserId);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");

            entity.Property(m => m.IsText)
                .HasDefaultValue(true);

            entity.Property(m => m.SentAt)
                .HasColumnType("timestamptz")
                .HasDefaultValueSql("NOW()");

            entity.Property(m => m.IsEdited)
                .HasDefaultValue(false);

            entity.Property(m => m.IsDeleted)
                .HasDefaultValue(false);

            entity.HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId);

            entity.HasOne(m => m.Sender)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.SenderId);
        });

        modelBuilder.Entity<TextMessage>(entity =>
        {
            entity.ToTable("text_messages");

            entity.HasKey(tm => tm.MessageId);

            entity.HasOne(tm => tm.Message)
                .WithOne(m => m.TextMessage)
                .HasForeignKey<TextMessage>(tm => tm.MessageId);
        });
    }
}