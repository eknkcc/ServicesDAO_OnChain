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
using Casper.Network.SDK;
using Casper.Network.SDK.Types;
using System.Threading;
using Casper.Network.SDK.Utils;
using Microsoft.AspNetCore.Components;
using Casper.Network.SDK.Web;
using Casper.Network.SDK.Clients;
using System.Threading.Tasks;

namespace DAO_WebPortal.Controllers
{
    public class ChainController : Controller
    {
        //[Parameter] public IERC20Client ERC20Client { get; set; }

        //[Inject] protected CasperSignerInterop SignerInterop { get; set; }


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
                if (chainProfile.IsVA)
                {
                    HttpContext.Session.SetString("UserType", "VotingAssociate");
                }
                else
                {
                    HttpContext.Session.SetString("UserType", "Associate");
                }
                HttpContext.Session.SetString("Balance", chainProfile.Balance.ToString());
                HttpContext.Session.SetString("Reputation", chainProfile.Reputation.ToString());

                //Create model
                LoginChainModel LoginModelPost = new LoginChainModel() { walletAddress = publicAddress, isVA = chainProfile.IsVA, ip = ip, port = port, application = Helpers.Constants.Enums.AppNames.DAO_WebPortal };

                //Post model to ApiGateway
                var loginJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/PublicActions/LoginChain", Helpers.Serializers.SerializeJson(LoginModelPost));

                //Parse response
                LoginResponse loginModel = Helpers.Serializers.DeserializeJson<LoginResponse>(loginJson);

                string token = loginModel.Token.ToString();

                HttpContext.Session.SetInt32("UserID", 0);
                HttpContext.Session.SetString("Email", "");
                HttpContext.Session.SetString("Token", token);
                HttpContext.Session.SetString("LoginType", "user");
                HttpContext.Session.SetString("NameSurname", publicAddress.Substring(0, 5) + "..." + publicAddress.Substring(publicAddress.Length - 5, 5));
                HttpContext.Session.SetString("ProfileImage", "");
                HttpContext.Session.SetString("KYCStatus", "");
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
                if (chainProfile.IsVA)
                {
                    HttpContext.Session.SetString("UserType", "VotingAssociate");
                }
                else
                {
                    HttpContext.Session.SetString("UserType", "Associate");
                }
                HttpContext.Session.SetString("Balance", chainProfile.Balance.ToString());
                HttpContext.Session.SetString("Reputation", chainProfile.Reputation.ToString());
                HttpContext.Session.SetString("WalletAddress", publicAddress);

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
                var hex = publicAddress;
                var publicKey = PublicKey.FromHexString(hex);
                var casperSdk = new NetCasperClient(Program._settings.NodeUrl + ":7777/rpc");
                var rpcResponse = casperSdk.GetAccountBalance(publicKey).Result;

                double balanceParsed = Convert.ToInt64(rpcResponse.Parse().BalanceValue.ToString()) / (double)1000000000;
                profile.Balance = balanceParsed.ToString("N2");

                Console.WriteLine("Public Key Balance: " + rpcResponse.Parse().BalanceValue);

                // CasperClient casperClient = new CasperClient(rpcUrl);
                // var result = casperClient.RpcService.GetAccountBalance(publicAddress);
                // double balanceParsed = Convert.ToInt64(result.result.balance_value) / (double)1000000000;
                // profile.Balance = balanceParsed.ToString("N2");
            }
            catch (Exception ex)
            {

            }

            return profile;
        }

        [HttpPost]
        public JsonResult SendSignedDeploy(string deployObj)
        {
            ChainActionDto chainAction = new ChainActionDto();

            try
            {
                Program.monitizer.AddApplicationLog(LogTypes.ChainLog, "Sending Deploy: " + deployObj);
                string walletAddress = HttpContext.Session.GetString("WalletAddress");

                chainAction = new ChainActionDto() { ActionType = "Contract Call", CreateDate = DateTime.Now, WalletAddress = walletAddress, DeployJson = deployObj, Status = "In Progress" };
                var chainQuePostJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/ChainAction/Post", Helpers.Serializers.SerializeJson(chainAction));
                chainAction = Helpers.Serializers.DeserializeJson<ChainActionDto>(chainQuePostJson);
                if (chainAction != null && chainAction.ChainActionId > 0)
                {
                    Program.chainQue.Add(chainAction);
                }

                Deploy deploy = Deploy.Parse(deployObj);

                new Thread(() => 
                {
                    Thread.CurrentThread.IsBackground = true; 


                    NetCasperClient casperSdk = new NetCasperClient(Program._settings.NodeUrl + ":7777/rpc");

                    var response = casperSdk.PutDeploy(deploy).Result;

                    //chainAction.Result = " Error:" + response.Error;

                    Program.monitizer.AddApplicationLog(LogTypes.ChainLog, "Deploy Result: " + response.Result.GetRawText());
                    Program.monitizer.AddApplicationLog(LogTypes.ChainLog, "Deploy Error: " + response.Error);

                    var deployHash = response.GetDeployHash();

                    chainAction.DeployHash = deployHash;
                    Program.monitizer.AddApplicationLog(LogTypes.ChainLog, "Deploy Hash: " + deployHash);

                    var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                    var deployResponse = casperSdk.GetDeploy(deployHash, tokenSource.Token).Result;

                    chainAction.Result = deployResponse.Result.GetRawText();
                    
                    Program.monitizer.AddApplicationLog(LogTypes.ChainLog, "Deploy Response: " + deployResponse.Result.GetRawText()+ " Error: " + deployResponse.Error);

                    if(deployResponse.Error == null)
                    {
                        chainAction.Status = "Completed";
                    }
                    else
                    {
                        chainAction.Status = "Failed";
                    }

                    var updateJson = Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/ChainAction/Update", Helpers.Serializers.SerializeJson(chainAction));
                }).Start();

                TempData["toastr-message"] = "Your deploy is in progress.";
                TempData["toastr-type"] = "success";

                return base.Json(new SimpleResponse { Success = true, Message = "Your deploy is in progress." });
            }
            catch (Exception ex)
            {
                chainAction.Status = "Error";
                var updateJson = Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/ChainAction/Update", Helpers.Serializers.SerializeJson(chainAction));

                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return base.Json(new SimpleResponse { Success = false, Message = "An error occured while sending the deploy to the chain. Please check chain logs for details." });
            }
        }

        public IActionResult ChainActionDetail(int id)
        {
            //Get model from ApiGateway
            var chainjson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/ChainAction/GetId?id=" + id);
            //Parse response
            ChainActionDto chainActionModel = Helpers.Serializers.DeserializeJson<ChainActionDto>(chainjson);

            return View(chainActionModel);
        }

        #region KYC 

        public JsonResult GetKYCVoteDeploy(string username, int stake)
        {
            try
            {
                //Get model from ApiGateway
                var userjson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetByUsername?username=" + username, HttpContext.Session.GetString("Token"));
                //Parse response
                UserDto profileModel = Helpers.Serializers.DeserializeJson<UserDto>(userjson);

                //Check user exists
                if (profileModel == null || profileModel.UserId <= 0)
                {
                    return base.Json(new SimpleResponse { Success = false, Message = "User not found." });
                }

                //Check user already completed KYC
                if (profileModel.KYCStatus == true)
                {
                    return base.Json(new SimpleResponse { Success = false, Message = "This user has already completed KYC process." });
                }

                //Check user's wallet exists
                if (string.IsNullOrEmpty(profileModel.WalletAddress))
                {
                    return base.Json(new SimpleResponse { Success = false, Message = "User's chain address could not be found. " + username + " needs to connect a wallet first." });
                }

                //Get model from ApiGateway
                var userkycjson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/UserKYC/GetUserId?id=" + profileModel.UserId, HttpContext.Session.GetString("Token"));
                //Parse response
                UserKYCDto userKycModel = Helpers.Serializers.DeserializeJson<UserKYCDto>(userkycjson);

                //Check if there is KYC application
                if (userKycModel == null || userKycModel.UserKYCID <= 0)
                {
                    return base.Json(new SimpleResponse { Success = false, Message = "There is no KYC application for this user. User has to submit KYC form from the 'User Profile' page." });
                }

                PublicKey kycUserAccountPK = PublicKey.FromHexString(profileModel.WalletAddress);
                PublicKey myAccountPK = PublicKey.FromHexString(HttpContext.Session.GetString("WalletAddress"));

                //"account-hash-6d87e1a98e9122460573b8bc6a4cf93c0fd2736b51d388ab28155f881e5d3c81"
                var subjectAddress = new AccountHashKey(kycUserAccountPK.GetAccountHash());

                var namedArgs = new List<NamedArg>()
                {
                    new NamedArg("subject_address", CLValue.Key(subjectAddress)),
                    //new NamedArg("document_hash", CLValue.String(userKycModel.VerificationId)),
                    new NamedArg("document_hash", CLValue.U256(13455)),
                    new NamedArg("stake", CLValue.U256(stake))
                };

                //Create deploy object
                HashKey contractHash = new HashKey(Program._settings.KYCVoterContract);
                var deploy = DeployTemplates.ContractCall(contractHash,
                       "create_voting",
                       namedArgs,
                       myAccountPK,
                       5_000_000_000,
                       Program._settings.ChainName);

                //Return deploy object in JSON
                return base.Json(new SimpleResponse { Success = true, Message = deploy.SerializeToJson() });

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