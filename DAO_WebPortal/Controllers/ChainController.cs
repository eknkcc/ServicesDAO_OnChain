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

namespace DAO_WebPortal.Controllers
{
    public class ChainController : Controller
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
                HttpContext.Session.SetString("PublicAddress", publicAddress);
                if (!string.IsNullOrEmpty(loginModel.WalletAddress))
                {
                    HttpContext.Session.SetString("WalletAddress", loginModel.WalletAddress.ToString());
                }
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
                HttpContext.Session.SetString("PublicAddress", publicAddress);

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

        #region KYC 

        public async void StartKYCVote()
        {
            try
            {
                var subjectAddress = new AccountHashKey("account-hash-6d87e1a98e9122460573b8bc6a4cf93c0fd2736b51d388ab28155f881e5d3c81");

                var namedArgs = new List<NamedArg>()
                {
                    new NamedArg("subject_address", CLValue.Key(subjectAddress)),
                    new NamedArg("document_hash", CLValue.U256(43)),
                    new NamedArg("stake", CLValue.U256(1000))
                };

                NetCasperClient casperSdk = new NetCasperClient(Program._settings.NodeUrl + ":7777/rpc");

                KeyPair myAccount = KeyPair.FromPem(Program._settings.ChainAssetsPath + "/integration_kyctoken_secret_key.cer");
                PublicKey myAccountPK = PublicKey.FromPem(Program._settings.ChainAssetsPath + "/integration_kyctoken_public_key.cer");

                HashKey contractHash = new HashKey("hash-fb3c8edbfffbd3ff7bb294457f9682ab391e7fb181799ee7a8fb8f264f2e19fc");
                var deploy = DeployTemplates.ContractCall(contractHash,
                       "create_voting",
                       namedArgs,
                       myAccountPK,
                       5_000_000_000,
                       Program._settings.ChainName);
                deploy.Sign(myAccount);

                var response = await casperSdk.PutDeploy(deploy);
                var deployHash = response.GetDeployHash();

                var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                var deployResponse = await casperSdk.GetDeploy(deployHash, tokenSource.Token);

                Console.WriteLine(deployResponse.Result.GetRawText());
            }
            catch (Exception ex)
            {

            }

        }

        #endregion
    }
}