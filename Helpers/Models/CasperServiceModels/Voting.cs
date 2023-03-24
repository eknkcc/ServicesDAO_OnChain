namespace Helpers.Models.CasperServiceModels
{
    public class Voting
    {
        public long? config_time_between_informal_and_formal_voting { get; set; }
        public int? config_total_onboarded { get; set; }
        public int? config_voting_clearness_delta { get; set; }
        public string creator { get; set; }
        public string deploy_hash { get; set; }
        public string formal_voting_ends_at { get; set; }
        public int? formal_voting_quorum { get; set; }
        public int? formal_voting_result { get; set; }
        public string formal_voting_starts_at { get; set; }
        public long formal_voting_time { get; set; }
        public string informal_voting_ends_at { get; set; }
        public int? informal_voting_quorum { get; set; }
        public int? informal_voting_result { get; set; }
        public string informal_voting_starts_at { get; set; }
        public long? informal_voting_time { get; set; }
        public bool? is_canceled { get; set; }
        public int? voting_id { get; set; }
        public int? voting_type_id { get; set; }
    }
}