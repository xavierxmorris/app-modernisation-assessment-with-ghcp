using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Contoso.Mapping
{
    public static class JsonExtensions
    {
        // System.Net.Http 4.3.x is on the package list — known transitive dep mess.
        private static readonly HttpClient _http = new HttpClient();

        public static async Task<T> FetchAsync<T>(string url)
        {
            var json = await _http.GetStringAsync(url);
            return JsonConvert.DeserializeObject<T>(json,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto // ⚠ insecure with untrusted input
                });
        }

        public static string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(value, Formatting.Indented);
        }
    }
}
