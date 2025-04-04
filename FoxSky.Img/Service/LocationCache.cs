using GeoCoordinatePortable;
using GoogleApi.Entities.Common;

namespace FoxSky.Img.Service
{
    public class LocationCache
    {
        private static List<Tuple<Coordinate, string>> locationCache = new();

        public string? GetCachedLocation(Coordinate location, string radius)
        {
            return locationCache.FirstOrDefault(c => IsWithinDistance(location, c.Item1, double.Parse(radius)))?.Item2;
        }

        public void AddLocationToCache(Coordinate location, string locationName)
        {
            if (!string.IsNullOrEmpty(locationName))
            {
                locationCache.Add(new(location, locationName));
            }
        }

        private static bool IsWithinDistance(Coordinate loc1, Coordinate loc2, double distanceInMeters)
        {
            var coord1 = new GeoCoordinate(loc1.Latitude, loc1.Longitude);
            var coord2 = new GeoCoordinate(loc2.Latitude, loc2.Longitude);

            return coord1.GetDistanceTo(coord2) <= distanceInMeters;
        }
    }
}
