using System.Text;
using System.Threading.RateLimiting;
using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Interfaces;
using LocaLe.EscrowApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════
// 1. DATABASE — SQLite for dev, PostgreSQL for prod
// ═══════════════════════════════════════════════════════════
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "SQLite";
if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<EscrowContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));
}
else
{
    builder.Services.AddDbContext<EscrowContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("SQLite")
            ?? "Data Source=localedb.sqlite"));
}

// ═══════════════════════════════════════════════════════════
// 2. DEPENDENCY INJECTION — Services (loosely coupled)
// ═══════════════════════════════════════════════════════════
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IEscrowService, EscrowService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();

// ═══════════════════════════════════════════════════════════
// 3. AUTHENTICATION — JWT with HttpOnly Cookie support
// ═══════════════════════════════════════════════════════════
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? "LocaLe_SuperSecretKey_ChangeThisInProduction_2026!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "LocaLe",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "LocaLe.Users",
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };

    // Read JWT from HttpOnly cookie if no Authorization header is present
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("locale_token"))
            {
                context.Token = context.Request.Cookies["locale_token"];
            }
            return Task.CompletedTask;
        }
    };
});

// ═══════════════════════════════════════════════════════════
// 4. CONTROLLERS & RATE LIMITING
// ═══════════════════════════════════════════════════════════
builder.Services.AddControllers();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("default", limiter =>
    {
        limiter.PermitLimit = 100;               // max 100 requests
        limiter.Window = TimeSpan.FromMinutes(1); // per minute
        limiter.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

// ═══════════════════════════════════════════════════════════
// 5. SWAGGER & REDOC — Automatic API documentation
// ═══════════════════════════════════════════════════════════
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LocaLe Escrow API",
        Version = "v1",
        Description = "Peer-to-peer escrow engine with QR-based release for neighborhood services."
    });

    // Add JWT auth button to Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (or just use the cookie-based flow)."
    });

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer")] = new List<string>()
    });
});

// ═══════════════════════════════════════════════════════════
// BUILD & CONFIGURE PIPELINE
// ═══════════════════════════════════════════════════════════
var app = builder.Build();

// Auto-create/migrate database on startup (dev convenience)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EscrowContext>();
    context.Database.EnsureCreated();
}

// Developer exception page — shows detailed errors in dev mode
app.UseDeveloperExceptionPage();

// Swagger UI at /docs
app.UseSwagger(c =>
{
    c.RouteTemplate = "docs/{documentName}/swagger.json";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/docs/v1/swagger.json", "LocaLe Escrow API v1");
    c.RoutePrefix = "docs";
});

// ReDoc at /redoc
app.UseReDoc(c =>
{
    c.DocumentTitle = "LocaLe API Documentation";
    c.SpecUrl = "/docs/v1/swagger.json";
    c.RoutePrefix = "redoc";
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
