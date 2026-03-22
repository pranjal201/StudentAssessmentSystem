using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudentAssessment.Core.Entities;

namespace StudentAssessment.Infrastructure.Database
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions dbContextOptions): base(dbContextOptions)
        {

        }
        public DbSet<Class> Class {get;set;}
        public DbSet<Exam> Exam {get;set;}
        public DbSet<Mark> Mark {get;set;}
        public DbSet<MarkSubmission> MarkSubmission {get;set;}
        public DbSet<Ranking> Ranking {get;set;}
        public DbSet<Section> Section {get;set;}
        public DbSet<Student> Student {get;set;}
        public DbSet<Subject> Subject {get;set;}
        public DbSet<TeacherSection> TeacherSection {get;set;}
        public DbSet<User> User {get;set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Configuration
            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(u => u.Id);
                e.Property(u => u.Username).IsRequired().HasMaxLength(100);
                e.Property(u => u.PasswordHash).IsRequired();
                e.Property(u => u.Role).IsRequired();
                e.Property(u => u.CreatedAt).IsRequired();
                e.HasIndex(u => u.Username).IsUnique();

                e.HasMany(u => u.TeacherSections)
                    .WithOne(ts => ts.Teacher)
                    .HasForeignKey(ts => ts.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Class Configuration
            modelBuilder.Entity<Class>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Name).IsRequired().HasMaxLength(100);

                e.HasMany(c => c.Sections)
                    .WithOne(s => s.Class)
                    .HasForeignKey(s => s.ClassId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(c => c.Students)
                    .WithOne(s => s.Class)
                    .HasForeignKey(s => s.ClassId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(c => c.Exams)
                    .WithOne(ex => ex.Class)
                    .HasForeignKey(ex => ex.ClassId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Section Configuration
            modelBuilder.Entity<Section>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.Name).IsRequired().HasMaxLength(100);
                e.Property(s => s.ClassId).IsRequired();

                e.HasOne(s => s.Class)
                    .WithMany(c => c.Sections)
                    .HasForeignKey(s => s.ClassId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(s => s.Students)
                    .WithOne(st => st.Section)
                    .HasForeignKey(st => st.SectionId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasMany(s => s.TeacherSections)
                    .WithOne(ts => ts.Section)
                    .HasForeignKey(ts => ts.SectionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Student Configuration
            modelBuilder.Entity<Student>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.FirstName).IsRequired().HasMaxLength(50);
                e.Property(s => s.LastName).IsRequired().HasMaxLength(50);
                e.Property(s => s.ClassId).IsRequired();
                e.Property(s => s.SectionId).IsRequired();
                e.Property(s => s.CreatedAt).IsRequired();

                e.HasOne(s => s.Class)
                    .WithMany(c => c.Students)
                    .HasForeignKey(s => s.ClassId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(s => s.Section)
                    .WithMany(sec => sec.Students)
                    .HasForeignKey(s => s.SectionId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasMany(s => s.Marks)
                    .WithOne(m => m.Student)
                    .HasForeignKey(m => m.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(s => s.Rankings)
                    .WithOne(r => r.Student)
                    .HasForeignKey(r => r.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Exam Configuration
            modelBuilder.Entity<Exam>(e =>
            {
                e.HasKey(ex => ex.Id);
                e.Property(ex => ex.Name).IsRequired().HasMaxLength(100);
                e.Property(ex => ex.Type).IsRequired();
                e.Property(ex => ex.ClassId).IsRequired();

                e.HasOne(ex => ex.Class)
                    .WithMany(c => c.Exams)
                    .HasForeignKey(ex => ex.ClassId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(ex => ex.Marks)
                    .WithOne(m => m.Exam)
                    .HasForeignKey(m => m.ExamId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(ex => ex.Rankings)
                    .WithOne(r => r.Exam)
                    .HasForeignKey(r => r.ExamId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Subject Configuration
            modelBuilder.Entity<Subject>(e =>
            {
                e.HasKey(s => s.Code);
                e.Property(s => s.Code).HasMaxLength(20);
                e.Property(s => s.Name).IsRequired().HasMaxLength(100);

                e.HasMany(s => s.Marks)
                    .WithOne(m => m.Subject)
                    .HasForeignKey(m => m.SubjectCode)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Mark Configuration
            modelBuilder.Entity<Mark>(e =>
            {
                e.HasKey(m => m.Id);
                e.Property(m => m.StudentId).IsRequired();
                e.Property(m => m.SubjectCode).IsRequired().HasMaxLength(20);
                e.Property(m => m.ExamId).IsRequired();
                e.Property(m => m.Score).IsRequired().HasPrecision(5, 2);
                e.Property(m => m.IdempotencyKey).IsRequired();
                e.Property(m => m.CreatedAt).IsRequired();
                e.Property(m => m.UpdatedAt).IsRequired();

                e.HasOne(m => m.Student)
                    .WithMany(s => s.Marks)
                    .HasForeignKey(m => m.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(m => m.Subject)
                    .WithMany(s => s.Marks)
                    .HasForeignKey(m => m.SubjectCode)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(m => m.Exam)
                    .WithMany(ex => ex.Marks)
                    .HasForeignKey(m => m.ExamId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint on StudentId, ExamId, SubjectCode
                e.HasIndex(m => new { m.StudentId, m.ExamId, m.SubjectCode })
                    .IsUnique(true);

                // Unique constraint on IdempotencyKey
                e.HasIndex(m => m.IdempotencyKey)
                    .IsUnique(true);
            });

            // TeacherSection Configuration
            modelBuilder.Entity<TeacherSection>(e =>
            {
                e.HasKey(ts => new { ts.TeacherId, ts.SectionId });

                e.HasOne(ts => ts.Teacher)
                    .WithMany(u => u.TeacherSections)
                    .HasForeignKey(ts => ts.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(ts => ts.Section)
                    .WithMany(s => s.TeacherSections)
                    .HasForeignKey(ts => ts.SectionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Ranking Configuration
            modelBuilder.Entity<Ranking>(e =>
            {
                e.HasKey(r => new { r.StudentId, r.ExamId });
                e.Property(r => r.TotalMarks).IsRequired().HasPrecision(5, 2);
                e.Property(r => r.SectionRank).IsRequired();
                e.Property(r => r.ClassRank).IsRequired();
                e.Property(r => r.UpdatedAt).IsRequired();

                e.HasOne(r => r.Student)
                    .WithMany(s => s.Rankings)
                    .HasForeignKey(r => r.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(r => r.Exam)
                    .WithMany(ex => ex.Rankings)
                    .HasForeignKey(r => r.ExamId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // MarkSubmission Configuration
            modelBuilder.Entity<MarkSubmission>(e =>
            {
                e.HasKey(ms => ms.Id);
                e.Property(ms => ms.Payload).IsRequired();
                e.Property(ms => ms.Status).IsRequired();
                e.Property(ms => ms.RetryCount).IsRequired();
                e.Property(ms => ms.CorrelationId).IsRequired().HasMaxLength(100);
                e.Property(ms => ms.CreatedAt).IsRequired();

                e.HasIndex(ms => ms.CorrelationId);
            });
        }
    }
}