using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace arglonbot.Configuration;

public class MessageInfo()
{

    public enum AfterType
    {
        None,
        MoonPhase
    }

    public enum ReplacementType
    {
        Substitute,
        YearsSinceStart,
        MonthsSinceStart
    }

    public class AfterInfo()
    {
        [Required]
        public AfterType Type { get; set; }

        [Required] 
        public required string Value { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public WeekOfMonth WeekOfInterval { get; set; }

        [SetsRequiredMembers]
        public AfterInfo(AfterType type, string value, DayOfWeek dayOfWeek, WeekOfMonth weekOfInterval)
            : this()
        {
            Type = type;
            Value = value;
            DayOfWeek = dayOfWeek;
            WeekOfInterval = weekOfInterval;
        }
    }

    public class TokenReplacement()
    {
        [Required]
        public required string Token { get; set; }

        [Required]
        public ReplacementType ReplacementType { get; set; }

        public required string Value { get; set; } = "";

        [SetsRequiredMembers]
        public TokenReplacement(string token, ReplacementType replacementType, string value = "")
            : this()
        {
            Token = token;
            ReplacementType = replacementType;
            Value = value;
        }
    }

    [SetsRequiredMembers]
    public MessageInfo(
        [Required] string message,
        DateTime? date = default,
        DateTime? dateStart = default,
        DateTime? dateEnd = default,
        Month? month = default,
        DayOfWeek? dayOfWeek = default,
        WeekOfMonth? weekOfMonth = default,
        AfterInfo? after = null)
        : this()
    {
        Message = message;
        Date = date;
        DateStart = dateStart;
        DateEnd = dateEnd;
        Month = month;
        DayOfWeek = dayOfWeek;
        WeekOfMonth = weekOfMonth;
        After = after;
    }

    [Required]
    public required string Message { get; set; }

    public DateTime? Date { get; set; }

    public DateTime? DateStart { get; set; }

    public DateTime? DateEnd { get; set; }

    public Month? Month { get; set; }

    public DayOfWeek? DayOfWeek { get; set; }

    public WeekOfMonth? WeekOfMonth { get; set; }

    [ValidateObjectMembers]
    public AfterInfo? After { get; set; } = null;

    [ValidateEnumeratedItems]
    public List<TokenReplacement> TokenReplacements { get; set; } = [];
}