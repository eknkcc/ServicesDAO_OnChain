using Helpers.Models.WebsiteViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Helpers.Constants.Enums;
using System;
using Helpers.Models.CasperServiceModels;
using System.Collections.Generic;
using System.Linq;

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

            var additionalParameters = new Dictionary<string, string>();

            if (page != null)
            {
                additionalParameters.Add("page", Convert.ToString(page));
            }
            if (!String.IsNullOrEmpty(page_size))
            {
                additionalParameters.Add("page_size", page_size);
            }
            if (!String.IsNullOrEmpty(order_direction))
            {
                additionalParameters.Add("order_direction", order_direction);
            }
            if (!String.IsNullOrEmpty(order_by))
            {
                additionalParameters.Add("order_by", order_by);
            }

            //Additional Query Parameters
            var additionalParametersStr = "";

            for (int i = 0; i < additionalParameters.Count; i++)
            {
                if (i == 0) additionalParametersStr += "?";
                else additionalParametersStr += "&";

                additionalParametersStr += additionalParameters.ElementAt(i).Key.ToString() + "=" + additionalParameters.ElementAt(i).Value.ToString();
            }

            try
            {
                //Get Reputation Changes List of User from Middleware
                string reputationChangesJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/accounts/" + address + "/aggregated-reputation-changes" + additionalParametersStr);
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
        public SuccessResponse<TotalReputation> GetTotalReputation(string address)
        {
            SuccessResponse<TotalReputation> totalReputation = new SuccessResponse<TotalReputation>();

            try
            {
                //Get Total Reputation of User from Middleware
                string TotalReputationJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/accounts/" + address + "/total-reputation");
                //Parse response
                totalReputation = Helpers.Serializers.DeserializeJson<SuccessResponse<TotalReputation>>(TotalReputationJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
            }

            return totalReputation;
        }

        [HttpGet("GetVotesListbyAddress", Name = "GetVotesListbyAddress")]
        public PaginatedResponse<Vote> GetVotesListbyAddress(int? page, string page_size, string order_direction, string order_by, string includes, string address)
        {
            PaginatedResponse<Vote> votesList = new PaginatedResponse<Vote>();

            var additionalParameters = new Dictionary<string, string>();

            if (page != null)
            {
                additionalParameters.Add("page", Convert.ToString(page));
            }
            if (!String.IsNullOrEmpty(page_size))
            {
                additionalParameters.Add("page_size", page_size);
            }
            if (!String.IsNullOrEmpty(order_direction))
            {
                additionalParameters.Add("order_direction", order_direction);
            }
            if (!String.IsNullOrEmpty(order_by))
            {
                additionalParameters.Add("order_by", order_by);
            }
            if (!String.IsNullOrEmpty(includes))
            {
                additionalParameters.Add("includes", order_by);
            }

            //Additional Query Parameters
            var additionalParametersStr = "";

            for (int i = 0; i < additionalParameters.Count; i++)
            {
                if (i == 0) additionalParametersStr += "?";
                else additionalParametersStr += "&";

                additionalParametersStr += additionalParameters.ElementAt(i).Key.ToString() + "=" + additionalParameters.ElementAt(i).Value.ToString();
            }

            try
            {

                //Get Votes List of Voting from Middleware
                string votesListJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/accounts/" + address + "/votes" + additionalParametersStr);
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

            var additionalParameters = new Dictionary<string, string>();

            if (page != null)
            {
                additionalParameters.Add("page", Convert.ToString(page));
            }
            if (!String.IsNullOrEmpty(page_size))
            {
                additionalParameters.Add("page_size", page_size);
            }
            if (!String.IsNullOrEmpty(order_direction))
            {
                additionalParameters.Add("order_direction", order_direction);
            }
            if (!String.IsNullOrEmpty(order_by))
            {
                additionalParameters.Add("order_by", order_by);
            }
            if (!String.IsNullOrEmpty(includes))
            {
                additionalParameters.Add("includes", order_by);
            }

            //Additional Query Parameters
            var additionalParametersStr = "";

            for (int i = 0; i < additionalParameters.Count; i++)
            {
                if (i == 0) additionalParametersStr += "?";
                else additionalParametersStr += "&";

                additionalParametersStr += additionalParameters.ElementAt(i).Key.ToString() + "=" + additionalParameters.ElementAt(i).Value.ToString();
            }

            try
            {
                //Get Votes List of Voting from Middleware
                string votesListJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/votings/" + voting_id + "/votes" + additionalParametersStr);
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
        public PaginatedResponse<Voting> GetVotings(int? page, string page_size, string order_direction, string order_by, string includes, bool? is_formal, bool? has_ended)
        {
            PaginatedResponse<Voting> votingList = new PaginatedResponse<Voting>();

            var additionalParameters = new Dictionary<string, string>();

            if (page != null)
            {
                additionalParameters.Add("page", Convert.ToString(page));
            }
            if (!String.IsNullOrEmpty(page_size))
            {
                additionalParameters.Add("page_size", page_size);
            }
            if (!String.IsNullOrEmpty(order_direction))
            {
                additionalParameters.Add("order_direction", order_direction);
            }
            if (!String.IsNullOrEmpty(order_by))
            {
                additionalParameters.Add("order_by", order_by);
            }
            if (!String.IsNullOrEmpty(includes))
            {
                additionalParameters.Add("includes", order_by);
            }
            if (has_ended != null)
            {
                additionalParameters.Add("has_ended", Convert.ToString(has_ended));
            }
            if (is_formal != null)
            {
                additionalParameters.Add("is_formal", Convert.ToString(is_formal));
            }

            //Additional Query Parameters
            var additionalParametersStr = "";

            for (int i = 0; i < additionalParameters.Count; i++)
            {
                if (i == 0) additionalParametersStr += "?";
                else additionalParametersStr += "&";

                additionalParametersStr += additionalParameters.ElementAt(i).Key.ToString() + "=" + additionalParameters.ElementAt(i).Value.ToString();
            }

            try
            {

                //Get Voting List  from Middleware
                string votingListJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/votings" + additionalParametersStr);
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
