using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using TimeWatcher.Data;
using TimeWatcher.Models;

namespace TimeWatcher.Services;

public class StartMessageService(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            DateOnly date;
            await using (AsyncServiceScope scope = scopeFactory.CreateAsyncScope())
            {
                WorkDataContext dataContext = scope.ServiceProvider.GetRequiredService<WorkDataContext>();
                try
                {
                    date = await dataContext.WorkMessages.MaxAsync(x => x.Date, stoppingToken).ConfigureAwait(false);
                }
                catch (InvalidOperationException)
                {
                    date = DateOnly.FromDateTime(DateTime.UtcNow);
                }
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                DateOnly newDate = date.AddDays(1);
                DateTime triggerTime = new(newDate, TimeOnly.MinValue.Add(options.Value.MessageTimeUtc),
                    DateTimeKind.Utc);
                TimeSpan timeSpan = triggerTime - DateTime.UtcNow;
                await Task.Delay(timeSpan, stoppingToken).ConfigureAwait(false);
                date = newDate;

                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                WorkDataContext dataContext = scope.ServiceProvider.GetRequiredService<WorkDataContext>();
                HolidayService holidayService = scope.ServiceProvider.GetRequiredService<HolidayService>();
                ITelegramBotClient botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

                DayState state = await holidayService.GetDayState(date, stoppingToken).ConfigureAwait(false);

                switch (state)
                {
                    case DayState.Working:
                    case DayState.PreHoliday:
                        string text = "Рабочий день настает!..\n" +
                                      "Установите реакцию 👨‍💻, когда начнете работать. Установка 🎉 обозначает конец рабочего дня.";
                        if (state == DayState.PreHoliday)
                        {
                            text += "\n\nСегодня предпраздничный день, рабочее время уменьшено на 1 час!";
                        }

                        Message message = await botClient.SendMessage(options.Value.ChatId, text,
                            disableNotification: true, cancellationToken: stoppingToken).ConfigureAwait(false);
                        await dataContext.WorkMessages.AddAsync(new()
                        {
                            Date = date,
                            Id = message.Id,
                        }, stoppingToken).ConfigureAwait(false);
                        await dataContext.SaveChangesAsync(stoppingToken).ConfigureAwait(false);
                        continue;
                    case DayState.Holiday:
                    case DayState.HolidayWithPay:
                    case DayState.Playday:
                        continue;
                    default:
                        throw new ArgumentOutOfRangeException($"Unknown value of {nameof(DayState)}",
                            default(Exception));
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}