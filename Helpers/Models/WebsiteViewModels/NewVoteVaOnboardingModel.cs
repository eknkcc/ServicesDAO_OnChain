using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Models.WebsiteViewModels
{
    public class NewVoteVaOnboardingModel
    {
        public string newvausername { get; set; }
        public string reason { get; set; }
        public double stake { get; set; }
        public string signedDeployJson { get; set; }

    }
}
