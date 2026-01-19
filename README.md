# Graduation Project Backend

Simple ASP.NET Core Web API for our graduation project. It handles user login/registration,
transactions, coupons, and points. Uses PostgreSQL and includes Swagger.

## Tech
- .NET 8
- ASP.NET Core Web API
- Npgsql (PostgreSQL)
- Swagger / OpenAPI

## Getting started
1. Install .NET 8 SDK and have a PostgreSQL database ready.
2. Set the connection string:
   - Update `appsettings.json` or `appsettings.Development.json`
3. Run the API:
```bash
dotnet restore
dotnet run
```
4. Open Swagger at `https://localhost:<port>/swagger`

## Main routes
- POST `/api/auth/login-or-register`
- GET `/api/coupons`
- GET `/api/coupons/{id}`
- POST `/api/coupons/redeem`
- POST `/api/coupons/redeem-by-serial`
- GET `/api/coupons/user/{userId}`
- POST `/api/transactions`
- GET `/api/transactions/{id}`
- GET `/api/userinfo/points/{userId}`
