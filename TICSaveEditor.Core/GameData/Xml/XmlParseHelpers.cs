using System.Globalization;
using System.Xml.Linq;

namespace TICSaveEditor.Core.GameData.Xml;

internal static class XmlParseHelpers
{
    public static int ParseInt(XElement element, string fieldName, int? contextId, string tableLabel)
    {
        var raw = element.Value.Trim();
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            throw new InvalidDataException(
                $"{tableLabel}: cannot parse <{fieldName}> value '{raw}' as int " +
                $"(in entry {(contextId.HasValue ? $"Id={contextId}" : "(Id not yet seen)")}).");
        return v;
    }

    public static byte ParseByte(XElement element, string fieldName, int? contextId, string tableLabel)
    {
        var raw = element.Value.Trim();
        if (!byte.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            throw new InvalidDataException(
                $"{tableLabel}: cannot parse <{fieldName}> value '{raw}' as byte " +
                $"(in entry {(contextId.HasValue ? $"Id={contextId}" : "(Id not yet seen)")}).");
        return v;
    }

    public static string ParseString(XElement element)
        => element.Value.Trim();

    public static InvalidDataException MissingField(string tableLabel, string fieldName, int? id)
        => new(
            $"{tableLabel}: entry " +
            $"{(id.HasValue ? $"Id={id}" : "(Id missing)")} " +
            $"is missing required field '{fieldName}'.");
}
