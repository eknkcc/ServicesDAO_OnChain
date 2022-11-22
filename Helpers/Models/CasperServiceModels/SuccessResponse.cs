using System.Collections.Generic;

namespace Helpers.Models.CasperServiceModels
{
    public class SuccessResponse<T>
    {
        public List<T> data { get; set; } = new List<T>();
    }
}