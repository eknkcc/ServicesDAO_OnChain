using Helpers.Models.WebsiteViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Helpers.Constants.Enums;
using System;
using Helpers.Models.CasperServiceModels;
using System.Collections.Generic;
using System.Linq;
using Casper.Network.SDK;
using Casper.Network.SDK.Types;
using Account = Helpers.Models.CasperServiceModels.Account;
using Bid = Helpers.Models.CasperServiceModels.Bid;

namespace DAO_CasperChainService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CasperMiddlewareController : ControllerBase
    {

        [HttpGet("GetUserChainProfile", Name = "GetUserChainProfile")]
        public UserChainProfile GetUserChainProfile(string publicAddress)
        {
            UserChainProfile profile = new UserChainProfile();

            try
            {
                var hex = publicAddress;
                var publicKey = PublicKey.FromHexString(hex);
                var casperSdk = new NetCasperClient(Program._settings.NodeUrl/* + ":7777/rpc"*/);
                var rpcResponse = casperSdk.GetAccountBalance(publicKey).Result;

                double balanceParsed = Convert.ToInt64(rpcResponse.Parse().BalanceValue.ToString()) / (double)1000000000;
                profile.Balance = balanceParsed.ToString("N2");


                // Console.WriteLine("Public Key Balance: " + rpcResponse.Parse().BalanceValue);

                // CasperClient casperClient = new CasperClient(rpcUrl);
                // var result = casperClient.RpcService.GetAccountBalance(publicAddress);
                // double balanceParsed = Convert.ToInt64(result.result.balance_value) / (double)1000000000;
                // profile.Balance = balanceParsed.ToString("N2");
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
            }

            try
            {
                PaginatedResponse<ReputationSnapshot> totalRep = GetTotalReputationSnapshots(publicAddress);
                if (totalRep != null && totalRep.error == null && totalRep.data != null && totalRep.data.Count > 0)
                {
                    profile.AvailableReputation = totalRep.data[totalRep.data.Count - 1].total_liquid_reputation.ToString();
                    profile.StakedReputation = totalRep.data[totalRep.data.Count - 1].total_staked_reputation.ToString();
                    profile.Reputation = (Convert.ToInt32(profile.AvailableReputation) + Convert.ToInt32(profile.StakedReputation)).ToString();
                }

                SuccessResponse<Account> account = GetAccountByAddress(publicAddress);
                if (account != null && account.error == null && account.data != null)
                {
                    profile.IsVA = account.data.is_va;
                    profile.IsKYC = account.data.is_kyc;
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
            }


            //OLD CASPER MIDDLEWARE METHOD
            //try
            //{
            //    SuccessResponse<TotalReputation> totalRep = GetTotalReputation(publicAddress);
            //    if (totalRep != null && totalRep.error == null && totalRep.data != null)
            //    {
            //        profile.AvailableReputation = totalRep.data.available_amount.ToString();
            //        profile.StakedReputation = totalRep.data.staked_amount.ToString();
            //        profile.Reputation = (totalRep.data.available_amount + totalRep.data.staked_amount).ToString();
            //    }

            //    SuccessResponse<Account> account = GetAccountByAddress(publicAddress);
            //    if (account != null && account.error == null && account.data != null)
            //    {
            //        profile.IsVA = account.data.is_va;
            //        profile.IsKYC = account.data.is_kyc;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
            //}


            return profile;
        }

        #region Reputation

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
                foreach (var item in reputationChanges.data)
                {
                    if (item.earned_amount != null && item.earned_amount > 0) item.earned_amount /= 1_000_000_000;
                    if (item.staked_amount != null && item.staked_amount > 0) item.staked_amount /= 1_000_000_000;
                    if (item.lost_amount != null && item.lost_amount > 0) item.lost_amount /= 1_000_000_000;
                    if (item.released_amount != null && item.released_amount > 0) item.released_amount /= 1_000_000_000;
                }

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
                if (totalReputation.data.available_amount != null && totalReputation.data.available_amount > 0) totalReputation.data.available_amount /= 1_000_000_000;
                if (totalReputation.data.staked_amount != null && totalReputation.data.staked_amount > 0) totalReputation.data.staked_amount /= 1_000_000_000;

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                totalReputation = new SuccessResponse<TotalReputation> { error = new ErrorResult { message = "Request Error" } };
            }

            return totalReputation;
        }

        [HttpGet("GetTotalReputationSnapshots", Name = "GetTotalReputationSnapshots")]
        public PaginatedResponse<ReputationSnapshot> GetTotalReputationSnapshots(string address)
        {
            PaginatedResponse<ReputationSnapshot> reputationSnapshots = new PaginatedResponse<ReputationSnapshot>();

            try
            {
                //Get Total Reputation of User from Middleware
                string reputationSnapshotsJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/accounts/" + address + "/total-reputation-snapshots");
                //Parse response
                reputationSnapshots = Helpers.Serializers.DeserializeJson<PaginatedResponse<ReputationSnapshot>>(reputationSnapshotsJson);
                foreach (var item in reputationSnapshots.data)
                {
                    if (item.total_liquid_reputation != null && item.total_liquid_reputation > 0) item.total_liquid_reputation /= 1_000_000_000;
                    if (item.total_staked_reputation != null && item.total_staked_reputation > 0) item.total_staked_reputation /= 1_000_000_000;
                    if (item.voting_lost_reputation != null && item.voting_lost_reputation > 0) item.voting_lost_reputation /= 1_000_000_000;
                    if (item.voting_earned_reputation != null && item.voting_earned_reputation > 0) item.voting_earned_reputation /= 1_000_000_000;
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                reputationSnapshots = new PaginatedResponse<ReputationSnapshot> { error = new ErrorResult { message = "Request Error" } };
            }

            return reputationSnapshots;
        }

        #endregion

        #region Voting

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

        #endregion

        #region Settings

        [HttpGet("GetSettings", Name = "GetSettings")]
        public PaginatedResponse<Setting> GetSettings(int? page, string page_size, string order_direction, string order_by)
        {
            PaginatedResponse<Setting> settingList = new PaginatedResponse<Setting>();

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
                //Get Voting List  from Middleware
                string setingJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/settings" + additionalParametersStr);
                //Parse response
                settingList = Helpers.Serializers.DeserializeJson<PaginatedResponse<Setting>>(setingJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                settingList = new PaginatedResponse<Setting> { error = new ErrorResult { message = "Request Error" } };
            }

            return settingList;
        }

        #endregion

        #region Accounts

        [HttpGet("GetAccounts", Name = "GetAccounts")]
        public PaginatedResponse<Account> GetAccounts(int? page, string page_size)
        {
            PaginatedResponse<Account> accountList = new PaginatedResponse<Account>();

            var additionalParameters = new Dictionary<string, string>();

            if (page != null)
            {
                additionalParameters.Add("page", Convert.ToString(page));
            }
            if (!String.IsNullOrEmpty(page_size))
            {
                additionalParameters.Add("page_size", page_size);
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
                string setingJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/accounts" + additionalParametersStr);
                //Parse response
                accountList = Helpers.Serializers.DeserializeJson<PaginatedResponse<Account>>(setingJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                accountList = new PaginatedResponse<Account> { error = new ErrorResult { message = "Request Error" } };
            }

            return accountList;
        }


        [HttpGet("GetAccountByAddress", Name = "GetAccountByAddress")]
        public SuccessResponse<Account> GetAccountByAddress(string address)
        {
            SuccessResponse<Account> account = new SuccessResponse<Account>();

            try
            {
                //Get Voting List  from Middleware
                string setingJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/accounts/" + address);
                //Parse response
                account = Helpers.Serializers.DeserializeJson<SuccessResponse<Account>>(setingJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                account = new SuccessResponse<Account> { error = new ErrorResult { message = "Request Error" } };
            }

            return account;
        }

        #endregion

        #region BidEscrow
        [HttpGet("GetJobByBidId", Name = "GetJobByBidId")]
        public SuccessResponse<Job> GetJobByBidId(int bidid)
        {
            SuccessResponse<Job> response = new SuccessResponse<Job>();

            try
            {
                string jobOfferJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/bids/" + bidid + "/job");
                //Parse response
                response = Helpers.Serializers.DeserializeJson<SuccessResponse<Job>>(jobOfferJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                response = new SuccessResponse<Job> { error = new ErrorResult { message = "Request Error" } };
            }

            return response;
        }

        [HttpGet("GetJobOffers", Name = "GetJobOffers")]
        public PaginatedResponse<JobOfferDetailed> GetJobOffers(int? page, string page_size, string order_direction, string order_by)
        {
            PaginatedResponse<JobOfferDetailed> response = new PaginatedResponse<JobOfferDetailed>();

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
                //Get Total Reputation of User from Middleware
                string jobsJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/job-offers" + additionalParametersStr);
                //Parse response
                response = Helpers.Serializers.DeserializeJson<PaginatedResponse<JobOfferDetailed>>(jobsJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                response = new PaginatedResponse<JobOfferDetailed> { error = new ErrorResult { message = "Request Error" } };
            }

            return response;
        }

        [HttpGet("GetBids", Name = "GetBids")]
        public PaginatedResponse<Bid> GetBids(int? page, string page_size, string order_direction, string order_by, int jobid)
        {
            PaginatedResponse<Bid> response = new PaginatedResponse<Bid>();

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
                //Get Total Reputation of User from Middleware
                string bidsJson = Helpers.Request.Get(Program._settings.CasperMiddlewareUrl + "/job-offers/" + jobid + "/bids" + additionalParametersStr);
                //Parse response
                response = Helpers.Serializers.DeserializeJson<PaginatedResponse<Bid>>(bidsJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                response = new PaginatedResponse<Bid> { error = new ErrorResult { message = "Request Error" } };
            }

            return response;
        }

        #endregion
    }
}
