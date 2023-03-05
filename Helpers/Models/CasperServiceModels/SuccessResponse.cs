using System.Collections.Generic;

namespace Helpers.Models.CasperServiceModels
{
    public class SuccessResponse<T> : ErrorResponse
    {
        public T data { get; set; }
    }
}