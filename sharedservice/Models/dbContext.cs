using Microsoft.EntityFrameworkCore;

namespace sharedservice.Models
{
    public partial class dbContext : DbContext
    {
        public dbContext()
        {
        }

        public dbContext(DbContextOptions<dbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Course> Courses { get; set; } = null!;
        public virtual DbSet<Enrollment> Enrollments { get; set; } = null!;
        public virtual DbSet<MyFile> Files { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("utf8mb4_unicode_ci")
                .HasCharSet("utf8mb4");

            modelBuilder.Entity<Course>(entity =>
            {
                entity.ToTable("courses");

                entity.UseCollation("utf8mb4_general_ci");

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.Code).HasMaxLength(45);

                entity.Property(e => e.Decription).HasMaxLength(255);
            });

            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.ToTable("enrollments");

                entity.UseCollation("utf8mb4_general_ci");

                entity.HasIndex(e => e.CouresId, "FK_course");

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.CouresId).HasColumnType("int(11)");

                entity.Property(e => e.EnrolledDate).HasColumnType("datetime");

                entity.Property(e => e.UserId).HasColumnType("varchar(255)");
            });

           /* OnModelCreatingPartial(modelBuilder);*/
        }

       /* partial void OnModelCreatingPartial(ModelBuilder modelBuilder);*/
    }
}