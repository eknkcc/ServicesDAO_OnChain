using System;
using System.ComponentModel.DataAnnotations;

namespace DAO_DbService.Models
{
    public class DaoSetting
    {
        [Key]
        public int DaoSettingID { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string ValueType { get; set; }
        public DateTime LastModified { get; set; }

    }
}
