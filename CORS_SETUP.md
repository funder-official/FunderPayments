# CORS Configuration - הסבר

## מה זה CORS?

CORS (Cross-Origin Resource Sharing) הוא מנגנון אבטחה בדפדפן שמונע מאתרים לקרוא ל-APIs מכתובות אחרות.

כאשר ה-Frontend שלך רץ על `http://localhost:8080` וה-Backend על `https://localhost:7042`, הדפדפן רואה אותם כ-"מקורות שונים" (different origins) ודורש אישור מהשרת.

## מה עשיתי?

### 1. הוספתי CORS Configuration ב-`Program.cs`

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()      // מאפשר GET, POST, PUT, DELETE, OPTIONS
            .AllowAnyHeader()      // מאפשר כל headers
            .AllowCredentials()   // מאפשר cookies/credentials
            .SetPreflightMaxAge(TimeSpan.FromHours(1)); // Cache preflight requests
    });
});
```

### 2. הוספתי Middleware

```csharp
app.UseCors(); // לפני UseAuthorization ו-MapControllers
```

**חשוב:** CORS חייב להיות לפני `UseAuthorization` ו-`MapControllers`!

### 3. הוספתי הגדרות ב-`appsettings.json`

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost",
      "https://localhost",
      "http://localhost:7042",
      "https://localhost:7042"
    ],
    "ProductionOrigins": [
      "https://app.funder.com",
      "https://www.funder.com"
    ]
  }
}
```

## איך זה עובד?

1. **Development:** משתמש ב-`AllowedOrigins` - מאפשר כל localhost
2. **Production:** משתמש ב-`ProductionOrigins` - רק domains מורשים

## איך להוסיף Origin חדש?

### Development:
ערוך `appsettings.Development.json`:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:8080",
      "https://your-frontend.com"
    ]
  }
}
```

### Production:
ערוך `appsettings.json`:
```json
{
  "Cors": {
    "ProductionOrigins": [
      "https://app.funder.com",
      "https://admin.funder.com"
    ]
  }
}
```

## בדיקה

1. הרץ את ה-Backend
2. פתח את ה-Frontend בדפדפן
3. פתח Developer Tools (F12) → Network
4. נסה לבצע AJAX request
5. בדוק שה-OPTIONS request מקבל `200 OK`
6. בדוק שה-POST request עובד

## פתרון בעיות

### שגיאה: "405 Method Not Allowed" על OPTIONS
- **סיבה:** CORS לא מוגדר או לא מופעל
- **פתרון:** ודא ש-`app.UseCors()` נמצא לפני `UseAuthorization`

### שגיאה: "Access-Control-Allow-Origin header missing"
- **סיבה:** ה-Origin שלך לא ברשימת המורשים
- **פתרון:** הוסף את ה-Origin ל-`AllowedOrigins` ב-`appsettings.json`

### שגיאה: "Credentials flag is true, but Access-Control-Allow-Credentials is not 'true'"
- **סיבה:** `AllowCredentials()` לא מוגדר
- **פתרון:** ודא ש-`AllowCredentials()` קיים ב-CORS policy

## אבטחה

⚠️ **חשוב ב-Production:**
- אל תאפשר `AllowAnyOrigin()` עם `AllowCredentials()`
- ציין רק את ה-domains שאתה צריך
- השתמש ב-`ProductionOrigins` ב-Production

## דוגמת Request

```javascript
$.ajax({
    type: "POST",
    url: "https://localhost:7042/api/payments/init",
    contentType: "application/json",
    data: JSON.stringify({ userId: "123", amount: 10 }),
    success: function(response) {
        console.log(response);
    }
});
```

הדפדפן יבצע:
1. **OPTIONS request** (preflight) → מקבל `200 OK` עם CORS headers
2. **POST request** (actual) → מתבצע בהצלחה

