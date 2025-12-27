using Microsoft.Extensions.Logging;
using PulseAPI.Core.Entities;
using PulseAPI.Core.Interfaces;
using System.Diagnostics;
using System.Text;

namespace PulseAPI.Infrastructure.Services;

public class HealthCheckService : IHealthCheckService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(HttpClient httpClient, ILogger<HealthCheckService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckApiAsync(Api api, CancellationToken cancellationToken = default)
    {
        var result = new HealthCheckResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var request = new HttpRequestMessage(new HttpMethod(api.Method), api.Url);
            
            // Add headers if provided
            if (!string.IsNullOrEmpty(api.Headers))
            {
                try
                {
                    var headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(api.Headers);
                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                                continue; // Will be set with content
                            
                            try
                            {
                                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to add header {HeaderKey}", header.Key);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse headers JSON");
                }
            }

            // Add body if provided (for POST, PUT, etc.)
            if (!string.IsNullOrEmpty(api.Body) && (api.Method == "POST" || api.Method == "PUT" || api.Method == "PATCH"))
            {
                var contentType = "application/json";
                if (request.Headers.Contains("Content-Type"))
                {
                    contentType = request.Headers.GetValues("Content-Type").FirstOrDefault() ?? contentType;
                }
                request.Content = new StringContent(api.Body, Encoding.UTF8, contentType);
            }

            _httpClient.Timeout = TimeSpan.FromSeconds(api.TimeoutSeconds);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            result.IsSuccess = response.IsSuccessStatusCode;
            result.StatusCode = (int)response.StatusCode;
            result.LatencyMs = stopwatch.ElapsedMilliseconds;

            // Read response body (limit size to prevent memory issues)
            try
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                result.ResponseBody = responseBody.Length > 10000 ? responseBody.Substring(0, 10000) + "..." : responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read response body");
            }
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            result.IsSuccess = false;
            result.StatusCode = 0;
            result.LatencyMs = stopwatch.ElapsedMilliseconds;
            result.ErrorMessage = "Request timeout";
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            result.IsSuccess = false;
            result.StatusCode = 0;
            result.LatencyMs = stopwatch.ElapsedMilliseconds;
            result.ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsSuccess = false;
            result.StatusCode = 0;
            result.LatencyMs = stopwatch.ElapsedMilliseconds;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error checking API {ApiName}", api.Name);
        }

        return result;
    }
}

