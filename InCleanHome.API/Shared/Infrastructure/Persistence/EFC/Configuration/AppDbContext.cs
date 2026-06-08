using EntityFrameworkCore.CreatedUpdatedDate.Extensions;
using InCleanHome.API.Booking.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.Messaging.Domain.Model.Aggregates;
using InCleanHome.API.Notifications.Domain.Model.Aggregates;
using InCleanHome.API.Payments.Domain.Model.Aggregates;
using InCleanHome.API.Profiles.Domain.Model.Aggregates;
using InCleanHome.API.Reports.Domain.Model.Aggregates;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;
using InCleanHome.API.SearchAndCatalog.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;
using Microsoft.EntityFrameworkCore;

namespace InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration;

/// <summary>
///     Application database context for the InCleanHome platform.
/// </summary>
/// <remarks>
///     Aggregates the persistence configuration of every bounded context: IAM,
///     Profiles, SearchAndCatalog, Booking, Payments, ReviewsAndEvaluation, Messaging.
///     <para>
///     Snake-case naming convention is applied at the end of <c>OnModelCreating</c>, so
///     C# <c>WorkerProfile.HourlyRate</c> ends up as <c>worker_profiles.hourly_rate</c> in
///     PostgreSQL.
///     </para>
/// </remarks>
public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.AddCreatedUpdatedInterceptor();
        base.OnConfiguring(builder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // IAM
        builder.Entity<User>().HasKey(u => u.Id);
        builder.Entity<User>().Property(u => u.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<User>().Property(u => u.Email).IsRequired().HasMaxLength(120);
        builder.Entity<User>().Property(u => u.PasswordHash).IsRequired();
        builder.Entity<User>().Property(u => u.Role).IsRequired().HasMaxLength(20);
        builder.Entity<User>().Property(u => u.IsVerified).HasDefaultValue(false);
        builder.Entity<User>().Property(u => u.DocumentsVerified).HasDefaultValue(false);
        builder.Entity<User>().Property(u => u.DocumentsUploaded).HasDefaultValue(false);
        builder.Entity<User>().Property(u => u.DocumentsRejected).HasDefaultValue(false);
        builder.Entity<User>().Property(u => u.ResetToken).HasMaxLength(64);
        builder.Entity<User>().Property(u => u.ResetTokenExpiresAt);
        builder.Entity<User>().Property(u => u.SuspendedUntil);
        builder.Entity<User>().Property(u => u.SuspensionReason).HasMaxLength(300);
        builder.Entity<User>().Property(u => u.DeviceToken).HasMaxLength(500);
        builder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        builder.Entity<WorkerDocument>().HasKey(d => d.Id);
        builder.Entity<WorkerDocument>().Property(d => d.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<WorkerDocument>().Property(d => d.UserId).IsRequired();
        builder.Entity<WorkerDocument>().Property(d => d.DocumentType).IsRequired().HasMaxLength(40);
        builder.Entity<WorkerDocument>().Property(d => d.FileName).IsRequired().HasMaxLength(200);
        builder.Entity<WorkerDocument>().Property(d => d.FileBase64).IsRequired().HasColumnType("TEXT");

        // Profiles
        builder.Entity<ClientProfile>().HasKey(c => c.Id);
        builder.Entity<ClientProfile>().Property(c => c.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<ClientProfile>().Property(c => c.UserId).IsRequired();
        builder.Entity<ClientProfile>().Property(c => c.Name).IsRequired().HasMaxLength(120);
        builder.Entity<ClientProfile>().Property(c => c.Phone).HasMaxLength(20);
        builder.Entity<ClientProfile>().Property(c => c.PhotoUrl);
        builder.Entity<ClientProfile>().HasIndex(c => c.UserId).IsUnique();

        builder.Entity<WorkerProfile>().HasKey(w => w.Id);
        builder.Entity<WorkerProfile>().Property(w => w.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<WorkerProfile>().Property(w => w.UserId).IsRequired();
        builder.Entity<WorkerProfile>().Property(w => w.Name).IsRequired().HasMaxLength(120);
        builder.Entity<WorkerProfile>().Property(w => w.Phone).HasMaxLength(20);
        builder.Entity<WorkerProfile>().Property(w => w.Age);
        builder.Entity<WorkerProfile>().Property(w => w.Gender).HasMaxLength(20);
        builder.Entity<WorkerProfile>().Property(w => w.HourlyRate).HasPrecision(10, 2);
        builder.Entity<WorkerProfile>().Property(w => w.ExperienceYears);
        builder.Entity<WorkerProfile>().Property(w => w.Bio).HasMaxLength(1000);
        builder.Entity<WorkerProfile>().Property(w => w.AverageRating).HasPrecision(3, 2);
        builder.Entity<WorkerProfile>().Property(w => w.TotalServices);
        builder.Entity<WorkerProfile>().Property(w => w.PhotoUrl);
        builder.Entity<WorkerProfile>().HasIndex(w => w.UserId).IsUnique();

        // PostgreSQL text[] mapping for the multi-value lists. Npgsql's provider maps
        // List<string> to text[] natively when the column type is set explicitly.
        builder.Entity<WorkerProfile>().Property(w => w.ServiceTypes).HasColumnType("text[]");
        builder.Entity<WorkerProfile>().Property(w => w.Zones).HasColumnType("text[]");
        
        // SearchAndCatalog
        builder.Entity<AvailabilitySlot>().HasKey(a => a.Id);
        builder.Entity<AvailabilitySlot>().Property(a => a.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<AvailabilitySlot>().Property(a => a.WorkerUserId).IsRequired();
        builder.Entity<AvailabilitySlot>().Property(a => a.DayOfWeek).IsRequired();
        builder.Entity<AvailabilitySlot>().Property(a => a.StartTime).IsRequired().HasMaxLength(5);
        builder.Entity<AvailabilitySlot>().Property(a => a.EndTime).IsRequired().HasMaxLength(5);
        builder.Entity<AvailabilitySlot>().Property(a => a.IsAvailable).HasDefaultValue(true);
        builder.Entity<AvailabilitySlot>().HasIndex(a => new { a.WorkerUserId, a.DayOfWeek });

        // Booking
        builder.Entity<BookingRequest>().HasKey(b => b.Id);
        builder.Entity<BookingRequest>().Property(b => b.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<BookingRequest>().Property(b => b.ClientId).IsRequired();
        builder.Entity<BookingRequest>().Property(b => b.WorkerId).IsRequired();
        builder.Entity<BookingRequest>().Property(b => b.ServiceType).IsRequired().HasMaxLength(40);
        builder.Entity<BookingRequest>().Property(b => b.Date).IsRequired();
        builder.Entity<BookingRequest>().Property(b => b.StartTime).IsRequired().HasMaxLength(5);
        builder.Entity<BookingRequest>().Property(b => b.EndTime).IsRequired().HasMaxLength(5);
        builder.Entity<BookingRequest>().Property(b => b.Hours).HasPrecision(5, 2);
        builder.Entity<BookingRequest>().Property(b => b.PaymentMethodId);
        builder.Entity<BookingRequest>().Property(b => b.Address).HasMaxLength(300);
        builder.Entity<BookingRequest>().Property(b => b.Notes).HasMaxLength(1000);
        builder.Entity<BookingRequest>().Property(b => b.HourlyRate).HasPrecision(10, 2);
        builder.Entity<BookingRequest>().Property(b => b.TotalAmount).HasPrecision(10, 2);
        builder.Entity<BookingRequest>().Property(b => b.PlatformFee).HasPrecision(10, 2);
        builder.Entity<BookingRequest>().Property(b => b.WorkerEarning).HasPrecision(10, 2);
        builder.Entity<BookingRequest>().Property(b => b.Status).IsRequired().HasMaxLength(30);
        builder.Entity<BookingRequest>().HasIndex(b => b.ClientId);
        builder.Entity<BookingRequest>().HasIndex(b => b.WorkerId);

        // Payments
        builder.Entity<PaymentMethod>().HasKey(p => p.Id);
        builder.Entity<PaymentMethod>().Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<PaymentMethod>().Property(p => p.UserId).IsRequired();
        builder.Entity<PaymentMethod>().Property(p => p.Type).IsRequired().HasMaxLength(30);
        builder.Entity<PaymentMethod>().Property(p => p.Label).IsRequired().HasMaxLength(80);
        builder.Entity<PaymentMethod>().Property(p => p.Details).HasMaxLength(200);
        builder.Entity<PaymentMethod>().Property(p => p.IsDefault).HasDefaultValue(false);
        builder.Entity<PaymentMethod>().HasIndex(p => p.UserId);

        // ServicePayment — pagos efectivos de servicios completados.
        builder.Entity<ServicePayment>().HasKey(s => s.Id);
        builder.Entity<ServicePayment>().Property(s => s.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<ServicePayment>().Property(s => s.BookingId).IsRequired();
        builder.Entity<ServicePayment>().Property(s => s.ClientId).IsRequired();
        builder.Entity<ServicePayment>().Property(s => s.WorkerId).IsRequired();
        builder.Entity<ServicePayment>().Property(s => s.Amount).HasColumnType("decimal(10,2)");
        builder.Entity<ServicePayment>().Property(s => s.PlatformFee).HasColumnType("decimal(10,2)");
        builder.Entity<ServicePayment>().Property(s => s.WorkerEarning).HasColumnType("decimal(10,2)");
        builder.Entity<ServicePayment>().Property(s => s.Channel).IsRequired().HasMaxLength(30);
        builder.Entity<ServicePayment>().Property(s => s.PayoutStatus).IsRequired().HasMaxLength(20);
        builder.Entity<ServicePayment>().Property(s => s.IzipayOrderId).HasMaxLength(100);
        builder.Entity<ServicePayment>().Property(s => s.IzipayTransactionId).HasMaxLength(100);
        builder.Entity<ServicePayment>().Property(s => s.PayPalOrderId).HasMaxLength(100);
        builder.Entity<ServicePayment>().Property(s => s.PayPalCaptureId).HasMaxLength(100);
        // Un booking solo puede pagarse una vez.
        builder.Entity<ServicePayment>().HasIndex(s => s.BookingId).IsUnique();
        builder.Entity<ServicePayment>().HasIndex(s => s.WorkerId);
        builder.Entity<ServicePayment>().HasIndex(s => s.PayoutStatus);
        
        // ReviewsAndEvaluation
        builder.Entity<Review>().HasKey(r => r.Id);
        builder.Entity<Review>().Property(r => r.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<Review>().Property(r => r.BookingId).IsRequired();
        builder.Entity<Review>().Property(r => r.ClientId).IsRequired();
        builder.Entity<Review>().Property(r => r.WorkerId).IsRequired();
        builder.Entity<Review>().Property(r => r.Rating).IsRequired();
        builder.Entity<Review>().Property(r => r.Comment).HasMaxLength(1000);
        builder.Entity<Review>().HasIndex(r => r.BookingId).IsUnique();
        builder.Entity<Review>().HasIndex(r => r.WorkerId);

        // Messaging
        builder.Entity<Message>().HasKey(m => m.Id);
        builder.Entity<Message>().Property(m => m.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<Message>().Property(m => m.SenderId).IsRequired();
        builder.Entity<Message>().Property(m => m.RecipientId).IsRequired();
        builder.Entity<Message>().Property(m => m.Content).IsRequired().HasMaxLength(4000);
        builder.Entity<Message>().Property(m => m.ReadAt);
        builder.Entity<Message>().HasIndex(m => new { m.SenderId, m.RecipientId });
        builder.Entity<Message>().HasIndex(m => m.RecipientId);
        
        // Notifications
        builder.Entity<Notification>().HasKey(n => n.Id);
        builder.Entity<Notification>().Property(n => n.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<Notification>().Property(n => n.UserId).IsRequired();
        builder.Entity<Notification>().Property(n => n.Type).IsRequired().HasMaxLength(30);
        builder.Entity<Notification>().Property(n => n.Title).IsRequired().HasMaxLength(150);
        builder.Entity<Notification>().Property(n => n.Body).HasMaxLength(1000);
        builder.Entity<Notification>().Property(n => n.Link).HasMaxLength(200);
        builder.Entity<Notification>().Property(n => n.Read).HasDefaultValue(false);
        builder.Entity<Notification>().HasIndex(n => n.UserId);

        // Reports
        builder.Entity<Report>().HasKey(r => r.Id);
        builder.Entity<Report>().Property(r => r.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<Report>().Property(r => r.ReporterUserId).IsRequired();
        builder.Entity<Report>().Property(r => r.ReportedUserId).IsRequired();
        builder.Entity<Report>().Property(r => r.ReportedRole).IsRequired().HasMaxLength(20);
        builder.Entity<Report>().Property(r => r.Reason).IsRequired().HasMaxLength(60);
        builder.Entity<Report>().Property(r => r.Details).HasMaxLength(2000);
        builder.Entity<Report>().Property(r => r.Status).IsRequired().HasMaxLength(20);
        builder.Entity<Report>().Property(r => r.ConfirmedByAdminUserId);
        builder.Entity<Report>().Property(r => r.ConfirmedAt);
        builder.Entity<Report>().Property(r => r.AdminNotes).HasMaxLength(1000);
        builder.Entity<Report>().HasIndex(r => r.ReportedUserId);
        builder.Entity<Report>().HasIndex(r => new { r.ReportedUserId, r.Status });

        // Apply snake_case for the entire model
        builder.UseSnakeCaseNamingConvention();
    }
}
