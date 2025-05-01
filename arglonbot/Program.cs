using arglonbot.Configuration;

using DSharpPlus;
using DSharpPlus.SlashCommands;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace arglonbot;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);

        builder.ConfigureServices(services =>
        {
            services
                .AddSingleton<IValidateOptions<ArglonBotConfiguration>, ArglonBotConfigurationValidator>()
                .AddOptionsWithValidateOnStart<ArglonBotConfiguration>()
                .BindConfiguration(ArglonBotConfiguration.SectionName);

            services.AddSingleton(CreateDiscordConfiguration);
            services.AddSingleton(CreateSlashCommandsConfiguration);
            services.AddSingleton<DiscordClient>();
            services.AddSingleton<IMessageSelector, MessageSelector>();

            services.AddHostedService<ArglonBot>();
        });

        await builder.RunConsoleAsync();
    }

    private static DiscordConfiguration CreateDiscordConfiguration(IServiceProvider services)
    {
        var arglonBotConfiguration = services.GetRequiredService<IOptions<ArglonBotConfiguration>>().Value;

        return new DiscordConfiguration
        {
            Token = arglonBotConfiguration.BotToken,
            TokenType = TokenType.Bot,
            AutoReconnect = true,
            MinimumLogLevel = arglonBotConfiguration.DiscordLogLevel,
        };
    }

    private static SlashCommandsConfiguration CreateSlashCommandsConfiguration(IServiceProvider services)
    {
        return new SlashCommandsConfiguration
        {
            Services = services
        };
    }
}
