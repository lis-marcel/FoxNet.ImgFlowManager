using FoxSky.Img.Utilities;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text;
using GoogleApi.Entities.Common;
using System.Device.Location;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxSky.Img.Service
{
    public class GeolocationService
    {
        private readonly string requestBaseUri = "https://nominatim.openstreetmap.org/reverse?";
        private readonly LocationCache locationCache;

        public GeolocationService(LocationCache locationCache)
        {
            this.locationCache = locationCache;
        }

        public async Task<string> ReverseGeolocationRequestTask(string imagePath, string userEmail, string radius)
        {
            var location = CreateLocation(imagePath);
            string fullLocationName;

            if (location == null)
            {
                return string.Empty;
            }

            var cachedLocation = locationCache.GetCachedLocation(location, radius);

            if (!string.IsNullOrEmpty(cachedLocation))
            {
                return cachedLocation;
            }
            else
            {
                StringBuilder sb = new();

                sb.Append($"{requestBaseUri}" +
                    $"format=json&email={userEmail}" +
                    $"&lat={location.Latitude.ToString(CultureInfo.InvariantCulture)}" +
                    $"&lon={location.Longitude.ToString(CultureInfo.InvariantCulture)}" +
                    $"&zoom=13&addressdetails=1&accept-language=en");

                string requestUri = sb.ToString();

                var response = await new HttpClient().GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    sb.Clear();

                    sb.Append('_');
                    sb.Append(ExtractCityAndCountry(await response.Content.ReadAsStringAsync()));

                    fullLocationName = sb.ToString();

                    locationCache.AddLocationToCache(location, fullLocationName);
                }
                else
                {
                    fullLocationName = string.Empty;
                }
            }

            return fullLocationName;
        }

        private static string ExtractCityAndCountry(string response)
        {
            var json = JObject.Parse(response);
            var address = json["address"];
            string fullLocation = string.Empty;
            StringBuilder sb = new();

            if (address != null)
            {
                var city = address["village"]?.ToString() ??
                           address["city"]?.ToString() ??
                           address["town"]?.ToString() ??
                           address["municipality"]?.ToString() ??
                           address["county"]?.ToString();

                sb.Append(city);

                sb.Append('_');

                var country = address?["country"]?.ToString();

                sb.Append(country);

                fullLocation = sb.ToString();
            }

            if (!string.IsNullOrEmpty(fullLocation))
            {
                TextUtils.ReplaceSpecialCharacters(fullLocation);
            }

            return fullLocation;
        }

        private static Coordinate? CreateLocation(string imgPath)
        {
            Coordinate? res = null;

            var gps = ImageMetadataReader.ReadMetadata(imgPath)?
                .OfType<GpsDirectory>()?
                .FirstOrDefault();

            var location = gps?.GetGeoLocation();

            if (location != null)
            {
                double lat = location.Latitude;
                double lon = location.Longitude;

                res = new(lat, lon);
            }

            return res;
        }
    }
}
