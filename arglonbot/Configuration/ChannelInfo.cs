using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Options;

namespace arglonbot.Configuration;

public class ChannelInfo()
{
    public static ChannelInfo None { get; } = new() { Name = string.Empty };

    [Required]
    public required string Name { get; set; }

    [Required] 
    public ulong GuildId { get; set; }


    [Required]
    public ulong ChannelId { get; set; }

    [ValidateEnumeratedItems]
    public List<MessageInfo> Messages { get; set; } = [];

    [SetsRequiredMembers]
    public ChannelInfo(string name, ulong guildId, ulong channelId, List<MessageInfo> messages)
        : this()
    {
        Name = name;
        GuildId = guildId;
        ChannelId = channelId;
        Messages = messages;
    }
}
