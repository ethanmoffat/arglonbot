using arglonbot;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

public class SlashCommands : ApplicationCommandModule
{
    private readonly IMessageSelector _messageSelector;

    public SlashCommands(IMessageSelector messageSelector)
    {
        _messageSelector = messageSelector;
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
            await ctx.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder { Content = _messageSelector.FindMessageForDateTime(dateTime) }
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