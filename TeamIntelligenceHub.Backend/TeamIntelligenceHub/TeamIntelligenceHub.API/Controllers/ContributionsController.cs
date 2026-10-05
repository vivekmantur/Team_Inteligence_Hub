// 1. Get all contributions for an initiative
// 2. Create a contribution on an initiative
// 3. Get one contribution by ID
// 4. Update a contribution
// 5. Delete a contribution
// 6. Get all submitted customer stories
// 7. Get all submitted testimonials
// 8. Get customer stories extracted from documents
// 9. Get testimonials extracted from documents
// 10. Get the tags already in use

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// Handles the contributions captured against an Initiative. Create and update take the
/// whole wizard graph in one call; attachments follow through ContributionAttachmentsController.
/// </summary>
[ApiController]
[Authorize]
[Route("api")]
public class ContributionsController : ControllerBase
{
    private readonly IContributionService _contributionService;
    private readonly ILogger<ContributionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContributionsController"/> class.
    /// </summary>
    /// <param name="contributionService">The service that reads and writes contributions.</param>
    /// <param name="logger">The logger used to record rejected contributions.</param>
    public ContributionsController(
        IContributionService contributionService,
        ILogger<ContributionsController> logger)
    {
        _contributionService = contributionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets an Initiative's contributions, newest first.
    /// </summary>
    /// <param name="initiativeId">The Initiative whose contributions are returned.</param>
    /// <returns>200 OK with the contributions, or 404 Not Found when the Initiative does not exist.</returns>
    [HttpGet("initiatives/{initiativeId:int}/contributions")]
    public async Task<IActionResult> GetByInitiative(int initiativeId)
    {
        try
        {
            var contributions =
                await _contributionService.GetByInitiativeAsync(initiativeId);

            return Ok(contributions);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Creates a contribution on the Initiative.
    /// </summary>
    /// <param name="initiativeId">The Initiative the contribution belongs to.</param>
    /// <param name="request">The full contribution submitted by the wizard.</param>
    /// <returns>201 Created with the new contribution, 404 Not Found when the Initiative does not exist, 400 Bad Request when validation fails, or 401 Unauthorized when the caller cannot be identified.</returns>
    [HttpPost("initiatives/{initiativeId:int}/contributions")]
    public async Task<IActionResult> Create(
        int initiativeId,
        [FromBody] CreateContributionRequestDto request)
    {
        try
        {
            var created = await _contributionService.CreateAsync(initiativeId, request);

            return CreatedAtAction(nameof(GetById), new { contributionId = created.Id }, created);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogInformation("Contribution rejected: {Reason}", ex.Message);

            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets the contribution with the given ID.
    /// </summary>
    /// <param name="contributionId">The contribution identifier.</param>
    /// <returns>200 OK with the contribution, or 404 Not Found when it does not exist.</returns>
    [HttpGet("contributions/{contributionId:int}")]
    public async Task<IActionResult> GetById(int contributionId)
    {
        try
        {
            return Ok(await _contributionService.GetByIdAsync(contributionId));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Replaces the whole contribution. Attachments are left alone.
    /// </summary>
    /// <param name="contributionId">The contribution identifier.</param>
    /// <param name="request">The full updated contribution.</param>
    /// <returns>200 OK with the updated contribution, 404 Not Found when it does not exist, 400 Bad Request when validation fails, or 401 Unauthorized when the caller cannot be identified.</returns>
    [HttpPut("contributions/{contributionId:int}")]
    public async Task<IActionResult> Update(
        int contributionId,
        [FromBody] UpdateContributionRequestDto request)
    {
        try
        {
            var updated =
                await _contributionService.UpdateAsync(contributionId, request);

            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes the contribution, its child rows, and the files behind its attachments.
    /// </summary>
    /// <param name="contributionId">The contribution identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>204 No Content when the contribution is deleted, 404 Not Found when it does not exist, 400 Bad Request when validation fails, or 401 Unauthorized when the caller cannot be identified.</returns>
    [HttpDelete("contributions/{contributionId:int}")]
    public async Task<IActionResult> Remove(
        int contributionId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _contributionService.RemoveAsync(contributionId, cancellationToken);

            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets every submitted Customer Story across all Initiatives, newest first.
    /// </summary>
    /// <returns>200 OK with the customer stories.</returns>
    /// <remarks>
    /// Feeds the Stories &amp; Evidence page's Customer Zero grid, which is company-wide
    /// rather than scoped to one Initiative like the other reads on this controller.
    /// </remarks>
    [HttpGet("contributions/customer-stories")]
    public async Task<IActionResult> GetCustomerStories()
    {
        return Ok(await _contributionService.GetCustomerStoriesAsync());
    }

    /// <summary>
    /// Gets every submitted Testimonial across all Initiatives, newest first.
    /// </summary>
    /// <returns>200 OK with the testimonials.</returns>
    /// <remarks>
    /// Feeds the Stories &amp; Evidence page's Testimonial grid, company-wide like
    /// GetCustomerStories.
    /// </remarks>
    [HttpGet("contributions/testimonials")]
    public async Task<IActionResult> GetTestimonials()
    {
        return Ok(await _contributionService.GetTestimonialsAsync());
    }

    /// <summary>
    /// Gets every customer story extracted from a Contribution attachment's document content, newest first.
    /// </summary>
    /// <returns>200 OK with the extracted customer stories.</returns>
    /// <remarks>
    /// Feeds the Stories &amp; Evidence page's "Extracted from documents" section, which
    /// sits below the hand-written Customer Zero and Testimonial grids rather than mixed
    /// into them.
    /// </remarks>
    [HttpGet("contributions/document-customer-stories")]
    public async Task<IActionResult> GetDocumentCustomerStories()
    {
        return Ok(await _contributionService.GetDocumentCustomerStoriesAsync());
    }

    /// <summary>
    /// Gets every testimonial extracted from a Contribution attachment's document content, newest first.
    /// </summary>
    /// <returns>200 OK with the extracted testimonials.</returns>
    /// <remarks>
    /// Feeds the Stories &amp; Evidence page's "Extracted from documents" section, same as
    /// GetDocumentCustomerStories.
    /// </remarks>
    [HttpGet("contributions/document-testimonials")]
    public async Task<IActionResult> GetDocumentTestimonials()
    {
        return Ok(await _contributionService.GetDocumentTestimonialsAsync());
    }

    /// <summary>
    /// Gets the tags already in use, for the client's typeahead.
    /// </summary>
    /// <param name="q">Optional text the returned tags must match.</param>
    /// <param name="take">Optional maximum number of tags to return.</param>
    /// <returns>200 OK with the matching tags.</returns>
    /// <remarks>
    /// Offering what exists is what stops "Copilot", "copilot", and "co-pilot" becoming
    /// three tags for one idea. Tags live in a JSON column with no unique index, so this
    /// is the only thing keeping the vocabulary coherent.
    /// </remarks>
    [HttpGet("contributions/tags")]
    public async Task<IActionResult> GetTagVocabulary(
        [FromQuery] string? q,
        [FromQuery] int? take)
    {
        return Ok(await _contributionService.GetTagVocabularyAsync(q, take));
    }
}
