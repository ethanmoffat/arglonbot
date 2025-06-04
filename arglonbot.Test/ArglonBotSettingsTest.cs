using System.Text;
using arglonbot.Configuration;
using Microsoft.Extensions.Configuration;
using static arglonbot.Configuration.PeriodicOpenMouthSettings;

namespace arglonbot.Test;

[TestFixture]
public class ArglonBotSettingsTest
{
    [Test]
    public void ArglonBotSettingsLoadsUlongValuesAsExpected()
    {
        const string ConfigString = @"{
    ""ArglonBotSettings"": {
        ""PeriodicOpenMouthSettings"": {
            ""Channels"": [
                {
                ""Name"": ""Phorophor#Lounge"",
                ""GuildId"": 723989119503696013,
                ""ChannelId"": 787685796055482368
                },
                {
                ""Name"": ""EOMobile#General"",
                ""GuildId"": 1306039236407066736,
                ""ChannelId"": 1306039236931223614
                }
            ]
        }
    }
}";

        var expectedModel = new List<ChannelInfo>
        {
            new("Phorophor#Lounge", 723989119503696013, 787685796055482368, []),
            new("EOMobile#General", 1306039236407066736, 1306039236931223614, [])
        };

        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(ConfigString)))
            .Build();
        var settings = new ArglonBotConfiguration();
        config.Bind(ArglonBotConfiguration.SectionName, settings);

        Assert.That(settings.PeriodicOpenMouthSettings.Channels.Select(x => (x.ChannelId, x.GuildId)), Is.EquivalentTo(expectedModel.Select(x => (x.ChannelId, x.GuildId))));
    }

    [Test]
    public void ArglonBotSettingsLoadsTimeSpansAsExpected()
    {
        const string ConfigString = @"{
    ""ArglonBotSettings"": {
        ""PeriodicOpenMouthSettings"": {
            ""NotificationTime"": ""08:00:00"",
            ""NotificationInterval"": ""1.00:00:00""
        }
    }
}";
        var expectedNotificationTime = TimeSpan.FromHours(8);
        var expectedNotificationInterval = TimeSpan.FromHours(24);

        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(ConfigString)))
            .Build();
        var settings = new ArglonBotConfiguration
        {
            BotToken = "",
            PeriodicOpenMouthSettings = new()
            {
                Channels = [],
                Messages = [],
                ExtraMessages = []
            }
        };
        config.Bind(ArglonBotConfiguration.SectionName, settings);

        Assert.Multiple(() =>
        {
            Assert.That(settings.PeriodicOpenMouthSettings.NotificationTime, Is.EqualTo(expectedNotificationTime));
            Assert.That(settings.PeriodicOpenMouthSettings.NotificationInterval, Is.EqualTo(expectedNotificationInterval));
        });
    }
}