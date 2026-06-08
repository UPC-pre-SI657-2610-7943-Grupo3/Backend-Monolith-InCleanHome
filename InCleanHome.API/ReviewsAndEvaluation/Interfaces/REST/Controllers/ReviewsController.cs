using System.Net.Mime;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.ValueObjects;
using InCleanHome.API.Profiles.Interfaces.ACL;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Commands;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Queries;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Services;
using InCleanHome.API.ReviewsAndEvaluation.Interfaces.REST.Resources;
using InCleanHome.API.ReviewsAndEvaluation.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.API.ReviewsAndEvaluation.Interfaces.REST.Controllers;

/// <summary>
///     Reviews endpoints.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>POST /reviews</description></item>
///         <item><description>GET /reviews/worker/{id}</description></item>
///     </list>
/// </remarks>
[ApiController]
[Route("api/reviews")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Reviews & evaluations")]
public class ReviewsController(
    IReviewCommandService commandService,
    IReviewQueryService queryService,
    IProfilesContextFacade profilesFacade) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation("Create Review", "A client posts a review on a completed booking.")]
    public async Task<IActionResult> Create([FromBody] CreateReviewResource resource)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Client) return Forbid();

        try
        {
            var review = await commandService.Handle(new CreateReviewCommand(
                resource.BookingId, current.Id, resource.WorkerId, resource.Rating, resource.Comment));

            var clientName = await profilesFacade.FetchUserNameByUserId(review.ClientId);
            return Ok(ReviewResourceFromEntityAssembler.ToResourceFromEntity(review, clientName));
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    [HttpGet("worker/{id:int}")]
    [SwaggerOperation("List Worker Reviews", "Returns all reviews for a given worker.")]
    public async Task<IActionResult> GetByWorker(int id)
    {
        var reviews = await queryService.Handle(new GetReviewsByWorkerIdQuery(id));

        var result = new List<ReviewResource>();
        foreach (var r in reviews)
        {
            var clientName = await profilesFacade.FetchUserNameByUserId(r.ClientId);
            result.Add(ReviewResourceFromEntityAssembler.ToResourceFromEntity(r, clientName));
        }
        return Ok(result);
    }
}
