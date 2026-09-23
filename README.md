# ECommerce API

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1?logo=postgresql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-cache-DC382D?logo=redis&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)

A backend for an online store, written in **ASP.NET Core (.NET 10)** using Clean Architecture. It covers the parts every shop needs: a product catalog, shopping baskets, orders, and user accounts with JWT authentication.

I built it to be deployable, not just a demo. Configuration is validated at startup, secrets never live in the repo, prices are always calculated on the server, and the whole thing runs in Docker.

---

## Table of contents

- [Features](#features)
- [Tech stack](#tech-stack)
- [Architecture](#architecture)
- [Getting started](#getting-started)
- [Configuration](#configuration)
- [API reference](#api-reference)
- [Deployment](#deployment)
- [Design decisions](#design-decisions)
- [Roadmap](#roadmap)

---

## Features

**Catalog**
- Product listing with paging, search, brand/type filters, and sorting
- Brands, product types, and delivery methods
- Product search and delivery methods are served from the output cache

**Baskets**
- Guests can shop without an account. The basket is tracked by an `X-Buyer-Id` header.
- The guest basket moves over to the user's account when they log in
- Baskets are stored in Redis through `HybridCache`

**Orders**
- Orders are created from the basket, and every price is read again from the catalog, so the client can't set its own prices
- Users can view their order history or a single order

**Identity**
- Register, log in, log out, and view the current user
- Short-lived access tokens (15 minutes) and refresh tokens that rotate on every use. Refresh tokens are stored hashed, and a reused token is detected.
- Change password, forgot password, and reset password
- Seeded roles: `SuperAdmin`, `Admin`, `User`

---

## Tech stack

| Concern | Technology |
| --- | --- |
| Web | ASP.NET Core Minimal APIs, Asp.Versioning, Swagger (Development only) |
| Application | MediatR (CQRS), FluentValidation, Mapster, Ardalis.Specification |
| Persistence | Entity Framework Core 10 + Npgsql (PostgreSQL 17) |
| Identity | ASP.NET Core Identity, JWT Bearer (HS256) |
| Caching | Output Cache, `Microsoft.Extensions.Caching.Hybrid` on Redis |
| Tooling | Docker, Docker Compose |

---

## Architecture

The solution has four projects. Each one depends only on the layers inside it:

```
┌──────────────────────────────────────────────┐
│  ECommerce.API             (Presentation)    │  endpoints, middleware, filters, DI root
├──────────────────────────────────────────────┤
│  ECommerce.Infrastructure  (Infrastructure)  │  EF Core, Identity, JWT, caching, seeding
├──────────────────────────────────────────────┤
│  ECommerce.UseCases        (Application)     │  commands, queries, validators, DTOs
├──────────────────────────────────────────────┤
│  ECommerce.Domain          (Domain)          │  entities, errors, Result<T>, contracts
└──────────────────────────────────────────────┘
```

- **Domain** has no framework dependencies. It holds the entities, the `Result`/`Error` types used for expected failures, and the repository interfaces.
- **UseCases** contains one MediatR handler for each operation. A pipeline behavior runs the FluentValidation checks before any handler.
- **Infrastructure** implements the repository and unit-of-work contracts. Reads go through dedicated query services that return data already projected into the response shape.
- **API** is kept thin: endpoints map HTTP to commands and queries, and every response uses the same `ApiResponse<T>` envelope. A global exception middleware handles unexpected errors.

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) to run PostgreSQL and Redis

### 1. Clone the repo and start the infrastructure

```sh
git clone https://github.com/mmaher32/ECommerceAPI.git
cd ECommerceAPI
cp .env.example .env        # then fill in the values
docker compose up -d
```

### 2. Set the development secrets

```sh
cd ECommerce.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=127.0.0.1;Port=5432;Database=ECommerceDb;Username=postgres;Password=change_me"
dotnet user-secrets set "Jwt:Secret"   "a-random-string-at-least-32-bytes-long"
dotnet user-secrets set "Jwt:Issuer"   "http://localhost:5254"
dotnet user-secrets set "Jwt:Audience" "http://localhost:5254"
```

### 3. Run the API

```sh
dotnet run --project ECommerce.API
```

When the API runs in Development, it applies migrations and seeds brands, types, products, delivery methods, and roles on its own. Swagger is available at **http://localhost:5254/swagger**.

---

## Configuration

Every setting can be overridden with an environment variable. Use `__` for nested keys.

| Key | Required | Description |
| --- | :---: | --- |
| `ConnectionStrings__DefaultConnection` | ✅ | PostgreSQL connection string |
| `ConnectionStrings__Redis` | Prod | Required in Production. In Development, the API falls back to an in-memory cache when it's missing. |
| `Jwt__Secret` | ✅ | Signing key, at least 32 bytes |
| `Jwt__Issuer` / `Jwt__Audience` | ✅ | Checked against every token |
| `Jwt__AccessTokenExpirationMinutes` | | Default `15` |
| `Jwt__RefreshTokenExpirationDays` | | Default `7` |
| `Database__MigrateOnStartup` | | `true` runs migrations and seeding at startup |
| `Seed__SuperAdmin__Email` / `Password` / `DisplayName` | | Creates the Super Admin account, or updates it if it already exists |

---

## API reference

All routes are versioned under `/api/v1`. 🔒 means the route requires a bearer token.

### Users — `/api/v1/users`

| Method | Route | Description |
| --- | --- | --- |
| `POST` | `/register` | Create an account |
| `POST` | `/login` | Get an access token and a refresh token |
| `POST` | `/refresh` | Exchange a refresh token for a new token pair |
| `POST` | `/logout` 🔒 | Revoke the current refresh token |
| `GET` | `/me` 🔒 | Get the current user's profile |
| `POST` | `/change-password` 🔒 | Change the password |
| `POST` | `/forgot-password` | Request a password-reset token |
| `POST` | `/reset-password` | Reset the password with a token |

### Catalog

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/products/paged` | Search, filter, sort, and page products |
| `GET` | `/products/{id}` | Get a single product |
| `GET` | `/brands` | List all brands |
| `GET` | `/types` | List all product types |
| `GET` | `/deliverymethods` | List delivery options |

### Basket — `/api/v1/baskets`

Pass `X-Buyer-Id` as a guest, or send a bearer token when logged in.

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/` | Get the current basket |
| `POST` | `/items` | Add an item |
| `PUT` | `/items/{productId}` | Change an item's quantity |
| `DELETE` | `/items/{productId}` | Remove an item |
| `DELETE` | `/` | Clear the basket |

### Orders — `/api/v1/orders` 🔒

| Method | Route | Description |
| --- | --- | --- |
| `POST` | `/` | Create an order from the basket |
| `GET` | `/` | List the user's orders |
| `GET` | `/{id}` | Get a single order |

Every successful response uses the same envelope. `pagination` is included only on paged endpoints.

```json
{
  "success": true,
  "message": null,
  "data": { },
  "meta": {
    "traceId": "00-4bf92f35...",
    "pagination": { }
  }
}
```

---

## Deployment

The API ships with a multi-stage `Dockerfile` that runs as a non-root user and exposes port **8080**.

```sh
docker build -f ECommerce.API/Dockerfile -t ecommerce-api .

docker run -d -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=ECommerceDb;Username=postgres;Password=change_me" \
  -e ConnectionStrings__Redis="redis:6379" \
  -e Jwt__Secret="a-random-string-at-least-32-bytes-long" \
  -e Jwt__Issuer="https://yourdomain.com" \
  -e Jwt__Audience="https://yourdomain.com" \
  -e Database__MigrateOnStartup="true" \
  ecommerce-api
```

Before you deploy, note the following:

- Enable `Database__MigrateOnStartup` for the **first** deploy only. Each seed run resets the Super Admin password to the value in configuration.
- Swagger is turned off in Production.
- Baskets are stored only in Redis, so Production won't start without it.
- Put a reverse proxy such as nginx, Caddy, or Traefik in front of the API to terminate TLS.

---

## Design decisions

- **No N+1 queries.** Lazy loading is off. Related data is loaded with SQL projections.
- **Soft deletes.** Every store entity inherits from `BaseEntity`, and a global query filter hides deleted rows.
- **Results instead of exceptions.** Expected failures such as "product not found" or "basket empty" return a `Result` with a typed `Error`. The API turns these into the right HTTP status codes.
- **Fail fast.** JWT and cache options are validated when the app starts, so a bad configuration fails at boot instead of on the first request.
- **Secrets stay out of git.** Only `.env.example` is committed.

---

## Roadmap

- [ ] SMTP email service for account confirmation and password-reset emails
- [ ] Rate limiting on authentication endpoints
- [ ] `/health/live` and `/health/ready` endpoints, plus a Docker `HEALTHCHECK`
- [ ] OpenTelemetry metrics with a Grafana dashboard
- [ ] Admin endpoints for catalog management, using the seeded roles

---

## Author

**Muhammed Maher** — [GitHub](https://github.com/mmaher32)
