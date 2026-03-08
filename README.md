# ClaimCheckPlayground

A production-quality demonstration of the [Claim-Check pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/claim-check) built with **.NET 10**, **ASP.NET Core Minimal APIs**, and **.NET Aspire**.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          ClaimCheckPlayground                               │
│                                                                             │
│  ┌──────────────┐   POST /orders        ┌────────────────────────────────┐  │
│  │    Client    │──────────────────────▶│       Producer API             │  │
│  │              │◀──────────────────────│  (ClaimCheckPlayground.        │  │
│  │              │  202 { claimToken }   │       Producer)                │  │
│  └──────┬───────┘                       └────────────┬───────────────────┘  │
│         │                                            │                      │
│         │  GET /orders/{token}/status    ② Upload payload JSON              │
│         │                               │  (BlobClient)                    │
│  ┌──────▼───────┐                       ▼                                  │
│  │   Processor  │              ┌──────────────────┐                        │
│  │     API      │              │  Azure Blob      │                        │
│  │  (status     │              │  Storage         │                        │
│  │   endpoint)  │              │  order-payloads/ │                        │
│  │              │              │  {token}.json    │                        │
│  └──────▲───────┘              └──────────────────┘                        │
│         │                                ③ Enqueue claim token             │
│         │  ④ Update in-memory            │  (ServiceBusSender)             │
│         │     state store                ▼                                 │
│         │                      ┌──────────────────┐                        │
│  ┌──────┴─────────────┐        │  Azure Service   │                        │
│  │  OrderProcessing   │◀───────│  Bus             │                        │
│  │  Service           │        │  Queue: orders   │                        │
│  │  (BackgroundService│        └──────────────────┘                        │
│  │  hosted inside     │                                                    │
│  │  Processor API)    │  ⑤ Download payload   ⑥ Process order             │
│  │                    │─────────────────────▶ ⑦ Delete blob                │
│  └────────────────────┘                                                    │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Flow

| Step | Description |
|------|-------------|
| ① | Client sends a large order JSON to `POST /orders` on the **Producer API**. |
| ② | Producer serialises the payload and uploads it to **Azure Blob Storage** (`order-payloads/{claimToken}.json`). |
| ③ | Producer enqueues a lightweight `ClaimCheckMessage` (token + order ID only) to the **Azure Service Bus** queue. The payload never passes through the bus. |
| ④ | **OrderProcessingService** (a `BackgroundService` inside the Processor) dequeues the message, reads the claim token. |
| ⑤ | Processor downloads the full payload from Blob Storage using the claim token. |
| ⑥ | Processor executes business logic (order fulfilment), marks the order as **Fulfilled** in the in-memory state store. |
| ⑦ | Processor completes the Service Bus message and optionally deletes the payload blob. |

---

## Projects

| Project | Type | Responsibility |
|---------|------|----------------|
| `ClaimCheckPlayground.Contracts` | Class Library | Shared DTOs and enums (`OrderRequest`, `ClaimCheckMessage`, `OrderStatusResponse`) |
| `ClaimCheckPlayground.Producer` | ASP.NET Core Minimal API | Receives orders, offloads payload to Blob, enqueues claim token to Service Bus |
| `ClaimCheckPlayground.Processor` | ASP.NET Core Minimal API + BackgroundService | Consumes Service Bus queue, retrieves payload from Blob, fulfils orders, exposes status endpoint |
| `ClaimCheckPlayground.AppHost` | .NET Aspire AppHost | Orchestrates all services locally with emulators; provisions Azure resources via `azd` |
| `ClaimCheckPlayground.ServiceDefaults` | Shared Library | OpenTelemetry, health checks, service discovery defaults |

---

## APIs

### Producer API — `POST /orders`

Submits a new order. Returns a claim token the client can use to poll for status.

**Request body**
```json
{
  "customerId": "customer-42",
  "items": [
    { "productId": "SKU-001", "quantity": 2, "unitPrice": 29.99 },
    { "productId": "SKU-005", "quantity": 1, "unitPrice": 149.00 }
  ],
  "shippingAddress": {
    "street": "123 Main St",
    "city": "Seattle",
    "postalCode": "98101",
    "country": "US"
  }
}
```

**Response — 202 Accepted**
```json
{
  "claimToken": "a3f7b2c1d0e4f568901234567890abcd",
  "orderId": "d3f2a1b4-c5e6-7890-abcd-ef1234567890"
}
```

---

### Processor API — `GET /orders/{claimToken}/status`

Polls the processing status of an order.

**Response — 200 OK**
```json
{
  "claimToken": "a3f7b2c1d0e4f568901234567890abcd",
  "orderId": "d3f2a1b4-c5e6-7890-abcd-ef1234567890",
  "status": "fulfilled",
  "processedAt": "2026-03-08T10:35:42.123Z"
}
```

Status values: `pending` → `processing` → `fulfilled` | `failed`

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ | |
| [Docker Desktop](https://www.docker.com/products/docker-desktop) | any recent | Required for local emulators (Azurite, Service Bus emulator) |
| [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) | 2.60+ | For `az login` (DefaultAzureCredential) |
| [Azure Developer CLI (azd)](https://aka.ms/azd) | 1.9+ | For cloud provisioning |

---

## Local Development

### 1. Authenticate

```bash
az login
```

This satisfies `DefaultAzureCredential` used by both the Producer and Processor.

### 2. Run via .NET Aspire

```bash
dotnet run --project ClaimCheckPlayground.AppHost
```

The Aspire dashboard opens automatically. Both services start with emulated Service Bus and Blob Storage (Docker containers).

### 3. Test

Open `ClaimCheckPlayground.Producer.http` in Visual Studio or VS Code REST Client, or use the Scalar UI:

- **Producer** Scalar UI: `https://localhost:{producer-port}/scalar/v1`
- **Processor** Scalar UI: `https://localhost:{processor-port}/scalar/v1`

The Aspire dashboard shows the assigned ports.

---

## Cloud Deployment (Azure)

### 1. Initialise azd

```bash
azd init
```

### 2. Provision and Deploy

```bash
# Login to Azure
azd auth login

# Provision cloud resources (Service Bus namespace + queue, Storage account + container)
# and deploy the containerised services
azd up
```

Resources provisioned:
- Azure Service Bus (Standard tier minimum) with queue `orders`
- Azure Storage Account with blob container `order-payloads`
- Azure Container Apps (or App Service) for Producer and Processor

Both services use **Managed Identity** (no connection strings in code or config).

---

## Configuration Reference

### Producer (`appsettings.json → ClaimCheck`)

| Key | Default | Description |
|-----|---------|-------------|
| `BlobContainerName` | `order-payloads` | Blob container for order payloads |
| `QueueName` | `orders` | Service Bus queue name |

### Processor (`appsettings.json → Processor`)

| Key | Default | Description |
|-----|---------|-------------|
| `BlobContainerName` | `order-payloads` | Must match Producer |
| `QueueName` | `orders` | Service Bus queue to consume |
| `DeleteBlobAfterProcessing` | `true` | Set to `false` to retain blobs for debugging |

---

## Key Pattern Points

> **The payload never travels through the message bus.**
>
> Service Bus only sees: `{ "claimToken": "...", "orderId": "...", "enqueuedAt": "..." }`  
> The full order JSON (which could be arbitrarily large) lives exclusively in Blob Storage.

This satisfies all four well-architected pillars:

- **Reliability** — Blob Storage offers geo-redundancy independent of the message bus.
- **Security** — Sensitive payload is stored with fine-grained RBAC; the bus sees only an opaque token.
- **Cost** — Service Bus message size stays tiny; premium size tiers are not needed.
- **Performance** — Bus throughput is unaffected by payload size.

---

## References

- [Claim-Check pattern — Azure Architecture Center](https://learn.microsoft.com/en-us/azure/architecture/patterns/claim-check)
- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [.NET Aspire overview](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
- [Azure Service Bus .NET client](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues)
- [Azure Blob Storage .NET client](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet)
