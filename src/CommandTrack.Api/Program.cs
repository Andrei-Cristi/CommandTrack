using CommandTrack.Api.Services;
using CommandTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Serviciile aplicației
builder.Services.AddControllers();

builder.Services.AddDbContext<CommandTrackDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "CommandTrackDatabase")));

builder.Services.AddScoped<IUnitService, EfUnitService>();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configurarea aplicației
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();