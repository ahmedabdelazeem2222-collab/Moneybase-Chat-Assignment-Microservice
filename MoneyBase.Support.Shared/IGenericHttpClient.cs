using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyBase.Support.Shared
{
    public interface IGenericHttpClient
    {
        Task<APIResponse<T>> GetAsync<T>(string url);
        Task<APIResponse<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest data);
        Task<APIResponse<TResponse>> PutAsync<TRequest, TResponse>(string url, TRequest data);
    }
}
