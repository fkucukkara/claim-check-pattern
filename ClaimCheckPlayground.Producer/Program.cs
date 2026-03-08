using ClaimCheckPlayground.Producer.Endpoints;
using ClaimCheckPlayground.Producer.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire service defaults (telemetry, health checks, service discovery) ──
builder.AddServiceDefaults();

// ── Azure integrations (injected by Aspire AppHost via connection strings) ──
// BlobServiceClient is registered by Aspire; the service-name "blobs" matches AppHost.
builder.AddAzureBlobServiceClient("blobs");
// ServiceBusClient is registered by Aspire; the service-name "servicebus" matches AppHost.
builder.AddAzureServiceBusClient("servicebus");

// ── Application services ──
builder.Services.Configure<ClaimCheckOptions>(builder.Configuration.GetSection("ClaimCheck"));
builder.Services.AddSingleton<IClaimCheckService, ClaimCheckService>();

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
app.MapOrderEndpoints();

app.Run();
