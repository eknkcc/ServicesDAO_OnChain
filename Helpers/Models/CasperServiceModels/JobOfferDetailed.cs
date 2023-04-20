using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Models.CasperServiceModels
{
    public class JobOfferDetailed
    {
        public int auction_type_id { get; set; }
        public string deploy_hash { get; set; }
        public int expected_time_frame { get; set; }
        public int job_offer_id { get; set; }
        public string job_poster { get; set; }
        public long max_budget { get; set; }
        public string timestamp { get; set; }
    }
}
