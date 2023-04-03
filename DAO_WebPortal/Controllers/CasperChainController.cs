using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PagedList.Core;
using Helpers.Models.NotificationModels;
using Helpers.Models.KYCModels;
using DAO_WebPortal.Utility;
using Helpers.Models.IdentityModels;
using Helpers.Models.SharedModels;
using DAO_WebPortal.Resources;
using static Helpers.Constants.Enums;
using DAO_WebPortal.Providers;
using Helpers.Models.DtoModels.MainDbDto;
using Helpers.Models.WebsiteViewModels;
using System.Threading;
using Org.BouncyCastle.Crypto.Tls;
using Helpers.Constants;
using Helpers.Models.DtoModels.VoteDbDto;
using Helpers.Models.DtoModels.ReputationDbDto;

namespace DAO_WebPortal.Controllers
{
    public class CasperChainController : Controller
    {
        /// <summary>
        ///  User login onchain function
        /// </summary>
        /// <param name="publicAddress">User's wallet public address</param>
        /// <param name="reputation">User's total reputation</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult LoginChain(string publicAddress)
        {
            try
            {
                //Get client Ip and Port
                string ip = IpHelper.GetClientIpAddress(HttpContext);
                string port = IpHelper.GetClientPort(HttpContext);


                //Get user balance, reputation and VA Status from chain
                var chainProfile = GetUserChainProfile(publicAddress);

                if (Convert.ToBoolean(chainProfile.IsVA))
                {
                    HttpContext.Session.SetString("UserType", "VotingAssociate");
                }
                else
                {
                    HttpContext.Session.SetString("UserType", "Associate");
                }

                if (Convert.ToBoolean(chainProfile.IsKYC))
                {
                    HttpContext.Session.SetString("KYCStatus", "true");
                }
                else
                {
                    HttpContext.Session.SetString("KYCStatus", "false");
                }

                HttpContext.Session.SetString("Balance", chainProfile.Balance.ToString());
                HttpContext.Session.SetString("Reputation", (Convert.ToInt32(chainProfile.AvailableReputation) + Convert.ToInt32(chainProfile.StakedReputation)).ToString());
                HttpContext.Session.SetString("LastUsableReputation", chainProfile.AvailableReputation.ToString());

                //Create model
                LoginChainModel LoginModelPost = new LoginChainModel() { walletAddress = publicAddress, isVA = Convert.ToBoolean(chainProfile.IsVA), ip = ip, port = port, application = Helpers.Constants.Enums.AppNames.DAO_WebPortal };

                //Post model to ApiGateway
                var loginJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/PublicActions/LoginChain", Helpers.Serializers.SerializeJson(LoginModelPost));

                //Parse response
                LoginResponse loginModel = Helpers.Serializers.DeserializeJson<LoginResponse>(loginJson);

                string token = loginModel.Token.ToString();

                HttpContext.Session.SetInt32("UserID", 0);
                HttpContext.Session.SetString("Email", "");
                HttpContext.Session.SetString("Token", token);
                HttpContext.Session.SetString("LoginType", "user");
                HttpContext.Session.SetString("NameSurname", Utility.StringHelper.ShortenWallet(publicAddress));
                HttpContext.Session.SetString("ProfileImage", "");
                HttpContext.Session.SetString("WalletAddress", loginModel.WalletAddress.ToString());
                HttpContext.Session.SetInt32("ChainSign", 1);

                return base.Json(new SimpleResponse { Success = true, Message = Lang.SuccessLogin });
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }

        /// <summary>
        /// Connect wallet button event
        /// </summary>
        /// <returns></returns>
        [AuthorizeUser]
        public IActionResult ConnectWallet(string publicAddress)
        {
            try
            {
                //Get model from ApiGateway
                var json = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetId?id=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token"));

                //Parse response
                UserDto userModel = Helpers.Serializers.DeserializeJson<UserDto>(json);

                userModel.WalletAddress = publicAddress;

                HttpContext.Session.SetInt32("ChainSign", 1);

                //Get user balance, reputation and VA Status from chain
                var chainProfile = GetUserChainProfile(publicAddress);

                if (Convert.ToBoolean(chainProfile.IsVA))
                {
                    HttpContext.Session.SetString("UserType", "VotingAssociate");
                }
                else
                {
                    HttpContext.Session.SetString("UserType", "Associate");
                }

                if (Convert.ToBoolean(chainProfile.IsKYC))
                {
                    HttpContext.Session.SetString("KYCStatus", "true");
                }
                else
                {
                    HttpContext.Session.SetString("KYCStatus", "false");
                }

                HttpContext.Session.SetString("Balance", chainProfile.Balance.ToString());
                HttpContext.Session.SetString("Reputation", (Convert.ToInt32(chainProfile.AvailableReputation) + Convert.ToInt32(chainProfile.StakedReputation)).ToString());
                HttpContext.Session.SetString("LastUsableReputation", chainProfile.AvailableReputation.ToString());


                var updatemodel = Helpers.Serializers.DeserializeJson<UserDto>(Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/Users/Update", Helpers.Serializers.SerializeJson(userModel), HttpContext.Session.GetString("Token")));

                TempData["toastr-message"] = "Your wallet is connected to your account successfully";
                TempData["toastr-type"] = "success";

                return base.Json(new SimpleResponse { Success = true, Message = "" });
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return base.Json(new SimpleResponse { Success = true, Message = "An error occured while connecting wallet." });
        }

        public UserChainProfile GetUserChainProfile(string publicAddress)
        {
            UserChainProfile profile = new UserChainProfile();

            try
            {
                //Get model from ApiGateway
                var userjson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/CasperChainService/CasperMiddleware/GetUserChainProfile?publicAddress=" + publicAddress);
                //Parse response
                profile = Helpers.Serializers.DeserializeJson<UserChainProfile>(userjson);
            }
            catch (Exception ex)
            {

            }

            return profile;
        }

        public IActionResult ChainActionDetail(int id)
        {
            //Get model from ApiGateway
            var chainjson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/ChainAction/GetId?id=" + id);
            //Parse response
            ChainActionDto chainActionModel = Helpers.Serializers.DeserializeJson<ChainActionDto>(chainjson);

            return View(chainActionModel);
        }

        #region BidEscrow
        public JsonResult GetPostJobOfferDeploy(long timeframe, int budget)
        {
            try
            {
                SimpleResponse controlResult = UserInputControls.ControlPostJobOfferRequest(timeframe, budget);

                if (controlResult.Success == false) return base.Json(controlResult);

                long time = (long)((dynamic)controlResult.Content).timeframe;

                //Get model from ApiGateway
                var deployJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/CasperChainService/Contracts/BidEscrowPostJobOffer?userwallet=" + HttpContext.Session.GetString("WalletAddress") + "&expectedtimeframe=" + time + "&budget=" + budget);
                //Parse response
                SimpleResponse deployModel = Helpers.Serializers.DeserializeJson<SimpleResponse>(deployJson);

                //Return deploy object in JSON
                return base.Json(deployModel);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }
        public JsonResult GetSubmitBidDeploy(uint jobofferid, ulong time, ulong userpayment, ulong repstake, bool onboard)
        {
            try
            {
                SimpleResponse controlResult = UserInputControls.ControlSubmitBidRequest(jobofferid, time, userpayment, repstake, onboard);

                if (controlResult.Success == false) return base.Json(controlResult);

                long timeframe = (long)((dynamic)controlResult.Content).time;

                //Get model from ApiGateway
                var deployJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/CasperChainService/Contracts/BidEscrowSubmitBid?userwallet=" + HttpContext.Session.GetString("WalletAddress") + "&jobofferid=" + jobofferid + "&time=" + timeframe + "&userpayment=" + userpayment + "&repstake=" + repstake + "&onboard=" + onboard);
                //Parse response
                SimpleResponse deployModel = Helpers.Serializers.DeserializeJson<SimpleResponse>(deployJson);

                //Return deploy object in JSON
                return base.Json(deployModel);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }
        public JsonResult GetCancelBidDeploy(uint bidid)
        {
            try
            {
                SimpleResponse controlResult = UserInputControls.ControlCancelBidRequest(bidid);

                if (controlResult.Success == false) return base.Json(controlResult);

                //Get model from ApiGateway
                var deployJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/CasperChainService/Contracts/BidEscrowCancelBid?userwallet=" + HttpContext.Session.GetString("WalletAddress") + "&bidid=" + bidid);
                //Parse response
                SimpleResponse deployModel = Helpers.Serializers.DeserializeJson<SimpleResponse>(deployJson);

                //Return deploy object in JSON
                return base.Json(deployModel);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }
        public JsonResult GetPickBidDeploy(uint jobid, uint bidid)
        {
            try
            {
                SimpleResponse controlResult = UserInputControls.ControlPickBidRequest(bidid, jobid);

                if (controlResult.Success == false) return base.Json(controlResult);

                //Get model from ApiGateway
                var deployJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/CasperChainService/Contracts/BidEscrowPickBid?userwallet=" + HttpContext.Session.GetString("WalletAddress") + "&bidid=" + bidid + "&jobid=" + jobid);
                //Parse response
                SimpleResponse deployModel = Helpers.Serializers.DeserializeJson<SimpleResponse>(deployJson);

                //Return deploy object in JSON
                return base.Json(deployModel);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }
        public JsonResult GetSubmitJobProofDeploy(uint jobid, string documenthash)
        {
            try
            {
                SimpleResponse controlResult = UserInputControls.ControlSubmitJobProofRequest(jobid, documenthash);

                if (controlResult.Success == false) return base.Json(controlResult);

                //Get model from ApiGateway
                var deployJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/CasperChainService/Contracts/BidEscrowSubmitJobProof?userwallet=" + HttpContext.Session.GetString("WalletAddress") + "&documenthash=" + documenthash + "&jobid=" + jobid);
                //Parse response
                SimpleResponse deployModel = Helpers.Serializers.DeserializeJson<SimpleResponse>(deployJson);

                //Return deploy object in JSON
                return base.Json(deployModel);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }
        public JsonResult GetSubmitJobProofGracePeriodDeploy(uint jobid, string proof, uint repstake, bool onboard)
        {
            try
            {
                SimpleResponse controlResult = UserInputControls.ControlSubmitJobProofGracePeriodRequest(jobid, proof, repstake, onboard);

                if (controlResult.Success == false) return base.Json(controlResult);

                //Get model from ApiGateway
                var deployJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/CasperChainService/Contracts/BidEscrowSubmitJobProofGracePeriod?userwallet=" + HttpContext.Session.GetString("WalletAddress") + "&jobid=" + jobid + "&proof=" + proof + "&repstake=" + repstake + "&onboard=" + onboard);
                //Parse response
                SimpleResponse deployModel = Helpers.Serializers.DeserializeJson<SimpleResponse>(deployJson);

                //Return deploy object in JSON
                return base.Json(deployModel);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }

        #endregion

        #region Voters
        public JsonResult GetSubmitVoteDeploy(int votingId, int direction, int stake)
        {
            try
            {
                bool isinfavor = false;
                if(direction == 0)
                {
                    isinfavor = true;
                }

                SimpleResponse controlResult = UserInputControls.ControlSubmitVoteRequest(votingId, stake);

                if (controlResult.Success == false) return base.Json(controlResult);

                //Get voting model from ApiGateway
                var votingJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Voting/Voting/GetId?id=" + votingId, HttpContext.Session.GetString("Token"));
                //Parse response
                VotingDto voting = Helpers.Serializers.DeserializeJson<VotingDto>(votingJson);

                //Get model from ApiGateway
                var deployJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/CasperChainService/Contracts/SubmitVote?votetype=" + voting.Type + "&isFormal=" + Convert.ToInt32(voting.IsFormal) + "&votingid=" + voting.BlockchainVotingID + "&choice=" + Convert.ToInt32(isinfavor) + "&stake=" + stake + "&userwallet=" + HttpContext.Session.GetString("WalletAddress"));

                //Parse response
                SimpleResponse deployModel = Helpers.Serializers.DeserializeJson<SimpleResponse>(deployJson);

                //Return deploy object in JSON
                return base.Json(deployModel);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }

        public JsonResult GetSimpleVoteDeploy(string documenthash, int stake)
        {
            try
            {
                SimpleResponse controlResult = UserInputControls.ControlSimpleVoteRequest(documenthash);

                if (controlResult.Success == false) return base.Json(controlResult);

                //Get model from ApiGateway
                var deployJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/CasperChainService/Contracts/SimpleVoterCreateVoting?userwallet=" + HttpContext.Session.GetString("WalletAddress") + "&stake=" + stake + "&documenthash=" + documenthash);
                //Parse response
                SimpleResponse deployModel = Helpers.Serializers.DeserializeJson<SimpleResponse>(deployJson);

                //Return deploy object in JSON
                return base.Json(deployModel);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }

        public JsonResult GetVaOnboardingVoteDeploy(string username, string vaaddress, string reason)
        {
            try
            {
                SimpleResponse controlResult = UserInputControls.ControlVaOnboardingVoteRequest(username, vaaddress, reason, HttpContext.Session.GetString("Token"));

                if (controlResult.Success == false) return base.Json(controlResult);

                //Get model from ApiGateway
                var deployJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/CasperChainService/Contracts/VaOnboardingVoterCreateVoting?userwallet=" + HttpContext.Session.GetString("WalletAddress") + "&reason=" + reason + "&onboardwallet=" + ((UserDto)((dynamic)controlResult.Content).user).WalletAddress);
                //Parse response
                SimpleResponse deployModel = Helpers.Serializers.DeserializeJson<SimpleResponse>(deployJson);

                //Return deploy object in JSON
                return base.Json(deployModel);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }

        public JsonResult GetRepoVoteDeploy(string key, string value, int stake)
        {
            try
            {
                SimpleResponse controlResult = UserInputControls.ControlGovernanceVoteRequest(key, value);

                if (controlResult.Success == false) return base.Json(controlResult);

                DateTime currentDateTime = DateTime.Now;
                long activationTime = ((DateTimeOffset)currentDateTime).ToUnixTimeSeconds();

                //Get model from ApiGateway
                var deployJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/CasperChainService/Contracts/RepoVoterCreateVoting?userwallet=" + HttpContext.Session.GetString("WalletAddress") + "&key=" + key + "&value=" + value + "&stake=" + stake + "&activationtime=" + activationTime);
                //Parse response
                SimpleResponse deployModel = Helpers.Serializers.DeserializeJson<SimpleResponse>(deployJson);

                //Return deploy object in JSON
                return base.Json(deployModel);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }

        public JsonResult GetKYCVoteDeploy(string username, int stake)
        {
            try
            {
                SimpleResponse controlResult = UserInputControls.ControlKYCVoteRequest(username, HttpContext.Session.GetString("Token"));

                if (controlResult.Success == false) return base.Json(controlResult);

                //Get model from ApiGateway
                var deployJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/CasperChainService/Contracts/KYCVoterCreateVoting?userwallet=" + HttpContext.Session.GetString("WalletAddress") + "&stake=" + stake + "&kycUserAddress=" + ((UserDto)((dynamic)controlResult.Content).user).WalletAddress + "&documenthash=" + ((UserKYCDto)((dynamic)controlResult.Content).kyc).VerificationId);
                //Parse response
                SimpleResponse deployModel = Helpers.Serializers.DeserializeJson<SimpleResponse>(deployJson);

                //Return deploy object in JSON
                return base.Json(deployModel);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }

        public JsonResult GetReputationVoteDeploy(int action, string subjectaddress, int amount, string documenthash, int stake, string repusername)
        {
            try
            {
                SimpleResponse controlResult = UserInputControls.ControlReputationVoteRequest(amount, documenthash, action, repusername, subjectaddress, HttpContext.Session.GetString("Token"));

                if (controlResult.Success == false) return base.Json(controlResult);

                //Get model from ApiGateway
                var deployJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/CasperChainService/Contracts/ReputationVoterCreateVoting?userwallet=" + HttpContext.Session.GetString("WalletAddress") + "&stake=" + stake + "&subjectaddress=" + ((UserDto)((dynamic)controlResult.Content).user).WalletAddress + "&action=" + action + "&amount=" + amount + "&documenthash=" + documenthash);
                //Parse response
                SimpleResponse deployModel = Helpers.Serializers.DeserializeJson<SimpleResponse>(deployJson);

                //Return deploy object in JSON
                return base.Json(deployModel);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }

        public JsonResult GetSlashingVoteDeploy(string addresstoslash, int slashratio, int stake, string slashusername)
        {
            try
            {
                SimpleResponse controlResult = UserInputControls.ControlSlashingVoteRequest(addresstoslash, slashusername, HttpContext.Session.GetString("Token"));

                if (controlResult.Success == false) return base.Json(controlResult);

                //Get model from ApiGateway
                var deployJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/CasperChainService/Contracts/SlashingVoterCreateVoting?userwallet=" + HttpContext.Session.GetString("WalletAddress") + "&addresstoslash=" + ((UserDto)((dynamic)controlResult.Content).user).WalletAddress + "&slashratio=" + slashratio + "&stake=" + stake);
                //Parse response
                SimpleResponse deployModel = Helpers.Serializers.DeserializeJson<SimpleResponse>(deployJson);

                //Return deploy object in JSON
                return base.Json(deployModel);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
            }
        }

        #endregion

    }
}