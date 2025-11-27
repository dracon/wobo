using Microsoft.EntityFrameworkCore;
using woboapi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add this in your service configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    string? envConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

    options.UseNpgsql(envConnectionString ?? connectionString);
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // app.UseSwagger();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
