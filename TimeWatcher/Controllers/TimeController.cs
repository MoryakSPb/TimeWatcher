using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgBotFrame.Commands;
using TgBotFrame.Commands.Attributes;
using TimeWatcher.Data;
using TimeWatcher.Data.Models;
using TimeWatcher.Models;

namespace TimeWatcher.Controllers;

[CommandController("time")]
public class TimeController(
    WorkDataContext workDataContext,
    ITelegramBotClient botClient,
    IOptions<WorkOptions> options) : CommandControllerBase
{
    private static readonly CultureInfo _ruCulture = CultureInfo.GetCultureInfoByIetfLanguageTag("ru-ru");

    private static readonly FrozenSet<ReactionTypeEmoji> _highVoltageEmoji =
        new[] { new ReactionTypeEmoji { Emoji = @"⚡" } }.ToFrozenSet();

    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    [Command(nameof(Time))]
    public Task Time() => Time(DateOnly.FromDateTime(DateTime.UtcNow));

    [Command(nameof(Time))]
    public async Task Time(DateOnly date)
    {
        StringBuilder text = new();
        text.Append(date.ToString("D", _ruCulture));
        text.Append(' ');
        text.Append(_timeZone.Id);
        text.AppendLine();
        text.AppendLine();

        DbWorkRecord[] data = await workDataContext.WorkRecords
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.Date == date)
            .OrderBy(x => x.User.FirstName + x.User.LastName)
            .ToArrayAsync().ConfigureAwait(false);
        TimeSpan offset =
            _timeZone.GetUtcOffset(date.ToDateTime(data.Length == 0 ? TimeOnly.MinValue : data.Min(x => x.Arrived)));

        foreach (DbWorkRecord record in data)
        {
            text.Append(record.User.GetMention());
            text.Append(": ");
            text.Append((record.Arrived.ToTimeSpan() + offset).ToString("hh\\:mm", CultureInfo.InvariantCulture));
            text.Append(" — ");
            text.Append(record.Left is null
                ? "--:--"
                : (record.Left.Value.ToTimeSpan() + offset).ToString("hh\\:mm", CultureInfo.InvariantCulture));
        }

        await botClient.SendMessage(Update.Message!.Chat.Id, text.ToString(), ParseMode.None, new()
        {
            ChatId = Update.Message.Chat.Id,
            AllowSendingWithoutReply = true,
            MessageId = Update.Message.MessageId,
        }, messageThreadId: Update.Message.MessageThreadId, disableNotification: true).ConfigureAwait(false);
    }

    [Command(nameof(SetArrive))]
    public async Task SetArrive(DateOnly date, TimeOnly time)
    {
        long userid = Update.Message!.From!.Id;
        DbWorkRecord? message = await workDataContext.WorkRecords.AsTracking()
            .FirstOrDefaultAsync(x => x.UserId == userid && x.Date == date, CancellationToken).ConfigureAwait(false);
        if (message is null)
        {
            await workDataContext.WorkRecords.AddAsync(new()
            {
                Date = date,
                Arrived = time,
                UserId = userid,
            }).ConfigureAwait(false);
        }
        else
        {
            message.Arrived = time;
        }

        await workDataContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);
        await botClient.SetMessageReaction(Update.Message.Chat.Id, Update.Message.Id, _highVoltageEmoji)
            .ConfigureAwait(false);
    }

    [Command(nameof(SetLeft))]
    public async Task SetLeft(DateOnly date, TimeOnly time)
    {
        long userid = Update.Message!.From!.Id;
        DbWorkRecord? message = await workDataContext.WorkRecords.AsTracking()
            .FirstOrDefaultAsync(x => x.UserId == userid && x.Date == date, CancellationToken).ConfigureAwait(false);
        if (message is null)
        {
            await workDataContext.WorkRecords.AddAsync(new()
            {
                Date = date,
                Arrived = time,
                Left = time,
                UserId = userid,
            }).ConfigureAwait(false);
        }
        else
        {
            message.Left = time;
        }

        await workDataContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);
        await botClient.SetMessageReaction(Update.Message.Chat.Id, Update.Message.Id, _highVoltageEmoji)
            .ConfigureAwait(false);
    }
}