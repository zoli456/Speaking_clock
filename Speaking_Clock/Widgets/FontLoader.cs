using System.Diagnostics;
using System.Globalization;
using Vortice.DirectWrite;

namespace Speaking_clock.Widgets;

public static class DirectWriteFontLoader
{
    public static IDWriteFontCollection1? LoadFontCollection(
        IDWriteFactory factory,
        string[] fontFilePaths,
        out List<string> loadedFontFamilyNames)
    {
        loadedFontFamilyNames = new List<string>();

        if (factory is null || fontFilePaths == null || fontFilePaths.Length == 0)
            return null;

        using var factory3 = factory.QueryInterface<IDWriteFactory3>();
        if (factory3 is null)
        {
            Debug.WriteLine(
                "Error: Failed to query IDWriteFactory3. System might not support DirectWrite v3 features.");
            return null;
        }

        using var baseFontSetBuilder = factory3.CreateFontSetBuilder();
        if (baseFontSetBuilder is null)
        {
            Debug.WriteLine("Error: Failed to create base FontSetBuilder.");
            return null;
        }

        using var fontSetBuilder = baseFontSetBuilder.QueryInterface<IDWriteFontSetBuilder1>();
        if (fontSetBuilder is null)
        {
            Debug.WriteLine(
                "Error: Failed to query IDWriteFontSetBuilder1 from base builder. AddFontFile method will not be available.");
            return null;
        }

        foreach (var path in fontFilePaths)
        {
            if (!File.Exists(path))
            {
                Debug.WriteLine($"Warning: Font file not found at path: {path}");
                continue;
            }

            try
            {
                using var fontFile = factory3.CreateFontFileReference(path);
                if (fontFile != null)
                    fontSetBuilder.AddFontFile(fontFile);
                else
                    Debug.WriteLine($"Warning: Failed to create font file reference for {path}, returned null.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing font file {path}: {ex.Message}");
            }
        }

        using var fontSet = fontSetBuilder.CreateFontSet();
        if (fontSet is null)
        {
            Debug.WriteLine("Error: Failed to create font set from builder.");
            return null;
        }

        var fontCollection = factory3.CreateFontCollectionFromFontSet(fontSet);
        if (fontCollection is null)
        {
            Debug.WriteLine("Error: Failed to create font collection from font set.");
            return null;
        }

        for (uint i = 0; i < fontCollection.FontFamilyCount; i++)
        {
            using var family = fontCollection.GetFontFamily(i);
            using var names = family.FamilyNames;

            string? familyName = null;
            if (names.FindLocaleName(CultureInfo.CurrentUICulture.Name, out var idxLocalized))
                familyName = names.GetString(idxLocalized);
            else if (names.FindLocaleName("en-us", out var idxEnUs))
                familyName = names.GetString(idxEnUs);
            else if (names.Count > 0)
                familyName = names.GetString(0);

            if (!string.IsNullOrEmpty(familyName)) loadedFontFamilyNames.Add(familyName);
        }

        return fontCollection;
    }
}