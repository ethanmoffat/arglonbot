using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Options;

namespace arglonbot.Configuration;

[method: SetsRequiredMembers]
public class PeriodicOpenMouthSettings()
{
    [Required]
    public TimeSpan NotificationTime { get; set; }

    public string NotificationTimeZone { get; set; } = "Pacific Standard Time";

    [Required]
    public TimeSpan NotificationInterval { get; set; }

    [ValidateEnumeratedItems]
    public required List<ChannelInfo> Channels { get; set; } = [];

    [ValidateEnumeratedItems]
    public required List<MessageInfo> Messages { get; set; } = [];

    [ValidateEnumeratedItems]
    public required List<MessageInfo> ExtraMessages { get; set; } = [];
}
