# הגדרת מפתחות API של Cardcom

## 📋 מה צריך לקבל מ-Cardcom

לפי המדריך הרשמי, מפתחות API מורכבים מ-3 פרמטרים:

1. **מספר מסוף (TerminalNumber)** - מספר המסוף שלך
2. **שם משתמש API (UserName / ApiName)** - שם המשתמש לממשקים
3. **סיסמת API (ApiPassword)** - סיסמת ה-API

---

## 🔑 איך לקבל את המפתחות

### שלב 1: התחברות לאתר Cardcom
1. בית העסק מקבל מייל התחברות לאתר של קארדקום
2. נכנסים לאתר עם שם המשתמש והסיסמה שקיבלתם

### שלב 2: מעבר לעמוד מפתחות API
1. נכנסים לאתר: https://secure.cardcom.solutions
2. בוחרים בתפריט: **הגדרות → הגדרות חברה ומשתמשים → ניהול מפתחות API לממשקים**
3. או ישירות: https://secure.cardcom.solutions/Definition/APIKey.aspx

### שלב 3: העתקת המפתחות
בעמוד הזה תראו:
- **מספר מסוף** (TerminalNumber)
- **שם משתמש API** (UserName / ApiName)
- **סיסמת API** (ApiPassword)

---

## ⚠️ פתרון בעיות

### שגיאת 2FA (אימות דו-שלבי)
אם מופיעה הודעת שגיאה הקשורה לאימות דו-שלבי:
1. יש להפעיל את האפשרות הזו במערכת
2. לחץ על המלל הכחול **"יש להפעיל אימות מוגבר (2FA)"**
3. תועבר לעמוד פרטי המשתמש, שם ניתן להפעיל את האימות הדו-שלבי

למידע נוסף: [אימות מוגבר במערכת קארדקום – הפעלת 2FA](https://support.cardcom.solutions/hc/he/articles/19568051743634)

---

## ⚙️ הגדרת המפתחות ב-FUNDER

### 1. עדכון `appsettings.json`

פתח את הקובץ `FunderPayments/appsettings.json` ועדכן את הסעיף `Cardcom`:

```json
{
  "Cardcom": {
    "BaseUrl": "https://secure.cardcom.solutions/",
    "PaymentPagePath": "apsiv11/Transactions/PaymentPage",
    "DoTransactionPath": "apsiv11/Transactions/Transaction",
    "TerminalNumber": "YOUR_TERMINAL_NUMBER_HERE",
    "UserName": "YOUR_API_USERNAME_HERE",
    "ApiPassword": "YOUR_API_PASSWORD_HERE",
    "ApiLevel": 10,
    "TimeoutSeconds": 30,
    "SuccessRedirectUrl": "https://app.funder.com/pay/success",
    "ErrorRedirectUrl": "https://app.funder.com/pay/error",
    "CallbackUrl": "https://api.funder.com/api/payments/callback",
    "DefaultCoinId": 1,
    "DefaultMonthlyAmount": 0
  }
}
```

**החלף:**
- `YOUR_TERMINAL_NUMBER_HERE` → מספר המסוף שקיבלת
- `YOUR_API_USERNAME_HERE` → שם המשתמש API שקיבלת
- `YOUR_API_PASSWORD_HERE` → סיסמת ה-API שקיבלת

### 2. עדכון URLs (אם נדרש)

אם ה-URLs של ה-Callback וה-Redirects שונים, עדכן אותם:
- `SuccessRedirectUrl` - לאן להעביר את המשתמש אחרי תשלום מוצלח
- `ErrorRedirectUrl` - לאן להעביר את המשתמש אחרי תשלום שנכשל
- `CallbackUrl` - ה-URL הציבורי של ה-API שלך שמקבל את ה-callback מקארדקום

**חשוב:** ה-`CallbackUrl` חייב להיות:
- נגיש מהאינטרנט (לא localhost)
- מוגדר גם במסוף של קארדקום (בהגדרות ה-API)

---

## ✅ בדיקה שהכל עובד

לאחר עדכון ה-`appsettings.json`:

1. **הרץ את הפרויקט:**
   ```bash
   dotnet run
   ```

2. **בדוק את ה-Swagger:**
   - פתח: `https://localhost:5001/swagger` (או הפורט שלך)
   - נסה את `POST /api/payments/init` עם body לדוגמה:
     ```json
     {
       "userId": "test-user-123",
       "amount": 100.00,
       "coinId": 1
     }
     ```

3. **אם הכל תקין:**
   - תקבל תשובה עם `PaymentPageUrl` - זה הקישור לדף התשלום של Cardcom
   - אם יש שגיאה - בדוק את ה-Logs לראות מה הבעיה

---

## 🔒 אבטחה

**חשוב מאוד:**
- **אל תעלה את `appsettings.json` עם מפתחות אמיתיים ל-Git!**
- השתמש ב-`appsettings.Development.json` או ב-**User Secrets** / **Environment Variables** לפרודקשן
- בפרודקשן, העדף להשתמש ב-Azure Key Vault / AWS Secrets Manager / או כלי ניהול סודות אחר

### שימוש ב-User Secrets (מומלץ לפיתוח):

```bash
dotnet user-secrets init
dotnet user-secrets set "Cardcom:TerminalNumber" "YOUR_TERMINAL_NUMBER"
dotnet user-secrets set "Cardcom:UserName" "YOUR_API_USERNAME"
dotnet user-secrets set "Cardcom:ApiPassword" "YOUR_API_PASSWORD"
```

---

## 📞 תמיכה

אם יש בעיות:
1. בדוק את ה-Logs של האפליקציה
2. וודא שהמפתחות נכונים בעמוד של Cardcom
3. וודא שה-2FA מופעל (אם נדרש)
4. פנה לתמיכה של Cardcom אם הבעיה נמשכת

---

## 📚 קישורים שימושיים

- [מפתחות API - איפה הם נמצאים ואיך לקבל?](https://support.cardcom.solutions/hc/he/articles/28083308888082)
- [ניהול מפתחות API לממשקים](https://secure.cardcom.solutions/Definition/APIKey.aspx)
- [אימות מוגבר במערכת קארדקום – הפעלת 2FA](https://support.cardcom.solutions/hc/he/articles/19568051743634)

