using Helpers.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Models.CasperServiceModels
{
    public class ReputationSnapshot
    {
        public string? address { get; set; }
        public double? total_liquid_reputation { get; set; }
        public double? total_staked_reputation { get; set; }
        public double? voting_lost_reputation { get; set; }
        public double? voting_earned_reputation { get; set; }
        public int? voting_id { get; set; }
        public string? deploy_hash { get; set; }
        public Enums.ReputationChangeReason? reason { get; set; }
        public string? timestamp { get; set; }

    }
}
