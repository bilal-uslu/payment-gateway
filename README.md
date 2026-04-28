# Payment Gateway

A payment gateway API built with **.NET 10** and **Clean Architecture**, enabling merchants to process card payments and retrieve transaction history via a secure, versioned REST API.

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Features](#features)
- [API Reference](#api-reference)
  - [POST /api/v1/payments — Process a Payment](#post-apiv1payments--process-a-payment)
  - [GET /api/v1/payments/{id} — Retrieve a Payment](#get-apiv1paymentsid--retrieve-a-payment)
- [Transaction Flow](#transaction-flow)
- [Validation Rules](#validation-rules)
- [Payment Statuses](#payment-statuses)
- [Authentication](#authentication)
- [Idempotency](#idempotency)
- [Rate Limiting](#rate-limiting)
- [Bank Simulator](#bank-simulator)
- [Running the Application](#running-the-application)
- [Running Tests](#running-tests)
- [CI Pipeline](#ci-pipeline)
- [Design Decisions & Assumptions](#design-decisions--assumptions)

---

## Overview

The Payment Gateway acts as the intermediary between merchants and an acquiring bank. It:

1. Validates incoming payment requests.
2. Enforces business rules.
3. Forwards valid requests to the acquiring bank.
4. Persists the result and returns a structured response to the merchant.

Merchants interact exclusively with this gateway. They never communicate with the acquiring bank directly.

---

## Architecture

The solution follows **Clean Architecture** with a clear separation of concerns:

```
┌─────────────────────────────────────────┐
│               PaymentGateway.Api        │  HTTP / Auth / Rate Limiting / Middleware
├─────────────────────────────────────────┤
│          PaymentGateway.Application     │  CQRS (MediatR), Validation, Behaviors
├─────────────────────────────────────────┤
│            PaymentGateway.Domain        │  Entities, Value Objects, Exceptions, Enums
├─────────────────────────────────────────┤
│        PaymentGateway.Infrastructure    │  Bank Client, Repository, Health Checks
└─────────────────────────────────────────┘
```

**Key patterns used:**

| Pattern | Purpose |
|---|---|
| **CQRS** via MediatR | Separates `ProcessPayment` (command) from `GetPayment` (query) |
| **Pipeline Behaviours** | Validation, Idempotency, and Logging as cross-cutting concerns |
| **Value Objects** | `CardNumber`, `ExpiryDate`, `Money`, `CardVerificationValue` enforce invariants at construction time |
| **Business Rules** | `IPaymentBusinessRule` pipeline checked before calling the bank (e.g. `BlockedBinRule`) |
| **API Key Auth** | Per-merchant API keys resolved to a `MerchantId` claim |

---

## Project Structure

```
src/
  PaymentGateway.Api/                  # Controllers, middleware, auth, mappers, models
  PaymentGateway.Application/          # Commands, queries, behaviors, interfaces, settings
  PaymentGateway.Domain/               # Entities, value objects, enums, domain exceptions
  PaymentGateway.Infrastructure/       # Acquiring bank client, in-memory repository, health checks

test/
  PaymentGateway.Domain.Tests/         # Value object and entity unit tests
  PaymentGateway.Application.Tests/    # Command/query handler, validator, behavior tests
  PaymentGateway.Infrastructure.Tests/ # Bank client, repository, and health check tests
  PaymentGateway.Api.Tests/            # Controller, mapper, and middleware tests
  PaymentGateway.Integration.Tests/    # End-to-end tests against the bank simulator
```

---

## Features

- ✅ Process card payments (authorize or decline via acquiring bank)
- ✅ Retrieve previously processed payments by ID
- ✅ Full request validation with structured `400` error responses
- ✅ Business rule enforcement with `422 Unprocessable Entity` rejections
- ✅ Idempotent payment processing via `Idempotency-Key` header
- ✅ Per-merchant API key authentication
- ✅ Per-merchant rate limiting
- ✅ Structured JSON logging via Serilog
- ✅ Correlation ID propagation across requests
- ✅ Acquiring bank health check endpoint
- ✅ OpenAPI documentation via Scalar (`/scalar/v1`)
- ✅ API versioning (`/api/v1/...`)

---

## API Reference

### POST /api/v1/payments — Process a Payment

Submits a new payment to the acquiring bank.

**Headers:**

| Header | Required | Description |
|---|---|---|
| `Authorization` | ✅ | `ApiKey <your-api-key>` |
| `Idempotency-Key` | ✅ | Unique string to prevent duplicate processing |

**Request Body:**

```json
{
  "cardNumber": "2222405343248877",
  "expiryMonth": 4,
  "expiryYear": 2025,
  "currency": "GBP",
  "amount": 100,
  "cvv": "123"
}
```

| Field | Type | Validation |
|---|---|---|
| `cardNumber` | string | Required, 14–19 numeric digits |
| `expiryMonth` | integer | Required, 1–12 |
| `expiryYear` | integer | Required, must be in the future (combined with month) |
| `currency` | string | Required, 3-character ISO 4217 code |
| `amount` | integer | Required, minor currency unit (e.g. `100` = £1.00) |
| `cvv` | string | Required, 3–4 numeric digits |

**Responses:**

| Status | Meaning |
|---|---|
| `200 OK` | Payment processed — check `status` field for `Authorized` or `Declined` |
| `400 Bad Request` | Validation failed — see error details |
| `422 Unprocessable Entity` | Rejected by a business rule before reaching the bank |
| `503 Service Unavailable` | Acquiring bank unreachable |

**Success Response (200):**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Authorized",
  "cardNumberLastFour": "8877",
  "expiryMonth": 4,
  "expiryYear": 2025,
  "currency": "GBP",
  "amount": 100
}
```

---

### GET /api/v1/payments/{id} — Retrieve a Payment

Returns details of a previously processed payment.

**Headers:**

| Header | Required | Description |
|---|---|---|
| `Authorization` | ✅ | `ApiKey <your-api-key>` |

**Responses:**

| Status | Meaning |
|---|---|
| `200 OK` | Payment found |
| `404 Not Found` | No payment with that ID exists for this merchant |

**Success Response (200):**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Authorized",
  "cardNumberLastFour": "8877",
  "expiryMonth": 4,
  "expiryYear": 2025,
  "currency": "GBP",
  "amount": 100
}
```

> The full card number is never returned. Only the last four digits are exposed.

---

## Transaction Flow

### Process Payment (`POST /api/v1/payments`)

```text
Merchant              Payment Gateway API            Acquiring Bank
   |                          |                            |
   |-- POST /payments ------->|                            |
   |   + Idempotency-Key      |                            |
   |                          |-- validate request         |
   |<-- 400 Bad Request ------|   (missing key / invalid   |
   |                          |    fields)                 |
   |                          |                            |
   |                          |-- check idempotency cache  |
   |<-- 200 (cached) ---------|   (duplicate request)      |
   |                          |                            |
   |                          |-- check business rules     |
   |<-- 422 Unprocessable ----|   (e.g. blocked BIN)       |
   |                          |                            |
   |                          |-- POST /payments --------->|
   |                          |<-- 200 authorized: true ---|
   |                          |<-- 200 authorized: false --|
   |                          |<-- 503 unavailable --------|
   |<-- 503 Unavailable ------|                            |
   |                          |                            |
   |<-- 200 OK ---------------|                            |
   |    (Authorized/Declined/ |                            |
   |    Rejected)             |                            |
```

### Retrieve Payment (`GET /api/v1/payments/{id}`)

```text
Merchant              Payment Gateway API
   |                          |
   |-- GET /payments/{id} --->|
   |                          |-- lookup by PaymentId + MerchantId
   |<-- 404 Not Found --------|   (not found)
   |<-- 200 OK ---------------|   (found — masked card + status)
```


## Validation Rules

Requests failing any rule below receive a `400 Bad Request` with a `ValidationProblemDetails` body:

- **Card number:** 14–19 numeric characters
- **Expiry month:** Integer between 1 and 12
- **Expiry year:** Must be in the future when combined with the expiry month
- **Currency:** Exactly 3 characters; must be one of the supported ISO 4217 codes
- **Amount:** Positive integer representing the minor currency unit
- **CVV:** 3–4 numeric characters

---

## Payment Statuses

| Status | Description |
|---|---|
| `Authorized` | The acquiring bank approved the payment |
| `Declined` | The acquiring bank declined the payment |
| `Rejected` | Rejected by the gateway before reaching the bank |

---

## Authentication

Authentication uses the **API Key** scheme. Each merchant is assigned an API key that maps to a `MerchantId`. Include the key in the `Authorization` header:

```
Authorization: ApiKey <your-api-key>
```

Merchants can only retrieve payments that belong to their own `MerchantId`.

---

## Idempotency

To prevent duplicate charges, every `POST /payments` request requires an `Idempotency-Key` header. If the same key is submitted more than once by the same merchant, the original response is returned without re-processing the payment.

The idempotency scope is **per merchant** — the same key used by two different merchants is treated as two independent requests.

---

## Rate Limiting

A per-merchant sliding window rate limit is enforced. Exceeding the limit returns `429 Too Many Requests`.

---

## Bank Simulator

A bank simulator is included via Docker to mock acquiring bank behaviour without a real banking connection:

```bash
docker-compose up
```

The simulator listens at `http://localhost:8080/payments` and accepts:

```json
{
  "card_number": "2222405343248877",
  "expiry_date": "04/2025",
  "currency": "GBP",
  "amount": 100,
  "cvv": "123"
}
```

**Simulator response logic based on the last digit of the card number:**

| Last digit | Behaviour |
|---|---|
| Odd (1, 3, 5, 7, 9) | `200 OK` — `authorized: true` with a random `authorization_code` |
| Even (2, 4, 6, 8) | `200 OK` — `authorized: false` |
| Zero (0) | `503 Service Unavailable` |

---

## Running the Application

**Prerequisites:**
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

**1. Start the bank simulator:**

```bash
docker-compose up -d
```

**2. Run the API:**

```bash
dotnet run --project src/PaymentGateway.Api
```

**3. Open the interactive API docs:**

Navigate to `https://localhost:<port>/scalar/v1`

**4. Try the API with the included HTTP file:**

Open [`src/PaymentGateway.Api/PaymentGateway.http`](PaymentGateway.http) in Visual Studio to send requests directly from the editor. The file covers all key scenarios:

| Request | Expected result |
|---|---|
| POST — card ending in **7** (odd) | `200 Authorized` |
| POST — card ending in **2** (even) | `200 Declined` |
| POST — blocked BIN | `422 Rejected` |
| GET — existing payment by ID | `200 OK` |
| GET — unknown payment ID | `404 Not Found` |
| GET — missing API key | `401 Unauthorized` |

> **Note:** Requests that trigger the bank simulator (scenarios 1, 2, and 8) require the simulator to be running (`docker-compose up -d`).

---

## Running Tests

```bash
# All tests
dotnet test

# By project
dotnet test test/PaymentGateway.Domain.Tests
dotnet test test/PaymentGateway.Application.Tests
dotnet test test/PaymentGateway.Infrastructure.Tests
dotnet test test/PaymentGateway.Api.Tests

# Integration tests (requires the bank simulator running via docker-compose up)
dotnet test test/PaymentGateway.Integration.Tests
```

---

## CI Pipeline

A GitHub Actions workflow is defined in [`.github/workflows/ci.yml`](.github/workflows/ci.yml).

**Triggers:**

| Event | Details |
|---|---|
| `push` to `main` | Runs automatically on every commit merged to `main` |
| `pull_request` | Runs automatically on every PR (any target branch) |
| `workflow_dispatch` | Can be triggered manually from the GitHub Actions UI against any branch |

**Steps:**

1. **Checkout** — checks out the repository at the target ref
2. **Setup .NET 10** — installs the required SDK
3. **Restore** — restores NuGet dependencies
4. **Build** — compiles the solution in `Release` configuration
5. **Unit tests** — runs all tests except the integration test project; produces a `.trx` report
6. **Integration tests** — runs the `Integration.Tests` project; the `ubuntu-latest` runner has Docker pre-installed so Testcontainers can start the mountebank bank simulator automatically (no `docker-compose up` required)
7. **Upload test results** — uploads all `.trx` files as a build artifact even when tests fail

> Integration tests start the bank simulator via Testcontainers, so `docker-compose up` is **not** required in CI.

---

## Design Decisions & Assumptions

| Decision | Rationale |
|---|---|
| **Clean Architecture** | Keeps domain logic independent of frameworks and infrastructure; makes testing straightforward |
| **CQRS with MediatR** | The read and write paths have different performance and validation requirements; separating them keeps each handler simple and focused |
| **Value Objects for card data** | Enforcing invariants (valid card number length, non-expired date, CVV format) at construction time means invalid state can never exist inside the domain |
| **Pipeline behaviours for cross-cutting concerns** | Validation, idempotency, and logging are applied consistently to all commands without polluting handler logic |
| **In-memory repository for payment storage** | No real database is needed to demonstrate the full payment flow. `IPaymentsRepository` is the only thing that would need to change to wire in a real store (e.g. SQL, Cosmos DB) |
| **In-memory cache for idempotency** | `IMemoryCache` is used for simplicity. In production this must be replaced with a **distributed cache** (e.g. Redis) so that idempotency is honoured across multiple API instances |
| **Amount as minor currency unit (integer)** | Avoids floating-point precision issues; aligns with how payment networks (including the bank simulator) represent amounts |
| **Per-merchant idempotency scope** | Scoping idempotency keys to `MerchantId:IdempotencyKey` prevents one merchant's keys from colliding with another's |
| **Business rules pipeline** | `IPaymentBusinessRule` allows new rules (e.g. blocked BINs, velocity checks) to be added without modifying the command handler |
| **Currency validation** | Validated against a fixed allow-list of supported ISO 4217 codes as specified in the requirements (no more than 3 codes) |
| **No authentication toward the acquiring bank** | The bank simulator exposes an unauthenticated endpoint. In production, a real acquiring bank integration would require an authentication mechanism such as API keys, mTLS, or OAuth 2.0 |
| **Acquiring bank health check always returns healthy** | The bank simulator provides no `/health` endpoint, so `AcquiringBankHealthCheck` is stubbed to always report `Healthy`. The real implementation is left in commented-out code ready to be enabled |
| **Correlation ID middleware** | Every request gets a `X-Correlation-Id` header for end-to-end traceability across logs |
| **Merchant IDs and API keys as configuration** | Merchant identities and their corresponding API keys are defined in application configuration (`appsettings.json`) for simplicity. In a production system these would be stored and managed in a database, allowing dynamic provisioning and rotation of credentials without redeployment |
