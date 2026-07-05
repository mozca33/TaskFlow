using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TaskFlow.Api.Persistence;

/// <summary>
/// O SQLite guarda datetime como texto sem fuso e o EF as materializa com
/// <see cref="DateTimeKind.Unspecified"/>, o que faz o timestamp perder o
/// sufixo <c>Z</c> na serialização. Este conversor marca toda data lida como
/// UTC, mantendo o contrato consistente (D9: timestamps em UTC / ISO 8601).
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            write => write,
            read => DateTime.SpecifyKind(read, DateTimeKind.Utc))
    {
    }
}
