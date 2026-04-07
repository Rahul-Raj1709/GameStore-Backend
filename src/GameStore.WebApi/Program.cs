using GameStore.Application;
using GameStore.Infrastructure;
using GameStore.WebApi.Endpoints;
using GameStore.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    // Use the class-based transformer for V2.0.0 compatibility
    options.AddDocumentTransformer<SecuritySchemeTransformer>();
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors("AllowAll");
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "GameStore API is running! Navigate to /scalar/v1 to see the documentation.");

app.MapGamesEndpoints();
app.MapAuthEndpoints();
app.MapUsersEndpoints();

app.Run();

// --- OPENAPI 2.0 TRANSFORMER (2026 Stable Implementation) ---
internal sealed class SecuritySchemeTransformer : Microsoft.AspNetCore.OpenApi.IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // 1. Initialize root properties
        document.Components ??= new OpenApiComponents();
        document.Security ??= new List<OpenApiSecurityRequirement>();

        // 2. Fix CS0019: Use the Interface type for the Dictionary initialization
        // We avoid ??= here because of the interface vs. concrete class mismatch.
        if (document.Components.SecuritySchemes == null)
        {
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>();
        }

        // 3. Define the Scheme
        var bearerScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste your JWT token (eyJ...)"
        };

        // 4. Add to Components if missing
        if (!document.Components.SecuritySchemes.ContainsKey("Bearer"))
        {
            document.Components.SecuritySchemes.Add("Bearer", bearerScheme);
        }

        // 5. Link the scheme to Security Requirements
        var reference = new OpenApiSecuritySchemeReference("Bearer", document);

        if (!document.Security.Any(req => req.ContainsKey(reference)))
        {
            var requirement = new OpenApiSecurityRequirement
            {
                [reference] = new List<string>()
            };
            document.Security.Add(requirement);
        }

        return Task.CompletedTask;
    }
}