# CLAUDE.md — Global Instructions for AI Coding Agents

> This file applies to **all agents** working on this codebase (Claude Code, Gemini CLI, GitHub Copilot, etc.).
> Read this file completely before writing any code, creating any file, or making any architectural decision.

---

## Project Identity

**Name:** Distributed Inventory Control System
**Course:** Software Concorrente e Distribuído (SCD) — UFG / Instituto de Informática — 2026.1
**Professor:** Fábio Moreira Costa
**Deadline:** 2026-06-28
**Architecture reference:** `docs/ARCHITECTURE.md`

---

## Prime Directive

> **This is an academic project. Optimize for clarity and simplicity first. Correctness second. Elegance third. Performance last.**

The professor is evaluating whether the group understands distributed systems and concurrency concepts — not whether the code is production-grade. Every implementation decision must be explainable to a professor in a 20-minute presentation.

When in doubt between two approaches, always pick the one that is **easier to understand and demonstrate**, even if it is less efficient or less robust.

---

## What This System Must Demonstrate

These are the hard requirements from the course specification (`INSTRUCTIONS.md`). Every one of them must be visibly present in the final code:

| # | Requirement | How it is demonstrated in this project |
|---|---|---|
| 1 | Service accessible to multiple clients on the Internet | CLI clients connecting to API Gateway on EC2 |
| 2 | Service built from integration of multiple distributed components | API Gateway + Inventory Service + RabbitMQ + Workers |
| 3 | Concurrent access to shared resources | Multiple clients selling/buying the same product simultaneously |
| 4 | Server-side background processing concurrent with client access | Maintenance Worker (Python) processing while clients operate |
| 5 | Synchronous (blocking) remote interaction | gRPC call from API Gateway → Inventory Service |
| 6 | Asynchronous remote interaction | RabbitMQ publish/subscribe; WebSocket events to Dashboard |
| 7 | Data replication and partitioning | PostgreSQL Primary (writes) + Replica (reads) |
| 8 | Consistency and availability guarantees | Optimistic locking + retry; durable queues; ACID transactions |

**If you are about to write code that does not visibly contribute to at least one of the above, question whether it is necessary.**

---

## Repository Structure

```
inventory-system/
├── CLAUDE.md                        ← this file
├── GEMINI.md                        ← symlink or copy of this file
├── docs/
│   └── ARCHITECTURE.md              ← full architecture reference
├── src/
│   ├── ApiGateway/                  ← .NET 8 / ASP.NET Core
│   ├── InventoryService/            ← .NET 8 / ASP.NET Core + gRPC
│   ├── Client/                      ← .NET 8 / Console App
│   ├── dashboard/                   ← Python 3.12 / FastAPI
│   └── maintenance_worker/          ← Python 3.12 / asyncio
├── infra/
│   ├── docker-compose.yml
│   ├── docker-compose.prod.yml
│   └── postgres/
│       └── init.sql
└── README.md
```

Before creating any file, check this structure. Place files exactly where the structure says.

---

## Language & Framework Rules

### C# / .NET 8

- Target framework: `net8.0`
- Use **top-level statements** in `Program.cs` (no `class Program`)
- Use **primary constructors** where they reduce noise
- Use `async/await` throughout — no `.Result` or `.Wait()`
- Use `ILogger<T>` for all logging — no `Console.WriteLine` in production paths (CLI is the exception)
- Use `IOptions<T>` for configuration — no `Environment.GetEnvironmentVariable` directly in service classes
- EF Core: use `async` methods (`ToListAsync`, `FindAsync`, `SaveChangesAsync`)
- Do **not** use MediatR, AutoMapper, FluentValidation, or any other third-party abstraction library — keep dependencies minimal and obvious
- Do **not** implement CQRS, event sourcing, or any pattern beyond what is explicitly in `ARCHITECTURE.md`
- NuGet packages allowed:
  - `Npgsql.EntityFrameworkCore.PostgreSQL`
  - `RabbitMQ.Client`
  - `Grpc.AspNetCore` (server)
  - `Grpc.Net.Client` (client)
  - `Google.Protobuf`
  - `Grpc.Tools`
  - `Microsoft.AspNetCore.Authentication.JwtBearer`
  - `xunit` (tests only)

### Python 3.12

- Use `asyncio` + `aio-pika` for all RabbitMQ operations
- Use `asyncpg` for direct PostgreSQL access
- Use **FastAPI** for the Dashboard HTTP + WebSocket server
- Use **type hints** on all function signatures
- Use `python-dotenv` for environment variable loading
- Do **not** use Django, SQLAlchemy, Celery, or any heavy framework
- pip packages allowed:
  - `fastapi`
  - `uvicorn[standard]`
  - `aio-pika`
  - `asyncpg`
  - `python-dotenv`
  - `pytest` + `pytest-asyncio` (tests only)

---

## Code Style Rules

### General (all languages)

- **Write for a reader who is also a student**, not for a senior engineer. Comments are welcome and encouraged.
- Each function/method does **one thing**. If you find yourself writing "and" in a method name, split it.
- Method length limit: **~40 lines**. If it is longer, refactor without adding abstraction layers — just extract helper methods in the same file/class.
- No magic numbers. Use named constants or configuration values.
- Error messages must be human-readable. They will appear in the demo.

### Naming

- C#: `PascalCase` for types and methods, `camelCase` for locals, `_camelCase` for private fields
- Python: `snake_case` for everything except classes (`PascalCase`)
- Avoid abbreviations unless they are universally understood (`qty` for quantity is fine; `prdc` for product is not)

### Comments

- Comment the **why**, not the **what**
- Required comment locations:
  - Above any concurrency mechanism (lock, semaphore, retry loop) — explain what race condition it prevents
  - Above any RabbitMQ publish call — explain what downstream consumers will do with this event
  - Above any code that reads from the replica instead of the primary — explain why eventual consistency is acceptable here

### Example of a well-commented concurrency block:

```csharp
// Optimistic concurrency: we do not hold a DB lock during the read phase.
// If another transaction modifies this product between our read and write,
// EF Core will detect the RowVersion mismatch and throw DbUpdateConcurrencyException.
// We retry up to 3 times with random backoff before giving up.
for (int attempt = 0; attempt < 3; attempt++)
{
    try { ... }
    catch (DbUpdateConcurrencyException)
    {
        _db.ChangeTracker.Clear(); // discard stale tracked entity before retrying
        await Task.Delay(Random.Shared.Next(10, 50));
    }
}
```

---

## Explicit Prohibitions

Do **not** implement the following, even if they seem like improvements:

| Prohibited | Why |
|---|---|
| Outbox Pattern | Correct publish-after-commit ordering is sufficient for this demo |
| Circuit Breaker (Polly) | Not required; adds complexity without demonstrating a required concept |
| Service discovery (Consul, etc.) | Hardcoded hostnames in docker-compose are acceptable and simpler |
| HTTPS between internal services | EC2 private network; TLS between services adds setup complexity for zero benefit |
| JWT refresh tokens | A long-TTL token is sufficient for demo purposes |
| Domain events / MediatR pipelines | Direct method calls are clearer and easier to trace in a demo |
| Repository pattern with generic interfaces | `DbContext` used directly in service classes is fine at this scale |
| Kubernetes / ECS / container orchestration | Plain Docker Compose on EC2 is the target environment |
| React / Vue / Angular for Dashboard frontend | Plain HTML + vanilla JS is sufficient; the backend is what is being assessed |
| Multiple RabbitMQ instances (clustering) | Single instance is enough to demonstrate the pub/sub concept |

---

## Configuration & Environment Variables

All configuration via environment variables. No hardcoded connection strings in source code.

### .NET services — `appsettings.json` + env override

```json
{
  "ConnectionStrings": {
    "Primary": "Host=localhost;Database=inventory;Username=postgres;Password=postgres",
    "Replica": "Host=localhost;Port=5433;Database=inventory;Username=postgres;Password=postgres"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "admin",
    "Password": "admin"
  },
  "Jwt": {
    "Secret": "change-this-in-production-min-256-bits",
    "ExpiryHours": 24
  },
  "InventoryService": {
    "GrpcUrl": "http://localhost:5001"
  }
}
```

### Python services — `.env` file

```env
RABBITMQ_URL=amqp://admin:admin@localhost/
POSTGRES_PRIMARY_URL=postgresql://postgres:postgres@localhost:5432/inventory
POSTGRES_REPLICA_URL=postgresql://postgres:postgres@localhost:5433/inventory
```

---

## Concurrency: The Core Concept to Demonstrate

The most important technical concept in this project is **concurrent access to shared inventory**.

### The scenario that must work correctly:

> 10 units of product #1 in stock.
> 20 clients simultaneously send `POST /inventory/sell` requesting 1 unit each.
> Expected result: exactly 10 succeed (HTTP 200), 10 fail gracefully (HTTP 409 or 400).
> Stock must end at exactly 0. No negative stock. No lost updates.

### The mechanism: optimistic locking via `RowVersion`

```
Client A reads product: quantity=10, rowVersion=AAA
Client B reads product: quantity=10, rowVersion=AAA
Client A writes: UPDATE ... WHERE rowVersion=AAA → succeeds, rowVersion becomes BBB
Client B writes: UPDATE ... WHERE rowVersion=AAA → fails (version changed), retries
Client B reads fresh: quantity=9, rowVersion=BBB
Client B writes: UPDATE ... WHERE rowVersion=BBB → succeeds
```

This must be implemented exactly as described in `ARCHITECTURE.md` Section 6.

### Demo amplification trick

During the live demo, add a short artificial delay inside the sell operation to widen the conflict window. This makes the race condition visible:

```csharp
// DEMO ONLY — remove before final submission or guard with a config flag
if (_demoConfig.ArtificialDelayMs > 0)
    await Task.Delay(_demoConfig.ArtificialDelayMs);
```

Control it via config so it can be toggled without recompiling.

---

## RabbitMQ Events

### Exchange configuration

- Name: `inventory`
- Type: `topic`
- Durable: `true`

### Queues

| Queue name | Binding key | Consumer |
|---|---|---|
| `alerts.dashboard` | `inventory.#` | Dashboard Worker |
| `maintenance.tasks` | `inventory.stock.low` | Maintenance Worker |

### Event schemas (JSON)

```json
// inventory.product.sold
{
  "event": "product.sold",
  "productId": 1,
  "quantity": 2,
  "remainingStock": 8,
  "actor": "client-sim-3",
  "timestamp": "2026-05-20T14:30:00Z"
}

// inventory.stock.low
{
  "event": "stock.low",
  "productId": 1,
  "currentStock": 3,
  "threshold": 10,
  "timestamp": "2026-05-20T14:30:00Z"
}

// inventory.product.bought
{
  "event": "product.bought",
  "productId": 1,
  "quantity": 5,
  "newStock": 15,
  "actor": "supplier-bot",
  "timestamp": "2026-05-20T14:30:00Z"
}
```

**Rule:** Always publish events **after** `SaveChangesAsync()` returns successfully. Never before.

---

## gRPC Contract

The `.proto` file is the source of truth for Gateway ↔ Inventory Service communication.
File location: `src/InventoryService/Protos/inventory.proto`

Do not change the RPC signatures without updating both the server implementation and the Gateway client.

The proto file is defined in `ARCHITECTURE.md` Section 9 and must be used exactly as specified.

---

## Data Access Rules

| Operation | Use | Reason |
|---|---|---|
| Write (sell, buy, adjust) | Primary PostgreSQL | Requires strong consistency |
| Read in business logic (sell check) | Primary PostgreSQL | Must see latest committed state |
| Dashboard queries (history, stats) | Replica PostgreSQL | Eventual consistency acceptable; reduces load on primary |
| Maintenance Worker reads | Primary PostgreSQL | Reconciliation requires authoritative data |

---

## Docker Compose Rules

- All services defined in `infra/docker-compose.yml` (dev) and `infra/docker-compose.prod.yml` (EC2)
- Service names are used as hostnames — keep them stable: `postgres-primary`, `postgres-replica`, `rabbitmq`, `inventory-service`, `api-gateway`, `dashboard`, `maintenance-worker`
- Each service must have a `restart: on-failure` policy
- Do not pin to `latest` tag for any image — use explicit versions as defined in `ARCHITECTURE.md`

---

## What to Do When Uncertain

1. **Read `docs/ARCHITECTURE.md` first.** Most questions are answered there.
2. If the architecture doc does not answer it, pick the **simpler** option.
3. If both options seem equally simple, pick the one that **makes the demo more visible and explainable**.
4. Do **not** add a new library or pattern without a clear reason tied to a course requirement.
5. Leave a `// TODO: [reason why this was left incomplete]` comment rather than implementing something wrong.

---

## Definition of "Done" for Each Component

A component is done when:

- [ ] It starts without errors via `docker-compose up`
- [ ] Its primary responsibility from `ARCHITECTURE.md` Section 2 is implemented
- [ ] It handles its expected error cases gracefully (no unhandled exceptions crashing the process)
- [ ] A human can read the main service class and understand what it does in under 5 minutes
- [ ] It is reachable from other components using the hostnames defined in `docker-compose.yml`

A component does **not** need:
- 100% unit test coverage
- Structured logging with correlation IDs
- Graceful shutdown handling
- Pagination on list endpoints
- Input validation beyond what prevents a crash

---

## Demo Scenario (must work end-to-end)

This is the sequence that will be executed live for the professor. All agents must ensure this works:

```
1. docker-compose up  →  all services healthy

2. Client CLI — list products
   Expected: 5 products, quantities as seeded

3. Client CLI — simulation mode: 20 concurrent sells of product #1 (10 in stock)
   Expected: ~10 success (HTTP 200), ~10 conflict (HTTP 409)
   Expected: Dashboard browser shows "product.sold" and "stock.low" alerts in real time

4. Client CLI — buy 20 units of product #1 (restock)
   Expected: stock restored; Dashboard shows "product.bought" event

5. Stop maintenance-worker container
   Trigger 5 more stock.low events via CLI
   Expected: RabbitMQ Management UI shows 5 messages queued in "maintenance.tasks"

6. Restart maintenance-worker container
   Expected: worker consumes the 5 queued messages; reconciliation_log table has 5 new rows

7. Query postgres-replica directly
   Expected: same product data as primary (with small replication lag acceptable)
```

---

## Delivery Checklist

Per the course specification, the final submission must include:

- [ ] Source code (this repository) submitted via GitHub Classroom
- [ ] `docs/ARCHITECTURE.md` — architecture documentation
- [ ] `README.md` — setup and run instructions (Portuguese is fine)
- [ ] `infra/postgres/init.sql` — test data
- [ ] Demo video with all group members visibly participating, uploaded to Plataforma Turing
- [ ] `docs/` folder with any additional documentation

**Deadline: 2026-06-28**
