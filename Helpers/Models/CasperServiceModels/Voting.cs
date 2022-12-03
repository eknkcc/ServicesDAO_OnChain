namespace Helpers.Models.CasperServiceModels
{
    public class Voting
    {
        public string creator { get; set; }
        public string deploy_hash { get; set; }
        public bool has_ended { get; set; }
        public int? informal_voting_id { get; set; }
        public bool? is_formal { get; set; }
        public string timestamp { get; set; }
        public int? voting_id { get; set; }
        public int? voting_quorum { get; set; }
        public int? voting_time { get; set; }

    }
}