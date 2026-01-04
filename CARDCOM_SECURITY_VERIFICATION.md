# Cardcom Security Verification - GetLpResult

## Overview

This document describes the **CRITICAL security step** that must be performed after receiving a webhook from Cardcom: **verifying transaction data via GetLpResult API**.

## Why This Is Critical

According to Cardcom documentation and security best practices:

> **"לצורך אבטחת מידע ומניעת התחזות - חובה לאחר קבלת פרטי העסקה יש לפנות אל שרתי קארדקום ייזום לבדיקת הנתונים."**
> 
> *(For data security and preventing impersonation - it is mandatory after receiving transaction details to proactively contact Cardcom servers to verify the data.)*

**Never trust webhook data alone!** Always verify by calling `GetLpResult` before processing the transaction.

## The Flow

### Step 1: Webhook Received
Cardcom sends a POST request to your callback endpoint (`POST /api/payments/callback`) with transaction data.

### Step 2: Extract LowProfileId
The webhook includes a `LowProfileId` field. This is required for verification.

### Step 3: Call GetLpResult (CRITICAL)
Your backend **must** call Cardcom's `GetLpResult` API to verify the transaction:

```
POST https://secure.cardcom.solutions/api/v11/LowProfile/GetLpResult
```

**Request Parameters:**
- `TerminalNumber` - Your terminal number
- `UserName` - Your API username
- `LowProfileId` - The LowProfileId from the webhook
- `APIPassword` - Your API password (optional)

**Response:**
- `ResponseCode` - Must be `0` for success
- `Token` - Verified token (use this, not the webhook token!)
- `ApproveNumber` - Verified approval number
- `CardType`, `L4digit`, etc. - Verified card details

### Step 4: Verify Response
- Check that `ResponseCode == 0`
- Use the **verified data** from `GetLpResult`, not the webhook data
- Mark the token as `IsVerified = true` in the database

### Step 5: Prevent Duplicates
- Check if token already exists and is verified
- If `IsVerified == true`, skip processing to prevent duplicate charges

## Implementation

The verification is implemented in `CallbackService.ProcessCallbackAsync()`:

```csharp
// Step 1: Basic validation
if (callback.ResponseCode != 0 || string.IsNullOrWhiteSpace(callback.Token))
    return;

// Step 2: CRITICAL - Verify via GetLpResult
var verifiedData = await VerifyTransactionAsync(callback.LowProfileId, cancellationToken);
if (verifiedData is null || verifiedData.ResponseCode != 0)
    return;

// Step 3: Check for duplicates
if (tokenEntity is not null && tokenEntity.IsVerified)
    return; // Already processed

// Step 4: Save verified data
tokenEntity.IsVerified = true;
await _dbContext.SaveChangesAsync(cancellationToken);
```

## Database Schema

The `PaymentToken` table includes an `IsVerified` field:

```sql
ALTER TABLE PaymentTokens ADD IsVerified BIT NOT NULL DEFAULT 0;
```

This field:
- Prevents duplicate processing
- Tracks which tokens have been verified
- Ensures we only process verified transactions

## Error Handling

If `GetLpResult` fails:
- **Log the error** with full details
- **Do NOT save the token** - it's not verified
- **Return 200 OK** to Cardcom (so they don't retry)
- **Investigate** the issue immediately

## Security Benefits

1. **Prevents Fraud**: Verifies transaction actually happened on Cardcom servers
2. **Prevents Impersonation**: Ensures webhook came from Cardcom, not an attacker
3. **Data Integrity**: Uses authoritative data from Cardcom, not webhook data
4. **Prevents Duplicates**: `IsVerified` flag prevents processing same transaction twice

## Testing

### Test GetLpResult Success
1. Complete a payment flow
2. Check logs for: `"GetLpResult verification successful"`
3. Verify `IsVerified = true` in database

### Test GetLpResult Failure
1. Simulate invalid `LowProfileId`
2. Check logs for: `"GetLpResult verification failed"`
3. Verify token is **NOT** saved to database

### Test Duplicate Prevention
1. Send same webhook twice
2. First call: Token saved with `IsVerified = true`
3. Second call: Logged as `"already verified. Skipping duplicate processing"`

## Migration

After adding `IsVerified` field, run:

```bash
dotnet ef migrations add AddIsVerifiedToPaymentToken
dotnet ef database update
```

Or manually:

```sql
ALTER TABLE PaymentTokens 
ADD IsVerified BIT NOT NULL DEFAULT 0;
```

## References

- Cardcom Documentation: Stage 1+2 - Payment Page
- Cardcom Security Best Practices
- PCI DSS Compliance Guidelines

