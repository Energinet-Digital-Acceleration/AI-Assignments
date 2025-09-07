using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using Energinet.DataTransform.Console;

// Interaktivt konsolprogram:
// 1) Dagens 3 billigste timer (DK1) + kaffe anbefaling
// 2) Top 5 prisforskelle mellem DK1 og DK2 (seneste 7 dage)
// Q) Afslut
// Brugeren vælger ved at taste 1 / 2 / Q.

await RunAsync();

static async Task RunAsync()
{
    using var http = new HttpClient();
    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("Vælg funktion:");
        Console.WriteLine("  1) Dagens 3 billigste timer (DK1) + kaffe anbefaling");
        Console.WriteLine("  2) Top 5 prisforskelle DK1 vs DK2 (seneste 7 dage)");
        Console.WriteLine("  Q) Afslut");
        Console.Write("Valg: ");
        var choice = Console.ReadLine()?.Trim().ToUpperInvariant();
        switch (choice)
        {
            case "1":
                decimal threshold = 0.50m;
                Console.Write($"Threshold for kaffe (DKK, ENTER for {threshold.ToString(CultureInfo.InvariantCulture)}): ");
                var tLine = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(tLine) && decimal.TryParse(tLine, NumberStyles.Number, CultureInfo.InvariantCulture, out var tParsed))
                    threshold = tParsed;
                try
                {
                    await ShowCheapestTodayAndCoffeeAsync(http, threshold);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fejl: " + ex.Message);
                }
                break;
            case "2":
                try
                {
                    await ShowTopDifferencesLast7DaysAsync(http);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fejl: " + ex.Message);
                }
                break;
            case "Q":
                Console.WriteLine("Farvel.");
                return;
            default:
                Console.WriteLine("Ugyldigt valg. Prøv igen.");
                break;
        }
    }
}

// === Funktioner ===

static async Task ShowCheapestTodayAndCoffeeAsync(HttpClient client, decimal threshold)
{
    var today = DateTime.UtcNow.Date; // UTC baseret filtrering
    string start = today.ToString("yyyy-MM-dd") + "T00:00";
    string end = today.ToString("yyyy-MM-dd") + "T23:59";

    string filterJson = "{\"PriceArea\":[\"DK1\"]}";
    string url = $"https://api.energidataservice.dk/dataset/Elspotprices?start={start}&end={end}&filter={Uri.EscapeDataString(filterJson)}&sort=HourUTC&limit=500";

    Console.WriteLine("Henter elspotpriser (DK1) for i dag...");
    using var response = await client.GetAsync(url);

    if ((int)response.StatusCode == 429)
    {
        Console.WriteLine("API rate limit (429 Too Many Requests). Prøv igen senere.");
        return;
    }

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Fejl ved kald til API. Status: {(int)response.StatusCode} {response.ReasonPhrase}");
        return;
    }

    await using var stream = await response.Content.ReadAsStreamAsync();
    var json = await JsonDocument.ParseAsync(stream);
    if (!json.RootElement.TryGetProperty("records", out var recordsEl) || recordsEl.ValueKind != JsonValueKind.Array)
    {
        Console.WriteLine("Uventet JSON-format: 'records' mangler.");
        return;
    }

    var hourly = new List<(DateTime Hour, decimal Price)>();

    foreach (var rec in recordsEl.EnumerateArray())
    {
        if (!TryGetSpotPrice(rec, out var price)) continue;
        if (!TryGetHour(rec, out var dt)) continue;
        hourly.Add((dt, price));
    }

    if (hourly.Count == 0)
    {
        Console.WriteLine("Ingen priser fundet.");
        return;
    }

    var cheapest3 = hourly
        .OrderBy(h => h.Price)
        .ThenBy(h => h.Hour)
        .Take(3)
        .ToList();

    Console.WriteLine("3 billigste timer (time: pris):");
    foreach (var c in cheapest3)
        Console.WriteLine($"{c.Hour:HH}: {c.Price.ToString(CultureInfo.InvariantCulture)}");
    Console.WriteLine($"(Hentet {hourly.Count} timer)");

    // Coffee decision
    var tz = ResolveCopenhagenTimeZone();
    var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
    var current = hourly.FirstOrDefault(h => h.Hour.Hour == nowLocal.Hour);
    if (current.Hour == default && current.Price == default)
    {
        Console.WriteLine("Kunne ikke finde pris for nuværende time.");
        return;
    }

    Console.WriteLine($"Aktuel time {nowLocal:HH}:00 pris: {current.Price.ToString(CultureInfo.InvariantCulture)} (threshold {threshold.ToString(CultureInfo.InvariantCulture)})");
    Console.WriteLine(current.Price < threshold ? "Brew coffee now!" : "Wait, too expensive ☕⚡");
}

static async Task ShowTopDifferencesLast7DaysAsync(HttpClient client)
{
    var endDay = DateTime.UtcNow.Date; // inclusive dagens dato
    var startDay = endDay.AddDays(-7); // sidste 7 dage
    string start = startDay.ToString("yyyy-MM-dd") + "T00:00";
    string end = endDay.ToString("yyyy-MM-dd") + "T23:59";

    string filterJson = "{\"PriceArea\":[\"DK1\",\"DK2\"]}";
    string url = $"https://api.energidataservice.dk/dataset/Elspotprices?start={start}&end={end}&filter={Uri.EscapeDataString(filterJson)}&sort=HourUTC&limit=1000";

    Console.WriteLine("Henter priser for DK1 og DK2 (seneste 7 dage)...");
    using var response = await client.GetAsync(url);

    if ((int)response.StatusCode == 429)
    {
        Console.WriteLine("API rate limit (429 Too Many Requests). Prøv igen senere.");
        return;
    }

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Fejl ved API kald. Status: {(int)response.StatusCode} {response.ReasonPhrase}");
        return;
    }

    await using var stream = await response.Content.ReadAsStreamAsync();
    var json = await JsonDocument.ParseAsync(stream);
    if (!json.RootElement.TryGetProperty("records", out var recordsEl) || recordsEl.ValueKind != JsonValueKind.Array)
    {
        Console.WriteLine("Uventet JSON-format: 'records' mangler.");
        return;
    }

    var dk1 = new Dictionary<DateTime, decimal>();
    var dk2 = new Dictionary<DateTime, decimal>();

    foreach (var rec in recordsEl.EnumerateArray())
    {
        if (!rec.TryGetProperty("PriceArea", out var areaProp) || areaProp.ValueKind != JsonValueKind.String) continue;
        var area = areaProp.GetString();
        if (area is not ("DK1" or "DK2")) continue;
        if (!TryGetSpotPrice(rec, out var price)) continue; // kræver SpotPriceDKK eller EUR (vi bruger DKK, hvis kun EUR -> skip?)
        if (!TryGetHourUTC(rec, out var hourUtc)) continue;

        // Brug kun timer med DKK hvis tilgængeligt; hvis ingen DKK -> skip (sikrer sammenlignelighed)
        if (rec.TryGetProperty("SpotPriceDKK", out var dkkProp) && dkkProp.ValueKind == JsonValueKind.Number && dkkProp.TryGetDecimal(out var dkkPrice))
            price = dkkPrice; // sikr DKK
        else if (!rec.TryGetProperty("SpotPriceDKK", out _))
            continue;

        var dict = area == "DK1" ? dk1 : dk2;
        dict[hourUtc] = price;
    }

    var commonHours = dk1.Keys.Intersect(dk2.Keys).OrderBy(h => h);
    var diffs = new List<(DateTime HourUtc, decimal DK1, decimal DK2, decimal Diff)>();
    foreach (var h in commonHours)
    {
        var p1 = dk1[h];
        var p2 = dk2[h];
        diffs.Add((h, p1, p2, Math.Abs(p1 - p2)));
    }

    if (diffs.Count == 0)
    {
        Console.WriteLine("Ingen fælles timer med DKK-priser fundet.");
        return;
    }

    var top5 = diffs.OrderByDescending(d => d.Diff).ThenBy(d => d.HourUtc).Take(5).ToList();

    Console.WriteLine("Top 5 største prisforskelle (HourUTC | DK1 | DK2 | Diff DKK)");
    foreach (var d in top5)
    {
        Console.WriteLine($"{d.HourUtc:yyyy-MM-dd HH}: {d.DK1.ToString(CultureInfo.InvariantCulture)} | {d.DK2.ToString(CultureInfo.InvariantCulture)} | {d.Diff.ToString(CultureInfo.InvariantCulture)}");
    }
}

// === Hjælpemetoder ===

static bool TryGetSpotPrice(JsonElement rec, out decimal price)
{
    price = default;
    if (rec.TryGetProperty("SpotPriceDKK", out var dkk) && dkk.ValueKind == JsonValueKind.Number && dkk.TryGetDecimal(out var dkkVal))
    {
        price = dkkVal; return true;
    }
    if (rec.TryGetProperty("SpotPriceEUR", out var eur) && eur.ValueKind == JsonValueKind.Number && eur.TryGetDecimal(out var eurVal))
    {
        price = eurVal; return true;
    }
    return false;
}

static bool TryGetHour(JsonElement rec, out DateTime dt)
{
    dt = default;
    if (rec.TryGetProperty("HourDK", out var dk) && dk.ValueKind == JsonValueKind.String && DateTime.TryParse(dk.GetString(), out dt)) return true;
    if (rec.TryGetProperty("HourUTC", out var utc) && utc.ValueKind == JsonValueKind.String && DateTime.TryParse(utc.GetString(), out dt)) return true;
    return false;
}

static bool TryGetHourUTC(JsonElement rec, out DateTime dt)
{
    dt = default;
    if (rec.TryGetProperty("HourUTC", out var utc) && utc.ValueKind == JsonValueKind.String && DateTime.TryParse(utc.GetString(), out dt)) return true;
    return false;
}

static TimeZoneInfo ResolveCopenhagenTimeZone()
{
    try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen"); }
    catch
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"); }
        catch { return TimeZoneInfo.Local; }
    }
}