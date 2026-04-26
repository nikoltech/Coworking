using Coworking.External.Squidex.Abstractions.Filters;

namespace Coworking.Application.Ports.Squidex.Schemas.City;

public static class CityPaths
{
    public static readonly string Title = SquidexPaths.Iv("Title");
    public static readonly string IsRegionCity = SquidexPaths.Iv("IsRegionCity");
    public static readonly string SOrder = SquidexPaths.Iv("SOrder");
    public static readonly string PlaceId = SquidexPaths.Iv("PlaceId");

    public static class OData
    {
        public static readonly string Title = SquidexPaths.ODataIv("Title");
        public static readonly string IsRegionCity = SquidexPaths.ODataIv("IsRegionCity");
        public static readonly string SOrder = SquidexPaths.ODataIv("SOrder");
    }
}