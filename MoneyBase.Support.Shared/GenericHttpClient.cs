using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace MoneyBase.Support.Shared
{
    public class GenericHttpClient : IGenericHttpClient
    {
        private readonly HttpClient _httpClient;

        public GenericHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<APIResponse<T>> GetAsync<T>(string url)
        {
            var response = await _httpClient.GetFromJsonAsync<APIResponse<T>>(url);
            return response!;
        }

        public async Task<APIResponse<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest data)
        {
            var response = await _httpClient.PostAsJsonAsync(url, data);
            return await response.Content.ReadFromJsonAsync<APIResponse<TResponse>>()!;
        }

        public async Task<APIResponse<TResponse>> PutAsync<TRequest, TResponse>(string url, TRequest data)
        {
            var response = await _httpClient.PutAsJsonAsync(url, data);
            return await response.Content.ReadFromJsonAsync<APIResponse<TResponse>>()!;
        }
    }
}
