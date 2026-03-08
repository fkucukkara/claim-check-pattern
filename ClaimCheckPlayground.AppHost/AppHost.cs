var builder = DistributedApplication.CreateBuilder(args);

// ── Azure Service Bus — carries lightweight claim-check tokens only ──
// NOTE: AddServiceBusQueue() is called for side-effect (queue declaration) only.
// serviceBus must remain the *namespace* resource builder so WithReference() injects
// ConnectionStrings:servicebus — the key AddAzureServiceBusClient("servicebus") expects.
var serviceBus = builder.AddAzureServiceBus("servicebus")
    .RunAsEmulator();
serviceBus.AddServiceBusQueue("orders");

// ── Azure Blob Storage — stores the full order payloads ──
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator()
    .AddBlobs("blobs");

// ── Producer API — receives orders, uploads payloads, enqueues claim tokens ──
builder.AddProject<Projects.ClaimCheckPlayground_Producer>("producer")
    .WithReference(serviceBus)
    .WithReference(storage)
    .WaitFor(serviceBus)
    .WaitFor(storage);

// ── Processor API — consumes claim tokens, retrieves payloads, fulfils orders ──
builder.AddProject<Projects.ClaimCheckPlayground_Processor>("processor")
    .WithReference(serviceBus)
    .WithReference(storage)
    .WaitFor(serviceBus)
    .WaitFor(storage);

builder.Build().Run();
