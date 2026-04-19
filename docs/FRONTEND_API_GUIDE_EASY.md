# Easy Frontend API Guide

This is the simple version of the API guide for the frontend team. Use it when you want to understand how to connect the app without reading backend code.

## 1. Backend URL

When running locally, use one of these:

```text
https://localhost:7280
http://localhost:5239
```

In this document, `{baseUrl}` means one of those URLs.

Example:

```text
{baseUrl}/api/auth/login
```

Becomes:

```text
https://localhost:7280/api/auth/login
```

Swagger is available here:

```text
{baseUrl}/swagger
```

Important: CORS is not configured in the backend yet. If the frontend runs on another port like `http://localhost:5173`, use a frontend proxy or ask backend team to add CORS.

## 2. How Login Works

The backend does not use JWT. It uses a custom session ID.

Simple flow:

1. Call login/register.
2. Backend returns `sessionId`.
3. Save `sessionId` in frontend.
4. Send it in the `X-Session-Id` header for protected APIs.

Header:

```http
X-Session-Id: SESSION_ID_HERE
```

Example fetch:

```js
await fetch(`${baseUrl}/api/stores`, {
  headers: {
    "X-Session-Id": sessionId
  }
});
```

## 3. Common Request Rules

For APIs with a body, always send:

```http
Content-Type: application/json
```

Use camelCase JSON fields:

```json
{
  "phoneNumber": "0791234567",
  "password": "123456",
  "mallID": "00000000-0000-0000-0000-000000000001"
}
```

Note: `mallID` is written exactly like this.

Dates should be ISO strings:

```json
"2026-04-18T09:00:00Z"
```

## 4. Error Handling

The backend can return errors in three formats.

### Format 1: Normal API Error

```json
{
  "success": false,
  "error": {
    "code": "INVALID_SESSION",
    "message": "Session id is invalid or expired."
  }
}
```

### Format 2: Plain Text Error

```json
"Coupon not found"
```

### Format 3: Validation Error

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "email": ["The Email field is not a valid e-mail address."]
  }
}
```

Frontend should support all three.

Important session errors:

| Status | Code | What frontend should do |
| --- | --- | --- |
| `401` | `SESSION_ID_REQUIRED` | User is not logged in or header missing. |
| `401` | `INVALID_SESSION` | Clear saved session and send user to login. |

## 5. Auth APIs

### Register User

```http
POST /api/auth/register
```

Body:

```json
{
  "name": "Test User",
  "phoneNumber": "0791234567",
  "password": "123456",
  "mallID": "00000000-0000-0000-0000-000000000001"
}
```

Response:

```json
{
  "message": "Registered successfully.",
  "userId": "11111111-1111-1111-1111-111111111111",
  "phoneNumber": "+962791234567",
  "name": "Test User",
  "totalPoints": 0,
  "role": "user",
  "sessionId": "SESSION_ID_HERE"
}
```

Save `sessionId`.

### Login User

```http
POST /api/auth/login
```

Body:

```json
{
  "phoneNumber": "0791234567",
  "password": "123456",
  "mallID": "00000000-0000-0000-0000-000000000001"
}
```

Response is the same auth response. Save `sessionId`.

### Manager Quick Login

```http
POST /api/auth/manager-quick-login
```

Body:

```json
{
  "managerId": "491ead79-af30-44f6-ac53-d835e5889e72"
}
```

Response is the same auth response. Save `sessionId`.

### Logout

```http
POST /api/auth/logout
```

Body:

```json
{
  "sessionId": "SESSION_ID_HERE"
}
```

Success:

```json
{
  "message": "Logged out successfully."
}
```

After success, remove session from frontend storage.

## 6. User Points

### Get Current Points

```http
GET /api/userinfo/points
X-Session-Id: SESSION_ID_HERE
```

Response:

```json
{
  "totalPoints": 1250
}
```

### Realtime Points Updates

This endpoint uses Server-Sent Events.

```http
GET /api/realtime/points-stream?sessionId=SESSION_ID_HERE
```

Frontend example:

```js
const source = new EventSource(
  `${baseUrl}/api/realtime/points-stream?sessionId=${encodeURIComponent(sessionId)}`
);

source.addEventListener("points-updated", (event) => {
  const data = JSON.parse(event.data);
  console.log(data.totalPoints);
});
```

Event data:

```json
{
  "userId": "11111111-1111-1111-1111-111111111111",
  "totalPoints": 1400,
  "source": "transaction",
  "occurredAtUtc": "2026-04-19T15:30:00Z"
}
```

## 7. Stores

Stores are mall shops.

### Get Stores

```http
GET /api/stores
X-Session-Id: SESSION_ID_HERE
```

Response:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000003",
    "name": "New Shop",
    "mallID": "00000000-0000-0000-0000-000000000001",
    "operatingHours": "9 AM - 10 PM",
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
]
```

### Get One Store

```http
GET /api/stores/{storeId}
X-Session-Id: SESSION_ID_HERE
```

Example:

```text
GET /api/stores/00000000-0000-0000-0000-000000000003
```

### Manager: Get Stores To Manage

```http
GET /api/stores/manage
X-Session-Id: SESSION_ID_HERE
```

Manager only.

Response:

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

### Manager: Create Store

```http
POST /api/stores
X-Session-Id: SESSION_ID_HERE
Content-Type: application/json
```

Mall-wide manager only.

Body:

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

Response is the full store object.

### Manager: Update Store

```http
PUT /api/stores/{storeId}
X-Session-Id: SESSION_ID_HERE
Content-Type: application/json
```

Body is same as create store.

## 8. Offers

Offers are discounts/promotions.

### Get Visible Offers

```http
GET /api/offers
X-Session-Id: SESSION_ID_HERE
```

Only active offers inside their date range are returned.

Response:

```json
[
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
]
```

### Manager: Get Offers To Manage

```http
GET /api/offers/manage
X-Session-Id: SESSION_ID_HERE
```

Response:

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

### Manager: Create Offer

```http
POST /api/offers
X-Session-Id: SESSION_ID_HERE
Content-Type: application/json
```

Body:

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

Response is the full offer object.

### Manager: Update Offer

```http
PUT /api/offers/{offerId}
X-Session-Id: SESSION_ID_HERE
Content-Type: application/json
```

Body is same as create offer.

### Manager: Enable Or Disable Offer

```http
PATCH /api/offers/{offerId}/status
X-Session-Id: SESSION_ID_HERE
Content-Type: application/json
```

Body:

```json
{
  "isActive": false
}
```

### Manager: Delete Offer

```http
DELETE /api/offers/{offerId}
X-Session-Id: SESSION_ID_HERE
```

Success response has no body.

## 9. Announcements

Announcements are news/events/messages shown to users.

### Get Visible Announcements

```http
GET /api/announcements
X-Session-Id: SESSION_ID_HERE
```

Only active announcements inside their date range are returned.

Response:

```json
[
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
]
```

### Manager: Get Announcements To Manage

```http
GET /api/announcements/manage
X-Session-Id: SESSION_ID_HERE
```

Response:

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

### Manager: Create Announcement

```http
POST /api/announcements
X-Session-Id: SESSION_ID_HERE
Content-Type: application/json
```

Body:

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

Response is the full announcement object.

For mall-wide announcement, mall-wide managers can omit `storeId`.
Store managers must send a `storeId`.

### Manager: Update Announcement

```http
PUT /api/announcements/{announcementId}
X-Session-Id: SESSION_ID_HERE
Content-Type: application/json
```

Body is same as create announcement.

### Manager: Enable Or Disable Announcement

```http
PATCH /api/announcements/{announcementId}/status
X-Session-Id: SESSION_ID_HERE
Content-Type: application/json
```

Body:

```json
{
  "isActive": false
}
```

### Manager: Pin Or Unpin Announcement

```http
PATCH /api/announcements/{announcementId}/pin
X-Session-Id: SESSION_ID_HERE
Content-Type: application/json
```

Body:

```json
{
  "isPinned": true
}
```

### Manager: Delete Announcement

```http
DELETE /api/announcements/{announcementId}
X-Session-Id: SESSION_ID_HERE
```

Success response has no body.

## 10. Coupons

Coupons can be activated by users and later redeemed by serial number.

Important backend note: coupon list returns `discription`, but coupon details returns `description`.

### Get Coupons

```http
GET /api/coupons
X-Session-Id: SESSION_ID_HERE
```

Optional active filter:

```http
GET /api/coupons?isActive=true
```

Response:

```json
[
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
]
```

### Get Coupon Details

```http
GET /api/coupons/{couponId}
X-Session-Id: SESSION_ID_HERE
```

Response:

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

### User: Activate Coupon

```http
POST /api/coupons/redeem
X-Session-Id: SESSION_ID_HERE
Content-Type: application/json
```

Body:

```json
{
  "couponId": "00000000-0000-0000-0000-000000000002"
}
```

Response:

```json
{
  "message": "Coupon redeemed successfully",
  "serial_number": "12345678"
}
```

Meaning: user spent points if the coupon has a point cost, and received a serial number.

### Staff: Redeem Coupon Serial

```http
POST /api/coupons/redeem-by-serial
Content-Type: application/json
```

Body:

```json
{
  "serialNumber": "12345678"
}
```

Response:

```json
{
  "message": "Coupon redeemed successfully",
  "serial_number": "12345678"
}
```

Current backend does not require session for this endpoint.

### User: Get My Coupons

```http
GET /api/coupons/user
X-Session-Id: SESSION_ID_HERE
```

Response:

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

## 11. Transactions And Receipts

Transactions are receipts that give users points.

### Add Transaction

```http
POST /api/transactions
Content-Type: application/json
```

No session is required in current backend.

Body:

```json
{
  "phoneNumber": "0791234567",
  "storeId": "00000000-0000-0000-0000-000000000003",
  "receiptId": "receipt-001",
  "receiptDescription": "Test receipt",
  "price": 19.99
}
```

Response:

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

Points are calculated like this:

```text
points = price * 100
```

Example:

```text
19.99 JOD = 1999 points
```

### Get My Receipts

```http
GET /api/transactions/my-receipts?page=1&pageSize=20
X-Session-Id: SESSION_ID_HERE
```

Optional filters:

```text
storeId
status
from
to
page
pageSize
```

Response:

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

### Get Receipt Details

```http
GET /api/transactions/{transactionId}
X-Session-Id: SESSION_ID_HERE
```

Response:

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

## 12. Chatbot

### Ask Chatbot

```http
POST /api/chatbot/ask
Content-Type: application/json
```

Body:

```json
{
  "msg": "What are the mall opening hours?"
}
```

Response:

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

### Get Chatbot History

```http
GET /api/chatbot/history
```

The simplified chatbot does not use the database, so history is empty:

```json
[]
```

## 13. Dashboard

Dashboard APIs are manager only.

All dashboard APIs can receive optional date filters:

```text
?from=2026-04-01T00:00:00Z&to=2026-04-30T23:59:59Z
```

### Summary

```http
GET /api/dashboard/summary
X-Session-Id: SESSION_ID_HERE
```

Response:

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

### Sales

```http
GET /api/dashboard/sales
X-Session-Id: SESSION_ID_HERE
```

### Points

```http
GET /api/dashboard/points
X-Session-Id: SESSION_ID_HERE
```

### Coupons

```http
GET /api/dashboard/coupons
X-Session-Id: SESSION_ID_HERE
```

### Activity

```http
GET /api/dashboard/activity
X-Session-Id: SESSION_ID_HERE
```

## 14. Recommended Frontend API Helper

Use one helper for all API calls.

```js
async function apiFetch(path, options = {}) {
  const headers = new Headers(options.headers);

  if (options.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const sessionId = localStorage.getItem("sessionId");
  if (sessionId) {
    headers.set("X-Session-Id", sessionId);
  }

  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers
  });

  const text = await response.text();
  const data = text ? safeJsonParse(text) : null;

  if (!response.ok) {
    throw getApiError(response.status, data);
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

function getApiError(status, data) {
  if (data?.error?.message) {
    return {
      status,
      code: data.error.code,
      message: data.error.message
    };
  }

  if (data?.errors) {
    const key = Object.keys(data.errors)[0];
    return {
      status,
      code: "VALIDATION_ERROR",
      message: key ? data.errors[key][0] : data.title
    };
  }

  if (typeof data === "string") {
    return {
      status,
      code: "ERROR",
      message: data
    };
  }

  return {
    status,
    code: "ERROR",
    message: "Request failed."
  };
}
```

Login example:

```js
const auth = await apiFetch("/api/auth/login", {
  method: "POST",
  body: JSON.stringify({
    phoneNumber: "0791234567",
    password: "123456",
    mallID: selectedMallId
  })
});

localStorage.setItem("sessionId", auth.sessionId);
```

Get stores example:

```js
const stores = await apiFetch("/api/stores");
```

Create offer example:

```js
const offer = await apiFetch("/api/offers", {
  method: "POST",
  body: JSON.stringify({
    storeId,
    title: "Weekend Sale",
    description: "20% off selected items",
    startAt: "2026-04-18T09:00:00Z",
    endAt: "2026-04-25T21:00:00Z",
    isActive: true
  })
});
```

## 15. Simple Screen Mapping

| Frontend screen | APIs to use |
| --- | --- |
| Login | `POST /api/auth/login` |
| Register | `POST /api/auth/register` |
| Home | `GET /api/offers`, `GET /api/announcements`, `GET /api/userinfo/points` |
| Stores page | `GET /api/stores` |
| Store details | `GET /api/stores/{id}` |
| Coupons page | `GET /api/coupons`, `GET /api/coupons/user` |
| Activate coupon | `POST /api/coupons/redeem` |
| Receipts page | `GET /api/transactions/my-receipts` |
| Receipt details | `GET /api/transactions/{id}` |
| Chatbot | `POST /api/chatbot/ask`, `GET /api/chatbot/history` |
| Manager dashboard | `GET /api/dashboard/summary`, `sales`, `points`, `coupons`, `activity` |
| Manage stores | `GET /api/stores/manage`, `POST /api/stores`, `PUT /api/stores/{id}` |
| Manage offers | `GET /api/offers/manage`, `POST /api/offers`, `PUT /api/offers/{id}`, `PATCH /api/offers/{id}/status`, `DELETE /api/offers/{id}` |
| Manage announcements | `GET /api/announcements/manage`, `POST /api/announcements`, `PUT /api/announcements/{id}`, `PATCH /api/announcements/{id}/status`, `PATCH /api/announcements/{id}/pin`, `DELETE /api/announcements/{id}` |

## 16. Final Frontend Checklist

- Save `sessionId` after login/register.
- Send `X-Session-Id` on protected APIs.
- For realtime points, use `sessionId` in query string.
- Handle `401` by clearing session and showing login.
- Use `mallID`, not `mallId`, in auth requests.
- Use ISO dates for offers, announcements, receipts filters, and dashboard filters.
- Remember coupon list uses `discription`, coupon details uses `description`.
- Manager APIs can return `403` if the manager does not have permission.
