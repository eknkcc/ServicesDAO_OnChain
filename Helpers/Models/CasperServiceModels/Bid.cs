using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Models.CasperServiceModels
{
    public class Bid
    {
        public int bid_id { get; set; }
        public long cspr_stake { get; set; }
        public string deploy_hash { get; set; }
        public int job_offer_id { get; set; }
        public bool onboard { get; set; }
        public bool picked_by_job_poster { get; set; }
        public long proposed_payment { get; set; }
        public long proposed_time_frame { get; set; }
        public long reputation_stake { get; set; }
        public string timestamp { get; set; }
        public string worker { get; set; }

    }
}
