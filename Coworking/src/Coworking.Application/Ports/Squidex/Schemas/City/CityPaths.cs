using Coworking.External.Squidex.Abstractions.Filters;

public static class CityPaths
{
    // ── JSON paths (dot) — use with RequestQuery ──────────────────────────────
    public static readonly string Title = SquidexPaths.Iv("Title");
    public static readonly string IsRegionCity = SquidexPaths.Iv("IsRegionCity");
    public static readonly string SOrder = SquidexPaths.Iv("SOrder");
    public static readonly string PlaceId = SquidexPaths.Iv("PlaceId");

    // ── OData paths (slash) — use with ODataQuery ─────────────────────────────
    public static class OData
    {
        public static readonly string Title = SquidexPaths.ODataIv("Title");
        public static readonly string IsRegionCity = SquidexPaths.ODataIv("IsRegionCity");
        public static readonly string SOrder = SquidexPaths.ODataIv("SOrder");
    }
}