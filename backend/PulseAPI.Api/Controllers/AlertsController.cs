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
public class AlertsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(ApplicationDbContext context, ILogger<AlertsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Alert>>> GetAlerts([FromQuery] bool? active)
    {
        var query = _context.Alerts
            .Include(a => a.CreatedByUser)
            .Include(a => a.Api)
            .Include(a => a.Collection)
            .AsQueryable();

        if (active.HasValue)
        {
            query = query.Where(a => a.IsActive == active.Value);
        }

        var alerts = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
        return Ok(alerts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Alert>> GetAlert(int id)
    {
        var alert = await _context.Alerts
            .Include(a => a.CreatedByUser)
            .Include(a => a.Api)
            .Include(a => a.Collection)
            .Include(a => a.AlertHistories)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (alert == null)
        {
            return NotFound();
        }

        return Ok(alert);
    }

    [HttpPost]
    public async Task<ActionResult<Alert>> CreateAlert([FromBody] Alert alert)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        alert.CreatedByUserId = userId;
        alert.CreatedAt = DateTime.UtcNow;
        alert.UpdatedAt = DateTime.UtcNow;

        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAlert), new { id = alert.Id }, alert);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAlert(int id, [FromBody] Alert alert)
    {
        if (id != alert.Id)
        {
            return BadRequest();
        }

        var existingAlert = await _context.Alerts.FindAsync(id);
        if (existingAlert == null)
        {
            return NotFound();
        }

        existingAlert.Name = alert.Name;
        existingAlert.Description = alert.Description;
        existingAlert.Type = alert.Type;
        existingAlert.Condition = alert.Condition;
        existingAlert.Threshold = alert.Threshold;
        existingAlert.ApiId = alert.ApiId;
        existingAlert.CollectionId = alert.CollectionId;
        existingAlert.IsActive = alert.IsActive;
        existingAlert.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAlert(int id)
    {
        var alert = await _context.Alerts.FindAsync(id);
        if (alert == null)
        {
            return NotFound();
        }

        _context.Alerts.Remove(alert);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<AlertHistory>>> GetAlertHistory(
        [FromQuery] bool? resolved,
        [FromQuery] int? alertId)
    {
        var query = _context.AlertHistories
            .Include(h => h.Alert)
            .AsQueryable();

        if (resolved.HasValue)
        {
            query = query.Where(h => h.IsResolved == resolved.Value);
        }

        if (alertId.HasValue)
        {
            query = query.Where(h => h.AlertId == alertId.Value);
        }

        var histories = await query.OrderByDescending(h => h.FiredAt).ToListAsync();
        return Ok(histories);
    }

    [HttpPost("history/{id}/resolve")]
    public async Task<IActionResult> ResolveAlert(int id)
    {
        var alertHistory = await _context.AlertHistories.FindAsync(id);
        if (alertHistory == null)
        {
            return NotFound();
        }

        alertHistory.IsResolved = true;
        alertHistory.ResolvedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(alertHistory);
    }
}



