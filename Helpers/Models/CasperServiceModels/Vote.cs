namespace Helpers.Models.CasperServiceModels
{
    public class Vote
    {
        public string address { get; set; }
        public int? amount { get; set; }
        public string deploy_hash { get; set; }
        public bool? is_in_favour { get; set; }
        public string timestamp { get; set; }
        public int? voting_id { get; set; }
    }
}