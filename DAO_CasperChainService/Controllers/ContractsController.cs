using Microsoft.AspNetCore.Http;
using Casper.Network.SDK;
using Casper.Network.SDK.Types;
using System.Threading;
using Casper.Network.SDK.Utils;
using Casper.Network.SDK.Clients;
using System.Threading.Tasks;
using Helpers.Models.WebsiteViewModels;
using Helpers.Models.DtoModels.MainDbDto;
using System;
using static Helpers.Constants.Enums;
using Helpers.Models.SharedModels;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Helpers.Constants;
using System.Text;
using Ubiety.Dns.Core;
using System.Collections;

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
                    var parsedResult = ParseDeployResult(chainAction.Result);

                    if (string.IsNullOrEmpty(parsedResult))
                    {
                        chainAction.Status = Enums.ChainActionStatus.Completed.ToString();
                    }
                    else
                    {
                        chainAction.Status = Enums.ChainActionStatus.BlockchainError.ToString();
                        chainAction.Result += "Blockchain Error: " + parsedResult;
                    }
                }
                else
                {
                    chainAction.Status = Enums.ChainActionStatus.Failed.ToString();
                    chainAction.Result += Environment.NewLine + deployResponse.Error.Message;
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                chainAction.Status = Enums.ChainActionStatus.Error.ToString();
                chainAction.Result += Environment.NewLine + ex.Message;
            }

            return chainAction;
        }

        public string ParseDeployResult(string rawResult)
        {
            string resultText = "";

            try
            {
                var errormsg = Helpers.Serializers.DeserializeJson<dynamic>(rawResult).execution_results[0].result.Failure.error_message.ToString();
                if (!string.IsNullOrEmpty(errormsg))
                {
                    resultText = errormsg;

                    var errorSplitted = errormsg.Split(":");
                    if (errorSplitted.Length > 1)
                    {
                        var errorCode = Convert.ToInt32(errorSplitted[1].Trim());
                        if (Models.CasperErrorDictionary.errorDictionary.ContainsKey(errorCode))
                        {
                            resultText += " (" + Models.CasperErrorDictionary.errorDictionary[errorCode] + ")";
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return resultText;
        }

        #region Variable Repository

        [HttpGet("VariableRepositoryAllVariables", Name = "VariableRepositoryAllVariables")]
        public SimpleResponse VariableRepository_AllVariables(string walletAddress)
        {
            try
            {
                var casperSdk = new NetCasperClient(Program._settings.NodeUrl + ":7777/rpc");

                var queryResponse = casperSdk.QueryGlobalState(Program._settings.VariableRepositoryContract, null, "balances__reputation_storage__contract").Result;

                var result = queryResponse.Parse();
                var balance = result.StoredValue.CLValue.ToBigInteger();
                Console.WriteLine("Balance: " + balance.ToString() + " $CSSDK");

                return new SimpleResponse();
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                return new SimpleResponse { Success = false };
            }

            //try
            //{
            //    var casperSdk = new NetCasperClient(Program._settings.NodeUrl + ":7777/rpc");

            //    var accountHash = new AccountHashKey("account-hash-"+walletAddress);
            //    var dictItem = Convert.ToBase64String(accountHash.GetBytes());

            //    var response = casperSdk.GetDictionaryItemByContract(Program._settings.VariableRepositoryContract, "all_variables", dictItem).Result;

            //    var result = response.Parse();
            //    var balance = result.StoredValue.CLValue.ToBigInteger();
            //    Console.WriteLine("Balance: " + balance.ToString() + " $CSSDK");

            //    return new SimpleResponse();
            //}
            //catch (Exception ex)
            //{
            //    Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
            //    return new SimpleResponse { Success = false };
            //}
        }

        #endregion

        #region Reputation

        [HttpGet("ReputationBalanceOf", Name = "ReputationBalanceOf")]
        public SimpleResponse Reputation_BalanceOf(string walletAddress)
        {
            try
            {
                var casperSdk = new NetCasperClient(Program._settings.NodeUrl + ":7777/rpc");

                var queryResponse = casperSdk.QueryGlobalState(Program._settings.ReputationContract, null, "balances__reputation_storage__contract").Result;

                var result = queryResponse.Parse();
                var balance = result.StoredValue.CLValue.ToBigInteger();
                Console.WriteLine("Balance: " + balance.ToString() + " $CSSDK");

                return new SimpleResponse();
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                return new SimpleResponse { Success = false };
            }
        }

        #endregion


        #region Voters

        [HttpGet("SimpleVoterCreateVoting", Name = "SimpleVoterCreateVoting")]
        public SimpleResponse SimpleVoter_CreateVoting(string userwallet, string documenthash, int stake)
        {
            try
            {
                PublicKey myAccountPK = PublicKey.FromHexString(userwallet);

                var namedArgs = new List<NamedArg>()
                {
                    new NamedArg("document_hash", CLValue.String(documenthash)),
                    new NamedArg("stake", CLValue.U512(stake))
                };

                //Create deploy object
                HashKey contractHash = new HashKey(Program._settings.SimpleVoterContract);
                var deploy = DeployTemplates.ContractCall(contractHash,
                       "create_voting",
                       namedArgs,
                       myAccountPK,
                       150_000_000_000,
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

        [HttpGet("VaOnboardingVoterCreateVoting", Name = "VaOnboardingVoterCreateVoting")]
        public SimpleResponse VaOnboarding_CreateVoting(string userwallet, string reason, string purse)
        {
            try
            {
                PublicKey myAccountPK = PublicKey.FromHexString(userwallet);

                var namedArgs = new List<NamedArg>()
                {
                    new NamedArg("reason", CLValue.String(reason)),
                    new NamedArg("purse", CLValue.URef(purse))
                };

                //Create deploy object
                HashKey contractHash = new HashKey(Program._settings.OnboardingRequestContract);
                var deploy = DeployTemplates.ContractCall(contractHash,
                       "create_voting",
                       namedArgs,
                       myAccountPK,
                       150_000_000_000,
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

        [HttpGet("RepoVoterCreateVoting", Name = "RepoVoterCreateVoting")]
        public SimpleResponse RepoVoter_CreateVoting(string userwallet, string repo, string key, string value, ulong activationtime, int stake)
        {
            try
            {
                PublicKey myAccountPK = PublicKey.FromHexString(userwallet);

                if(string.IsNullOrEmpty(repo))
                {
                    repo = "13216e15d887f1963af7685fc683b4e571a666a78965eace725cf5f4ba08dd96";
                }

                PublicKey repoAccountPK = PublicKey.FromHexString(repo);

                var subjectAddress = new AccountHashKey(repoAccountPK.GetAccountHash());

                List<CLValue> valueBytes = new List<CLValue>();
                foreach (var byt in Encoding.ASCII.GetBytes(value))
                {
                    valueBytes.Add(CLValue.U8(byt));
                }

                var namedArgs = new List<NamedArg>()
                {
                    new NamedArg("variable_repo_to_edit", CLValue.Key(subjectAddress)),
                    new NamedArg("key", CLValue.String(key)),
                    new NamedArg("value", CLValue.List(valueBytes.ToArray())),
                    new NamedArg("activation_time", CLValue.U64(activationtime)),
                    new NamedArg("stake", CLValue.U512(stake))
                };

                //Create deploy object
                HashKey contractHash = new HashKey(Program._settings.ReputationVoterContract);
                var deploy = DeployTemplates.ContractCall(contractHash,
                       "create_voting",
                       namedArgs,
                       myAccountPK,
                       150_000_000_000,
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

        [HttpGet("KYCVoterCreateVoting", Name = "KYCVoterCreateVoting")]
        public SimpleResponse KYCVoter_CreateVoting(string userwallet, string kycUserAddress, string documenthash, int stake)
        {
            try
            {
                PublicKey kycUserAccountPK = PublicKey.FromHexString(kycUserAddress);
                PublicKey myAccountPK = PublicKey.FromHexString(userwallet);

                var subjectAddress = new AccountHashKey(kycUserAccountPK.GetAccountHash());

                var namedArgs = new List<NamedArg>()
                {
                    new NamedArg("subject_address", CLValue.Key(subjectAddress)),
                    //new NamedArg("document_hash", CLValue.String(verificationId)),
                    //new NamedArg("document_hash", CLValue.U256(13455)),
                    new NamedArg("document_hash", CLValue.String(documenthash)),
                    new NamedArg("stake", CLValue.U512(stake))
                };

                //Create deploy object
                HashKey contractHash = new HashKey(Program._settings.KYCVoterContract);
                var deploy = DeployTemplates.ContractCall(contractHash,
                       "create_voting",
                       namedArgs,
                       myAccountPK,
                       150_000_000_000,
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

        [HttpGet("ReputationVoterCreateVoting", Name = "ReputationVoterCreateVoting")]
        public SimpleResponse ReputationVoter_CreateVoting(string userwallet, string subjectaddress, byte action, int amount, string documenthash, int stake)
        {
            try
            {
                PublicKey myAccountPK = PublicKey.FromHexString(userwallet);

                PublicKey repUserAccountPK = PublicKey.FromHexString(subjectaddress);

                var subjectAddress = new AccountHashKey(repUserAccountPK.GetAccountHash());

                var namedArgs = new List<NamedArg>()
                {
                    new NamedArg("account", CLValue.Key(subjectAddress)),
                    new NamedArg("action", CLValue.U8(action)),
                    new NamedArg("amount", CLValue.U512(amount)),
                    new NamedArg("document_hash", CLValue.String(documenthash)),
                    new NamedArg("stake", CLValue.U512(stake))
                };

                //Create deploy object
                HashKey contractHash = new HashKey(Program._settings.ReputationVoterContract);
                var deploy = DeployTemplates.ContractCall(contractHash,
                       "create_voting",
                       namedArgs,
                       myAccountPK,
                       150_000_000_000,
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

        [HttpGet("SlashingVoterCreateVoting", Name = "SlashingVoterCreateVoting")]
        public SimpleResponse SlashingVoter_CreateVoting(string userwallet, string addresstoslash, uint slashratio, int stake)
        {
            try
            {
                PublicKey myAccountPK = PublicKey.FromHexString(userwallet);

                PublicKey slashAccountPK = PublicKey.FromHexString(addresstoslash);
                var subjectAddress = new AccountHashKey(slashAccountPK.GetAccountHash());

                var namedArgs = new List<NamedArg>()
                {
                    new NamedArg("address_to_slash", CLValue.Key(subjectAddress)),
                    new NamedArg("slash_ratio", CLValue.U32(slashratio)),
                    new NamedArg("stake", CLValue.U512(stake))
                };

                //Create deploy object
                HashKey contractHash = new HashKey(Program._settings.SlashingVoterContract);
                var deploy = DeployTemplates.ContractCall(contractHash,
                       "create_voting",
                       namedArgs,
                       myAccountPK,
                       150_000_000_000,
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
