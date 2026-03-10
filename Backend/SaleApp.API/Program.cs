using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SaleApp.API.Hubs;
using SaleApp.API.Middlewares;
using SaleApp.Application.Interfaces;
using SaleApp.Infrastructure.Data;
using SaleApp.Infrastructure.Services;
using System.Text;
var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration with Bearer Token support
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

builder.Services.AddDbContext<SaleAppDbContext>();

//Services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<NotificationHelper>();

// Add SignalR
builder.Services.AddSignalR();

var jwtSettings = configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "SaleApp",
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"] ?? "SaleAppUsers",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Configure JWT authentication for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                // If the request is for our hub...
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && 
                    (path.StartsWithSegments("/hubs/chat") || path.StartsWithSegments("/hubs/notification")))
                {
                    // Read the token out of the query string
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// CORS configuration for Bearer Token authentication
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Allow any origin
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Important for SignalR with authentication
    });
});

var app = builder.Build();

// Seed database on startup in Development environment
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SaleAppDbContext>();
    if (app.Environment.IsDevelopment())
    {
        await DataSeeder.SeedDataAsync(context);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "";
        c.SwaggerEndpoint("swagger/v1/swagger.json", "SaleApp API v1.0");
    });
}

// Exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map SignalR Hub with CORS
app.MapHub<ChatHub>("/hubs/chat").RequireCors("AllowAll");
app.MapHub<NotificationHub>("/hubs/notification").RequireCors("AllowAll");

app.Run("http://0.0.0.0:5000");
