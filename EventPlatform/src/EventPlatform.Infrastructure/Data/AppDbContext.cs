using EventPlatform.Domain.Entities;
using EventPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace EventPlatform.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Speaker> Speakers => Set<Speaker>();
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<Registration> Registrations => Set<Registration>();
        public DbSet<AgendaItem> AgendaItems => Set<AgendaItem>();
        public DbSet<Favorite> Favorites => Set<Favorite>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Soft Delete Global Filters
            modelBuilder.Entity<Event>()
               .HasQueryFilter(e => !e.IsDeleted);

            modelBuilder.Entity<Session>()
                .HasQueryFilter(s => !s.IsDeleted);

            modelBuilder.Entity<Registration>()
                .HasQueryFilter(r => !r.IsDeleted);

            modelBuilder.Entity<AgendaItem>()
                .HasQueryFilter(a => !a.IsDeleted);

            modelBuilder.Entity<Favorite>()
                .HasQueryFilter(f => !f.IsDeleted);

            //User
            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(u => u.Id);
                e.HasIndex(u => u.Email).IsUnique();
                e.Property(u => u.Role).HasConversion<string>();
                e.Property(u => u.Name).HasMaxLength(100).IsRequired();
                e.Property(u => u.Email).HasMaxLength(150).IsRequired();
            });

            //Event
            modelBuilder.Entity<Event>(e =>
            {
                e.HasKey(ev => ev.Id);
                e.Property(ev => ev.Title).HasMaxLength(200).IsRequired();
            });

            //Speaker
            modelBuilder.Entity<Speaker>(e =>
            {
                e.HasKey(s => s.Id);
                e.HasOne(s => s.User)
                 .WithOne(u => u.Speaker)
                 .HasForeignKey<Speaker>(s => s.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            //Session 
            modelBuilder.Entity<Session>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.Title).HasMaxLength(200).IsRequired();
                e.Property(s => s.Status).HasConversion<string>();
                e.HasOne(s => s.Event)
                 .WithMany(ev => ev.Sessions)
                 .HasForeignKey(s => s.EventId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(s => s.Speaker)
                 .WithMany(sp => sp.Sessions)
                 .HasForeignKey(s => s.SpeakerId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            //Registration
            modelBuilder.Entity<Registration>(e =>
            {
                e.HasKey(r => r.Id);
                e.HasIndex(r => new { r.UserId, r.EventId }).IsUnique();
                e.HasOne(r => r.User)
                 .WithMany(u => u.Registrations)
                 .HasForeignKey(r => r.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(r => r.Event)
                 .WithMany(ev => ev.Registrations)
                 .HasForeignKey(r => r.EventId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            //AgendaItem
            modelBuilder.Entity<AgendaItem>(e =>
            {
                e.HasKey(a => a.Id);
                e.HasIndex(a => new { a.UserId, a.SessionId }).IsUnique();
                e.HasOne(a => a.User)
                 .WithMany(u => u.AgendaItems)
                 .HasForeignKey(a => a.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(a => a.Session)
                 .WithMany(s => s.AgendaItems)
                 .HasForeignKey(a => a.SessionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            //Favorite
            modelBuilder.Entity<Favorite>(e =>
            {
                e.HasKey(f => f.Id);
                e.HasIndex(f => new { f.UserId, f.SessionId }).IsUnique();
                e.HasOne(f => f.User)
                 .WithMany(u => u.Favorites)
                 .HasForeignKey(f => f.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(f => f.Session)
                 .WithMany(s => s.Favorites)
                 .HasForeignKey(f => f.SessionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            //Notification
            modelBuilder.Entity<Notification>(e =>
            {
                e.HasKey(n => n.Id);
                e.Property(n => n.Type).HasConversion<string>();
                e.HasOne(n => n.User)
                 .WithMany(u => u.Notifications)
                 .HasForeignKey(n => n.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
