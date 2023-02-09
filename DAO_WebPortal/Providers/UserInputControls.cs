using Helpers.Constants;
using Helpers.Models.DtoModels.MainDbDto;
using Helpers.Models.SharedModels;
using Microsoft.AspNetCore.Http;

namespace DAO_WebPortal.Providers
{
    public static class UserInputControls
    {
        public static SimpleResponse ControlSimpleVoteRequest(string documenthash)
        {
            if (string.IsNullOrEmpty(documenthash))
            {
                return new SimpleResponse { Success = false, Message = "Document hash cannot be null." };
            }

            return new SimpleResponse { Success = true };
        }
        public static SimpleResponse ControlVaOnboardingVoteRequest(string newvausername, string reason, string token)
        {
            //Get model from ApiGateway
            var userjson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetByUsername?username=" + newvausername, token);
            //Parse response
            UserDto profileModel = Helpers.Serializers.DeserializeJson<UserDto>(userjson);

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
                return new SimpleResponse { Success = false, Message = "User's chain address could not be found. " + newvausername + " needs to connect a wallet first." };
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
                return new SimpleResponse { Success = false, Message = "User's chain address could not be found. " + kycusername + " needs to connect a wallet first." };
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
        public static SimpleResponse ControlReputationVoteRequest(int amount, string documenthash, string action)
        {
            if (string.IsNullOrEmpty(documenthash))
            {
                return new SimpleResponse { Success = false, Message = "Document hash cannot be null." };
            }

            if (amount <= 0)
            {
                return new SimpleResponse { Success = false, Message = "Reputation amount must be greater than 0." };
            }


            return new SimpleResponse { Success = true };
        }
        public static SimpleResponse ControlSlashingVoteRequest(string address_to_slash)
        {
            if (string.IsNullOrEmpty(address_to_slash))
            {
                return new SimpleResponse { Success = false, Message = "Address to slash cannot be null." };
            }

            return new SimpleResponse { Success = true };
        }

    }
}
