using Microsoft.Extensions.Logging;

namespace HospitalManagement.Modules.IdentityAccess.Infrastructure;

internal static partial class IdentityAccessLog
{
    [LoggerMessage(
        EventId = 2101,
        Level = LogLevel.Warning,
        Message = "MOCK identity message delivery failed; recipient address and action code were not logged.")]
    internal static partial void IdentityMessageDeliveryFailed(ILogger logger);
}
