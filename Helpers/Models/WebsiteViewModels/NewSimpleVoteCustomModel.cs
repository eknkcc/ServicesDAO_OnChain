using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Models.WebsiteViewModels
{
    public class NewSimpleVoteCustomModel
    {
        public string title { get; set; }
        public string description { get; set; }
        public double stake { get; set; }
        public string signedDeployJson { get; set; }

    }
}
