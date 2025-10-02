using IotTgBot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");

// config
builder.Configuration.AddJsonFile("appsettings.json", optional: false);
builder.Services.Configure<BotOptions>(builder.Configuration.GetSection("Bots"));

IConfiguration configuration = builder.Configuration;

builder.Services.AddDbContext<IotDbContext>(opts =>
                        opts.UseSqlite(configuration.GetConnectionString("IotDb")));
builder.Services.AddScoped<IReadingRepository, ReadingRepository>();

// services
builder.Services.AddSingleton<TelegramUpdateHandler>();
builder.Services.AddSingleton<ITelegramService, TelegramService>();
builder.Services.AddHostedService<TelegramHostedService>();
builder.Services.AddSingleton<IPlotService, PlotService>();
builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IotDbContext>();
    db.Database.EnsureCreated();
}

// pipeline
app.MapControllers();

app.Run();