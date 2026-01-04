# FUNDER API Integration Guide

This document describes the complete integration between the FunderPayments service and the FUNDER platform API.

## Overview

The FunderPayments service integrates with the FUNDER API at **4 key points** in the payment flow:

1. **After Token Registration** - Notify FUNDER when a payment token is successfully registered
2. **Before Monthly Billing** - Query FUNDER to determine which users are eligible for billing
3. **After Successful Charge** - Notify FUNDER when a monthly charge succeeds
4. **After Failed Charge** - Notify FUNDER when a monthly charge fails

## Configuration

Add the following configuration to `appsettings.json`:

```json
{
  "FunderApi": {
    "BaseUrl": "https://api.funder.com/",
    "ApiKey": "your-api-key-here",
    "TimeoutSeconds": 30,
    "PaymentTokenRegisteredPath": "api/payments/token-registered",
    "EligibleForBillingPath": "api/payments/eligible-for-billing",
    "BillingSuccessPath": "api/payments/billing/success",
    "BillingFailedPath": "api/payments/billing/failed"
  }
}
```

### Authentication

The service uses **API Key authentication** via the `X-API-Key` header. Set the `ApiKey` value in `appsettings.json` or use User Secrets/Environment Variables for production.

## Integration Points

### Integration #1: Payment Token Registered

**When:** After Cardcom successfully returns a payment token via callback

**Endpoint:** `POST {BaseUrl}/{PaymentTokenRegisteredPath}`

**Request Body:**
```json
{
  "userId": "user123",
  "token": "cardcom-token-abc123",
  "cardType": "Visa",
  "last4Digits": "1234",
  "monthlyAmount": 99.99,
  "status": "Active",
  "registeredAt": "2025-12-18T10:30:00Z"
}
```

**Response:** HTTP 200 OK (any response is accepted, failures are logged but don't block the callback)

**Implementation:** `CallbackService.ProcessCallbackAsync()` → `FunderApiClient.NotifyTokenRegisteredAsync()`

---

### Integration #2: Eligible Users for Billing

**When:** Before executing monthly billing (called by `MonthlyBillingJob`)

**Endpoint:** `GET {BaseUrl}/{EligibleForBillingPath}`

**Request:** No body required

**Response Body:**
```json
{
  "eligibleUsers": [
    {
      "userId": "user123",
      "token": "cardcom-token-abc123",
      "monthlyAmount": 99.99,
      "isEligible": true,
      "reason": null
    },
    {
      "userId": "user456",
      "token": "cardcom-token-xyz789",
      "monthlyAmount": 149.99,
      "isEligible": false,
      "reason": "User follows less than 5 funds"
    }
  ]
}
```

**Implementation:** `BillingService.RunMonthlyBillingAsync()` → `FunderApiClient.GetEligibleUsersAsync()`

**Logic:**
- Only users with `isEligible: true` will be charged
- The service matches users by `userId` and `token` from the database
- If FUNDER API returns no eligible users, billing is skipped

---

### Integration #3: Billing Success

**When:** After a successful Cardcom Do-Transaction charge (ResponseCode == 0)

**Endpoint:** `POST {BaseUrl}/{BillingSuccessPath}`

**Request Body:**
```json
{
  "userId": "user123",
  "token": "cardcom-token-abc123",
  "amount": 99.99,
  "coinId": 1,
  "orderId": "ORDER-user123-20251218103045",
  "approveNumber": "123456",
  "internalDealNumber": "DEAL-789",
  "chargedAt": "2025-12-18T10:30:45Z"
}
```

**Response:** HTTP 200 OK (failures are logged but don't block the billing process)

**Implementation:** `BillingService.PersistHistoryAsync()` → `FunderApiClient.NotifyBillingSuccessAsync()`

---

### Integration #4: Billing Failed

**When:** After a failed Cardcom Do-Transaction charge (ResponseCode != 0)

**Endpoint:** `POST {BaseUrl}/{BillingFailedPath}`

**Request Body:**
```json
{
  "userId": "user123",
  "token": "cardcom-token-abc123",
  "amount": 99.99,
  "coinId": 1,
  "orderId": "ORDER-user123-20251218103045",
  "responseCode": 100,
  "description": "Insufficient funds",
  "error": "Insufficient funds",
  "attemptedAt": "2025-12-18T10:30:45Z"
}
```

**Response:** HTTP 200 OK (failures are logged but don't block the billing process)

**Implementation:** `BillingService.PersistHistoryAsync()` → `FunderApiClient.NotifyBillingFailedAsync()`

---

## Error Handling

All FUNDER API calls are **non-blocking**:

- If Integration #1 fails, the token is still saved to the database
- If Integration #2 fails, billing is skipped (no users are charged)
- If Integration #3 or #4 fails, the billing history is still saved

All errors are logged with full details for debugging.

## Retry Policy

The `FunderApiClient` uses Polly retry policy:
- **3 retries** with exponential backoff (200ms, 400ms, 600ms)
- Only retries on transient HTTP errors (5xx, network errors)

## Testing

### Manual Testing

1. **Test Integration #1:**
   - Call `POST /api/payments/init` with a test userId
   - Complete the Cardcom payment flow
   - Check logs for "Successfully notified FUNDER API: Token registered"

2. **Test Integration #2:**
   - Ensure you have active tokens in the database
   - Manually trigger `MonthlyBillingJob` or wait for scheduled run
   - Check logs for "FUNDER API returned X eligible users"

3. **Test Integration #3 & #4:**
   - Trigger a manual charge via `POST /api/billing/charge`
   - Check logs for "Successfully notified FUNDER API: Billing success/failed"

### Mock FUNDER API

For development/testing, you can use a mock API server or tools like:
- [Mockoon](https://mockoon.com/)
- [Postman Mock Server](https://learning.postman.com/docs/designing-and-developing-your-api/mocking-data/)
- [httpbin.org](https://httpbin.org/)

## Monitoring

Monitor the following log messages:

- `"Successfully notified FUNDER API"` - Integration working correctly
- `"Failed to notify FUNDER API"` - Integration failure (check API key, URL, network)
- `"FUNDER API returned X eligible users"` - Eligibility check working
- `"Error notifying FUNDER API"` - Exception occurred (check exception details)

## Security

- **Never commit API keys to Git** - Use User Secrets or Environment Variables
- **Use HTTPS** for all FUNDER API calls in production
- **Validate API responses** - Don't trust external data without validation
- **Rate limiting** - Consider implementing rate limiting if FUNDER API has limits

## Next Steps

1. Update `appsettings.json` with your FUNDER API BaseUrl and ApiKey
2. Verify the endpoint paths match your FUNDER API implementation
3. Test each integration point individually
4. Monitor logs during first production billing cycle

