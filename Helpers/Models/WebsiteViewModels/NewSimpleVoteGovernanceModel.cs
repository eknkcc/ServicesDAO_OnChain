using Helpers.Models.DtoModels.MainDbDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Models.WebsiteViewModels
{
    public class NewSimpleVoteGovernanceModel
    {
        public double stake { get; set; }
        public string signedDeployJson { get; set; }
        public PlatformSettingDto settings { get; set; } //For variable repository (governance) vote
    }
}
