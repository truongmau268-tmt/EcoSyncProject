using EcoSyncWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoSyncWeb.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectRegistration> ProjectRegistrations { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }

        // AI & nội dung
        public DbSet<AiChatLog> AiChatLogs { get; set; }
        public DbSet<AiSummary> AiSummaries { get; set; }
        public DbSet<News> News { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Feedback>()
                .HasIndex(feedback => new { feedback.ProjectId, feedback.AuthorEmail })
                .IsUnique();
        }
    }
}
