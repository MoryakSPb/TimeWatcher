using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using TimeWatcher.Models;

namespace TimeWatcher.Services;

public sealed class HolidayService(HttpClient httpClient)
{
    private static FrozenDictionary<int, DayState> DayStates { get; set; } = FrozenDictionary<int, DayState>.Empty;

    public async ValueTask<DayState> GetDayState(DateOnly date, CancellationToken cancellationToken = default)
    {
        if (DayStates.TryGetValue(date.DayNumber, out DayState dayState))
        {
            return dayState;
        }

        using HttpRequestMessage request = new(HttpMethod.Get,
            $"https://isdayoff.ru/api/getdata?year={date.Year:D}&cc=ru&pre=1");

        using HttpResponseMessage response = await httpClient
            .SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable stream1 = stream.ConfigureAwait(false);

        List<KeyValuePair<int, DayState>> dayStates = [];
        int day = new DateOnly(date.Year, 01, 01).DayNumber;
        while (true)
        {
            int symbol = stream.ReadByte();
            if (symbol == -1)
            {
                break;
            }

            dayStates.Add(new(day++, (DayState)(symbol - 0x30)));
        }

        DayStates = DayStates.Concat(dayStates).ToFrozenDictionary();
        return DayStates[date.DayNumber];
    }
}