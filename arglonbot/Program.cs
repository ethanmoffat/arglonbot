using DSharpPlus;
using DSharpPlus.SlashCommands;

public static class Program
{
    private static readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private static DiscordClient? _discordClient;

    public static async Task Main(string[] args)
    {
        Console.CancelKeyPress += Console_CancelKeyPress;

        try
        {
            _discordClient = new DiscordClient(
                new DiscordConfiguration
                {
                    Token = Environment.GetEnvironmentVariable("bottoken"),
                    TokenType = TokenType.Bot,
                    AutoReconnect = true,
                    MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug
                }
            );
            await _discordClient.ConnectAsync();

            var slash = _discordClient!.UseSlashCommands();
            slash.RegisterCommands<SlashCommands>();

            var now = Now();
            var firstPeriod = (now.Hour < 8
                ? now.Date.AddHours(8)
                : now.Date.AddDays(1).AddHours(8)) - now;

            using var firstPeriodTimer = new PeriodicTimer(firstPeriod);
            await PeriodicOpenMouth(firstPeriodTimer, repeat: false);

            using var repeatTimer = new PeriodicTimer(TimeSpan.FromHours(24));
            await PeriodicOpenMouth(repeatTimer, repeat: true);
        }
        catch
        {
            _cts.Cancel();
        }
        finally
        {
            if (_discordClient != null)
            {
                await _discordClient.DisconnectAsync();
                _discordClient.Dispose();
            }
        }
    }

    private static async Task PeriodicOpenMouth(PeriodicTimer timer, bool repeat)
    {
        const ulong Guild_404_ID = 723989119503696013;
        const ulong EO_Mobile_ID = 1306039236407066736;
        const ulong Guild_404_Channel_Lounge_ID = 787685796055482368;
        const ulong EO_Mobile_Channel_General_ID = 1306039236931223614;

        List<(ulong GuildId, ulong ChannelId)> guildChannelPairs = [
            (Guild_404_ID, Guild_404_Channel_Lounge_ID),
    (EO_Mobile_ID, EO_Mobile_Channel_General_ID)
        ];

        while (await timer.WaitForNextTickAsync(_cts.Token))
        {
            foreach (var (guildId, channelId) in guildChannelPairs)
            {
                var guild = await _discordClient!.GetGuildAsync(guildId);
                if (!guild.Channels.TryGetValue(channelId, out var channel))
                    continue;

                await channel.SendMessageAsync(Message());
            }

            if (!repeat)
                break;
        }
    }

    private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        _cts.Cancel();
    }

    private static string Message() => Now() switch
    {
        { Month: 12, Day: 24 } now => "Merry christmas eve 🎅🏻",
        { Month: 12, Day: 25 } => "Merry christmas 🎅🏻",
        _ => "Good morning 😮"
    };

    private static DateTime Now()
    {
        var zoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zoneInfo);
    }
}
