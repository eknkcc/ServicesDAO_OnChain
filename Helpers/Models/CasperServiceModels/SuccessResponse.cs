using System.Collections.Generic;

namespace Helpers.Models.CasperServiceModels
{
    public class SuccessResponse<T>
    {
        public T data { get; set; }
    }
}