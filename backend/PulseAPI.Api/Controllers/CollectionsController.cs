using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseAPI.Core.Entities;
using PulseAPI.Infrastructure.Data;
using System.Security.Claims;

namespace PulseAPI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CollectionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CollectionsController> _logger;

    public CollectionsController(ApplicationDbContext context, ILogger<CollectionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Collection>>> GetCollections()
    {
        var collections = await _context.Collections
            .Include(c => c.CreatedByUser)
            .Include(c => c.ApiCollections)
            .ThenInclude(ac => ac.Api)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return Ok(collections);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Collection>> GetCollection(int id)
    {
        var collection = await _context.Collections
            .Include(c => c.CreatedByUser)
            .Include(c => c.ApiCollections)
            .ThenInclude(ac => ac.Api)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (collection == null)
        {
            return NotFound();
        }

        return Ok(collection);
    }

    [HttpPost]
    public async Task<ActionResult<Collection>> CreateCollection([FromBody] Collection collection)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        collection.CreatedByUserId = userId;
        collection.CreatedAt = DateTime.UtcNow;
        collection.UpdatedAt = DateTime.UtcNow;

        _context.Collections.Add(collection);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCollection), new { id = collection.Id }, collection);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCollection(int id, [FromBody] Collection collection)
    {
        if (id != collection.Id)
        {
            return BadRequest();
        }

        var existingCollection = await _context.Collections.FindAsync(id);
        if (existingCollection == null)
        {
            return NotFound();
        }

        existingCollection.Name = collection.Name;
        existingCollection.Description = collection.Description;
        existingCollection.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCollection(int id)
    {
        var collection = await _context.Collections.FindAsync(id);
        if (collection == null)
        {
            return NotFound();
        }

        _context.Collections.Remove(collection);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{collectionId}/apis/{apiId}")]
    public async Task<IActionResult> AddApiToCollection(int collectionId, int apiId)
    {
        var collection = await _context.Collections.FindAsync(collectionId);
        var api = await _context.Apis.FindAsync(apiId);

        if (collection == null || api == null)
        {
            return NotFound();
        }

        // Check if already exists
        var exists = await _context.ApiCollections
            .AnyAsync(ac => ac.CollectionId == collectionId && ac.ApiId == apiId);

        if (exists)
        {
            return BadRequest(new { message = "API is already in this collection" });
        }

        var apiCollection = new ApiCollection
        {
            ApiId = apiId,
            CollectionId = collectionId,
            AddedAt = DateTime.UtcNow
        };

        _context.ApiCollections.Add(apiCollection);
        await _context.SaveChangesAsync();

        return Ok(apiCollection);
    }

    [HttpDelete("{collectionId}/apis/{apiId}")]
    public async Task<IActionResult> RemoveApiFromCollection(int collectionId, int apiId)
    {
        var apiCollection = await _context.ApiCollections
            .FirstOrDefaultAsync(ac => ac.CollectionId == collectionId && ac.ApiId == apiId);

        if (apiCollection == null)
        {
            return NotFound();
        }

        _context.ApiCollections.Remove(apiCollection);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}



