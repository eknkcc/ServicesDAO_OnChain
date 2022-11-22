using Helpers.Models.WebsiteViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Helpers.Constants.Enums;
using System;
using Helpers.Models.CasperServiceModels;

namespace DAO_CasperChainService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CasperMiddlewareController : ControllerBase
    {
        [HttpGet("GetReputationChangesList", Name = "GetReputationChangesList")]
        public PaginatedResponse<AggregatedReputationChange> GetReputationChangesList(int? page, string page_size, string order_direction, string order_by, string address)
        {
            PaginatedResponse<AggregatedReputationChange> reputationChanges = new PaginatedResponse<AggregatedReputationChange>();

            try
            {
                //Additional Query Parameters
                var additionalParameters = "?page=" + page + "&page_size=" + page_size + "&order_direction=" + order_direction + "&order_by=" + order_by;
                //Get Reputation Changes List of User from Middleware
                string reputationChangesJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/accounts/" + address + "/aggregated-reputation-changes" + additionalParameters);
                //Parse response
                reputationChanges = Helpers.Serializers.DeserializeJson<PaginatedResponse<AggregatedReputationChange>>(reputationChangesJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                reputationChanges = new PaginatedResponse<AggregatedReputationChange> { error = new ErrorResult { message = "Request Error" } };
            }

            return reputationChanges;
        }

        [HttpGet("GetTotalReputation", Name = "GetTotalReputation")]
        public TotalReputation GetTotalReputation(string address)
        {
            TotalReputation totalReputation = new TotalReputation();

            try
            {
                //Get Total Reputation of User from Middleware
                string TotalReputationJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/accounts/" + address + "/total-reputation");
                //Parse response
                totalReputation = Helpers.Serializers.DeserializeJson<TotalReputation>(TotalReputationJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                totalReputation = new TotalReputation { error = new ErrorResult { message = "Request Error" } };
            }

            return totalReputation;
        }

        [HttpGet("GetVotesListbyAddress", Name = "GetVotesListbyAddress")]
        public PaginatedResponse<Vote> GetVotesListbyAddress(int? page, string page_size, string order_direction, string order_by, string includes, string address)
        {
            PaginatedResponse<Vote> votesList = new PaginatedResponse<Vote>();

            try
            {
                //Additional Query Parameters
                var additionalParameters = "?page=" + page + "&page_size=" + page_size + "&order_direction=" + order_direction + "&order_by=" + order_by + "&includes=" + includes;
                //Get Votes List of Voting from Middleware
                string votesListJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/accounts/" + address + "/votes" + additionalParameters);
                //Parse response
                votesList = Helpers.Serializers.DeserializeJson<PaginatedResponse<Vote>>(votesListJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                votesList = new PaginatedResponse<Vote> { error = new ErrorResult { message = "Request Error" } };
            }

            return votesList;
        }

        [HttpGet("GetVotesListbyVotingId", Name = "GetVotesListbyVotingId")]
        public PaginatedResponse<Vote> GetVotesListbyVotingId(int? page, string page_size, string order_direction, string order_by, string includes, string voting_id)
        {
            PaginatedResponse<Vote> votesList = new PaginatedResponse<Vote>();

            try
            {
                //Additional Query Parameters
                var additionalParameters = "?page=" + page + "&page_size=" + page_size + "&order_direction=" + order_direction + "&order_by=" + order_by + "&includes=" + includes;
                //Get Votes List of Voting from Middleware
                string votesListJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/votings/" + voting_id + "/votes" + additionalParameters);
                //Parse response
                votesList = Helpers.Serializers.DeserializeJson<PaginatedResponse<Vote>>(votesListJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                votesList = new PaginatedResponse<Vote> { error = new ErrorResult { message = "Request Error" } };
            }

            return votesList;
        }

        [HttpGet("GetVotings", Name = "GetVotings")]
        public PaginatedResponse<Voting> GetVotings(int? page, string page_size, string order_direction, string order_by, string includes, bool is_formal, bool is_active)
        {
            PaginatedResponse<Voting> votingList = new PaginatedResponse<Voting>();

            try
            {
                //Additional Query Parameters
                var additionalParameters = "?page=" + page + "&page_size=" + page_size + "&order_direction=" + order_direction + "&order_by=" + order_by + "&includes=" + includes + "&is_formal=" + is_formal + "&is_active=" + is_active;
                //Get Voting List  from Middleware
                string votingListJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/votings" + additionalParameters);
                //Parse response
                votingList = Helpers.Serializers.DeserializeJson<PaginatedResponse<Voting>>(votingListJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                votingList = new PaginatedResponse<Voting> { error = new ErrorResult { message = "Request Error" } };
            }

            return votingList;
        }
    }
}
