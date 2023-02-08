using Helpers.Models.DtoModels.MainDbDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Models.WebsiteViewModels
{
    public class NewVoteGovernanceModel
    {
        public double stake { get; set; }
        public string signedDeployJson { get; set; }
        public string key { get; set; }
        public string value { get; set; }
    }
}
