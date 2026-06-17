<div align="center">
  <img src="./docs/allmarket-logo.svg" alt="AllMarket logo" width="220" />

  <h1>AllMarket API</h1>

  <p>
    Backend for <strong>AllMarket</strong>, an e-commerce portfolio project built with ASP.NET Core.
  </p>

  <p>
    <strong>This is not a real place to buy products.</strong><br />
    AllMarket is a portfolio project.
  </p>
</div>

## Overview

AllMarket API powers a small e-commerce flow: product browsing, user accounts, cart checkout, Stripe payments (sandbox), admin management, image uploads, stock reservation and order expiration.

The frontend is deployed separately. This repository contains the backend API, database model, Docker setup and backend tests.

## Features

- User registration, login, logout and refresh-token sessions.
- JWT authentication stored in secure HTTP-only cookies.
- CSRF protection using a readable CSRF token and `X-CSRF-Token` header.
- Product and category catalog with pagination, sorting and filters.
- Admin dashboard endpoints for products, categories, users and orders.
- Product image uploads through Cloudinary.
- Stripe Checkout integration and webhook handling.
- Order stock reservation, payment confirmation, cancellation, refunds and expiration.
- Background service that expires unpaid orders and releases reserved stock.
- Redis-backed cache for catalog data.
- Global and endpoint-specific rate limiting.
- Consistent JSON error responses through middleware.

## Tech Stack

- ASP.NET Core 10
- Entity Framework Core 10
- PostgreSQL
- Redis
- Stripe.net
- Cloudinary upload API
- Docker and Docker Compose
- xUnit

## Project Structure

```text
AllMarketAPI/
|-- Constants/
|-- Features/
|   |-- Admin/
|   |-- Auth/
|   |-- Categories/
|   |-- Orders/
|   |-- Payments/
|   |-- Products/
|   `-- Users/
|-- Infrastructure/
|   |-- BackgroundServices/
|   |-- Caching/
|   |-- Data/
|   |-- Images/
|   |-- Middleware/
|   `-- Security/
|-- Migrations/
|-- AllMarketAPI.Tests/
|-- docker-compose.yml
|-- docker-compose.prod.yml
`-- Dockerfile
```

## Requirements

- .NET SDK 10
- Docker Desktop
- Stripe test account
- Cloudinary account
- ngrok account, only for the production-style compose file

## Environment Variables

Create a local `.env` file from the example:

```powershell
Copy-Item .env.example .env
```

Then fill the required values:

```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_password
POSTGRES_DB=AllMarketDb
DB_CONNECTION=Host=postgres;Port=5432;Database=AllMarketDb;Username=postgres;Password=your_password

JWT__SECRETKEY=super_secret_key_minimum_32_character
JWT__ISSUER=AllMarketAPI
JWT__AUDIENCE=AllMarketClient
JWT__EXPIRATIONMINUTES=60
JWT__REFRESHTOKENEXPIRATIONDAYS=7

Cors__AllowedOrigins__0=http://localhost:3000

STRIPE_SECRET_KEY=sk_test_your_secret_key
STRIPE_WEBHOOK_SECRET=whsec_your_webhook_secret
FRONTEND_URL=http://localhost:3000

CLOUDINARY_URL=cloudinary://<your_api_key>:<your_api_secret>@<your_cloudinary_name>
```

For production-style Docker deployment with ngrok, also set:

```env
NGROK_AUTHTOKEN=your_ngrok_authtoken
NGROK_DOMAIN=your-domain.ngrok-free.dev
```

## Run Locally With Docker

```powershell
docker compose up --build -d
```

The API will be available at:

```text
http://localhost:5095
```

Development mode exposes the OpenAPI document at:

```text
http://localhost:5095/openapi/v1.json
```

To stop the containers:

```powershell
docker compose down
```

## Run Without Docker

Use this only if PostgreSQL and Redis are already running locally and your connection strings point to them.

```powershell
dotnet restore
dotnet build --no-restore
dotnet run
```

## Production-Style Docker Compose

The production compose file runs the API, PostgreSQL, Redis and ngrok:

```powershell
docker compose -f docker-compose.prod.yml up -d --build
```

In this setup only ngrok exposes the API publicly. PostgreSQL and Redis stay inside the Docker network.

## Main API Routes

| Area | Routes |
| --- | --- |
| Auth | `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout`, `GET /api/auth/csrf` |
| Products | `GET /api/products`, `GET /api/products/{productId}` |
| Categories | `GET /api/categories` |
| Users | `GET /api/users/me`, `PATCH /api/users/update`, `PUT /api/users/me/password`, `GET /api/users/me/history` |
| Orders | `POST /api/orders/checkout` |
| Payments | `POST /api/payments/checkout/{orderId}`, `POST /api/payments/stripe/webhook` |
| Admin Products | `GET /api/admin/products`, `POST /api/admin/products`, `PUT /api/admin/products/{productId}`, `PUT /api/admin/products/{productId}/status`, `DELETE /api/admin/products/{productId}` |
| Admin Categories | `GET /api/admin/categories`, `POST /api/admin/categories`, `PUT /api/admin/categories`, `DELETE /api/admin/categories/{categoryId}` |
| Admin Users | `GET /api/admin/users`, `GET /api/admin/users/{userId}`, `PUT /api/admin/users/{userId}/status` |
| Admin Orders | `GET /api/admin/orders`, `GET /api/admin/orders/{orderId}`, `PUT /api/admin/orders/{orderId}/status`, `POST /api/admin/orders/{orderId}/refund` |

## Authentication And CSRF

The API uses cookies for authentication:

- `access_token`: HTTP-only JWT cookie.
- `refresh_token`: HTTP-only refresh token cookie scoped to `/api/auth`.
- `csrf_token`: readable CSRF cookie.

For unsafe requests (`POST`, `PUT`, `PATCH`, `DELETE`), the frontend must send:

```http
X-CSRF-Token: <csrf_token>
```

Stripe webhooks are excluded from CSRF validation because Stripe signs requests with the `Stripe-Signature` header.

## Payments

Stripe is configured for test mode through environment variables:

- `STRIPE_SECRET_KEY`
- `STRIPE_WEBHOOK_SECRET`
- `FRONTEND_URL`

Checkout sessions are created by the backend. Webhooks confirm successful payments and update order status.

## Images

Product images are uploaded by the backend using:

```env
CLOUDINARY_URL=cloudinary://<api_key>:<api_secret>@<cloud_name>
```

## Tests

The test project covers critical backend flows:

- Authentication and JWT generation.
- Orders and stock reservation.
- Product service behavior.
- User password changes.
- CSRF middleware.
- Order expiration background service.

Run tests with:

```powershell
dotnet test
```

## Notes

- This backend is part of a portfolio project, not a production business.
- Payments should stay in Stripe test mode.
- Secrets belong in `.env` or the hosting provider environment, never in Git.