using System.Net.Http.Headers;
using FunderPayments.Data;
using FunderPayments.Models.Options;
using FunderPayments.Services;
using FunderPayments.HostedServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CardcomOptions>(builder.Configuration.GetSection("Cardcom"));
builder.Services.Configure<FunderPayments.Models.Options.FunderApiOptions>(builder.Configuration.GetSection("FunderApi"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS Configuration
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost", "https://localhost", "http://localhost:7042", "https://localhost:7042" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
    
    // Development policy - more permissive (allows any localhost origin)
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("Development", policy =>
        {
            policy.SetIsOriginAllowed(origin =>
            {
                try
                {
                    // Allow any localhost origin in development
                    var uri = new Uri(origin);
                    var isLocalhost = uri.Host == "localhost" || 
                                     uri.Host == "127.0.0.1" || 
                                     uri.Host == "[::1]" ||
                                     uri.Host.StartsWith("localhost:");
                    return isLocalhost;
                }
                catch
                {
                    return false;
                }
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromHours(1));
        });
    }
    
    // Additional policy for production (more restrictive)
    options.AddPolicy("Production", policy =>
    {
        var productionOrigins = builder.Configuration.GetSection("Cors:ProductionOrigins").Get<string[]>();
        if (productionOrigins != null && productionOrigins.Length > 0)
        {
            policy.WithOrigins(productionOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .SetPreflightMaxAge(TimeSpan.FromHours(1));
        }
    });
});

builder.Services.AddDbContext<FunderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<CardcomClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<CardcomOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
    {
        client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
    }

    var timeout = opts.TimeoutSeconds > 0 ? opts.TimeoutSeconds : 30;
    client.Timeout = TimeSpan.FromSeconds(timeout);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})
.AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError()
    .WaitAndRetryAsync(3, retry => TimeSpan.FromMilliseconds(200 * retry)));

builder.Services.AddHttpClient<FunderApiClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<FunderPayments.Models.Options.FunderApiOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
    {
        client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
    }

    var timeout = opts.TimeoutSeconds > 0 ? opts.TimeoutSeconds : 30;
    client.Timeout = TimeSpan.FromSeconds(timeout);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    
    // Add API Key authentication if provided
    if (!string.IsNullOrWhiteSpace(opts.ApiKey))
    {
        client.DefaultRequestHeaders.Add("X-API-Key", opts.ApiKey);
    }
})
.AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError()
    .WaitAndRetryAsync(3, retry => TimeSpan.FromMilliseconds(200 * retry)));

builder.Services.AddScoped<CardcomClient>();
builder.Services.AddScoped<FunderApiClient>();
builder.Services.AddScoped<PaymentInitService>();
builder.Services.AddScoped<CallbackService>();
builder.Services.AddScoped<BillingService>();

builder.Services.AddHostedService<MonthlyBillingJob>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Routing must be before CORS
app.UseRouting();

// CORS must be after UseRouting and before UseAuthorization
if (app.Environment.IsProduction())
{
    app.UseCors("Production");
}
else
{
    // Use Development policy if available, otherwise default
    app.UseCors("Development");
}

// Add logging middleware to see CORS requests
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var origin = context.Request.Headers["Origin"].ToString();
        var allowedOrigins = context.RequestServices.GetRequiredService<IConfiguration>()
            .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        logger.LogInformation("OPTIONS request from Origin: {Origin}. Allowed Origins: {AllowedOrigins}", 
            origin, string.Join(", ", allowedOrigins));
    }
    await next();
});

app.UseAuthorization();
app.MapControllers();

app.Run();
