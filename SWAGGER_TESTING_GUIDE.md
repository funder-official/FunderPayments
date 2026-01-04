# 🧪 מדריך בדיקה מלא דרך Swagger - FUNDER Payments

מדריך שלב אחר שלב לבדיקת כל התהליך: מיצירת אסימון דרך Cardcom ועד לחיוב חודשי אוטומטי.

---

## 📋 תהליך הבדיקה המלא

### **שלב 1: הרצת הפרויקט**

```bash
cd FunderPayments
dotnet run
```

הפרויקט יעלה על:
- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`

פתח בדפדפן: **`https://localhost:5001/swagger`**

---

## 🔄 **שלב 2: יצירת דף תשלום (Payment Init)**

**מטרה:** ליצור דף תשלום של Cardcom ולקבל אסימון.

### **API Endpoint:**
```
POST /api/payments/init
```

### **Request Body:**
```json
{
  "userId": "user-123",
  "amount": 100.00,
  "coinId": 1,
  "successRedirectUrl": "https://app.funder.com/pay/success",
  "errorRedirectUrl": "https://app.funder.com/pay/error",
  "metadata": {
    "SubscriptionType": "Premium",
    "PlanId": "plan-001"
  }
}
```

**הסבר השדות:**
- `userId` - מזהה המשתמש שלך (חובה)
- `amount` - סכום התשלום (חובה, חייב להיות > 0)
- `coinId` - קוד מטבע (1 = ש"ח, 2 = דולר, וכו')
- `successRedirectUrl` - לאן להעביר אחרי תשלום מוצלח (אופציונלי - אם לא מועבר, יקח מ-appsettings)
- `errorRedirectUrl` - לאן להעביר אחרי תשלום שנכשל (אופציונלי)
- `metadata` - מידע נוסף שיועבר ב-JParams (אופציונלי)

### **Response (הצלחה):**
```json
{
  "paymentPageUrl": "https://secure.cardcom.solutions/...",
  "iframeHtml": "<iframe src=\"https://secure.cardcom.solutions/...\" ...></iframe>",
  "payload": {
    "TerminalNumber": "8132",
    "UserName": "pHD4mDxXH6xxrI1qV9Nq",
    ...
  }
}
```

### **מה לעשות עם התשובה:**
1. **אם אתה בודק ידנית:**
   - העתק את `paymentPageUrl` והדבק בדפדפן
   - או השתמש ב-`iframeHtml` להטמעה באתר

2. **בדף התשלום של Cardcom:**
   - הזן פרטי כרטיס אשראי (בסביבת טסט - השתמש בכרטיסי בדיקה)
   - השלם את התשלום

3. **לאחר התשלום:**
   - Cardcom יגדיר את המשתמש ל-`successRedirectUrl` או `errorRedirectUrl`
   - **במקביל**, Cardcom ישלח **Callback** ל-`/api/payments/callback` עם האסימון

---

## 📞 **שלב 3: קבלת Callback (אוטומטי)**

**מטרה:** Cardcom שולח callback אוטומטית אחרי תשלום מוצלח.

### **API Endpoint:**
```
POST /api/payments/callback
```

**זה קורה אוטומטית!** אתה לא צריך לקרוא לזה ידנית.

Cardcom שולח `application/x-www-form-urlencoded` עם:
- `ResponseCode` (0 = הצלחה)
- `Token` (האסימון לשמירה)
- `ApproveNumber`
- `JParams[UserId]` (ה-userId ששלחת בשלב 1)

### **מה קורה בקוד:**
1. `CallbackService` מקבל את ה-Callback
2. בודק ש-`ResponseCode == 0` ושיש `Token`
3. שומר את האסימון ב-`PaymentTokens` עם:
   - `UserId`
   - `Token`
   - `ApproveNumber`
   - `IsActive = true`

---

## ✅ **שלב 4: בדיקה שהאסימון נשמר**

**מטרה:** לוודא שהאסימון נשמר בהצלחה ב-DB.

### **API Endpoint:**
```
GET /api/billing/tokens?userId=user-123
```

### **Response:**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "userId": "user-123",
    "isActive": true,
    "monthlyAmount": null,
    "coinId": 1,
    "createdAt": "2025-12-16T12:00:00Z"
  }
]
```

**אם אתה רואה את האסימון כאן - הכל עבד! ✅**

---

## 💳 **שלב 5: חיוב ידני של אסימון (Manual Charge)**

**מטרה:** לחייב אסימון קיים באופן ידני (בדיקה לפני חיוב חודשי).

### **API Endpoint:**
```
POST /api/billing/charge
```

### **Request Body:**
```json
{
  "userId": "user-123",
  "tokenId": null,
  "amount": 50.00,
  "coinId": 1
}
```

**הסבר השדות:**
- `userId` - מזהה המשתמש (חובה)
- `tokenId` - מזהה האסימון הספציפי (אופציונלי - אם null, יקח את האסימון הפעיל האחרון של המשתמש)
- `amount` - סכום החיוב (חובה, חייב להיות > 0)
- `coinId` - קוד מטבע (אופציונלי - אם לא מועבר, יקח מהאסימון)

### **Response (הצלחה):**
```json
{
  "responseCode": 0,
  "description": "העסקה בוצעה בהצלחה",
  "approveNumber": "123456",
  "internalDealNumber": "654321",
  "dealResponse": "...",
  "raw": "ResponseCode=0&Description=..."
}
```

**קוד תשובה:**
- `0` = הצלחה ✅
- כל דבר אחר = שגיאה ❌

### **מה קורה בקוד:**
1. `BillingService` מוצא את האסימון לפי `userId` (או `tokenId`)
2. שולח `Do-Transaction` ל-Cardcom עם האסימון
3. שומר את התוצאה ב-`BillingHistories` עם כל הפרטים

---

## 📊 **שלב 6: בדיקת היסטוריית חיובים**

**מטרה:** לראות את כל ניסיונות החיוב (מוצלחים ונכשלים).

### **API Endpoint:**
```
GET /api/billing/billing-history?userId=user-123
```

### **Response:**
```json
[
  {
    "id": "660e8400-e29b-41d4-a716-446655440000",
    "userId": "user-123",
    "tokenId": "550e8400-e29b-41d4-a716-446655440000",
    "orderId": "user-123-20251216120000123",
    "amount": 50.00,
    "coinId": 1,
    "responseCode": 0,
    "description": "העסקה בוצעה בהצלחה",
    "approveNumber": "123456",
    "internalDealNumber": "654321",
    "dealResponse": "...",
    "succeeded": true,
    "attemptedAt": "2025-12-16T12:00:00Z",
    "rawRequest": "{...}",
    "rawResponse": "ResponseCode=0&...",
    "error": null
  }
]
```

**מה לראות כאן:**
- כל ניסיונות החיוב (מוצלחים ונכשלים)
- `succeeded: true/false` - האם הצליח
- `responseCode` - קוד התשובה מ-Cardcom
- `rawRequest` / `rawResponse` - לוגים מלאים לדיבוג

---

## 🔄 **שלב 7: הגדרת חיוב חודשי (Monthly Billing)**

**מטרה:** להגדיר אסימון לחיוב חודשי אוטומטי.

### **API Endpoint:**
```
PATCH /api/billing/tokens/{tokenId}/monthly-amount
```

### **Request Body:**
```json
{
  "monthlyAmount": 100.00
}
```

**הסבר:**
- `tokenId` - מזהה האסימון (תקבל אותו מ-`GET /api/billing/tokens`)
- `monthlyAmount` - סכום החיוב החודשי (אם `null`, יבטל את החיוב החודשי)

### **Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "user-123",
  "monthlyAmount": 100.00,
  "isActive": true
}
```

### **איך זה עובד:**
1. **עדכן את האסימון דרך API:**
   - קבל את `tokenId` מ-`GET /api/billing/tokens`
   - שלח `PATCH /api/billing/tokens/{tokenId}/monthly-amount` עם הסכום

2. **החיוב החודשי רץ אוטומטית:**
   - `MonthlyBillingJob` רץ כל 24 שעות
   - הוא מוצא את כל האסימונים עם `IsActive = true` ו-`MonthlyAmount > 0`
   - לכל אסימון, הוא קורא ל-`BillingService.ChargeTokenAsync` עם הסכום החודשי

### **אלטרנטיבה - עדכון ידני ב-DB (דרך SSMS):**

```sql
-- מצא את האסימון שלך
SELECT * FROM PaymentTokens WHERE UserId = 'user-123';

-- עדכן את MonthlyAmount
UPDATE PaymentTokens 
SET MonthlyAmount = 100.00
WHERE UserId = 'user-123' AND IsActive = 1;
```

---

## ⏰ **שלב 8: בדיקת חיוב חודשי אוטומטי**

**מטרה:** לוודא שהחיוב החודשי רץ אוטומטית.

### **איך לבדוק:**

1. **הגדר אסימון עם MonthlyAmount:**
   ```sql
   UPDATE PaymentTokens 
   SET MonthlyAmount = 50.00
   WHERE UserId = 'user-123';
   ```

2. **המתן 24 שעות** (או שנה את ה-`MonthlyBillingJob` לרוץ כל דקה לבדיקה):
   - פתח את `HostedServices/MonthlyBillingJob.cs`
   - שנה את `TimeSpan.FromHours(24)` ל-`TimeSpan.FromMinutes(1)` לבדיקה

3. **בדוק את ה-Logs:**
   - תראה הודעות כמו:
     ```
     Starting monthly billing for 1 tokens
     Sending Do-Transaction request for order user-123-...
     ```

4. **בדוק את היסטוריית החיובים:**
   ```
   GET /api/billing/billing-history?userId=user-123
   ```
   - תראה רשומה חדשה עם `attemptedAt` של היום

---

## 🧪 **תרחיש בדיקה מלא (End-to-End)**

### **1. יצירת אסימון:**
```bash
POST /api/payments/init
{
  "userId": "test-user-001",
  "amount": 100.00,
  "coinId": 1
}
```
→ קבלת `paymentPageUrl` → פתיחה בדפדפן → הזנת פרטי כרטיס → תשלום

### **2. בדיקה שהאסימון נשמר:**
```bash
GET /api/billing/tokens?userId=test-user-001
```
→ אמור להחזיר את האסימון החדש

### **3. חיוב ידני:**
```bash
POST /api/billing/charge
{
  "userId": "test-user-001",
  "amount": 50.00,
  "coinId": 1
}
```
→ אמור להחזיר `responseCode: 0` אם הצליח

### **4. בדיקת היסטוריה:**
```bash
GET /api/billing/billing-history?userId=test-user-001
```
→ אמור להראות את החיוב הידני

### **5. הגדרת חיוב חודשי:**
```bash
# קודם קבל את tokenId
GET /api/billing/tokens?userId=test-user-001

# עכשיו עדכן את MonthlyAmount
PATCH /api/billing/tokens/{tokenId}/monthly-amount
{
  "monthlyAmount": 75.00
}
```

### **6. המתן לחיוב אוטומטי** (או שנה את ה-Job לרוץ כל דקה)

### **7. בדיקה שהחיוב החודשי רץ:**
```bash
GET /api/billing/billing-history?userId=test-user-001
```
→ אמור להראות חיוב חדש עם `attemptedAt` של היום

---

## ⚠️ **טיפים לבדיקה**

### **1. כרטיסי בדיקה של Cardcom:**
- בדוק עם Cardcom אילו כרטיסי בדיקה זמינים בסביבת הטסט שלך
- בדרך כלל יש כרטיסים מיוחדים לבדיקות שלא מחייבים בפועל

### **2. לוגים:**
- כל הקריאות ל-Cardcom נרשמות ב-Logs
- בדוק את ה-Console Output או Log Files לראות את ה-Requests/Responses

### **3. שגיאות נפוצות:**
- **"Active token not found"** → האסימון לא נשמר (בדוק את ה-Callback)
- **"ResponseCode != 0"** → Cardcom דחה את החיוב (בדוק את `Description` בתשובה)
- **"Callback received without userId"** → ה-`JParams[UserId]` לא הגיע ב-Callback

### **4. בדיקת DB ישירות:**
```sql
-- כל האסימונים
SELECT * FROM PaymentTokens;

-- כל החיובים
SELECT * FROM BillingHistories ORDER BY AttemptedAt DESC;

-- חיובים של משתמש ספציפי
SELECT * FROM BillingHistories WHERE UserId = 'test-user-001';
```

---

## 📝 **סיכום - סדר הפעולות לבדיקה**

1. ✅ **הרץ את הפרויקט** → `dotnet run`
2. ✅ **פתח Swagger** → `https://localhost:5001/swagger`
3. ✅ **צור דף תשלום** → `POST /api/payments/init`
4. ✅ **שלם דרך Cardcom** → פתח את `paymentPageUrl` והזן כרטיס
5. ✅ **בדוק שהאסימון נשמר** → `GET /api/billing/tokens`
6. ✅ **חיוב ידני** → `POST /api/billing/charge`
7. ✅ **בדוק היסטוריה** → `GET /api/billing/billing-history`
8. ✅ **הגדר חיוב חודשי** → `PATCH /api/billing/tokens/{tokenId}/monthly-amount`
9. ✅ **המתן לחיוב אוטומטי** → בדוק Logs ו-History

---

**🎉 הכל מוכן לבדיקה!**

