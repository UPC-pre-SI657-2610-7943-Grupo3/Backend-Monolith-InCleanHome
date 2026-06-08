using System.Net.Mime;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.Queries;
using InCleanHome.API.IAM.Domain.Model.ValueObjects;
using InCleanHome.API.IAM.Domain.Services;
using InCleanHome.API.Profiles.Domain.Model.Commands;
using InCleanHome.API.Profiles.Domain.Model.Queries;
using InCleanHome.API.Profiles.Domain.Services;
using InCleanHome.API.Profiles.Interfaces.REST.Resources;
using InCleanHome.API.Profiles.Interfaces.REST.Transform;
using InCleanHome.API.SearchAndCatalog.Domain.Model.Commands;
using InCleanHome.API.SearchAndCatalog.Domain.Model.Queries;
using InCleanHome.API.SearchAndCatalog.Domain.Services;
using InCleanHome.API.SearchAndCatalog.Interfaces.REST.Resources;
using InCleanHome.API.SearchAndCatalog.Interfaces.REST.Transform;
using InCleanHome.API.Booking.Domain.Model.Queries;
using InCleanHome.API.Booking.Domain.Model.ValueObjects;
using InCleanHome.API.Booking.Domain.Services;
using InCleanHome.API.Reports.Domain.Model.Queries;
using InCleanHome.API.Reports.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.API.SearchAndCatalog.Interfaces.REST.Controllers;

/// <summary>
///     Worker discovery, profile read/edit, availability and dashboard stats.
/// </summary>
/// <remarks>
///     Endpoints (matching <c>src/Shared/api.js</c> calls):
///     <list type="bullet">
///         <item><description>GET /workers — search with filters</description></item>
///         <item><description>GET /workers/{id} — one worker</description></item>
///         <item><description>GET /workers/{id}/availability</description></item>
///         <item><description>PUT /workers/{id}/availability</description></item>
///         <item><description>GET /workers/me/profile</description></item>
///         <item><description>PUT /workers/me/profile</description></item>
///         <item><description>GET /workers/me/stats</description></item>
///     </list>
/// </remarks>
[ApiController]
[Route("api/workers")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Worker discovery, profile and dashboard")]
public class WorkersController(
    IWorkerProfileQueryService workerQueryService,
    IWorkerProfileCommandService workerCommandService,
    IUserQueryService userQueryService,
    IAvailabilitySlotQueryService availabilityQueryService,
    IAvailabilitySlotCommandService availabilityCommandService,
    IBookingRequestQueryService bookingQueryService,
    IReportQueryService reportQueryService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation("Search Workers", "Search workers with optional filters.")]
    public async Task<IActionResult> Search(
        [FromQuery] string? serviceType,
        [FromQuery] string? zone,
        [FromQuery] string? gender,
        [FromQuery] int? minAge,
        [FromQuery] int? maxAge,
        [FromQuery] decimal? maxHourlyRate,
        [FromQuery] decimal? minRating)
    {
        var workers = await workerQueryService.Handle(new SearchWorkersQuery(
            serviceType, zone, gender, minAge, maxAge, maxHourlyRate, minRating));

        var resources = new List<WorkerResource>();
        foreach (var w in workers)
        {
            var user = await userQueryService.Handle(new GetUserByIdQuery(w.UserId));
            // Exclude workers who haven't been approved by admin yet.
            if (user is null || !user.DocumentsVerified) continue;
            // Workers currently under suspension are also excluded from public search.
            if (user.IsCurrentlySuspended()) continue;
            var documentsVerified = user?.DocumentsVerified ?? false;
            var confirmedReportsCount = await reportQueryService.Handle(new CountConfirmedReportsByReportedUserIdQuery(w.UserId));
            resources.Add(WorkerResourceFromEntityAssembler.ToResourceFromEntity(w, documentsVerified, user?.SuspendedUntil, confirmedReportsCount));
        }
        return Ok(resources);
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation("Get Worker", "Get a worker by user id (the id used by the frontend).")]
    public async Task<IActionResult> GetById(int id)
    {
        var worker = await workerQueryService.Handle(new GetWorkerProfileByUserIdQuery(id));
        if (worker is null) return NotFound(new { error = "Worker not found" });

        var user = await userQueryService.Handle(new GetUserByIdQuery(id));
        // We DO return suspended workers when fetched directly (the profile view
        // shows the suspension notice instead of letting them be contacted).
        var confirmedReportsCount = await reportQueryService.Handle(new CountConfirmedReportsByReportedUserIdQuery(id));
        return Ok(WorkerResourceFromEntityAssembler.ToResourceFromEntity(
            worker, user?.DocumentsVerified ?? false, user?.SuspendedUntil, confirmedReportsCount));
    }

    // Availability 
    [HttpGet("{id:int}/availability")]
    [SwaggerOperation("Get Worker Availability", "List the weekly availability slots of a worker.")]
    public async Task<IActionResult> GetAvailability(int id)
    {
        var slots = await availabilityQueryService.Handle(new GetAvailabilityByWorkerUserIdQuery(id));
        return Ok(slots.Select(AvailabilitySlotResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpPut("{id:int}/availability")]
    [SwaggerOperation("Replace Worker Availability", "Replace the weekly availability slots of the authenticated worker.")]
    public async Task<IActionResult> ReplaceAvailability(int id, [FromBody] ReplaceAvailabilityResource resource)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null || (current.Role != UserRole.Worker && current.Role != UserRole.Admin))
            return Forbid();
        if (current.Role == UserRole.Worker && current.Id != id)
            return Forbid();

        var slots = resource.Slots
            .Select(s => new SlotInput(s.DayOfWeek, s.StartTime, s.EndTime, s.IsAvailable))
            .ToList();

        var saved = await availabilityCommandService.Handle(new ReplaceWorkerAvailabilityCommand(id, slots));
        return Ok(saved.Select(AvailabilitySlotResourceFromEntityAssembler.ToResourceFromEntity));
    }

    // me endpoints - for the authenticated worker to manage their own profile and availability without needing to know their user id

    [HttpGet("me/availability")]
    [SwaggerOperation("Get My Availability", "Returns the authenticated worker's availability slots.")]
    public async Task<IActionResult> GetMyAvailability()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null || current.Role != UserRole.Worker) return Forbid();
        var slots = await availabilityQueryService.Handle(new GetAvailabilityByWorkerUserIdQuery(current.Id));
        return Ok(slots.Select(AvailabilitySlotResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("me/profile")]
    [SwaggerOperation("Get My Worker Profile")]
    public async Task<IActionResult> GetMyProfile()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Worker) return Forbid();

        var w = await workerQueryService.Handle(new GetWorkerProfileByUserIdQuery(current.Id));
        if (w is null) return NotFound();
        return Ok(WorkerResourceFromEntityAssembler.ToResourceFromEntity(w, current.DocumentsVerified, current.SuspendedUntil));
    }

    [HttpPut("me/profile")]
    [SwaggerOperation("Update My Worker Profile")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateWorkerProfileResource resource)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Worker) return Forbid();

        var updated = await workerCommandService.Handle(new UpdateWorkerProfileCommand(
            current.Id,
            resource.Name,
            resource.Phone ?? string.Empty,
            resource.Age,
            resource.ServiceTypes ?? new(),
            resource.Zones ?? new(),
            resource.HourlyRate,
            resource.ExperienceYears,
            resource.Bio ?? string.Empty));

        if (updated is null) return NotFound();
        return Ok(WorkerResourceFromEntityAssembler.ToResourceFromEntity(updated, current.DocumentsVerified, current.SuspendedUntil));
    }

    [HttpPost("me/photo")]
    [SwaggerOperation("Update My Worker Photo", "Sets the authenticated worker's profile photo (base64 data URL).")]
    public async Task<IActionResult> UpdateMyPhoto([FromBody] UpdatePhotoResource resource)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Worker) return Forbid();

        var updated = await workerCommandService.Handle(new UpdateWorkerPhotoCommand(current.Id, resource.PhotoUrl));
        if (updated is null) return NotFound();
        return Ok(WorkerResourceFromEntityAssembler.ToResourceFromEntity(updated, current.DocumentsVerified, current.SuspendedUntil));
    }
    
    [HttpGet("me/stats")]
    [SwaggerOperation("Get My Worker Stats", "Aggregated dashboard stats for the authenticated worker.")]
    public async Task<IActionResult> GetMyStats()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Worker) return Forbid();

        var bookings = (await bookingQueryService.Handle(new GetBookingsByWorkerUserIdQuery(current.Id))).ToList();
        var completed = bookings.Where(b => b.Status == BookingStatus.Completed).ToList();

        var totalEarnings = completed.Sum(b => b.TotalAmount);
        var platformFee   = Math.Round(totalEarnings * 0.10m, 2);
        var netEarnings   = totalEarnings - platformFee;

        var profile = await workerQueryService.Handle(new GetWorkerProfileByUserIdQuery(current.Id));

        var monthly = completed
            .GroupBy(b => $"{b.Date.Year:D4}-{b.Date.Month:D2}")
            .OrderBy(g => g.Key)
            .Select(g => new MonthlyEarning(g.Key, Math.Round(g.Sum(b => b.WorkerEarning), 2)))
            .ToList();

        var stats = new WorkerStatsResource(
            netEarnings,
            platformFee,
            completed.Count,
            profile?.AverageRating ?? 0m,
            monthly);
        return Ok(stats);
    }
}
