using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseAPI.Core.Entities;
using PulseAPI.Core.Interfaces;
using PulseAPI.Infrastructure.Data;
using System.Security.Claims;
using ApiEntity = PulseAPI.Core.Entities.Api;

namespace PulseAPI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApisController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApisController> _logger;

    public ApisController(ApplicationDbContext context, ILogger<ApisController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiEntity>>> GetApis([FromQuery] string? environment)
    {
        var query = _context.Apis.AsQueryable();

        if (!string.IsNullOrEmpty(environment))
        {
            query = query.Where(a => a.Environment == environment);
        }

        var apis = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
        return Ok(apis);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiEntity>> GetApi(int id)
    {
        var api = await _context.Apis
            .Include(a => a.CreatedByUser)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (api == null)
        {
            return NotFound();
        }

        return Ok(api);
    }

    [HttpPost]
    public async Task<ActionResult<ApiEntity>> CreateApi([FromBody] ApiEntity api)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        api.CreatedByUserId = userId;
        api.CreatedAt = DateTime.UtcNow;
        api.UpdatedAt = DateTime.UtcNow;

        _context.Apis.Add(api);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetApi), new { id = api.Id }, api);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateApi(int id, [FromBody] ApiEntity api)
    {
        if (id != api.Id)
        {
            return BadRequest();
        }

        var existingApi = await _context.Apis.FindAsync(id);
        if (existingApi == null)
        {
            return NotFound();
        }

        existingApi.Name = api.Name;
        existingApi.Url = api.Url;
        existingApi.Method = api.Method;
        existingApi.Headers = api.Headers;
        existingApi.Body = api.Body;
        existingApi.CheckIntervalSeconds = api.CheckIntervalSeconds;
        existingApi.TimeoutSeconds = api.TimeoutSeconds;
        existingApi.IsActive = api.IsActive;
        existingApi.Environment = api.Environment;
        existingApi.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApi(int id)
    {
        var api = await _context.Apis.FindAsync(id);
        if (api == null)
        {
            return NotFound();
        }

        _context.Apis.Remove(api);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

