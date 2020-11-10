using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DynamicDns.Services
{
    public record IPResponse(string ip);

    public interface IIPService
    {
        Task<string> GetIPAsync();
    }

    public class IPService : IIPService
    {
        private readonly HttpClient _httpClient;

        public IPService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetIPAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<IPResponse>("https://api.ipify.org?format=json");

            return response.ip;
        }
    }
}
