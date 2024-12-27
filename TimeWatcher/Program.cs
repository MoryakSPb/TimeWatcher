using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using TgBotFrame.Commands.Authorization.Extensions;
using TgBotFrame.Commands.Authorization.Interfaces;
using TgBotFrame.Commands.Injection;
using TgBotFrame.Commands.Start;
using TgBotFrame.Injection;
using TimeWatcher.Data;
using TimeWatcher.Middlewares;
using TimeWatcher.Models;
using TimeWatcher.ServiceDefaults;
using TimeWatcher.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<WorkOptions>(builder.Configuration.GetSection("WorkOptions"));

string? tgToken = builder.Configuration.GetConnectionString("Telegram");
builder.Services.AddTelegramHttpClient();
builder.Services.AddSingleton<ITelegramBotClient, TelegramBotClient>(provider =>
{
    IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();
    return new(tgToken!, factory.CreateClient(nameof(ITelegramBotClient)));
});

const string sqliteConnectionString = "Data Source=./data/data.sqlite";
builder.Services.AddDbContext<WorkDataContext>(optionsBuilder =>
    optionsBuilder.UseSqlite(sqliteConnectionString));
builder.Services.AddScoped<IAuthorizationData, WorkDataContext>();

builder.Services.AddScoped<HolidayService>();
builder.Services.AddHostedService<StartMessageService>();

builder.Services.AddTgBotFrameCommands(commandsBuilder =>
{
    commandsBuilder.AddAuthorization();
    commandsBuilder.TryAddCommandMiddleware<WorkReactionMiddleware>();

    commandsBuilder.AddStartCommand("Этот бот позволяет отмечать время начала и конца работы");
    commandsBuilder.TryAddControllers(Assembly.GetEntryAssembly()!);
});

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

IServiceScopeFactory scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
await using (scope.ConfigureAwait(false))
{
    Directory.CreateDirectory("./data");
    WorkDataContext dbContext = scope.ServiceProvider.GetRequiredService<WorkDataContext>();
    await dbContext.Database.MigrateAsync().ConfigureAwait(false);
}

await app.RunAsync(CancellationToken.None);