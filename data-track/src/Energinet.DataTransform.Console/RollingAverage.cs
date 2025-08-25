namespace Energinet.DataTransform.Console;

public static class RollingAverage
{
    public static IEnumerable<(DateTimeOffset ts, double mw, double rolling)> Add(IReadOnlyList<Row> rows, int window)
    {
        if (rows.Count == 0) yield break;
        var q = new Queue<double>(); double sum = 0;
        foreach (var (dateTimeOffset, mw) in rows)
        {
            q.Enqueue(mw); sum += mw;
            if (q.Count > window) sum -= q.Dequeue();
            var avg = sum / q.Count;
            yield return (dateTimeOffset, mw, Math.Round(avg, 3));
        }
    }
}