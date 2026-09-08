using arglonbot.Configuration;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace arglonbot;

public class ArglonBot : BackgroundService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly DiscordClient _discordClient;
    private readonly SlashCommandsConfiguration _slashCommandsConfiguration;
    private readonly IMessageSelector _messageSelector;
    private readonly IOptionsMonitor<ArglonBotConfiguration> _arglonBotConfiguration;
    private readonly ILogger<ArglonBot> _logger;

    public ArglonBot(IHostApplicationLifetime hostApplicationLifetime,
                     DiscordClient discordClient,
                     SlashCommandsConfiguration slashCommandsConfiguration,
                     IMessageSelector messageSelector,
                     IOptionsMonitor<ArglonBotConfiguration> arglonBotConfiguration,
                     ILoggerFactory loggerFactory)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _discordClient = discordClient;
        _slashCommandsConfiguration = slashCommandsConfiguration;
        _messageSelector = messageSelector;
        _arglonBotConfiguration = arglonBotConfiguration;
        _logger = loggerFactory.CreateLogger<ArglonBot>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _discordClient.ConnectAsync();

            var slash = _discordClient.UseSlashCommands(_slashCommandsConfiguration);
            slash.RegisterCommands<SlashCommands>();
            slash.SlashCommandErrored += OnSlashCommandErrored;

            var settings = _arglonBotConfiguration.CurrentValue.PeriodicOpenMouthSettings;

            var now = Now(settings.NotificationTimeZone);
            var nowAtTime = now.Date.Add(settings.NotificationTime);
            var tomorrowAtTime = now.Date.AddDays(1).Add(settings.NotificationTime);

            var firstPeriod = now < nowAtTime
                ? nowAtTime - now
                : tomorrowAtTime - now;
            _logger.LogInformation("First period timer firing at {time} in {remaining}", settings.NotificationTime, firstPeriod);
            _logger.LogInformation("Periodic interval set to {interval}", settings.NotificationInterval);

            using var firstPeriodTimer = new PeriodicTimer(firstPeriod);
            await PeriodicOpenMouth(firstPeriodTimer, repeat: false, stoppingToken);

            using var repeatTimer = new PeriodicTimer(settings.NotificationInterval);
            await PeriodicOpenMouth(repeatTimer, repeat: true, stoppingToken);
        }
        finally
        {
            await _discordClient.DisconnectAsync();
            _discordClient.Dispose();

            _hostApplicationLifetime.StopApplication();
        }
    }


    private async Task PeriodicOpenMouth(PeriodicTimer timer, bool repeat, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {repeat} timer with interval {interval}", repeat ? "repeating" : "non-repeating", timer.Period);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            var configuration = _arglonBotConfiguration.CurrentValue;

            var now = Now(configuration.PeriodicOpenMouthSettings.NotificationTimeZone);

            foreach (var channel in configuration.PeriodicOpenMouthSettings.Channels)
            {
                _logger.LogInformation("Posting to {channelName}", channel.Name);

                DiscordGuild discordGuild;
                try
                {
                    discordGuild = await _discordClient.GetGuildAsync(channel.GuildId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting discord guild id {guildId} ({name})", channel.GuildId, channel.Name);
                    continue;
                }

                if (!discordGuild.Channels.TryGetValue(channel.ChannelId, out var discordChannel))
                {
                    _logger.LogWarning("Channel {channelId} did not exist in guild ({name})", channel.ChannelId, channel.Name);
                    continue;
                }

                try
                {
                    await discordChannel.SendMessageAsync(_messageSelector.FindMessageForDateTime(channel, now));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error posting message to channel {channelId} ({name})", channel.ChannelId, channel.Name);
                    continue;
                }
            }

            if (!repeat)
                break;
        }
    }

    private async Task OnSlashCommandErrored(SlashCommandsExtension sender, SlashCommandErrorEventArgs args)
    {
        var context = args.Context;

        // A 404 means the interaction is unknown to Discord - it expired, or it was already answered
        // (for example by another running instance of the bot). There is no way to reach the user,
        // and editing would clobber a response this execution did not create.
        if (args.Exception is NotFoundException)
        {
            _logger.LogWarning(args.Exception, "Slash command {command} could not be answered; the interaction is no longer available", context?.CommandName);
            return;
        }

        // Log the full detail server-side, but never surface exception details to the user.
        _logger.LogError(args.Exception, "Slash command {command} failed", context?.CommandName);

        if (context == null)
        {
            return;
        }

        const string GenericError = "Something went wrong 😮";

        try
        {
            var builder = new DiscordWebhookBuilder().WithContent(GenericError);
            await context.EditResponseAsync(builder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send error response for slash command {command}", context.CommandName);
        }
    }

    private static DateTime Now(string timeZone)
    {
        var zoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zoneInfo);
    }
}
