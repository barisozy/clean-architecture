using Google.Protobuf;

namespace IntegrationTests.Observability;

/// <summary>
/// Minimal reader for OTLP/HTTP protobuf log payloads. The generated OTLP types are internal to
/// the exporter and sink assemblies, so this walks the wire format directly:
/// ExportLogsServiceRequest → resource_logs → scope_logs → log_records.
/// </summary>
internal static class OtlpLogPayload
{
    // Field numbers from the OTLP protocol definitions (opentelemetry-proto).
    private const int ResourceLogsField = 1;
    private const int ResourceLogsScopeLogsField = 2;
    private const int ScopeLogsLogRecordsField = 2;
    private const int LogRecordAttributesField = 6;
    private const int LogRecordTraceIdField = 9;
    private const int LogRecordSpanIdField = 10;
    private const int KeyValueKeyField = 1;
    private const int KeyValueValueField = 2;
    private const int AnyValueStringField = 1;

    public static IReadOnlyList<OtlpLogRecord> Parse(byte[] payload)
    {
        var records = new List<OtlpLogRecord>();

        foreach (byte[] resourceLogs in ReadMessages(payload, ResourceLogsField))
        {
            foreach (byte[] scopeLogs in ReadMessages(resourceLogs, ResourceLogsScopeLogsField))
            {
                foreach (byte[] logRecord in ReadMessages(scopeLogs, ScopeLogsLogRecordsField))
                {
                    records.Add(ReadLogRecord(logRecord));
                }
            }
        }

        return records;
    }

    private static OtlpLogRecord ReadLogRecord(byte[] bytes)
    {
        byte[] traceId = [];
        byte[] spanId = [];
        var attributes = new Dictionary<string, string>();

        using CodedInputStream input = new(bytes);

        while (!input.IsAtEnd)
        {
            uint tag = input.ReadTag();

            switch (WireFormat.GetTagFieldNumber(tag))
            {
                case LogRecordTraceIdField:
                    traceId = ReadBytes(input);
                    break;
                case LogRecordSpanIdField:
                    spanId = ReadBytes(input);
                    break;
                case LogRecordAttributesField:
                    ReadAttribute(ReadBytes(input), attributes);
                    break;
                default:
                    input.SkipLastField();
                    break;
            }
        }

        return new OtlpLogRecord(traceId, spanId, attributes);
    }

    private static void ReadAttribute(byte[] bytes, Dictionary<string, string> attributes)
    {
        string key = string.Empty;
        string? value = null;

        using CodedInputStream input = new(bytes);

        while (!input.IsAtEnd)
        {
            uint tag = input.ReadTag();

            switch (WireFormat.GetTagFieldNumber(tag))
            {
                case KeyValueKeyField:
                    key = input.ReadString();
                    break;
                case KeyValueValueField:
                    value = ReadStringValue(ReadBytes(input));
                    break;
                default:
                    input.SkipLastField();
                    break;
            }
        }

        if (value is not null)
        {
            attributes[key] = value;
        }
    }

    private static string? ReadStringValue(byte[] anyValue)
    {
        using CodedInputStream input = new(anyValue);

        while (!input.IsAtEnd)
        {
            uint tag = input.ReadTag();

            if (WireFormat.GetTagFieldNumber(tag) == AnyValueStringField)
            {
                return input.ReadString();
            }

            input.SkipLastField();
        }

        return null;
    }

    private static IEnumerable<byte[]> ReadMessages(byte[] bytes, int fieldNumber)
    {
        using CodedInputStream input = new(bytes);

        while (!input.IsAtEnd)
        {
            uint tag = input.ReadTag();

            if (WireFormat.GetTagFieldNumber(tag) == fieldNumber)
            {
                yield return ReadBytes(input);
            }
            else
            {
                input.SkipLastField();
            }
        }
    }

    private static byte[] ReadBytes(CodedInputStream input) =>
        input.ReadBytes().ToByteArray();
}
