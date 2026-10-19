using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using PharmaLink.Context;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Persistencia con SQLite y EF Core
builder.Services.AddDbContext<PharmaLinkContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
                      ?? "Data Source=pharmalink.db"));

// OpenAPI / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Seed Data para desarrollo
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<PharmaLinkContext>();
    DbInitializer.Initialize(context);
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
