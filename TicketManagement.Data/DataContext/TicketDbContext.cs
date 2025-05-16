using Microsoft.EntityFrameworkCore;
using TicketManagement.Data.Models;

namespace TicketManagement.Data.DataContext
{
    public class TicketDbContext : DbContext
    {
        public TicketDbContext(DbContextOptions<TicketDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User entity configurations
            modelBuilder.Entity<User>()
                .HasKey(u => u.UserId);

            modelBuilder.Entity<User>()
                .Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<User>()
                .Property(u => u.Password)
                .IsRequired()
                .HasMaxLength(100);

            // Ticket entity configurations
            modelBuilder.Entity<Ticket>()
                .HasKey(t => t.TicketId);

            modelBuilder.Entity<Ticket>()
                .Property(t => t.Subject)
                .IsRequired()
                .HasMaxLength(200);

            modelBuilder.Entity<Ticket>()
                .Property(t => t.Description)
                .IsRequired();

            modelBuilder.Entity<Ticket>()
                .Property(t => t.Category)
                .HasMaxLength(100);

            modelBuilder.Entity<Ticket>()
                .Property(t => t.SubCategory)
                .HasMaxLength(100);

            // One-to-many relationship between User and Tickets
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.User)
                .WithMany(u => u.Tickets)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Message entity configurations
            modelBuilder.Entity<Message>()
                .HasKey(m => m.MessageId);
                
            modelBuilder.Entity<Message>()
                .Property(m => m.Content)
                .IsRequired();
                
            // One-to-many relationship between Ticket and Messages
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Ticket)
                .WithMany(t => t.Messages)
                .HasForeignKey(m => m.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // One-to-many relationship between User (Sender) and Messages
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent circular cascade delete

            // Seed admin user
            modelBuilder.Entity<User>().HasData(new User
            {
                UserId = 1,
                Name = "Admin",
                Email = "admin@admin.com",
                Password = "Admin@123",
                IsAdmin = true
            });
        }
    }
} 