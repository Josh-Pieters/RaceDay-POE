using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserID);
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
        });

        b.Entity<UserProfile>(e =>
        {
            e.ToTable("UserProfiles");
            e.HasKey(x => x.ProfileID);
            e.HasIndex(x => x.UserID).IsUnique();
            e.Property(x => x.Gender).HasMaxLength(10);
            e.Property(x => x.EmergencyContactName).HasMaxLength(100);
            e.Property(x => x.EmergencyContactPhone).HasMaxLength(20);
            e.Property(x => x.MedicalNotes).HasMaxLength(500);
            e.HasOne(x => x.User).WithOne(u => u.Profile)
                .HasForeignKey<UserProfile>(x => x.UserID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Event>(e =>
        {
            e.ToTable("Events");
            e.HasKey(x => x.EventID);
            e.Property(x => x.EventName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Location).HasMaxLength(200).IsRequired();
            e.Property(x => x.Province).HasMaxLength(50);
            e.Property(x => x.EventType).HasMaxLength(10).IsRequired();
            e.Property(x => x.DistanceKM).HasPrecision(5, 2);
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryID);
            e.Property(x => x.CategoryName).HasMaxLength(50).IsRequired();
            e.Property(x => x.DistanceKM).HasPrecision(5, 2);
            e.Property(x => x.Price).HasPrecision(8, 2);
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments");
            e.HasKey(x => x.EnrolmentID);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.ParticipantID, x.CategoryID }).IsUnique();
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.ParticipantID)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasKey(x => x.ResultID);
            e.HasIndex(x => x.EnrolmentID).IsUnique();
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result)
                .HasForeignKey<Result>(x => x.EnrolmentID)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CapturedByOrganiser).WithMany()
                .HasForeignKey(x => x.CapturedByOrganiserID)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
