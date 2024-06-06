using ExifLib;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Maps.Geocoding.Location.Request;
using Newtonsoft.Json;

namespace FoxSky.Img
{
    public class GeolocationUtils
    {
        private static string _apiKey => "";

        public static string? ReverseGeolocationRequestTask(string imgPath)
        {
            var location = CreateLocation(imgPath);
            string? res = null;

            if (location != null)
            {

                LocationGeocodeRequest locationGeocodeRequest = new()
                {
                    Key = _apiKey,
                    Location = location
                };

                var apiResponse = GoogleApi.GoogleMaps.Geocode.LocationGeocode.QueryAsync(locationGeocodeRequest).Result;

                if (apiResponse.Status == GoogleApi.Entities.Common.Enums.Status.Ok)
                {
                    var deserializedResponse = JsonConvert.DeserializeObject<Root>(apiResponse.RawJson.ToString());

                    var queryResult = deserializedResponse?.results?.FirstOrDefault()?.address_components
                        ?.Where(ac => ac.types.Contains("country") || ac.types.Contains("locality"))
                        ?.ToDictionary(ac => ac.types.Contains("country") ? "Country" : "City", ac => ac.long_name);

                    queryResult?.TryGetValue("City", out res);
                    res = res?.ReplaceSpecialCharacters();
                }
            }

            return res;
        }
        private static Coordinate? CreateLocation(string imgPath)
        {
            using var reader = new ExifReader(imgPath);
            if (reader.GetTagValue(ExifTags.GPSLatitude, out double[] latitudeTab) &&
                reader.GetTagValue(ExifTags.GPSLongitude, out double[] longitudeTab))
            {
                reader.GetTagValue(ExifTags.GPSLatitudeRef, out string latRef);
                reader.GetTagValue(ExifTags.GPSLongitudeRef, out string longRef);

                double lat = (latRef == "N" ? 1 : -1) * Math.Abs(latitudeTab[0] + (latitudeTab[1] / 60.0) + (latitudeTab[2] / 3600.0));
                double lon = (longRef == "E" ? 1 : -1) * Math.Abs(longitudeTab[0] + (longitudeTab[1] / 60.0) + (longitudeTab[2] / 3600.0));

                Coordinate coordinate = new(lat, lon);

                return coordinate;
            }

            else return null;
        }
    }
}
