using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyBase.Support.Shared
{
    public class APIResponse<T>
    {
        public bool Success { get; set; }
        public int ResponseCode { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public static APIResponse<T> Ok(T data, string? message = null)
        {
            return new APIResponse<T>
            {
                Success = true,
                ResponseCode = 200,
                Message = message ?? "Request completed successfully.",
                Data = data
            };
        }

        public static APIResponse<T> BadRequest(string message, List<string>? errors = null)
        {
            return new APIResponse<T>
            {
                Success = false,
                ResponseCode = 400,
                Message = message,
                Errors = errors
            };
        }

        public static APIResponse<T> InternalServerError(string message, List<string>? errors = null)
        {
            return new APIResponse<T>
            {
                Success = false,
                ResponseCode = 500,
                Message = message,
                Errors = errors
            };
        }
        public static APIResponse<T> NotFound(string message, List<string>? errors = null)
        {
            return new APIResponse<T>
            {
                Success = false,
                ResponseCode = 404,
                Message = message,
                Errors = errors
            };
        }
    }
}
