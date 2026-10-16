namespace IntegrationTests.Observability;

/// <summary>
/// The parts of an OTLP log record that the integration tests assert on.
/// </summary>
internal sealed class OtlpLogRecord(byte[] traceId, byte[] spanId, IReadOnlyDictionary<string, string> attributes)
{
    /// <summary>The record is correlated with a trace: both ids are present and non-zero.</summary>
    public bool HasTraceAndSpanIds =>
        traceId.Length == 16 && traceId.Any(id => id != 0) &&
        spanId.Length == 8 && spanId.Any(id => id != 0);

    public bool HasAttribute(string key, string value) =>
        attributes.TryGetValue(key, out string? actual) && actual == value;
}
