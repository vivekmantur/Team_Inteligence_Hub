using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Services;

namespace TeamIntelligenceHub.API.Controllers;

/// <summary>
/// Contributions captured against an Initiative.
/// </summary>
/// <remarks>
/// Listing and creating hang off the Initiative, because a contribution has no meaning
/// without one. Reading, editing, and deleting a single contribution use its own id, so
/// the client does not have to carry the Initiative around with it.
///
/// Create and update both take the whole graph in one call, matching a wizard that
/// submits all eight of its steps at once. Attachments are the exception: a file needs a
/// ContributionId to hang off, so it goes through ContributionAttachmentsController
/// after this returns.
/// </remarks>
[ApiController]
[Authorize]
[Route("api")]
public class ContributionsController : ControllerBase
{
    private readonly IContributionService _contributionService;
    private readonly ILogger<ContributionsController> _logger;

    public ContributionsController(
        IContributionService contributionService,
        ILogger<ContributionsController> logger)
    {
        _contributionService = contributionService;
        _logger = logger;
    }

    /// <summary>Returns an Initiative's contributions, newest first.</summary>
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

    /// <summary>Replaces the whole contribution. Attachments are left alone.</summary>
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
    /// Every submitted Customer Story, across all Initiatives, newest first.
    /// </summary>
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
    /// Every submitted Testimonial, across all Initiatives, newest first.
    /// </summary>
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
    /// Every customer story extracted from a Contribution attachment's document content,
    /// newest first.
    /// </summary>
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
    /// Every testimonial extracted from a Contribution attachment's document content,
    /// newest first.
    /// </summary>
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
    /// Tags already in use, for the client's typeahead.
    /// </summary>
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
