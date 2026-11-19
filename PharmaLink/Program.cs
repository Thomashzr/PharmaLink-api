using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PharmaLink.Context;
using PharmaLink.ExternalServices;
using PharmaLink.Interfaces;
using PharmaLink.Middleware;
using PharmaLink.Services;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Persistencia con SQLite y EF Core
builder.Services.AddDbContext<PharmaLinkContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
                      ?? "Data Source=pharmalink.db"));

// Inyección de dependencias
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMedicamentoService, MedicamentoDbService>();
builder.Services.AddScoped<IInsumoService, InsumoDbService>();
builder.Services.AddScoped<IDispensacionService, DispensacionDbService>();
builder.Services.AddScoped<IReposicionService, ReposicionDbService>();

// Cliente HTTP externo para integración hospitalaria (MediLink)
builder.Services.AddHttpClient<IHospitalApiClient, HospitalApiClient>(client =>
{
    var baseUrl = Environment.GetEnvironmentVariable("HOSPITAL_API_BASE_URL")
                  ?? builder.Configuration["HospitalApi:BaseUrl"]
                  ?? "http://localhost:5144";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);

    var apiKey = Environment.GetEnvironmentVariable("HOSPITAL_API_KEY")
                 ?? builder.Configuration["HospitalApi:ApiKey"]
                 ?? "compilaDeMilagro123";
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        client.DefaultRequestHeaders.Remove("X-Api-Key");
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }
});

// Configuración de Autenticación JWT Bearer
var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                   ?? builder.Configuration["Jwt:SecretKey"]
                   ?? "PharmaLinkSuperSecretKey2026Minimum32CharsLength!";
var keyBytes = Encoding.UTF8.GetBytes(jwtSecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

// Configuración de CORS para permitir conexiones desde otras apps (Frontend / clientes)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddAuthorization();

// OpenAPI / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PharmaLink API",
        Version = "v1",
        Description = "API RESTful para gestión de medicamentos, insumos, dispensaciones y reposiciones."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT obtenido en /api/auth/login"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});

var app = builder.Build();

// Middleware Global de Excepciones (debe ser el primero en el pipeline)
app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Inicializar Seed Data
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<PharmaLinkContext>();
    DbInitializer.Initialize(context);
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();

// Middleware de autenticación por API Key (para integraciones externas)
app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
