using System.Globalization;

namespace Energinet.DataTransform.Console;

public static class Csv
{
    public static List<(DateTimeOffset timestamp, double? mw)> Read(string path)
    {
        var list = new List<(DateTimeOffset, double?)>();
        foreach (var (line, idx) in File.ReadLines(path).Select((l, i) => (l, i)))
        {
            if (idx == 0 || string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            if (parts.Length < 2) continue;
            if (!DateTimeOffset.TryParse(parts[0], null, DateTimeStyles.AssumeUniversal, out var ts)) continue;
            if (double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
                list.Add((ts, val));
            else
                list.Add((ts, null));
        }
        return list;
    }

    public static void Write(string path, IEnumerable<(DateTimeOffset ts, double mw, double rolling)> rows)
    {
        using var w = new StreamWriter(path);
        w.WriteLine("timestamp,mw,rolling24h");
        foreach (var r in rows)
            w.WriteLine($"{r.ts:u},{r.mw.ToString(CultureInfo.InvariantCulture)},{r.rolling.ToString(CultureInfo.InvariantCulture)}");
    }
}