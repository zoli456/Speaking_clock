using System.Xml.Linq;

namespace Speaking_clock.Widgets;

public class MoonData
{
    /// <summary>
    ///     Percent of the Moon’s disk that’s illuminated (e.g. 98.8).
    /// </summary>
    public int PercentIlluminated { get; set; }

    /// <summary>
    ///     Name of the lunar phase (e.g. "Waxing Gibbous").
    /// </summary>
    public string Phase { get; set; }
}

public static class MoonService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    /// <summary>
    ///     Downloads and parses moonlite.xml from iohelix.net, returning percentIlluminated and phase.
    /// </summary>
    public static async Task<MoonData> LoadMoonDataAsync()
    {
        const string url = "https://iohelix.net/moon/moonlite.xml";

        // Download the XML document
        using var response = await HttpClient.GetAsync(url).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        // Load into LINQ-to-XML
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var doc = XDocument.Load(stream);

        // Navigate to <data><moon>
        var moonElem = doc.Root?
            .Element("moon");

        if (moonElem == null)
            throw new InvalidOperationException("Missing <data><moon> element in XML.");

        // Extract the two values
        var pctText = moonElem.Element("percentIlluminated")?.Value;
        var phaseText = moonElem.Element("phase")?.Value;

        if (pctText == null) throw new InvalidOperationException("Missing <percentIlluminated>.");
        if (phaseText == null) throw new InvalidOperationException("Missing <phase>.");

        pctText = pctText.Replace(".", ",");

        if (!double.TryParse(pctText, out var pct))
            throw new FormatException($"Invalid number in <percentIlluminated>: '{pctText}'");

        return new MoonData
        {
            PercentIlluminated = (int)Math.Truncate(pct),
            Phase = phaseText.Trim()
        };
    }
}