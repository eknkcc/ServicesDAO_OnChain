using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Models.DtoModels.MainDbDto
{
    public class DaoSettingDto
    {
        [Key]
        public int DaoSettingID { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string ValueType { get; set; }

        public DateTime LastModified { get; set; }

    }
}
