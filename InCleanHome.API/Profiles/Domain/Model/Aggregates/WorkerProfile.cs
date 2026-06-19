using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.CreatedUpdatedDate.Contracts;

namespace InCleanHome.API.Profiles.Domain.Model.Aggregates;

/// <summary>
///     Worker profile aggregate root — domestic worker offering services.
/// </summary>
/// <remarks>
///     <c>ServiceTypes</c> and <c>Zones</c> are stored as PostgreSQL <c>text[]</c> columns
///     (Npgsql maps <c>List&lt;string&gt;</c> to it natively). Aggregate stats
///     (<c>AverageRating</c>, <c>TotalServices</c>) are denormalized for fast search/listing
///     and are updated by ReviewsAndEvaluation / Booking via domain events or direct calls.
/// </remarks>
public class WorkerProfile : IEntityWithCreatedUpdatedDate
{
    public int Id { get; private set; }
    public int UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public int Age { get; private set; }
    public string Gender { get; private set; } = string.Empty;

    public List<string> ServiceTypes { get; private set; } = new();
    public List<string> Zones { get; private set; } = new();

    public decimal HourlyRate { get; private set; }
    /// <summary>
    ///     Tarifa por hora aplicable cuando el servicio se ejecuta un domingo.
    ///     Cada trabajadora la define obligatoriamente al registrarse y puede
    ///     diferir libremente de la tarifa normal (mayor, igual o incluso menor).
    ///     Si en algún booking la fecha cae domingo, el cálculo de
    ///     <c>TotalAmount</c> usa este valor en lugar de <c>HourlyRate</c>.
    /// </summary>
    public decimal HourlyRateSunday { get; private set; }
    public int ExperienceYears { get; private set; }
    public string Bio { get; private set; } = string.Empty;

    public decimal AverageRating { get; private set; }
    public int TotalServices { get; private set; }

    // Profile photo stored as a data URL / base64 string (no external storage needed).
    public string? PhotoUrl { get; private set; }

    [Column("CreatedAt")] public DateTimeOffset? CreatedDate { get; set; }
    [Column("UpdatedAt")] public DateTimeOffset? UpdatedDate { get; set; }

    public WorkerProfile() { }

    public WorkerProfile(int userId, string name, string phone, int age, string gender,
        List<string> serviceTypes, List<string> zones,
        decimal hourlyRate, decimal hourlyRateSunday,
        int experienceYears, string bio)
    {
        UserId           = userId;
        Name             = name;
        Phone            = phone ?? string.Empty;
        Age              = age;
        Gender           = gender;
        ServiceTypes     = serviceTypes ?? new();
        Zones            = zones ?? new();
        HourlyRate       = hourlyRate;
        // Si por alguna razón no se pasa tarifa de domingo válida, defaultea
        // a la tarifa normal — defensa, no debería pasar porque el formulario
        // del frontend la exige.
        HourlyRateSunday = hourlyRateSunday > 0 ? hourlyRateSunday : hourlyRate;
        ExperienceYears  = experienceYears;
        Bio              = bio ?? string.Empty;
        AverageRating    = 0m;
        TotalServices    = 0;
    }

    public WorkerProfile Update(string name, string phone, int age,
        List<string> serviceTypes, List<string> zones,
        decimal hourlyRate, decimal hourlyRateSunday,
        int experienceYears, string bio)
    {
        Name             = name;
        Phone            = phone ?? string.Empty;
        Age              = age;
        ServiceTypes     = serviceTypes ?? new();
        Zones            = zones ?? new();
        HourlyRate       = hourlyRate;
        HourlyRateSunday = hourlyRateSunday > 0 ? hourlyRateSunday : hourlyRate;
        ExperienceYears  = experienceYears;
        Bio              = bio ?? string.Empty;
        return this;
    }

    /// <summary>Recomputes the running average rating after a new review.</summary>
    public WorkerProfile RegisterCompletedService(int newRating)
    {
        var totalRatings = AverageRating * TotalServices;
        TotalServices  += 1;
        AverageRating   = Math.Round((totalRatings + newRating) / TotalServices, 2);
        return this;
    }

    public WorkerProfile SetPhoto(string? photoUrl)
    {
        PhotoUrl = photoUrl;
        return this;
    }
}
