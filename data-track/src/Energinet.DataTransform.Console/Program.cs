using Energinet.DataTransform.Console;

if (args.Length == 0 || args.Contains("--help"))
{
    Console.WriteLine("Usage: --input <path> --output <path> [--window <hours=24>]");
    return;
}

string? input = null, output = null; var window = 24;
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--input": input = args[++i]; break;
        case "--output": output = args[++i]; break;
        case "--window": window = int.Parse(args[++i]); break;
    }
}
if (input is null || output is null) { Console.Error.WriteLine("Missing --input or --output"); Environment.Exit(1); }

var rows = Csv.Read(input);
var cleaned = rows.Where(r => r.mw.HasValue).Select(r => new Row(r.timestamp, r.mw!.Value)).ToList();
var withAvg = RollingAverage.Add(cleaned, window);
Csv.Write(output, withAvg);