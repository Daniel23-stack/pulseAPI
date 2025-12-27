using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PulseAPI.Infrastructure;
using PulseAPI.Infrastructure.Data;
using PulseAPI.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Add Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Add Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
