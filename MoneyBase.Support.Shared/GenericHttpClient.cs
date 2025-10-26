using Microsoft.Extensions.Logging;
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
        private readonly ILogger<GenericHttpClient> _logger;
        public GenericHttpClient(HttpClient httpClient, ILogger<GenericHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<APIResponse<T>> GetAsync<T>(string url)
        {
            _logger.LogInformation("GET request from cash assignement to {Url}", url);
            try
            {
                var response = await _httpClient.GetFromJsonAsync<APIResponse<T>>(url);
                _logger.LogInformation("GET {Url} succeeded", url);
                return response!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET {Url} failed: {Message}", url, ex.Message);
                throw;
            }
        }

        public async Task<APIResponse<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest data)
        {
            _logger.LogInformation("POST request from cash assignement to {Url} with payload: {@Payload}", url, data);
            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, data);
                _logger.LogInformation("POST {Url} returned {StatusCode}", url, response.StatusCode);

                var content = await response.Content.ReadFromJsonAsync<APIResponse<TResponse>>();
                return content!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "POST {Url} failed: {Message}", url, ex.Message);
                throw;
            }
        }

        public async Task<APIResponse<TResponse>> PutAsync<TRequest, TResponse>(string url, TRequest data)
        {
            _logger.LogInformation("PUT request from cash assignement to {Url} with payload: {@Payload}", url, data);

            try
            {
                var response = await _httpClient.PutAsJsonAsync(url, data);
                _logger.LogInformation("PUT {Url} returned {StatusCode}", url, response.StatusCode);

                var content = await response.Content.ReadFromJsonAsync<APIResponse<TResponse>>();
                return content!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PUT {Url} failed: {Message}", url, ex.Message);
                throw;
            }
        }
    }
}
