using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TaskFlow.Infrastructure.Persistence;

/// <summary>
/// Stores a <see cref="DateTimeOffset"/> as its UTC ticks and reads it back in UTC.
/// The original offset is not kept; the application only produces UTC timestamps.
/// </summary>
internal sealed class UtcTicksConverter() : ValueConverter<DateTimeOffset, long>(
    value => value.UtcTicks,
    ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
