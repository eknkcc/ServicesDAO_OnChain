using Microsoft.AspNetCore.Http;
using Casper.Network.SDK;
using Casper.Network.SDK.Types;
using System.Threading;
using Casper.Network.SDK.Utils;
using Casper.Network.SDK.Web;
using Casper.Network.SDK.Clients;
using System.Threading.Tasks;
using Helpers.Models.WebsiteViewModels;
using Helpers.Models.DtoModels.MainDbDto;
using System;
using static Helpers.Constants.Enums;
using Helpers.Models.SharedModels;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace DAO_CasperChainService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ContractsController : ControllerBase
    {
        [HttpGet("GetUserChainProfile", Name = "GetUserChainProfile")]
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

            return profile;
        }

        [HttpPost("SendSignedDeploy", Name = "SendSignedDeploy")]
        public ChainActionDto SendSignedDeploy(ChainActionDto chainAction)
        {
            try
            {
                Deploy deploy = Deploy.Parse(chainAction.DeployJson);

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

                Program.monitizer.AddApplicationLog(LogTypes.ChainLog, "Deploy Response: " + deployResponse.Result.GetRawText() + " Error: " + deployResponse.Error);

                if (deployResponse.Error == null)
                {
                    chainAction.Status = "Completed";
                }
                else
                {
                    chainAction.Status = "Failed";
                    chainAction.Result += Environment.NewLine + deployResponse.Error.Message;
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                chainAction.Status = "Failed";
                chainAction.Result += Environment.NewLine + ex.Message;
            }

            return chainAction;
        }

        #region KYC 

        [HttpGet("GetKYCVoteDeploy", Name = "GetKYCVoteDeploy")]
        public SimpleResponse GetKYCVoteDeploy(string walletAddress, int stake)
        {
            try
            {
                PublicKey kycUserAccountPK = PublicKey.FromHexString(walletAddress);
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
                return new SimpleResponse { Success = true, Message = deploy.SerializeToJson() };

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                return new SimpleResponse { Success = false };
            }
        }

        #endregion
    }
}
