using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Newtonsoft.Json;

namespace TestAuto
{
    public class StatusControllerAuthTests
    {
        private readonly string _baseUrl = "http://localhost:5000/api";

        private async Task<string?> GetJwtTokenAsync(string username, string password)
        {
            using var client = new HttpClient();
            var loginData = new { Username = username, Password = password };
            var content = new StringContent(JsonConvert.SerializeObject(loginData), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_baseUrl}/auth/login", content);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            dynamic obj = JsonConvert.DeserializeObject(json);
            return (string?)obj?.token;
        }

    }
}
