using arglonbot.Configuration;

using DSharpPlus;
using DSharpPlus.SlashCommands;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace arglonbot;

public class ArglonBot : BackgroundService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly DiscordClient _discordClient;
    private readonly IOptions<ArglonBotConfiguration> _arglonBotConfiguration;

    public ArglonBot(IHostApplicationLifetime hostApplicationLifetime,
                     DiscordClient discordClient,
                     IOptions<ArglonBotConfiguration> arglonBotConfiguration)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _discordClient = discordClient;
        _arglonBotConfiguration = arglonBotConfiguration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _discordClient.ConnectAsync();

            var slash = _discordClient.UseSlashCommands();
            slash.RegisterCommands<SlashCommands>();

            var now = Now();
            var firstPeriod = (now.Hour < 8
                ? now.Date.AddHours(8)
                : now.Date.AddDays(1).AddHours(8)) - now;

            using var firstPeriodTimer = new PeriodicTimer(firstPeriod);
            await PeriodicOpenMouth(firstPeriodTimer, repeat: false, stoppingToken);

            using var repeatTimer = new PeriodicTimer(TimeSpan.FromHours(24));
            await PeriodicOpenMouth(repeatTimer, repeat: true, stoppingToken);
        }
        finally
        {
            await _discordClient.DisconnectAsync();
            _discordClient?.Dispose();
        }

        _hostApplicationLifetime.StopApplication();
    }


    private async Task PeriodicOpenMouth(PeriodicTimer timer, bool repeat, CancellationToken cancellationToken)
    {
        const ulong Guild_404_ID = 723989119503696013;
        const ulong EO_Mobile_ID = 1306039236407066736;
        const ulong Guild_404_Channel_Lounge_ID = 787685796055482368;
        const ulong EO_Mobile_Channel_General_ID = 1306039236931223614;

        List<(ulong GuildId, ulong ChannelId)> guildChannelPairs = [
            (Guild_404_ID, Guild_404_Channel_Lounge_ID),
    (EO_Mobile_ID, EO_Mobile_Channel_General_ID)
        ];

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            foreach (var (guildId, channelId) in guildChannelPairs)
            {
                var guild = await _discordClient.GetGuildAsync(guildId);
                if (!guild.Channels.TryGetValue(channelId, out var channel))
                    continue;

                await channel.SendMessageAsync(Message());
            }

            if (!repeat)
                break;
        }
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
