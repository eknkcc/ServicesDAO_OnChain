using Helpers.Constants;
using Helpers.Models.DtoModels.MainDbDto;
using Helpers.Models.SharedModels;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;

namespace DAO_WebPortal.Providers
{
    public static class UserInputControls
    {
        #region Voters
        public static SimpleResponse ControlSimpleVoteRequest(string documenthash)
        {
            if (string.IsNullOrEmpty(documenthash))
            {
                return new SimpleResponse { Success = false, Message = "Document hash cannot be null." };
            }

            return new SimpleResponse { Success = true };
        }
        public static SimpleResponse ControlVaOnboardingVoteRequest(string newvausername, string newvaaddress, string reason, string token)
        {
            if (string.IsNullOrEmpty(newvausername) && string.IsNullOrEmpty(newvaaddress))
            {
                return new SimpleResponse { Success = false, Message = "Username OR public key must be filled." };
            }

            UserDto profileModel = new UserDto();

            if (!string.IsNullOrEmpty(newvausername))
            {
                //Get model from ApiGateway
                var userjson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetByUsername?username=" + newvausername, token);
                //Parse response
                profileModel = Helpers.Serializers.DeserializeJson<UserDto>(userjson);

                //Check user exists
                if (profileModel == null || profileModel.UserId <= 0)
                {
                    return new SimpleResponse { Success = false, Message = "User not found." };
                }

                //Check user is already VA
                if (profileModel.UserType == Enums.UserIdentityType.VotingAssociate.ToString())
                {
                    return new SimpleResponse { Success = false, Message = "This user is already VA." };
                }

                //Check user's wallet exists
                if (string.IsNullOrEmpty(profileModel.WalletAddress))
                {
                    return new SimpleResponse { Success = false, Message = "User's wallet address could not be found. " + newvausername + " needs to connect a wallet first." };
                }
            }
            else if (!string.IsNullOrEmpty(newvaaddress))
            {
                profileModel.WalletAddress = newvaaddress;
            }

            //Check reason
            if (string.IsNullOrEmpty(reason))
            {
                return new SimpleResponse { Success = false, Message = "Reason cannot be null." };
            }

            return new SimpleResponse { Success = true, Content = new { user = profileModel } };
        }
        public static SimpleResponse ControlGovernanceVoteRequest(string key, string value)
        {
            //Check user exists
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
            {
                return new SimpleResponse { Success = false, Message = "Key and value cannot be null" };
            }

            return new SimpleResponse { Success = true };
        }
        public static SimpleResponse ControlKYCVoteRequest(string kycusername, string token)
        {
            //Get model from ApiGateway
            var userjson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetByUsername?username=" + kycusername, token);
            //Parse response
            UserDto profileModel = Helpers.Serializers.DeserializeJson<UserDto>(userjson);

            //Check user exists
            if (profileModel == null || profileModel.UserId <= 0)
            {
                return new SimpleResponse { Success = false, Message = "User not found." };
            }

            //Check user already completed KYC
            if (profileModel.KYCStatus == true)
            {
                return new SimpleResponse { Success = false, Message = "This user has already completed KYC process." };
            }

            //Check user's wallet exists
            if (string.IsNullOrEmpty(profileModel.WalletAddress))
            {
                return new SimpleResponse { Success = false, Message = "User's wallet address could not be found. " + kycusername + " needs to connect a wallet first." };
            }

            //Get model from ApiGateway
            var userkycjson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/UserKYC/GetUserId?id=" + profileModel.UserId, token);
            //Parse response
            UserKYCDto userKycModel = Helpers.Serializers.DeserializeJson<UserKYCDto>(userkycjson);

            //Check if there is KYC application
            if (userKycModel == null || userKycModel.UserKYCID <= 0)
            {
                return new SimpleResponse { Success = false, Message = "There is no KYC application for this user. User has to submit KYC form from the 'User Profile' page." };
            }

            return new SimpleResponse { Success = true, Content = new { kyc = userKycModel, user = profileModel } };
        }
        public static SimpleResponse ControlReputationVoteRequest(int amount, string documenthash, int action, string username, string address, string token)
        {
            if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(address))
            {
                return new SimpleResponse { Success = false, Message = "Username OR public key must be filled." };
            }

            UserDto profileModel = new UserDto();

            if (!string.IsNullOrEmpty(username))
            {
                //Get model from ApiGateway
                var userjson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetByUsername?username=" + username, token);
                //Parse response
                profileModel = Helpers.Serializers.DeserializeJson<UserDto>(userjson);

                //Check user exists
                if (profileModel == null || profileModel.UserId <= 0)
                {
                    return new SimpleResponse { Success = false, Message = "User not found." };
                }

                //Check user's wallet exists
                if (string.IsNullOrEmpty(profileModel.WalletAddress))
                {
                    return new SimpleResponse { Success = false, Message = "User's wallet address could not be found. " + username + " needs to connect a wallet first." };
                }


            }
            else if (!string.IsNullOrEmpty(address))
            {
                profileModel.WalletAddress = address;
            }

            if (string.IsNullOrEmpty(documenthash))
            {
                return new SimpleResponse { Success = false, Message = "Document hash cannot be null." };
            }

            if (amount <= 0)
            {
                return new SimpleResponse { Success = false, Message = "Reputation amount must be greater than 0." };
            }


            return new SimpleResponse { Success = true, Content = new { user = profileModel } };
        }
        public static SimpleResponse ControlSlashingVoteRequest(string address_to_slash, string username, string token)
        {
            if (string.IsNullOrEmpty(address_to_slash) && string.IsNullOrEmpty(username))
            {
                return new SimpleResponse { Success = false, Message = "Username OR public key must be filled." };
            }

            UserDto profileModel = new UserDto();

            if (!string.IsNullOrEmpty(username))
            {
                //Get model from ApiGateway
                var userjson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetByUsername?username=" + username, token);
                //Parse response
                profileModel = Helpers.Serializers.DeserializeJson<UserDto>(userjson);

                //Check user exists
                if (profileModel == null || profileModel.UserId <= 0)
                {
                    return new SimpleResponse { Success = false, Message = "User not found." };
                }

                //Check user's wallet exists
                if (string.IsNullOrEmpty(profileModel.WalletAddress))
                {
                    return new SimpleResponse { Success = false, Message = "User's wallet address could not be found. " + username + " needs to connect a wallet first." };
                }
            }
            else if (!string.IsNullOrEmpty(address_to_slash))
            {
                profileModel.WalletAddress = address_to_slash;
            }

            return new SimpleResponse { Success = true, Content = new { user = profileModel } };
        }

        public static SimpleResponse ControlSubmitVoteRequest(int votingId, int stake)
        {
            if (votingId <= 0)
            {
                return new SimpleResponse { Success = false, Message = "Invalid voting id." };
            }

            if (stake <= 0)
            {
                return new SimpleResponse { Success = false, Message = "Stake must be higher than 0." };
            }

            return new SimpleResponse { Success = true };

        }
        #endregion

        #region BidEscrow
        public static SimpleResponse ControlPostJobOfferRequest(string userkyc,long timeframe, long budget)
        {
            if (timeframe <= 0)
            {
                return new SimpleResponse { Success = false, Message = "Timeframe must be a positive number" };
            }
            if (budget <= 0)
            {
                return new SimpleResponse { Success = false, Message = "CSPR Budget must be a positive number" };
            }
            if(userkyc.ToLower() != "true")
            {
                return new SimpleResponse { Success = false, Message = "This account must complete KYC process before posting a job." };
            }

            long timeframestamp = 0;
            try
            {
                var enddate = DateTime.Now.AddDays(timeframe);
                timeframestamp = ((DateTimeOffset)enddate).ToUnixTimeMilliseconds();
            }
            catch
            {
                return new SimpleResponse { Success = false, Message = "Timeframe parse error" };
            }

            return new SimpleResponse { Success = true, Content = new { timeframe = timeframestamp } };
        }
        public static SimpleResponse ControlSubmitBidRequest(uint jobofferid, long time, ulong userpayment, ulong repstake, bool onboard)
        {

            if (userpayment <= 0)
            {
                return new SimpleResponse { Success = false, Message = "Payment must be a positive number" };
            }
            if (time <= 0)
            {
                return new SimpleResponse { Success = false, Message = "Timeframe must be a positive number" };
            }
            if (jobofferid < 0)
            {
                return new SimpleResponse { Success = false, Message = "Job offer id not found" };
            }

            long timeframestamp = 0;
            try
            {
                var enddate = DateTime.Now.AddDays(time);
                timeframestamp = ((DateTimeOffset)enddate).ToUnixTimeMilliseconds();
            }
            catch
            {
                return new SimpleResponse { Success = false, Message = "Timeframe parse error" };
            }

            return new SimpleResponse { Success = true, Content = new { time = timeframestamp } };
        }
        public static SimpleResponse ControlCancelBidRequest(uint bidid)
        {
            if (bidid < 0)
            {
                return new SimpleResponse { Success = false, Message = "Bid ID must be a positive number" };
            }

            return new SimpleResponse { Success = true };
        }
        public static SimpleResponse ControlPickBidRequest(uint bidid, uint jobid)
        {
            if (bidid < 0)
            {
                return new SimpleResponse { Success = false, Message = "Bid ID must be a positive number" };
            }
            if (jobid < 0)
            {
                return new SimpleResponse { Success = false, Message = "Job ID must be a positive number" };
            }

            return new SimpleResponse { Success = true };
        }
        public static SimpleResponse ControlSubmitJobProofRequest(uint jobid, string documenthash)
        {
            if (string.IsNullOrEmpty(documenthash))
            {
                return new SimpleResponse { Success = false, Message = "Document proof cannot be empty." };
            }
            if (jobid < 0)
            {
                return new SimpleResponse { Success = false, Message = "Job ID must be a positive number" };
            }

            return new SimpleResponse { Success = true };
        }
        public static SimpleResponse ControlSubmitJobProofGracePeriodRequest(uint jobid, string proof, uint repstake, bool onboard)
        {
            if (string.IsNullOrEmpty(proof))
            {
                return new SimpleResponse { Success = false, Message = "Document proof cannot be empty." };
            }
            if (jobid < 0)
            {
                return new SimpleResponse { Success = false, Message = "Job ID must be a positive number" };
            }
            if (repstake <= 0)
            {
                return new SimpleResponse { Success = false, Message = "Reputation stake must be a positive number" };
            }

            return new SimpleResponse { Success = true };
        }
        #endregion

    }
}
