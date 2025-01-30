using System.Diagnostics;
using System.Xml.Linq;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;


namespace Speaking_clock;

public static class NvidiaDriverFinder
{
    private static readonly string LookupUrl = "https://www.nvidia.com/Download/API/lookupValueSearch.aspx?TypeID=3";
    private static readonly string DriverBaseUrl = "https://www.nvidia.com/Download/processDriver.aspx";
    private static readonly HttpClient HttpClient = new();

    public static async Task<string> GetLatestNvidiaDriverForGpuAsync(string gpuName, Driver driver, bool Windows10)
    {
        var gpuEntry = await GetGpuEntryAsync(gpuName);
        if (gpuEntry == null)
        {
            MessageBox.Show("Nem sikerült egyezést találni.", "Hiba", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            throw new Exception($"GPU '{gpuName}' not found.");
        }

        var psid = gpuEntry.Series;
        var pfid = gpuEntry.Id;
        var dtcid = driver.Edition == DriverEdition.DCH ? 1 : 0; // 1=DCH, 0=Standard
        var whql = driver.Channel == DriverChannel.GameReady ? 1 : 4; // 1=Game Ready, 4=Studio
        var osid = Windows10 ? "57" : "135";

        var driverUrl = $"{DriverBaseUrl}?psid={psid}&pfid={pfid}&osid={osid}&lid=1&whql={whql}&dtcid={dtcid}";
        var driverPage = await HttpClient.GetStringAsync(driverUrl);

        //return await ParseDriverNvidiaParseDriverDownloadLink($"https://www.nvidia.com/Download/{driverPage}");
        return $"https://www.nvidia.com/Download/{driverPage}";
    }

    private static async Task<GpuEntry> GetGpuEntryAsync(string gpuName)
    {
        // Fetch XML data from NVIDIA's API
        var xmlData = await HttpClient.GetStringAsync(LookupUrl);

        // Parse XML
        var document = XDocument.Parse(xmlData);

        // Debugging: Check document structure
        Debug.WriteLine("XML Structure:");
        Debug.WriteLine(document);

        foreach (var lookupValue in document.Descendants("LookupValue"))
        {
            var nameElement = lookupValue.Element("Name");
            var seriesElement = lookupValue.Attribute("ParentID");
            var idElement = lookupValue.Element("Value");

            // Debugging: Print individual elements
            Debug.WriteLine($"Name: {nameElement?.Value}, Series: {seriesElement?.Value}, ID: {idElement?.Value}");

            if (nameElement != null &&
                nameElement.Value.Equals(gpuName, StringComparison.OrdinalIgnoreCase)) //Contains->Equal
                return new GpuEntry
                {
                    Name = nameElement.Value,
                    Series = int.Parse(seriesElement?.Value ?? "0"),
                    Id = int.Parse(idElement?.Value ?? "0")
                };
        }

        return null;
    }


    private static async Task<string> ParseDriverNvidiaParseDriverDownloadLink(string pageContent)
    {
        // Load the page content into HtmlAgilityPack's HTML document
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(pageContent);

        // Debugging: Log the loaded HTML content
        Debug.WriteLine("Loaded HTML Content:");
        Debug.WriteLine(pageContent);

        // Locate the download button using an accurate XPath
        var downloadButton =
            htmlDoc.DocumentNode.SelectSingleNode("//a[contains(@class, 'btn-') or contains(@class, 'btn-download')]");

        if (downloadButton == null)
        {
            MessageBox.Show("Nem sikerült megtalálni a letöltési linket.", "Hiba", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            throw new Exception("Download button not found on the driver page.");
        }

        // Get the href attribute from the button
        var downloadLink = downloadButton.GetAttributeValue("href", null);

        if (string.IsNullOrEmpty(downloadLink))
            throw new Exception("Download link not found in the button.");

        // Ensure the link is absolute (modify this transformation based on the actual link structure)
        if (!Uri.IsWellFormedUriString(downloadLink, UriKind.Absolute))
        {
            downloadLink = downloadLink.Replace("&lang=us&type=GeForce", "")
                .Replace("/content/DriverDownloads/confirmation.php?url=", "");
            downloadLink = $"https://uk.download.nvidia.com{downloadLink}";
        }

        return downloadLink;
    }


    private class GpuEntry
    {
        public string Name { get; set; }
        public int Series { get; set; }
        public int Id { get; set; }
    }
}

// Enums and Driver Class (Reused from previous example)
public class Driver
{
    public DriverChannel Channel { get; set; } = DriverChannel.GameReady;
    public DriverEdition Edition { get; set; } = DriverEdition.DCH;
}

public enum DriverChannel
{
    GameReady,
    Studio
}

public enum DriverEdition
{
    DCH,
    STD
}