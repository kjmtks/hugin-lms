using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Data
{
    public static class DbInitializer
    {
        public static void Initialize(DatabaseContext context)
        {
        }
    }
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        public void WaitForConnection()
        {
            Task.Run(async () => {
                while (!Database.CanConnect()) { await Task.Delay(500); }
            }).Wait();
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Lecture> Lectures { get; set; }
        public DbSet<LectureUserRelationship> LectureUserRelationships { get; set; }
        public DbSet<Sandbox> Sandboxes { get; set; }
        public DbSet<ActivityAction> ActivityActions { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<ActivityMessage> ActivityMessages { get; set; }
        public DbSet<SandboxTemplate> SandboxTemplates { get; set; }
        public DbSet<ActivityTemplate> ActivityTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<User>().HasKey(x => x.Id);
            modelBuilder.Entity<User>().Property(x => x.Uid).ValueGeneratedOnAdd();
            modelBuilder.Entity<User>().HasIndex(x => x.Account).IsUnique();
            modelBuilder.Entity<User>().HasMany(x => x.Lectures);

            modelBuilder.Entity<Lecture>().ToTable("Lectures");
            modelBuilder.Entity<Lecture>().HasKey(x => x.Id);
            modelBuilder.Entity<Lecture>().HasIndex(x => new { x.Name, x.OwnerId }).IsUnique();
            modelBuilder.Entity<Lecture>().HasOne(x => x.Owner);
            modelBuilder.Entity<Lecture>().HasMany(x => x.LectureUserRelationships).WithOne(x => x.Lecture).HasForeignKey(x => x.LectureId);
            modelBuilder.Entity<Lecture>().HasMany(x => x.Sandboxes);
                        
            modelBuilder.Entity<LectureUserRelationship>().ToTable("LectureUserRelationships");
            modelBuilder.Entity<LectureUserRelationship>().HasKey(a => new { a.UserId, a.LectureId });
            modelBuilder.Entity<LectureUserRelationship>().HasOne(a => a.Lecture).WithMany(l => l.LectureUserRelationships).HasForeignKey(a => a.LectureId);
            modelBuilder.Entity<LectureUserRelationship>().HasOne(a => a.User).WithMany(u => u.LectureUserRelationships).HasForeignKey(a => a.UserId);

            modelBuilder.Entity<Sandbox>().ToTable("Sandboxes");
            modelBuilder.Entity<Sandbox>().HasKey(x => x.Id);
            modelBuilder.Entity<Sandbox>().HasIndex(x => new { x.Name, x.LectureId }).IsUnique();
            modelBuilder.Entity<Sandbox>().HasOne(x => x.Lecture);
                        
            modelBuilder.Entity<ActivityAction>().ToTable("ActivityActions");
            modelBuilder.Entity<ActivityAction>().HasKey(x => x.Id);
            modelBuilder.Entity<ActivityAction>().HasOne(x => x.User);
            modelBuilder.Entity<ActivityAction>().HasOne(x => x.Lecture);

            modelBuilder.Entity<Submission>().ToTable("Submissions");
            modelBuilder.Entity<Submission>().HasKey(x => x.Id);
            modelBuilder.Entity<Submission>().HasOne(x => x.User);
            modelBuilder.Entity<Submission>().HasOne(x => x.Lecture);
            modelBuilder.Entity<Submission>().HasOne(x => x.MarkerUser);

            modelBuilder.Entity<ActivityMessage>().ToTable("ActivityMessages");
            modelBuilder.Entity<ActivityMessage>().HasKey(x => x.Id);
            modelBuilder.Entity<ActivityMessage>().HasOne(x => x.Author);
            modelBuilder.Entity<ActivityMessage>().HasOne(x => x.ToUser);
            modelBuilder.Entity<ActivityMessage>().HasOne(x => x.Lecture);

            modelBuilder.Entity<SandboxTemplate>().ToTable("SandboxTemplates");
            modelBuilder.Entity<SandboxTemplate>().HasKey(x => x.Id);
            modelBuilder.Entity<SandboxTemplate>().HasIndex(x => x.Name).IsUnique();

            modelBuilder.Entity<ActivityTemplate>().ToTable("ActivityTemplates");
            modelBuilder.Entity<ActivityTemplate>().HasKey(x => x.Id);
            modelBuilder.Entity<ActivityTemplate>().HasIndex(x => x.Name).IsUnique();
            modelBuilder.Entity<ActivityTemplate>().HasOne(x => x.SandboxTemplate);
        }
    }
}
