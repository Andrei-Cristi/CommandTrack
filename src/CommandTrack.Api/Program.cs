using CommandTrack.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Serviciile aplicației
builder.Services.AddControllers();
builder.Services.AddSingleton<IUnitService, InMemoryUnitService>();
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