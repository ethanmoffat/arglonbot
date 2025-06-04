using arglonbot;
using arglonbot.Configuration;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using Microsoft.Extensions.Options;

using static arglonbot.Configuration.PeriodicOpenMouthSettings;

public class SlashCommands : ApplicationCommandModule
{
    private readonly IMessageSelector _messageSelector;
    private readonly PeriodicOpenMouthSettings _periodicOpenMouthSettings;

    public SlashCommands(IMessageSelector messageSelector,
                         IOptions<ArglonBotConfiguration> arglonbotConfigurationOptions)
    {
        _messageSelector = messageSelector;
        _periodicOpenMouthSettings = arglonbotConfigurationOptions.Value.PeriodicOpenMouthSettings;
    }

    [SlashCommand("react", "Arglon will react appropriately to the specified user.")]
    public async Task React(
        InteractionContext ctx,
        [Option("user", "The user to which Arglon should react.")] DiscordUser user)
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
        [Option("user", "The user to which Arglon should reply.")] DiscordUser user)
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

    [SlashCommand("holiday", "Arglon sends a holiday greeting")]
    public async Task Holiday(
        InteractionContext ctx,
        [Option("date", "The date of the holiday")] string date)
    {
        if (!DateTime.TryParse(date, out var dateTime))
        {
            await ctx.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder { Content = $"`DateTime.TryParse` didn't like the input date {date}. Try again." }
            );
        }
        else
        {
            var channelInfo = _periodicOpenMouthSettings.Channels
                .Where(x => x.GuildId == ctx.Channel.GuildId && x.ChannelId == ctx.Channel.Id)
                .FirstOrDefault();

            await ctx.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder { Content = _messageSelector.FindMessageForDateTime(channelInfo ?? ChannelInfo.None, dateTime) }
            );
        }
    }

    private static async Task<DiscordMessage?> GetMostRecentMessageFor(InteractionContext ctx, DiscordUser user)
    {
        var messages = await ctx.Channel.GetMessagesAsync().ConfigureAwait(false);
        return messages.OrderByDescending(x => x.Timestamp)
            .FirstOrDefault(x => x.Author.Equals(user));
    }
}