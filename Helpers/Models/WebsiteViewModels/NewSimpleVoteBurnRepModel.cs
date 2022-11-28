using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Models.WebsiteViewModels
{
    public class NewSimpleVoteBurnRepModel
    {
        public double stake { get; set; }
        public string signedDeployJson { get; set; }
        public string vausername { get; set; }
        public double burnAmount { get; set; }
    }
}
