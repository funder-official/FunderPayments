# API Documentation - FunderPayments

מסמך זה מתאר את כל ה-API endpoints הזמינים במערכת FunderPayments.

---

## 📋 תוכן עניינים

1. [Payments API](#payments-api)
   - [POST /api/payments/init](#post-apipaymentsinit)
   - [POST /api/payments/callback](#post-apipaymentscallback)
2. [Billing API](#billing-api)
   - [POST /api/billing/charge](#post-apibillingcharge)
   - [GET /api/billing/tokens](#get-apibillingtokens)
   - [GET /api/billing/billing-history](#get-apibillingbilling-history)
   - [PATCH /api/billing/tokens/{tokenId}/monthly-amount](#patch-apibillingtokenstokenidmonthly-amount)

---

## 💳 Payments API

### POST /api/payments/init

**תיאור:** יוצר דף תשלום ב-Cardcom ומחזיר URL להפניית המשתמש.

**מתי להשתמש:** כאשר משתמש בוחר להירשם לחיוב חודשי או לבצע תשלום חד-פעמי.

**Request Body:**
```json
{
  "userId": "user123",
  "amount": 99.99,
  "coinId": 1,
  "successRedirectUrl": "https://app.funder.com/pay/success",
  "errorRedirectUrl": "https://app.funder.com/pay/error",
  "metadata": {
    "orderId": "ORDER-123",
    "planType": "premium"
  }
}
```

**פרמטרים:**
- `userId` (required, string) - מזהה המשתמש במערכת
- `amount` (required, decimal) - סכום לחיוב (חייב להיות > 0)
- `coinId` (optional, int) - מטבע (1 = שקל, 2 = דולר). Default: 1
- `successRedirectUrl` (optional, string) - URL להפניה אחרי הצלחה. אם לא מוגדר, משתמש ב-default מ-appsettings.json
- `errorRedirectUrl` (optional, string) - URL להפניה אחרי כישלון. אם לא מוגדר, משתמש ב-default מ-appsettings.json
- `metadata` (optional, Dictionary<string, string>) - נתונים נוספים שיועברו ב-CustomFields ל-Cardcom

**Response 200 OK:**
```json
{
  "paymentPageUrl": "https://secure.cardcom.solutions/...",
  "iframeHtml": "<iframe src=\"https://secure.cardcom.solutions/...\" width=\"100%\" height=\"600\" frameborder=\"0\"></iframe>",
  "payload": {
    "TerminalNumber": "8132",
    "ApiName": "pHD4mDxXH6xxrI1qV9Nq",
    "Operation": "CreateTokenOnly",
    "Amount": 99.99,
    "ReturnValue": "ORDER-user123-99.99-20251218103045",
    "SuccessRedirectUrl": "https://app.funder.com/pay/success",
    "FailedRedirectUrl": "https://app.funder.com/pay/error",
    "WebHookUrl": "https://api.funder.com/api/payments/callback"
  }
}
```

**מה קורה מאחורי הקלעים:**
1. Backend בונה בקשה ל-Cardcom `LowProfile/Create`
2. Cardcom מחזיר `LowProfileId` ו-`Url`
3. Backend מחזיר את ה-URL ל-Client
4. Client מפנה את המשתמש ל-URL או טוען אותו ב-Iframe

**דוגמת שימוש (JavaScript):**
```javascript
const response = await fetch('/api/payments/init', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    userId: 'user123',
    amount: 99.99,
    coinId: 1
  })
});

const data = await response.json();
// אפשרות 1: Redirect
window.location.href = data.paymentPageUrl;

// אפשרות 2: Iframe
document.getElementById('payment-iframe').innerHTML = data.iframeHtml;
```

---

### POST /api/payments/callback

**תיאור:** Webhook endpoint שמקבל דיווח מ-Cardcom אחרי שהמשתמש סיים את התשלום.

**מתי נקרא:** אוטומטית על ידי Cardcom אחרי שהמשתמש מזין פרטי כרטיס.

**⚠️ חשוב:** זה לא endpoint שאתה קורא אליו! Cardcom קורא אליו אוטומטית.

**Request (Form-UrlEncoded):**
```
ResponseCode=0
Token=abc123xyz
LowProfileId=LP-12345
ApproveNumber=123456
CardType=Visa
L4digit=1234
ReturnValue=ORDER-user123-99.99-20251218103045
...
```

**Response:** תמיד `200 OK` (גם אם יש שגיאה, כדי ש-Cardcom לא ינסה שוב)

**מה קורה מאחורי הקלעים:**
1. Cardcom שולח Webhook עם נתוני העסקה
2. Backend מחלץ `LowProfileId` מה-Webhook
3. **CRITICAL:** Backend קורא ל-`GetLpResult` לאימות הנתונים
4. Backend שומר טוקן ב-DB עם `IsVerified = true`
5. Backend מודיע ל-FUNDER API שהטוקן נרשם (Integration #1)

**Security:**
- ✅ כל Webhook מאומת דרך `GetLpResult`
- ✅ מונע כפילות עם `IsVerified` flag
- ✅ משתמש בנתונים מאומתים, לא בנתוני Webhook

---

## 💰 Billing API

### POST /api/billing/charge

**תיאור:** מבצע חיוב ידני של טוקן שמור.

**מתי להשתמש:** לחיוב חד-פעמי, בדיקות, או חיוב שלא דרך ה-Monthly Billing Job.

**Request Body:**
```json
{
  "userId": "user123",
  "amount": 99.99,
  "coinId": 1,
  "tokenId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**פרמטרים:**
- `userId` (required, string) - מזהה המשתמש
- `amount` (required, decimal) - סכום לחיוב (חייב להיות > 0)
- `coinId` (optional, int) - מטבע. אם לא מוגדר, משתמש ב-coinId של הטוקן
- `tokenId` (optional, Guid) - מזהה הטוקן הספציפי. אם לא מוגדר, משתמש בטוקן הפעיל האחרון של המשתמש

**Response 200 OK (הצלחה):**
```json
{
  "responseCode": 0,
  "description": "OK",
  "approveNumber": "123456",
  "internalDealNumber": "DEAL-789",
  "dealResponse": "Transaction approved",
  "raw": "ResponseCode=0&Description=OK&ApproveNumber=123456&..."
}
```

**Response 200 OK (כישלון):**
```json
{
  "responseCode": 100,
  "description": "Insufficient funds",
  "approveNumber": null,
  "internalDealNumber": null,
  "dealResponse": null,
  "raw": "ResponseCode=100&Description=Insufficient funds&..."
}
```

**Response 404 Not Found:**
```json
{
  "error": "Active token not found for user."
}
```

**Response 400 Bad Request:**
```json
{
  "error": "Amount must be greater than zero."
}
```

**מה קורה מאחורי הקלעים:**
1. Backend מוצא טוקן פעיל למשתמש
2. Backend קורא ל-Cardcom `Do-Transaction` עם הטוקן
3. Backend שומר תוצאה ב-`BillingHistory`
4. Backend מודיע ל-FUNDER API על התוצאה (Integration #3 או #4)

**דוגמת שימוש:**
```javascript
const response = await fetch('/api/billing/charge', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    userId: 'user123',
    amount: 99.99,
    coinId: 1
  })
});

const result = await response.json();
if (result.responseCode === 0) {
  console.log('Charge successful!', result.approveNumber);
} else {
  console.error('Charge failed:', result.description);
}
```

---

### GET /api/billing/tokens

**תיאור:** מחזיר רשימת טוקנים שמורים במערכת.

**מתי להשתמש:** לבדיקה, ניהול, או הצגת טוקנים למשתמש.

**Query Parameters:**
- `userId` (optional, string) - לסנן לפי משתמש ספציפי

**Request Examples:**
```
GET /api/billing/tokens
GET /api/billing/tokens?userId=user123
```

**Response 200 OK:**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "userId": "user123",
    "isActive": true,
    "monthlyAmount": 99.99,
    "coinId": 1,
    "createdAt": "2025-12-18T10:30:00Z"
  },
  {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "userId": "user456",
    "isActive": true,
    "monthlyAmount": 149.99,
    "coinId": 1,
    "createdAt": "2025-12-17T15:20:00Z"
  }
]
```

**הערות:**
- הטוקן עצמו (`Token`) לא מוחזר מטעמי אבטחה
- התוצאות ממוינות לפי תאריך יצירה (החדש ביותר ראשון)
- רק טוקנים פעילים (`IsActive = true`) יופיעו בחיוב חודשי

---

### GET /api/billing/billing-history

**תיאור:** מחזיר היסטוריית חיובים.

**מתי להשתמש:** לבדיקה, דוחות, או הצגת היסטוריה למשתמש.

**Query Parameters:**
- `userId` (optional, string) - לסנן לפי משתמש ספציפי

**Request Examples:**
```
GET /api/billing/billing-history
GET /api/billing/billing-history?userId=user123
```

**Response 200 OK:**
```json
[
  {
    "id": "770e8400-e29b-41d4-a716-446655440000",
    "userId": "user123",
    "tokenId": "550e8400-e29b-41d4-a716-446655440000",
    "orderId": "ORDER-user123-20251218103045",
    "amount": 99.99,
    "coinId": 1,
    "responseCode": 0,
    "description": "OK",
    "approveNumber": "123456",
    "internalDealNumber": "DEAL-789",
    "dealResponse": "Transaction approved",
    "succeeded": true,
    "attemptedAt": "2025-12-18T10:30:45Z",
    "rawRequest": "{...}",
    "rawResponse": "ResponseCode=0&...",
    "error": null
  },
  {
    "id": "880e8400-e29b-41d4-a716-446655440001",
    "userId": "user123",
    "tokenId": "550e8400-e29b-41d4-a716-446655440000",
    "orderId": "ORDER-user123-20251217150000",
    "amount": 99.99,
    "coinId": 1,
    "responseCode": 100,
    "description": "Insufficient funds",
    "approveNumber": null,
    "internalDealNumber": null,
    "dealResponse": null,
    "succeeded": false,
    "attemptedAt": "2025-12-17T15:00:00Z",
    "rawRequest": "{...}",
    "rawResponse": "ResponseCode=100&Description=Insufficient funds&...",
    "error": "Insufficient funds"
  }
]
```

**הערות:**
- מחזיר עד 200 רשומות (החדש ביותר ראשון)
- `succeeded = true` אם `responseCode == 0`
- `rawRequest` ו-`rawResponse` מכילים את הנתונים הגולמיים מ-Cardcom

---

### PATCH /api/billing/tokens/{tokenId}/monthly-amount

**תיאור:** מעדכן את סכום החיוב החודשי של טוקן.

**מתי להשתמש:** כאשר משתמש משנה תוכנית, מעדכן סכום, או כשהמערכת קובעת סכום חדש.

**Route Parameters:**
- `tokenId` (required, Guid) - מזהה הטוקן

**Request Body:**
```json
{
  "monthlyAmount": 149.99
}
```

**פרמטרים:**
- `monthlyAmount` (required, decimal) - סכום חדש לחיוב חודשי (חייב להיות >= 0)

**Response 200 OK:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "user123",
  "monthlyAmount": 149.99,
  "isActive": true
}
```

**Response 404 Not Found:**
```json
{
  "error": "Token not found."
}
```

**Response 400 Bad Request:**
```json
{
  "error": "Monthly amount cannot be negative."
}
```

**מה קורה מאחורי הקלעים:**
1. Backend מוצא טוקן לפי `tokenId`
2. Backend מעדכן `MonthlyAmount`
3. Backend מעדכן `UpdatedAt`
4. ה-Monthly Billing Job ישתמש בסכום החדש בפעם הבאה

**דוגמת שימוש:**
```javascript
const response = await fetch('/api/billing/tokens/550e8400-e29b-41d4-a716-446655440000/monthly-amount', {
  method: 'PATCH',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    monthlyAmount: 149.99
  })
});

const result = await response.json();
console.log('Updated monthly amount:', result.monthlyAmount);
```

---

## 🔄 Monthly Billing Job

**תיאור:** Background Service שרץ אוטומטית כל יום ומבצע חיוב חודשי.

**איך זה עובד:**
1. ה-Job רץ כל 24 שעות
2. קורא ל-FUNDER API לקבל רשימת משתמשים זכאים (Integration #2)
3. רק משתמשים עם `isEligible = true` נחייבים
4. לכל משתמש זכאי:
   - קורא ל-Cardcom `Do-Transaction` עם הטוקן
   - שומר תוצאה ב-`BillingHistory`
   - מודיע ל-FUNDER API על התוצאה (Integration #3 או #4)

**לא API Endpoint:** זה Background Service, לא endpoint שאתה קורא אליו.

**הגדרה:** מוגדר ב-`Program.cs` כ-`HostedService`.

---

## 📊 Response Codes

### Cardcom Response Codes
- `0` - הצלחה
- `100` - כישלון (לדוגמה: כספים לא מספיקים)
- `-1` - שגיאת תקשורת

### HTTP Status Codes
- `200 OK` -שה הצליחה
- `400 Bad Request` - פרמטרים לא תקינים
- `404 Not Found` - משאב לא נמצא
- `500 Internal Server Error` - שגיאת שרת

---

## 🔐 Security

1. **Authentication:** הוסף Authentication/Authorization לפי הצורך
2. **API Keys:** FUNDER API משתמש ב-API Key (מוגדר ב-`appsettings.json`)
3. **HTTPS:** כל התקשורת חייבת להיות ב-HTTPS ב-Production
4. **Validation:** כל הקלט מאומת לפני עיבוד
5. **GetLpResult:** כל Webhook מאומת דרך `GetLpResult`

---

## 📝 הערות חשובות

1. **Callback URL:** חייב להיות Public URL (לא localhost) כדי ש-Cardcom יוכל לקרוא אליו
2. **Webhook Retry:** Cardcom ינסה 7 פעמים אם לא מקבל 200 OK
3. **Idempotency:** כל Webhook מאומת ומונע כפילות עם `IsVerified` flag
4. **Logging:** כל פעולה מתועדת ב-Logs לניפוי באגים

---

## 🧪 Testing

### Swagger UI
פתח `https://localhost:5001/swagger` (או הפורט שלך) לבדיקת כל ה-endpoints.

### דוגמת Flow מלא:
1. `POST /api/payments/init` - יצירת דף תשלום
2. המשתמש מזין פרטי כרטיס ב-Cardcom
3. Cardcom קורא ל-`POST /api/payments/callback` (אוטומטי)
4. `GET /api/billing/tokens` - בדיקה שהטוקן נשמר
5. `POST /api/billing/charge` - חיוב ידני לבדיקה
6. `GET /api/billing/billing-history` - בדיקת היסטוריה

---

## 📚 קישורים נוספים

- [FUNDER API Integration Guide](./FUNDER_API_INTEGRATION.md)
- [Cardcom Security Verification](./CARDCOM_SECURITY_VERIFICATION.md)
- [Swagger Testing Guide](./SWAGGER_TESTING_GUIDE.md)

