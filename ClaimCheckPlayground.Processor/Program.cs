using ClaimCheckPlayground.Processor.Endpoints;
using ClaimCheckPlayground.Processor.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire service defaults (telemetry, health checks, service discovery) ──
builder.AddServiceDefaults();

// ── Azure integrations (injected by Aspire AppHost via connection strings) ──
builder.AddAzureBlobServiceClient("blobs");
builder.AddAzureServiceBusClient("servicebus");

// ── Application services ──
builder.Services.Configure<ProcessorOptions>(builder.Configuration.GetSection("Processor"));
builder.Services.AddSingleton<IOrderStateStore, OrderStateStore>();
builder.Services.AddHostedService<OrderProcessingService>();

// ── OpenAPI ──
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Middleware ──
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar/v1");
}

app.MapDefaultEndpoints();      // Aspire health / alive probes

// ── API endpoints ──
app.MapOrderStatusEndpoints();

app.Run();
