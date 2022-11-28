using Helpers.Models.DtoModels.MainDbDto;
using Helpers.Models.SharedModels;
using Microsoft.AspNetCore.Http;

namespace DAO_WebPortal.Providers
{
    public static class UserInputControls
    {
        public static SimpleResponse ControlKYCRequest(string username, string token)
        {
            //Get model from ApiGateway
            var userjson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetByUsername?username=" + username, token);
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
                return new SimpleResponse { Success = false, Message = "User's chain address could not be found. " + username + " needs to connect a wallet first." };
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
    }
}
