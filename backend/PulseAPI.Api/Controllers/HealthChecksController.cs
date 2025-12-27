using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseAPI.Core.Entities;
using PulseAPI.Infrastructure.Data;

namespace PulseAPI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HealthChecksController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HealthChecksController> _logger;

    public HealthChecksController(ApplicationDbContext context, ILogger<HealthChecksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HealthCheck>>> GetHealthChecks(
        [FromQuery] int? apiId,
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        var query = _context.HealthChecks
            .Include(h => h.Api)
            .AsQueryable();

        if (apiId.HasValue)
        {
            query = query.Where(h => h.ApiId == apiId.Value);
        }

        var end = endTime ?? DateTime.UtcNow;
        var start = startTime ?? end.AddHours(-1);

        query = query.Where(h => h.CheckedAt >= start && h.CheckedAt <= end);

        var totalCount = await query.CountAsync();
        var healthChecks = await query
            .OrderByDescending(h => h.CheckedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            Data = healthChecks,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<HealthCheck>> GetHealthCheck(int id)
    {
        var healthCheck = await _context.HealthChecks
            .Include(h => h.Api)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (healthCheck == null)
        {
            return NotFound();
        }

        return Ok(healthCheck);
    }
}



