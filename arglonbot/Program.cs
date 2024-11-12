using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += Console_CancelKeyPress;

DiscordClient? discordClient = null;

try
{
    discordClient = new DiscordClient(
        new DiscordConfiguration
        {
            Token = Environment.GetEnvironmentVariable("bottoken"),
            TokenType = TokenType.Bot,
            AutoReconnect = true,
            MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug
        }
    );
    await discordClient.ConnectAsync();

    var slash = discordClient!.UseSlashCommands();
    slash.RegisterCommands<SlashCommands>();

    var zoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
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
    cts.Cancel();
}
finally
{
    if (discordClient != null)
    {
        await discordClient.DisconnectAsync();
        discordClient.Dispose();
    }
}

async Task PeriodicOpenMouth(PeriodicTimer timer, bool repeat)
{
    const ulong Guild_404_ID = 723989119503696013;
    const ulong Channel_Lounge_ID = 787685796055482368;

    while (await timer.WaitForNextTickAsync(cts.Token))
    {
        var guild = await discordClient.GetGuildAsync(Guild_404_ID);
        if (!guild.Channels.TryGetValue(Channel_Lounge_ID, out var channel))
            continue;

        await channel.SendMessageAsync(Message());

        if (!repeat)
            break;
    }
}

void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    e.Cancel = true;
    cts.Cancel();
}

string Message() => Now() switch
{
    { Month: 12, Day: 24 } now => "Happy christmas eve 🎅🏻",
    { Month: 12, Day: 25 } => "Happy christmas 🎅🏻",
    _ => "Good morning 😮"
};

DateTime Now()
{
    var zoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zoneInfo);
}

public class SlashCommands : ApplicationCommandModule
{
    [SlashCommand("react", "Arglon will react appropriately to the specified user.")]
    public async Task React(
        InteractionContext ctx,
        [Option("user", "The user to which Arglon should react.")]DiscordUser user)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        var completionBuilder = new DiscordWebhookBuilder();

        var match = await GetMostRecentMessageFor(ctx, user);
        if (match == null)
        {
            completionBuilder = completionBuilder.WithContent("This user hasn't posted in a while 😮");
        }
        else
        {
            completionBuilder = completionBuilder.WithContent("(done)");
            if (!match.Reactions.Any(x => x.IsMe && x.Emoji.Equals(DiscordEmoji.FromUnicode("😮"))))
            {
                await match.CreateReactionAsync(DiscordEmoji.FromUnicode("😮"));
            }
        }

        await ctx.EditResponseAsync(completionBuilder);
    }

    [SlashCommand("reply", "Arglon will reply appropriately to the specified user.")]
    public async Task Reply(
        InteractionContext ctx,
        [Option("user", "The user to which Arglon should reply.")]DiscordUser user)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        var completionBuilder = new DiscordWebhookBuilder();

        var match = await GetMostRecentMessageFor(ctx, user);
        if (match == null)
        {
            completionBuilder = completionBuilder.WithContent("This user hasn't posted in a while 😮");
        }
        else
        {
            completionBuilder = completionBuilder.WithContent("(done)");
            await match.RespondAsync(DiscordEmoji.FromUnicode("😮"));
        }

        await ctx.EditResponseAsync(completionBuilder);
    }

    [SlashCommand("post", "Classic Arglon.")]
    public async Task Post(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(
            InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder { Content = "😮" }
        );
    }

    private static async Task<DiscordMessage?> GetMostRecentMessageFor(InteractionContext ctx, DiscordUser user)
    {
        var messages = await ctx.Channel.GetMessagesAsync().ConfigureAwait(false);
        return messages.OrderByDescending(x => x.Timestamp)
            .FirstOrDefault(x => x.Author.Equals(user));
    }
}
