using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Services
{
    public class ServiceResult<T>
    {
        public T Data { get; set; } // The resulting data if the operation is successful
        public IQueryable<T> QueryableData { get; set; } // For IQueryable<T> results

        public Dictionary<string, string> Errors { get; set; } // Error messages if the operation fails
        public HttpStatusCode StatusCode { get; set; } // HTTP Status Code
        public bool IsSuccess => StatusCode == HttpStatusCode.OK;

        // Factory methods for convenience
        public static ServiceResult<T> Success(T data) => new ServiceResult<T> { Data = data, StatusCode = HttpStatusCode.OK };

        public static ServiceResult<T> Success(IQueryable<T> queryableData) =>
            new ServiceResult<T> { QueryableData = queryableData, StatusCode = HttpStatusCode.OK };

        public static ServiceResult<T> Failure(Dictionary<string, string> errors) =>
            new ServiceResult<T> { Errors = errors, StatusCode = HttpStatusCode.Conflict };

        public static ServiceResult<T> Failure(Dictionary<string, string> errors, HttpStatusCode statusCode) =>
            new ServiceResult<T> { Errors = errors, StatusCode = statusCode };
    }
}

