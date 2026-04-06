# Graduation Project Backend

ASP.NET Core 8 backend for authentication, session-based user access, stores, transactions, coupons, offers, and realtime points updates.

## Stack
- .NET 8
- ASP.NET Core MVC + Web API
- Entity Framework Core
- PostgreSQL
- Swagger / OpenAPI

## Run
1. Set the PostgreSQL connection string in `appsettings.json` or `appsettings.Development.json`.
2. Restore and start the project:

```bash
dotnet restore
dotnet run
```

3. Open:
- Swagger: `https://localhost:<port>/swagger`
- Test UI: `https://localhost:<port>/`

## Main endpoints
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/logout`
- `GET /api/coupons`
- `GET /api/coupons/{id}`
- `POST /api/coupons/redeem`
- `POST /api/coupons/redeem-by-serial`
- `GET /api/coupons/user`
- `GET /api/offers`
- `GET /api/stores`
- `GET /api/stores/{id}`
- `POST /api/transactions`
- `GET /api/transactions/{id}`
- `GET /api/userinfo/points`
- `GET /api/realtime/points-stream?sessionId=...`

## Session-based endpoints
Protected endpoints expect the session id in the `X-Session-Id` request header.
