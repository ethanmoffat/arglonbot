using Microsoft.Extensions.Options;

namespace arglonbot.Configuration
{
    [OptionsValidator]
    public partial class ArglonBotConfigurationValidator : IValidateOptions<ArglonBotConfiguration>
    {
    }
}
