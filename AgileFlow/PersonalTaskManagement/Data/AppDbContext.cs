using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PersonalTaskManagement.Models;

namespace PersonalTaskManagement.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Board> Boards => Set<Board>();
        public DbSet<BoardColumn> Columns => Set<BoardColumn>();
        public DbSet<TaskItem> Tasks => Set<TaskItem>();
        public DbSet<Tag> Tags => Set<Tag>();

        public static string DatabasePath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AgileFlow",
            "agileflow.db");

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured) return;

            var dir = Path.GetDirectoryName(DatabasePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
        }

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Board>()
                .HasMany(x => x.Columns)
                .WithOne(c => c.Board!)
                .HasForeignKey(c => c.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<BoardColumn>()
                .HasMany(c => c.Tasks)
                .WithOne(t => t.BoardColumn!)
                .HasForeignKey(t => t.BoardColumnId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<TaskItem>()
                .HasMany(t => t.Tags)
                .WithMany(g => g.Tasks)
                .UsingEntity(j => j.ToTable("TaskTags"));

            b.Entity<Tag>().HasIndex(t => t.Name).IsUnique();
        }

        public override int SaveChanges()
        {
            ApplyTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyTimestamps()
        {
            var now = DateTime.UtcNow;
            foreach (EntityEntry entry in ChangeTracker.Entries())
            {
                if (entry.State != EntityState.Added && entry.State != EntityState.Modified) continue;

                if (TryWrite(entry, "LastModified", now) && entry.State == EntityState.Added)
                {
                    TryWrite(entry, "CreatedAt", now);
                }
            }
        }

        private static bool TryWrite(EntityEntry entry, string propertyName, DateTime value)
        {
            var prop = entry.Metadata.FindProperty(propertyName);
            if (prop == null) return false;
            entry.Property(propertyName).CurrentValue = value;
            return true;
        }
    }
}
