using Microsoft.EntityFrameworkCore;
using Backend.Model;

namespace Backend.Db
{
    public class NoteDbContext : DbContext
    {
        public NoteDbContext(DbContextOptions<NoteDbContext> options) : base(options) { }

        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<Note> Notes { get; set; } = null!;
        public DbSet<User> Users {get; set;} = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Note>()
                .HasOne<Project>()
                .WithMany(project => project.Notes)
                .HasForeignKey(note => note.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);  // Ensures notes can exist without a project

            modelBuilder.Entity<User>()
                .HasIndex(user => user.Username)
                .IsUnique();
        }
    }
}
