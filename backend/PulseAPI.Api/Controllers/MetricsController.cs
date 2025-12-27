using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PulseAPI.Core.Interfaces;

namespace PulseAPI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MetricsController : ControllerBase
{
    private readonly IMetricsService _metricsService;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(IMetricsService metricsService, ILogger<MetricsController> logger)
    {
        _metricsService = metricsService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<MetricsDto>> GetMetrics(
        [FromQuery] string? environment,
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime)
    {
        try
        {
            var metrics = await _metricsService.GetMetricsAsync(environment, startTime, endTime);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics");
            return StatusCode(500, new { message = "An error occurred while retrieving metrics" });
        }
    }

    [HttpGet("apis")]
    public async Task<ActionResult<List<ApiMetricsDto>>> GetApiMetrics(
        [FromQuery] string? environment,
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime)
    {
        try
        {
            var metrics = await _metricsService.GetApiMetricsAsync(environment, startTime, endTime);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API metrics");
            return StatusCode(500, new { message = "An error occurred while retrieving API metrics" });
        }
    }
}



