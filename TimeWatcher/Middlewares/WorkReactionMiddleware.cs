using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Telegram.Bot.Types;
using TgBotFrame.Middleware;
using TimeWatcher.Data;
using TimeWatcher.Data.Models;

namespace TimeWatcher.Middlewares;

public class WorkReactionMiddleware(WorkDataContext workDataContext) : FrameMiddleware
{
    internal const string START_EMOJI = "\ud83d\udc68\u200d\ud83d\udcbb";
    internal const string END_EMOJI = "\ud83c\udf89";

    public override async Task InvokeAsync(Update update, FrameContext context, CancellationToken ct = default)
    {
        if (update.MessageReaction?.User is not null)
        {
            await ProcessReaction(update.MessageReaction, ct).ConfigureAwait(false);
        }

        await Next(update, context, ct).ConfigureAwait(false);
    }

    private async Task ProcessReaction(MessageReactionUpdated update, CancellationToken ct = default)
    {
        DateOnly date =
            await workDataContext.WorkMessages
                .AsNoTracking()
                .Where(x => x.Id == update.MessageId)
                .Select(x => x.Date)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (date == default)
        {
            return;
        }

        string[] newReactions = update.NewReaction.OfType<ReactionTypeEmoji>().Select(x => x.Emoji)
            .Except(update.OldReaction.OfType<ReactionTypeEmoji>().Select(x => x.Emoji)).ToArray();

        bool hasStartEmoji = newReactions.Any(x => x == START_EMOJI);
        bool hasEndEmoji = newReactions.Any(x => x == END_EMOJI);

        if (hasStartEmoji || hasEndEmoji)
        {
            DbWorkRecord? work = await workDataContext.WorkRecords.FirstOrDefaultAsync(x =>
                x.UserId == update.User!.Id && x.Date == date, ct).ConfigureAwait(false);

            if (hasStartEmoji)
            {
                if (work is null)
                {
                    await workDataContext.WorkRecords.AddAsync(new()
                    {
                        UserId = update.User!.Id,
                        Date = date,
                        Arrived = TimeOnly.FromDateTime(update.Date),
                    }, ct).ConfigureAwait(false);
                }
                else
                {
                    work.Arrived = TimeOnly.FromDateTime(update.Date);
                }
            }

            if (hasEndEmoji)
            {
                if (work is null)
                {
                    EntityEntry<DbWorkRecord> entity = await workDataContext.WorkRecords.AddAsync(new()
                    {
                        UserId = update.User!.Id,
                        Date = date,
                        Arrived = TimeOnly.FromDateTime(update.Date),
                    }, ct).ConfigureAwait(false);
                    work = entity.Entity;
                }

                work.Left = TimeOnly.FromDateTime(update.Date);
            }

            await workDataContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}