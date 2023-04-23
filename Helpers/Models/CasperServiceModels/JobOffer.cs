using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Helpers.Constants.Enums;

namespace Helpers.Models.CasperServiceModels
{
    public class JobOffer
    {
        public int bid_id { get; set; }
        public string caller { get; set; }
        public string deploy_hash { get; set; }
        public long finish_time { get; set; }
        public string job_poster { get; set; }
        public JobOfferStatus job_status_id { get; set; }
        public string result { get; set; }
        public string timestamp { get; set; }
        public string worker { get; set; }

    }
}
