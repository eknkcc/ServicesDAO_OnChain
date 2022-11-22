using System.Collections.Generic;

namespace Helpers.Models.CasperServiceModels
{
    public class PaginatedResponse<T>:ErrorResponse
    {
        public List<T> data { get; set; } = new List<T>();
        public int item_count { get; set; }
        public int page_count { get; set; }
    }
}