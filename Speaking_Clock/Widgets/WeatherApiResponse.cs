using System.Text.Json.Serialization;

namespace Speaking_clock.Widgets;

public class WeatherApiResponse
{
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("v3-wx-observations-current")]
    public V3WxObservationsCurrent V3WxObservationsCurrent { get; set; }

    [JsonPropertyName("v3-wx-forecast-daily-7day")]
    public V3WxForecastDaily7Day V3WxForecastDaily7Day { get; set; }
}

// Current weather observations
public class V3WxObservationsCurrent
{
    [JsonPropertyName("cloudCeiling")] public object CloudCeiling { get; set; }

    [JsonPropertyName("cloudCoverPhrase")] public string CloudCoverPhrase { get; set; }

    [JsonPropertyName("dayOfWeek")] public string DayOfWeek { get; set; }

    [JsonPropertyName("dayOrNight")] public string DayOrNight { get; set; }

    [JsonPropertyName("expirationTimeUtc")]
    public long ExpirationTimeUtc { get; set; }

    [JsonPropertyName("iconCode")] public int? IconCode { get; set; }

    [JsonPropertyName("iconCodeExtend")] public int? IconCodeExtend { get; set; }

    [JsonPropertyName("obsQualifierCode")] public object ObsQualifierCode { get; set; }

    [JsonPropertyName("obsQualifierSeverity")]
    public object ObsQualifierSeverity { get; set; }

    [JsonPropertyName("precip1Hour")] public double? Precip1Hour { get; set; }

    [JsonPropertyName("precip6Hour")] public double? Precip6Hour { get; set; }

    [JsonPropertyName("precip24Hour")] public double? Precip24Hour { get; set; }

    [JsonPropertyName("pressureAltimeter")]
    public double? PressureAltimeter { get; set; }

    [JsonPropertyName("pressureChange")] public double? PressureChange { get; set; }

    [JsonPropertyName("pressureMeanSeaLevel")]
    public double? PressureMeanSeaLevel { get; set; }

    [JsonPropertyName("pressureTendencyCode")]
    public int? PressureTendencyCode { get; set; }

    [JsonPropertyName("pressureTendencyTrend")]
    public string PressureTendencyTrend { get; set; }

    [JsonPropertyName("relativeHumidity")] public int? RelativeHumidity { get; set; }

    [JsonPropertyName("snow1Hour")] public double? Snow1Hour { get; set; }

    [JsonPropertyName("snow6Hour")] public double? Snow6Hour { get; set; }

    [JsonPropertyName("snow24Hour")] public double? Snow24Hour { get; set; }

    [JsonPropertyName("sunriseTimeLocal")] public string SunriseTimeLocal { get; set; }

    [JsonPropertyName("sunriseTimeUtc")] public long SunriseTimeUtc { get; set; }

    [JsonPropertyName("sunsetTimeLocal")] public string SunsetTimeLocal { get; set; }

    [JsonPropertyName("sunsetTimeUtc")] public long SunsetTimeUtc { get; set; }

    [JsonPropertyName("temperature")] public int? Temperature { get; set; }

    [JsonPropertyName("temperatureChange24Hour")]
    public int? TemperatureChange24Hour { get; set; }

    [JsonPropertyName("temperatureDewPoint")]
    public int? TemperatureDewPoint { get; set; }

    [JsonPropertyName("temperatureFeelsLike")]
    public int? TemperatureFeelsLike { get; set; }

    [JsonPropertyName("temperatureHeatIndex")]
    public int? TemperatureHeatIndex { get; set; }

    [JsonPropertyName("temperatureMax24Hour")]
    public int? TemperatureMax24Hour { get; set; }

    [JsonPropertyName("temperatureMaxSince7Am")]
    public int? TemperatureMaxSince7Am { get; set; }

    [JsonPropertyName("temperatureMin24Hour")]
    public int? TemperatureMin24Hour { get; set; }

    [JsonPropertyName("temperatureWindChill")]
    public int? TemperatureWindChill { get; set; }

    [JsonPropertyName("uvDescription")] public string UvDescription { get; set; }

    [JsonPropertyName("uvIndex")] public int? UvIndex { get; set; }

    [JsonPropertyName("validTimeLocal")] public string ValidTimeLocal { get; set; }

    [JsonPropertyName("validTimeUtc")] public long ValidTimeUtc { get; set; }

    [JsonPropertyName("visibility")] public double? Visibility { get; set; }

    [JsonPropertyName("windDirection")] public int? WindDirection { get; set; }

    [JsonPropertyName("windDirectionCardinal")]
    public string WindDirectionCardinal { get; set; }

    [JsonPropertyName("windGust")] public object WindGust { get; set; } // Can be int or null

    [JsonPropertyName("windSpeed")] public int? WindSpeed { get; set; }

    [JsonPropertyName("wxPhraseLong")] public string WxPhraseLong { get; set; }

    [JsonPropertyName("wxPhraseMedium")] public string WxPhraseMedium { get; set; }

    [JsonPropertyName("wxPhraseShort")] public string WxPhraseShort { get; set; }
}

// 7-day daily forecast
public class V3WxForecastDaily7Day
{
    [JsonPropertyName("calendarDayTemperatureMax")]
    public List<int?> CalendarDayTemperatureMax { get; set; }

    [JsonPropertyName("calendarDayTemperatureMin")]
    public List<int?> CalendarDayTemperatureMin { get; set; }

    [JsonPropertyName("dayOfWeek")] public List<string> DayOfWeek { get; set; }

    [JsonPropertyName("expirationTimeUtc")]
    public List<long> ExpirationTimeUtc { get; set; }

    [JsonPropertyName("moonPhase")] public List<string> MoonPhase { get; set; }

    [JsonPropertyName("moonPhaseCode")] public List<string> MoonPhaseCode { get; set; }

    [JsonPropertyName("moonPhaseDay")] public List<int> MoonPhaseDay { get; set; }

    [JsonPropertyName("moonriseTimeLocal")]
    public List<string> MoonriseTimeLocal { get; set; }

    [JsonPropertyName("moonriseTimeUtc")] public List<long?> MoonriseTimeUtc { get; set; }

    [JsonPropertyName("moonsetTimeLocal")] public List<string> MoonsetTimeLocal { get; set; }

    [JsonPropertyName("moonsetTimeUtc")] public List<long?> MoonsetTimeUtc { get; set; }

    [JsonPropertyName("narrative")] public List<string> Narrative { get; set; }

    [JsonPropertyName("qpf")] public List<double?> Qpf { get; set; }

    [JsonPropertyName("qpfSnow")] public List<double?> QpfSnow { get; set; }

    [JsonPropertyName("sunriseTimeLocal")] public List<string> SunriseTimeLocal { get; set; }

    [JsonPropertyName("sunriseTimeUtc")] public List<long> SunriseTimeUtc { get; set; }

    [JsonPropertyName("sunsetTimeLocal")] public List<string> SunsetTimeLocal { get; set; }

    [JsonPropertyName("sunsetTimeUtc")] public List<long> SunsetTimeUtc { get; set; }

    [JsonPropertyName("temperatureMax")] public List<int?> TemperatureMax { get; set; }

    [JsonPropertyName("temperatureMin")] public List<int?> TemperatureMin { get; set; }

    [JsonPropertyName("validTimeLocal")] public List<string> ValidTimeLocal { get; set; }

    [JsonPropertyName("validTimeUtc")] public List<long> ValidTimeUtc { get; set; }

    [JsonPropertyName("daypart")] public List<DayPart> Daypart { get; set; }
}

// Daypart forecast
public class DayPart
{
    [JsonPropertyName("cloudCover")] public List<int?> CloudCover { get; set; }

    [JsonPropertyName("dayOrNight")] public List<string> DayOrNight { get; set; }

    [JsonPropertyName("daypartName")] public List<string> DaypartName { get; set; }

    [JsonPropertyName("iconCode")] public List<int?> IconCode { get; set; }

    [JsonPropertyName("iconCodeExtend")] public List<int?> IconCodeExtend { get; set; }

    [JsonPropertyName("narrative")] public List<string> Narrative { get; set; }

    [JsonPropertyName("precipChance")] public List<int?> PrecipChance { get; set; }

    [JsonPropertyName("precipType")] public List<string> PrecipType { get; set; }

    [JsonPropertyName("qpf")] public List<double?> Qpf { get; set; }

    [JsonPropertyName("qpfSnow")] public List<double?> QpfSnow { get; set; }

    [JsonPropertyName("qualifierCode")] public List<object> QualifierCode { get; set; }

    [JsonPropertyName("qualifierPhrase")] public List<object> QualifierPhrase { get; set; }

    [JsonPropertyName("relativeHumidity")] public List<int?> RelativeHumidity { get; set; }

    [JsonPropertyName("snowRange")] public List<string> SnowRange { get; set; }

    [JsonPropertyName("temperature")] public List<int?> Temperature { get; set; }

    [JsonPropertyName("temperatureHeatIndex")]
    public List<int?> TemperatureHeatIndex { get; set; }

    [JsonPropertyName("temperatureWindChill")]
    public List<int?> TemperatureWindChill { get; set; }

    [JsonPropertyName("thunderCategory")] public List<object> ThunderCategory { get; set; }

    [JsonPropertyName("thunderIndex")] public List<int?> ThunderIndex { get; set; }

    [JsonPropertyName("uvDescription")] public List<string> UvDescription { get; set; }

    [JsonPropertyName("uvIndex")] public List<int?> UvIndex { get; set; }

    [JsonPropertyName("windDirection")] public List<int?> WindDirection { get; set; }

    [JsonPropertyName("windDirectionCardinal")]
    public List<string> WindDirectionCardinal { get; set; }

    [JsonPropertyName("windPhrase")] public List<string> WindPhrase { get; set; }

    [JsonPropertyName("windSpeed")] public List<int?> WindSpeed { get; set; }

    [JsonPropertyName("wxPhraseLong")] public List<string> WxPhraseLong { get; set; }

    [JsonPropertyName("wxPhraseShort")] public List<string> WxPhraseShort { get; set; }
}