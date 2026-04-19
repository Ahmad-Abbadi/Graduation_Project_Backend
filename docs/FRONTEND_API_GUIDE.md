# Frontend API Integration Guide

This document is the frontend handoff for the ASP.NET Core backend. It documents the API routes implemented in `Controllers/`, the JSON fields expected by the current DTOs, session handling, manager access rules, common responses, and frontend usage examples.

## Base URLs

Development profiles from `Properties/launchSettings.json`:

| Profile | Base URL |
| --- | --- |
| HTTPS | `https://localhost:7280` |
| HTTP | `http://localhost:5239` |

Swagger is enabled at:

```text
GET {baseUrl}/swagger
```

The checked-in HTTP client examples are in `Graduation_Project_Backend.http`.

Current `Program.cs` does not configure CORS. If the frontend runs on a different origin, for example Vite/React at `http://localhost:5173`, browser requests will need either:

- A frontend dev proxy to the backend.
- A backend CORS policy added before frontend browser integration.

## JSON And Headers

Use JSON for request bodies:

```http
Content-Type: application/json
```

Protected endpoints require a session header:

```http
X-Session-Id: {sessionId}
```

The backend uses ASP.NET Core JSON defaults. Fields are camelCase in JSON. The `MallID` C# property serializes as `mallID`, so send `mallID` in auth requests.

Dates are ISO 8601 strings. Prefer UTC:

```json
"2026-04-18T09:00:00Z"
```

## Session Flow

1. User registers, logs in, or manager quick-logs in.
2. Backend returns `sessionId`.
3. Store the `sessionId` on the client.
4. Send `X-Session-Id: {sessionId}` on all protected requests.
5. For realtime points only, pass the same session ID as a query parameter: `GET /api/realtime/points-stream?sessionId=...`.
6. On logout, call `POST /api/auth/logout` with `{ "sessionId": "..." }`, then remove it from client storage.

### Auth Response

Returned by register, login, and manager quick login:

```json
{
  "message": "Logged in successfully.",
  "userId": "11111111-1111-1111-1111-111111111111",
  "phoneNumber": "+962791234567",
  "name": "Test User",
  "totalPoints": 0,
  "role": "user",
  "sessionId": "session-token"
}
```

## Error Shapes

Several newer endpoints return a structured API error:

```json
{
  "success": false,
  "error": {
    "code": "INVALID_SESSION",
    "message": "Session id is invalid or expired."
  }
}
```

Session errors:

| Status | Code | Meaning |
| --- | --- | --- |
| `401` | `SESSION_ID_REQUIRED` | `X-Session-Id` header was missing or empty. |
| `401` | `INVALID_SESSION` | Session ID is invalid or expired. |

Some legacy validation branches return plain strings, for example:

```json
"Coupon not found"
```

ASP.NET model validation can return `400` ProblemDetails:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "email": ["The Email field is not a valid e-mail address."]
  }
}
```

Frontend error handling should support all three shapes:

- Structured `{ success, error: { code, message } }`
- Plain string response bodies
- ASP.NET validation problem details with `errors`

## Endpoint Summary

| Method | Path | Auth | Main Use |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | No | Register user or manager account. |
| `POST` | `/api/auth/login` | No | Login using phone, password, mall. |
| `POST` | `/api/auth/manager-quick-login` | No | Manager login by manager ID. |
| `POST` | `/api/auth/logout` | Body session ID | Delete session. |
| `GET` | `/api/userinfo/points` | Session | Current user points. |
| `GET` | `/api/realtime/points-stream` | Query session ID | SSE points updates. |
| `GET` | `/api/coupons` | Session | Visible coupons, optional active filter. |
| `GET` | `/api/coupons/{id}` | Session | Coupon details. |
| `POST` | `/api/coupons/redeem` | Session | User activates a coupon and receives a serial. |
| `POST` | `/api/coupons/redeem-by-serial` | No | Staff redeems a serial number. |
| `GET` | `/api/coupons/user` | Session | Current user's activated coupons. |
| `POST` | `/api/transactions` | No | Add receipt/transaction by phone and store. |
| `GET` | `/api/transactions/{id}` | Session | Receipt details by transaction ID. |
| `GET` | `/api/transactions/my-receipts` | Session | Current user's receipt list. |
| `GET` | `/api/stores` | Session | Visible stores in current mall. |
| `GET` | `/api/stores/{id}` | Session | Visible store details. |
| `GET` | `/api/stores/manage` | Manager session | Stores for mall-wide manager. |
| `POST` | `/api/stores` | Mall-wide manager session | Create store. |
| `PUT` | `/api/stores/{id}` | Mall-wide manager session | Update store. |
| `GET` | `/api/offers` | Session | Active visible offers. |
| `GET` | `/api/offers/manage` | Manager session | Manage offers in manager scope. |
| `POST` | `/api/offers` | Manager session | Create offer. |
| `PUT` | `/api/offers/{id}` | Manager session | Update offer. |
| `PATCH` | `/api/offers/{id}/status` | Manager session | Enable/disable offer. |
| `DELETE` | `/api/offers/{id}` | Manager session | Delete offer. |
| `GET` | `/api/announcements` | Session | Active visible announcements. |
| `GET` | `/api/announcements/manage` | Manager session | Manage announcements in manager scope. |
| `POST` | `/api/announcements` | Manager session | Create announcement. |
| `PUT` | `/api/announcements/{id}` | Manager session | Update announcement. |
| `PATCH` | `/api/announcements/{id}/status` | Manager session | Enable/disable announcement. |
| `PATCH` | `/api/announcements/{id}/pin` | Manager session | Pin/unpin announcement. |
| `DELETE` | `/api/announcements/{id}` | Manager session | Delete announcement. |
| `POST` | `/api/chatbot/ask` | Public | Ask chatbot question. |
| `GET` | `/api/chatbot/history` | Public | Returns an empty list because the simplified chatbot does not use the database. |
| `GET` | `/api/dashboard/summary` | Manager session | Dashboard summary metrics. |
| `GET` | `/api/dashboard/sales` | Manager session | Sales metrics. |
| `GET` | `/api/dashboard/points` | Manager session | Points metrics. |
| `GET` | `/api/dashboard/coupons` | Manager session | Coupon metrics. |
| `GET` | `/api/dashboard/activity` | Manager session | Activity metrics. |

## Auth APIs

### Register

```http
POST /api/auth/register
Content-Type: application/json
```

User registration body:

```json
{
  "name": "Test User",
  "phoneNumber": "0791234567",
  "password": "123456",
  "mallID": "00000000-0000-0000-0000-000000000001"
}
```

Manager registration body:

```json
{
  "phoneNumber": "0791234567",
  "password": "123456",
  "mallID": "00000000-0000-0000-0000-000000000001",
  "managerId": "491ead79-af30-44f6-ac53-d835e5889e72"
}
```

Notes:

- `name` is required for normal user registration.
- `managerId` is optional. If present, it must exist in `managers` and belong to the selected mall.
- Phone numbers must be Jordanian mobile numbers. Accepted formats include `0791234567`, `962791234567`, and `+962791234567`.
- Response is `AuthResponse`.

Common errors:

| Status | Code |
| --- | --- |
| `400` | `INVALID_BODY`, `NAME_REQUIRED`, `PHONE_NUMBER_REQUIRED`, `PASSWORD_REQUIRED`, `MALL_ID_REQUIRED`, `INVALID_PHONE_NUMBER`, `MANAGER_ID_INVALID`, `MANAGER_NOT_FOUND`, `MANAGER_MALL_MISMATCH` |
| `409` | `USER_ALREADY_EXISTS`, `PHONE_ALREADY_REGISTERED`, `MANAGER_ALREADY_REGISTERED` |

### Login

```http
POST /api/auth/login
Content-Type: application/json
```

```json
{
  "phoneNumber": "0791234567",
  "password": "123456",
  "mallID": "00000000-0000-0000-0000-000000000001"
}
```

Response is `AuthResponse`.

Common errors:

| Status | Code |
| --- | --- |
| `400` | `INVALID_BODY`, `PHONE_NUMBER_REQUIRED`, `PASSWORD_REQUIRED`, `MALL_ID_REQUIRED`, `INVALID_PHONE_NUMBER` |
| `401` | `INVALID_CREDENTIALS` |

### Manager Quick Login

```http
POST /api/auth/manager-quick-login
Content-Type: application/json
```

```json
{
  "managerId": "491ead79-af30-44f6-ac53-d835e5889e72"
}
```

Response is `AuthResponse`.

This endpoint creates or updates a matching `UserProfile` for the manager and returns a session. It does not require phone/password.

### Logout

```http
POST /api/auth/logout
Content-Type: application/json
```

```json
{
  "sessionId": "session-token"
}
```

Success:

```json
{
  "message": "Logged out successfully."
}
```

Errors:

| Status | Code |
| --- | --- |
| `400` | `SESSION_ID_REQUIRED` |
| `404` | `SESSION_NOT_FOUND` |

## User Info And Realtime

### Get Current User Points

```http
GET /api/userinfo/points
X-Session-Id: {sessionId}
```

Success:

```json
{
  "totalPoints": 1250
}
```

### Realtime Points Stream

```http
GET /api/realtime/points-stream?sessionId={sessionId}
Accept: text/event-stream
```

This is Server-Sent Events (SSE), not JSON polling. The stream sends an initial event and then sends future point updates.

Event type:

```text
points-updated
```

Event payload:

```json
{
  "userId": "11111111-1111-1111-1111-111111111111",
  "totalPoints": 1400,
  "source": "transaction",
  "occurredAtUtc": "2026-04-19T15:30:00Z"
}
```

Browser example:

```js
const source = new EventSource(`${baseUrl}/api/realtime/points-stream?sessionId=${encodeURIComponent(sessionId)}`);

source.addEventListener("points-updated", (event) => {
  const payload = JSON.parse(event.data);
  setTotalPoints(payload.totalPoints);
});
```

Errors before the stream starts:

| Status | Code |
| --- | --- |
| `401` | `SESSION_ID_REQUIRED`, `INVALID_SESSION` |
| `404` | `USER_NOT_FOUND` |

## Coupon APIs

### Coupon Entity Response

`GET /api/coupons` currently returns database coupon entities:

```json
{
  "id": "00000000-0000-0000-0000-000000000002",
  "createdAt": "2026-04-01T09:00:00Z",
  "managerId": "491ead79-af30-44f6-ac53-d835e5889e72",
  "type": "Discount",
  "startAt": "2026-04-01T09:00:00Z",
  "endAt": "2026-04-30T21:00:00Z",
  "discription": "10% off selected items",
  "isActive": true,
  "costPoint": 500,
  "mallID": "00000000-0000-0000-0000-000000000001"
}
```

Note the property is currently spelled `discription` in the entity response.
Current implementation does not scope coupon list/details by the authenticated user's mall. It applies only the optional active/date filter.

### Get Coupons

```http
GET /api/coupons?isActive=true
X-Session-Id: {sessionId}
```

Query:

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `isActive` | boolean | No | If provided, filters to coupons where `isActive` matches and the coupon is currently inside its date range. |

Success:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000002",
    "type": "Discount",
    "discription": "10% off selected items",
    "isActive": true,
    "costPoint": 500
  }
]
```

### Get Coupon Details

```http
GET /api/coupons/{couponId}
X-Session-Id: {sessionId}
```

Success:

```json
{
  "id": "00000000-0000-0000-0000-000000000002",
  "type": "Discount",
  "description": "10% off selected items",
  "startAt": "2026-04-01T09:00:00Z",
  "endAt": "2026-04-30T21:00:00Z",
  "isActive": true,
  "costPoint": 500,
  "createdAt": "2026-04-01T09:00:00Z",
  "managerId": "491ead79-af30-44f6-ac53-d835e5889e72"
}
```

Not found returns `404` with plain string:

```json
"Coupon not found."
```

### Activate Coupon For Current User

```http
POST /api/coupons/redeem
Content-Type: application/json
X-Session-Id: {sessionId}
```

```json
{
  "couponId": "00000000-0000-0000-0000-000000000002"
}
```

Success:

```json
{
  "message": "Coupon redeemed successfully",
  "serial_number": "12345678"
}
```

Important behavior:

- This creates a `UserCoupon` serial for the current user.
- The serial starts as `isRedeemed = false`.
- If the coupon has `costPoint`, points are deducted immediately.
- Point updates are published to the SSE stream with source `coupon_redeem`.

Possible plain string errors include `Coupon not found`, `Coupon is not active`, `Coupon outside redeem period`, `User not found`, and `Not enough points`.

### Redeem Coupon By Serial

```http
POST /api/coupons/redeem-by-serial
Content-Type: application/json
```

```json
{
  "serialNumber": "12345678"
}
```

Success:

```json
{
  "message": "Coupon redeemed successfully",
  "serial_number": "12345678"
}
```

This endpoint has no session requirement in the current controller. Use it for staff/cashier serial redemption flows only if the product accepts that open access behavior.

Possible plain string errors include `Serial number is required.`, `Coupon serial not found`, `Coupon already redeemed`, `Coupon not found`, `Coupon is not active`, and `Coupon outside redeem period`.

### Get Current User Coupons

```http
GET /api/coupons/user
X-Session-Id: {sessionId}
```

Success:

```json
[
  {
    "serialNumber": "12345678",
    "couponId": "00000000-0000-0000-0000-000000000002",
    "couponType": "Discount",
    "couponDescription": "10% off selected items",
    "isRedeemed": false,
    "validFrom": "2026-04-01T09:00:00Z",
    "validUntil": "2026-04-30T21:00:00Z",
    "createdAt": "2026-04-19T15:20:00Z"
  }
]
```

## Transaction And Receipt APIs

### Add Transaction

```http
POST /api/transactions
Content-Type: application/json
```

```json
{
  "phoneNumber": "0791234567",
  "storeId": "00000000-0000-0000-0000-000000000003",
  "receiptId": "receipt-001",
  "receiptDescription": "Test receipt",
  "price": 19.99
}
```

Success status: `201 Created`

```json
{
  "transactionId": 1,
  "userId": "11111111-1111-1111-1111-111111111111",
  "storeId": "00000000-0000-0000-0000-000000000003",
  "receiptId": "receipt-001",
  "price": 19.99,
  "points": 1999,
  "newTotalPoints": 3249,
  "createdAt": "2026-04-19T15:22:00Z"
}
```

Important behavior:

- No session is required in the current controller.
- Phone number is normalized and matched to a user in the same mall as the store.
- Points are calculated as `(int)(price * 100)`.
- Duplicate `receiptId` is rejected.
- Point updates are published to the SSE stream with source `transaction`.

Possible plain string errors include `Price cannot be negative.`, `Phone number is required.`, `Receipt ID is required.`, `Store ID is required.`, `Receipt ID already exists`, `Store not found`, and `User not found`.

### Get Receipt Details

```http
GET /api/transactions/{transactionId}
X-Session-Id: {sessionId}
```

Access:

- Normal users can access only their own receipts.
- Managers can access receipts in their mall/store scope.

Success:

```json
{
  "transactionId": 1,
  "userId": "11111111-1111-1111-1111-111111111111",
  "receiptId": "receipt-001",
  "receiptDescription": "Test receipt",
  "receiptUrl": null,
  "receiptImageUrl": null,
  "storeId": "00000000-0000-0000-0000-000000000003",
  "storeName": "New Shop",
  "mallID": "00000000-0000-0000-0000-000000000001",
  "price": 19.99,
  "pointsEarned": 1999,
  "status": "completed",
  "createdAt": "2026-04-19T15:22:00Z"
}
```

Errors:

| Status | Shape |
| --- | --- |
| `404` | Plain string: `"Transaction not found."` |
| `403` | Structured error with code `RECEIPT_ACCESS_DENIED` |

### Get My Receipts

```http
GET /api/transactions/my-receipts?page=1&pageSize=20
X-Session-Id: {sessionId}
```

Query:

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `page` | int | No | Default `1`, minimum `1`. |
| `pageSize` | int | No | Default `20`, range `1..100`. |
| `storeId` | guid | No | Filter by store. |
| `status` | string | No | Exact match against transaction status, for example `completed`. |
| `from` | datetime | No | Inclusive lower bound. |
| `to` | datetime | No | Inclusive upper bound. |

Success:

```json
{
  "items": [
    {
      "transactionId": 1,
      "receiptId": "receipt-001",
      "storeId": "00000000-0000-0000-0000-000000000003",
      "storeName": "New Shop",
      "price": 19.99,
      "pointsEarned": 1999,
      "receiptDescription": "Test receipt",
      "receiptImageUrl": null,
      "status": "completed",
      "createdAt": "2026-04-19T15:22:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1
}
```

If `from > to`, returns `400` structured error with code `INVALID_DATE_RANGE`.

## Store APIs

### Store Response

```json
{
  "id": "00000000-0000-0000-0000-000000000003",
  "name": "New Shop",
  "mallID": "00000000-0000-0000-0000-000000000001",
  "operatingHours": "9 AM - 10 PM",
  "socialMediaLinks": {
    "instagram": "https://instagram.com/newshop"
  },
  "description": "New test store",
  "phoneNumber": "+962799999999",
  "email": "newshop@example.com",
  "floorNumber": "Second Floor",
  "storeImageUrl": "https://example.com/store.png",
  "categories": [
    {
      "id": 11,
      "name": "Fashion"
    }
  ]
}
```

### Get Stores

```http
GET /api/stores
X-Session-Id: {sessionId}
```

Returns stores in the current user's mall.

Success:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000003",
    "name": "New Shop",
    "mallID": "00000000-0000-0000-0000-000000000001",
    "categories": []
  }
]
```

### Get Store By ID

```http
GET /api/stores/{storeId}
X-Session-Id: {sessionId}
```

Returns one visible store in the current user's mall.

Not found returns `404` with plain string:

```json
"Store not found."
```

### Get Managed Stores

```http
GET /api/stores/manage
X-Session-Id: {sessionId}
```

Requires mall-wide manager access.

Success:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000003",
    "name": "New Shop",
    "floorNumber": "Second Floor",
    "phoneNumber": "+962799999999",
    "email": "newshop@example.com",
    "categories": ["Fashion", "Shoes"]
  }
]
```

### Create Store

```http
POST /api/stores
Content-Type: application/json
X-Session-Id: {sessionId}
```

Requires mall-wide manager access.

```json
{
  "name": "New Shop",
  "operatingHours": "9 AM - 10 PM",
  "socialMediaLinks": {
    "instagram": "https://instagram.com/newshop"
  },
  "description": "New test store",
  "phoneNumber": "+962799999999",
  "email": "newshop@example.com",
  "floorNumber": "Second Floor",
  "storeImageUrl": "https://example.com/store.png",
  "categoryIds": [11, 12]
}
```

Success status: `201 Created`, body is `StoreResponse`.

Validation:

| Field | Rule |
| --- | --- |
| `name` | Required, max length 200. |
| `operatingHours` | Optional, max length 200. |
| `description` | Optional, max length 2000. |
| `phoneNumber` | Optional, max length 50. |
| `email` | Optional valid email, max length 200. |
| `floorNumber` | Optional, max length 50. |
| `storeImageUrl` | Optional, max length 1000. |
| `categoryIds` | Optional. IDs must belong to the manager mall. Non-positive IDs are ignored. |
| `socialMediaLinks` | Optional JSON object/value. |

Common structured errors:

| Status | Code |
| --- | --- |
| `400` | `VALUE_REQUIRED`, `INVALID_CATEGORY_IDS` |
| `403` | `MANAGER_REQUIRED`, `MALL_MANAGER_REQUIRED` |

### Update Store

```http
PUT /api/stores/{storeId}
Content-Type: application/json
X-Session-Id: {sessionId}
```

Requires mall-wide manager access. Body is the same as create. Success body is `StoreResponse`.

Common structured errors:

| Status | Code |
| --- | --- |
| `400` | `VALUE_REQUIRED`, `INVALID_CATEGORY_IDS` |
| `403` | `MANAGER_REQUIRED`, `MALL_MANAGER_REQUIRED` |
| `404` | `STORE_NOT_FOUND` |

## Offer APIs

### Offer Response

```json
{
  "id": 1,
  "mallID": "00000000-0000-0000-0000-000000000001",
  "storeId": "00000000-0000-0000-0000-000000000003",
  "storeName": "New Shop",
  "title": "Weekend Sale",
  "description": "20% off selected items",
  "startAt": "2026-04-18T09:00:00Z",
  "endAt": "2026-04-25T21:00:00Z",
  "isActive": true,
  "madeAt": "2026-04-19T15:30:00Z"
}
```

### Get Visible Offers

```http
GET /api/offers
X-Session-Id: {sessionId}
```

Returns active offers for the user's mall where `startAt <= now <= endAt`.

Success:

```json
[
  {
    "id": 1,
    "storeName": "New Shop",
    "title": "Weekend Sale",
    "isActive": true
  }
]
```

### Get Managed Offers

```http
GET /api/offers/manage
X-Session-Id: {sessionId}
```

Requires manager access. Mall-wide managers see all mall offers. Store managers see only offers for assigned stores.

Success:

```json
[
  {
    "id": 1,
    "storeId": "00000000-0000-0000-0000-000000000003",
    "storeName": "New Shop",
    "title": "Weekend Sale",
    "isActive": true,
    "startAt": "2026-04-18T09:00:00Z",
    "endAt": "2026-04-25T21:00:00Z"
  }
]
```

### Create Offer

```http
POST /api/offers
Content-Type: application/json
X-Session-Id: {sessionId}
```

Requires manager access.

```json
{
  "storeId": "00000000-0000-0000-0000-000000000003",
  "title": "Weekend Sale",
  "description": "20% off selected items",
  "startAt": "2026-04-18T09:00:00Z",
  "endAt": "2026-04-25T21:00:00Z",
  "isActive": true
}
```

Success status: `201 Created`, body is `OfferResponse`.

Validation:

| Field | Rule |
| --- | --- |
| `storeId` | Required. Store must exist and be in manager scope. |
| `title` | Required, max length 200. |
| `description` | Optional, max length 2000. |
| `startAt` | Required datetime. |
| `endAt` | Required datetime, must be same or later than `startAt`. |
| `isActive` | Optional, defaults to `true` if omitted. |

Common structured errors:

| Status | Code |
| --- | --- |
| `400` | `INVALID_STORE`, `INVALID_DATE_RANGE`, `VALUE_REQUIRED` |
| `403` | `MANAGER_REQUIRED`, `STORE_OUTSIDE_SCOPE` |

### Update Offer

```http
PUT /api/offers/{offerId}
Content-Type: application/json
X-Session-Id: {sessionId}
```

Body is the same as create. Success body is `OfferResponse`.

Common structured errors:

| Status | Code |
| --- | --- |
| `400` | `INVALID_STORE`, `INVALID_DATE_RANGE`, `VALUE_REQUIRED` |
| `403` | `MANAGER_REQUIRED`, `STORE_OUTSIDE_SCOPE` |
| `404` | `OFFER_NOT_FOUND` |

### Set Offer Status

```http
PATCH /api/offers/{offerId}/status
Content-Type: application/json
X-Session-Id: {sessionId}
```

```json
{
  "isActive": false
}
```

Success body is `OfferResponse`.

### Delete Offer

```http
DELETE /api/offers/{offerId}
X-Session-Id: {sessionId}
```

Success status: `204 No Content`.

## Announcement APIs

### Announcement Response

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "mallID": "00000000-0000-0000-0000-000000000001",
  "storeId": "00000000-0000-0000-0000-000000000003",
  "storeName": "New Shop",
  "managerId": "491ead79-af30-44f6-ac53-d835e5889e72",
  "title": "Mall Event",
  "content": "Live music this weekend",
  "announcementType": "event",
  "priority": "high",
  "isActive": true,
  "isPinned": true,
  "imageUrl": "https://example.com/event.png",
  "startDate": "2026-04-18T09:00:00Z",
  "endDate": "2026-04-22T21:00:00Z",
  "createdAt": "2026-04-19T15:30:00Z",
  "updatedAt": "2026-04-19T15:30:00Z"
}
```

### Get Visible Announcements

```http
GET /api/announcements
X-Session-Id: {sessionId}
```

Returns active announcements for the user's mall where `startDate <= now <= endDate`. Results are ordered by pinned first, then high priority, then latest start date.

### Get Managed Announcements

```http
GET /api/announcements/manage
X-Session-Id: {sessionId}
```

Requires manager access. Mall-wide managers see all mall announcements. Store managers see only store-specific announcements for assigned stores.

Success:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000001",
    "storeId": "00000000-0000-0000-0000-000000000003",
    "storeName": "New Shop",
    "title": "Mall Event",
    "priority": "high",
    "isActive": true,
    "isPinned": true,
    "startDate": "2026-04-18T09:00:00Z",
    "endDate": "2026-04-22T21:00:00Z"
  }
]
```

### Create Announcement

```http
POST /api/announcements
Content-Type: application/json
X-Session-Id: {sessionId}
```

Requires manager access.

```json
{
  "storeId": "00000000-0000-0000-0000-000000000003",
  "title": "Mall Event",
  "content": "Live music this weekend",
  "announcementType": "event",
  "priority": "high",
  "startDate": "2026-04-18T09:00:00Z",
  "endDate": "2026-04-22T21:00:00Z",
  "isActive": true,
  "isPinned": true,
  "imageUrl": "https://example.com/event.png"
}
```

For mall-wide announcements, mall-wide managers can send `"storeId": null` or omit it. Store managers must provide a `storeId` in their assigned stores.

Success status: `201 Created`, body is `AnnouncementResponse`.

Validation:

| Field | Rule |
| --- | --- |
| `storeId` | Optional for mall-wide managers. Required for store managers. If provided, store must exist and be in manager scope. |
| `title` | Required, max length 200. |
| `content` | Required, max length 4000. |
| `announcementType` | Optional, default `general`, max length 100. |
| `priority` | Optional, default `normal`, max length 50. |
| `isActive` | Optional, defaults to `true`. |
| `isPinned` | Optional, defaults to `false`. |
| `imageUrl` | Optional, max length 1000. |
| `startDate` | Required datetime. |
| `endDate` | Required datetime, must be same or later than `startDate`. |

Common structured errors:

| Status | Code |
| --- | --- |
| `400` | `INVALID_STORE`, `INVALID_DATE_RANGE`, `VALUE_REQUIRED` |
| `403` | `MANAGER_REQUIRED`, `STORE_SCOPE_REQUIRED`, `STORE_OUTSIDE_SCOPE` |

### Update Announcement

```http
PUT /api/announcements/{announcementId}
Content-Type: application/json
X-Session-Id: {sessionId}
```

Body is the same as create. Success body is `AnnouncementResponse`.

Common structured errors:

| Status | Code |
| --- | --- |
| `400` | `INVALID_STORE`, `INVALID_DATE_RANGE`, `VALUE_REQUIRED` |
| `403` | `MANAGER_REQUIRED`, `STORE_SCOPE_REQUIRED`, `STORE_OUTSIDE_SCOPE` |
| `404` | `ANNOUNCEMENT_NOT_FOUND` |

### Set Announcement Status

```http
PATCH /api/announcements/{announcementId}/status
Content-Type: application/json
X-Session-Id: {sessionId}
```

```json
{
  "isActive": false
}
```

Success body is `AnnouncementResponse`.

### Set Announcement Pin

```http
PATCH /api/announcements/{announcementId}/pin
Content-Type: application/json
X-Session-Id: {sessionId}
```

```json
{
  "isPinned": true
}
```

Success body is `AnnouncementResponse`.

### Delete Announcement

```http
DELETE /api/announcements/{announcementId}
X-Session-Id: {sessionId}
```

Success status: `204 No Content`.

## Chatbot APIs

### Ask Chatbot

```http
POST /api/chatbot/ask
Content-Type: application/json
```

```json
{
  "msg": "What are the mall opening hours?"
}
```

Fields:

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `msg` | string | Yes | Max length 1000. |

Success:

```json
{
  "conversationId": "22222222-2222-2222-2222-222222222222",
  "conversationSessionId": "33333333-3333-3333-3333-333333333333",
  "userMessage": "What are the mall opening hours?",
  "botResponse": "Mall hours: Sunday: 10 AM - 10 PM.",
  "matchedFaqId": null,
  "matchSource": "ai_model",
  "responseTimeMs": 12,
  "createdAt": "2026-04-19T15:35:00Z"
}
```

Common structured errors:

| Status | Code |
| --- | --- |
| `400` | `VALUE_REQUIRED` |

### Get Chatbot History

```http
GET /api/chatbot/history
```

The simplified chatbot does not use the database, so this endpoint returns an empty list.

Success:

```json
[]
```

## Dashboard APIs

All dashboard routes require manager access. Normal users receive `403` with code `MANAGER_REQUIRED`.

All dashboard routes accept an optional date range:

| Query | Type | Required | Notes |
| --- | --- | --- | --- |
| `from` | datetime | No | Inclusive lower bound. |
| `to` | datetime | No | Inclusive upper bound. |

Example:

```http
GET /api/dashboard/summary?from=2026-04-01T00:00:00Z&to=2026-04-30T23:59:59Z
X-Session-Id: {sessionId}
```

If `from > to`, returns `400` structured error with code `INVALID_DATE_RANGE`.

### Dashboard Summary

```http
GET /api/dashboard/summary
X-Session-Id: {sessionId}
```

Success:

```json
{
  "totalTransactions": 18,
  "totalSalesAmount": 450.75,
  "totalPointsIssued": 45075,
  "totalPointsRedeemed": 1500,
  "activeOffersCount": 3,
  "activeAnnouncementsCount": 2,
  "redeemedCouponsCount": 4,
  "activatedCouponsCount": 9
}
```

### Dashboard Sales

```http
GET /api/dashboard/sales
X-Session-Id: {sessionId}
```

Success:

```json
{
  "totalSalesAmount": 450.75,
  "totalTransactions": 18,
  "dailySales": [
    {
      "date": "2026-04-19T00:00:00",
      "salesAmount": 120.5,
      "transactionsCount": 5
    }
  ],
  "topStores": [
    {
      "storeId": "00000000-0000-0000-0000-000000000003",
      "storeName": "New Shop",
      "salesAmount": 220.75,
      "transactionsCount": 9
    }
  ]
}
```

### Dashboard Points

```http
GET /api/dashboard/points
X-Session-Id: {sessionId}
```

Success:

```json
{
  "totalPointsIssued": 45075,
  "totalPointsRedeemed": 1500,
  "dailyIssued": [
    {
      "date": "2026-04-19T00:00:00",
      "pointsIssued": 12050
    }
  ],
  "dailyRedeemed": [
    {
      "date": "2026-04-19T00:00:00",
      "pointsRedeemed": 500
    }
  ]
}
```

### Dashboard Coupons

```http
GET /api/dashboard/coupons
X-Session-Id: {sessionId}
```

Success for mall-wide managers:

```json
{
  "isScopeLimited": false,
  "totalActiveCoupons": 3,
  "totalActivatedUserCoupons": 9,
  "totalRedeemedCoupons": 4,
  "redemptionRate": 0.4444
}
```

Success for store-scoped managers:

```json
{
  "isScopeLimited": true,
  "totalActiveCoupons": null,
  "totalActivatedUserCoupons": null,
  "totalRedeemedCoupons": null,
  "redemptionRate": null
}
```

### Dashboard Activity

```http
GET /api/dashboard/activity
X-Session-Id: {sessionId}
```

Success:

```json
{
  "totalOffers": 6,
  "totalAnnouncements": 4,
  "activeOffers": 3,
  "activeAnnouncements": 2,
  "unreadNotifications": 5,
  "recentTransactions": [
    {
      "transactionId": 1,
      "storeId": "00000000-0000-0000-0000-000000000003",
      "storeName": "New Shop",
      "receiptId": "receipt-001",
      "price": 19.99,
      "points": 1999,
      "createdAt": "2026-04-19T15:22:00Z",
      "status": "completed"
    }
  ],
  "categoryDistribution": [
    {
      "categoryId": 11,
      "categoryName": "Fashion",
      "storesCount": 4
    }
  ]
}
```

`unreadNotifications` is only populated for mall-wide managers. It is `null` for store-scoped managers.

## Frontend Client Helpers

### Fetch Wrapper

```js
export async function apiFetch(path, options = {}) {
  const headers = new Headers(options.headers);
  const hasBody = options.body !== undefined && options.body !== null;

  if (hasBody && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const sessionId = getSessionId();
  if (sessionId && !headers.has("X-Session-Id")) {
    headers.set("X-Session-Id", sessionId);
  }

  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers
  });

  const text = await response.text();
  const data = text ? safeJsonParse(text) : null;

  if (!response.ok) {
    throw normalizeApiError(response.status, data);
  }

  return data;
}

function safeJsonParse(text) {
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

function normalizeApiError(status, data) {
  if (data?.error?.message) {
    return { status, code: data.error.code, message: data.error.message, raw: data };
  }

  if (data?.errors) {
    const firstKey = Object.keys(data.errors)[0];
    const firstMessage = firstKey ? data.errors[firstKey][0] : data.title;
    return { status, code: "VALIDATION_ERROR", message: firstMessage || data.title, raw: data };
  }

  if (typeof data === "string") {
    return { status, code: "ERROR", message: data, raw: data };
  }

  return { status, code: "ERROR", message: "Request failed.", raw: data };
}
```

### Login Example

```js
const auth = await apiFetch("/api/auth/login", {
  method: "POST",
  body: JSON.stringify({
    phoneNumber: "0791234567",
    password: "123456",
    mallID: selectedMallId
  })
});

saveSessionId(auth.sessionId);
```

### Authenticated Request Example

```js
const stores = await apiFetch("/api/stores");
```

### Query String Example

```js
const params = new URLSearchParams({
  page: "1",
  pageSize: "20",
  status: "completed"
});

const receipts = await apiFetch(`/api/transactions/my-receipts?${params}`);
```

## Permission Notes For UI

Use `role` from the auth response to decide which areas to show:

- `user`: shopping app features, points, coupons, receipts, stores, offers, announcements, chatbot.
- `manager` or manager-like roles: management features can be shown, but the backend still enforces exact scope.

Backend manager rules:

- A manager exists when the authenticated user ID exists in the `managers` table.
- Mall-wide manager: manager has no rows in `management`; can manage all mall stores, offers, announcements, and sees mall-wide dashboard/coupon metrics.
- Store-scoped manager: manager has assigned stores in `management`; can manage offers and store-specific announcements for assigned stores, can view dashboard metrics for assigned stores, cannot create/update stores, and cannot create mall-wide announcements.

## Implementation Checklist For Frontend

- Persist `sessionId` after auth.
- Attach `X-Session-Id` to protected routes.
- Use `sessionId` query param for SSE.
- Treat `401 SESSION_ID_REQUIRED` and `401 INVALID_SESSION` as logged-out/session-expired states.
- Handle structured errors, plain string errors, and validation ProblemDetails.
- Use `mallID` casing for auth request bodies.
- Use ISO datetime strings for date fields and dashboard filters.
- Do not assume all `GET` lists return the same item shape. Coupons currently differ between list and details (`discription` vs `description`).
- For management screens, handle `403` as a permission/scope denial from the backend.
