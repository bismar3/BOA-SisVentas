# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

BOA-SisVentas is a Bolivian airline (Boliviana de Aviación) ticket-sales system. It has two parts:

- `BOA-Frontend/app-ventas-detalle` — Angular 19 SPA (PrimeNG + Tailwind).
- `BOA-Backend/MSVenta` — .NET 5 microservices solution (`boa.sln`) with an Ocelot API gateway.

The frontend talks **only** to the gateway (port 6005). Services never call each other over HTTP —
cross-service communication happens via RabbitMQ. Each service owns its own MySQL database.

## Common commands

### Infrastructure (databases + message broker)
`docker-compose.yml` provisions the 4 MySQL instances and RabbitMQ. It does **not** run the apps.
```bash
docker-compose up -d          # starts MySQL x4 + RabbitMQ (management UI at http://localhost:15672, guest/guest)
```

### Backend (.NET 5)
Run each microservice in its own terminal from `BOA-Backend/MSVenta`. There is no single command that
starts everything — the gateway plus every downstream service you need must run.
```bash
dotnet build boa.sln
dotnet run --project MSVenta.Gateway     # gateway → 6005 (start this + whichever services you need)
dotnet run --project MSVenta.Seguridad   # → 6001
dotnet run --project MSVenta.Venta       # → 6002
dotnet run --project BOA.Finanzas        # → 6003
dotnet run --project BOA.Comercial       # → 6004
```

### Frontend (Angular 19)
From `BOA-Frontend/app-ventas-detalle`:
```bash
npm install
npm start                 # ng serve → http://localhost:4200
npm run build
npm test                  # Karma + Jasmine (headless needs Chrome)
ng test --include='**/venta.service.spec.ts'   # run a single spec
```

## Backend architecture

Five projects, each isolated with its own MySQL DB. Project folder names and assembly names differ
(e.g. folder `MSVenta.Venta`, assembly/namespace `BOA.Operaciones`).

| Project (folder)      | Assembly         | Port | Database (host port)          | Responsibility |
|-----------------------|------------------|------|-------------------------------|----------------|
| `MSVenta.Gateway`     | BOA.Gateway      | 6005 | —                             | Ocelot reverse proxy + JWT validation |
| `MSVenta.Seguridad`   | BOA.Seguridad    | 6001 | db_boa_seguridad (3311)       | Auth/JWT, usuarios, roles, permisos (BCrypt) |
| `MSVenta.Venta`       | BOA.Operaciones  | 6002 | db_boa_operaciones (3312)     | Aeropuertos, aeronaves, rutas, tramos, programación de vuelos, asientos, tripulación, salidas |
| `BOA.Finanzas`        | BOA.Finanzas     | 6003 | db_boa_finanzas (3314)        | Ingresos/egresos, reportes PDF + envío por correo; **RabbitMQ consumer** |
| `BOA.Comercial`       | BOA.Comercial    | 6004 | db_boa_comercial (3313)       | Clientes, ventas, tickets, pagos; **RabbitMQ publisher** |

### Gateway & routing
All client traffic enters at port 6005. Routes live in `MSVenta.Gateway/ocelot.json` — each entry maps
an `UpstreamPathTemplate` to a downstream `Port` (6001–6004). Routes with an `AuthenticationOptions`
block (`AuthenticationProviderKey: "SECURITY-TOKEN"`) require a valid JWT; login/registration routes are
open. The gateway wires this up in `Startup.cs` with `services.AddJwtCustomized()` + `services.AddOcelot()`.
**When you add or rename a backend endpoint, you must also add its route to `ocelot.json`** or it will be
unreachable from the frontend.

### Per-service layering
Each service follows `Controllers → Services (interface + implementation) → Repositories → Models`, with
EF Core (`ContextDatabase`, `opt.UseMySQL(...)`). Services and repositories are registered as `AddScoped`
in that service's `Startup.cs`.

Connection-string key is **not** uniform:
- Comercial / Seguridad / Venta read `Configuration["mysql:cn"]`.
- Finanzas reads `ConnectionStrings:DefaultConnection`.

JWT plumbing comes from the shared NuGet packages `Aforo255.Cross.Token` and `Aforo255.Cross.Discovery`.

### RabbitMQ event flow (payment → income)
When a payment is confirmed, `BOA.Comercial/Services/RabbitMQPublisher.cs` publishes to the durable queue
`pago.confirmado`. `BOA.Finanzas/Services/RabbitMQConsumer.cs` (registered via `AddHostedService`) consumes
it and creates a matching ingreso record. This is the only cross-service coupling. Publish failures are
swallowed by design ("si RabbitMQ falla, Comercial sigue funcionando") — Comercial stays up even if the
broker is down, so a missing ingreso can mean RabbitMQ was unavailable, not a bug in Finanzas.

## Frontend architecture

- Standalone Angular components (no NgModules); bootstrap config in `src/app/app.config.ts`
  (PrimeNG Aura preset, `provideHttpClient(withFetch())`, animations).
- All routes are lazy-loaded in `src/app/app.routes.ts`. Feature areas live under
  `src/app/modules/<name>/` — each has its own `<name>.route.ts` and a `service/` folder.
- Authenticated screens are children of `DashboardLayoutComponent` (`shared/layouts/`). Each route carries
  `data: { permission, title, icon, ... }`; access is gated by roles/permissions via
  `shared/guard/auth-guard.service.ts` (`hasRole` reads `sessionStorage['roles']`).
- **Auth state lives in `sessionStorage`**: JWT under key `token`, roles under `roles`. There is **no HTTP
  interceptor** — each service builds `Authorization: Bearer <token>` headers manually
  (see `modules/venta/service/venta.service.ts`).

## Conventions & gotchas

- **Hardcoded gateway URLs.** Many module services hardcode `http://localhost:6005/api/...` instead of
  reading `src/environments/environment.ts` (`URL_SERVICIOS`). Both environment files are currently
  identical, so changing the base URL means editing the services too. Prefer wiring new services through
  `environment` rather than adding more hardcoded URLs.
- **Naming is Spanish.** Domain entities, routes, and DB fields use Spanish (venta, ingreso, aeronave,
  tramo, tripulación). Match existing naming when adding code.
- **DB-per-service isolation.** Do not add cross-database joins or direct DB access from another service —
  go through the gateway (read) or RabbitMQ (write/event).
- **Legacy/unused frontend modules.** Some folders under `src/app/modules/` (e.g. `product`, `category`,
  `customer`, `sale`, `almacen`, `AsignarProducto`) are leftovers not wired into `app.routes.ts`. Don't
  assume they're live — check the route table first.
- Target framework is **.NET 5** (`net5.0`), which is out of support; keep new backend code compatible with
  it. `bin/`, `obj/`, and `.vs/` are tracked in git in this repo — avoid noisy build-artifact commits.
