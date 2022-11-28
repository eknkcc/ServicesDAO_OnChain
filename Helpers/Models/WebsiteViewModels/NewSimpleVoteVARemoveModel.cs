using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Models.WebsiteViewModels
{
    public class NewSimpleVoteVARemoveModel
    {
        public string vausername { get; set; }
        public double stake { get; set; }
        public string signedDeployJson { get; set; }
    }
}
