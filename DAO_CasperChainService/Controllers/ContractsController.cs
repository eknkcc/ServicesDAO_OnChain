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
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Helpers.Models.CasperServiceModels;
using Account = Helpers.Models.CasperServiceModels.Account;
using Casper.Network.SDK.ByteSerializers;
using Org.BouncyCastle.Utilities;
using System.Diagnostics.Metrics;
using Casper.Network.SDK.JsonRpc;
using Casper.Network.SDK.JsonRpc.ResultTypes;

namespace DAO_CasperChainService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ContractsController : ControllerBase
    {
        [HttpPost("SendSignedDeploy", Name = "SendSignedDeploy")]
        public ChainActionDto SendSignedDeploy(ChainActionDto chainAction)
        {
            try
            {
                Deploy deploy = Deploy.Parse(chainAction.DeployJson);

                NetCasperClient casperSdk = new NetCasperClient(Program._settings.NodeUrl /*+ ":7777/rpc"*/);

                var response = casperSdk.PutDeploy(deploy).Result;

                //chainAction.Result = " Error:" + response.Error;

                Program.monitizer.AddApplicationLog(LogTypes.ChainLog, "Deploy Result: " + response.Result.GetRawText());
                Program.monitizer.AddApplicationLog(LogTypes.ChainLog, "Deploy Error: " + response.Error);

                var deployHash = response.GetDeployHash();

                chainAction.DeployHash = deployHash;
                Program.monitizer.AddApplicationLog(LogTypes.ChainLog, "Deploy Hash: " + deployHash);

                int counter = 0;
                RpcResponse<GetDeployResult> deployResponse = new RpcResponse<GetDeployResult>();

                while (counter < 10) 
                {
                    counter++;
                    try
                    {
                        var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                        deployResponse = casperSdk.GetDeploy(deployHash, tokenSource.Token).Result;
                        break;
                    }
                    catch
                    {

                    }
                    Thread.Sleep(5000);
                }

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
                        chainAction.ErrorReason = "Contract Error: " + parsedResult;
                    }
                }
                else
                {
                    chainAction.Status = Enums.ChainActionStatus.Failed.ToString();
                    chainAction.ErrorReason = "Deploy Error: " + deployResponse.Error.Message;
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                chainAction.Status = Enums.ChainActionStatus.Error.ToString();
                chainAction.ErrorReason = "Error: " + ex.Message;
            }

            return chainAction;
        }

        [HttpGet("ParseDeployResult", Name = "ParseDeployResult")]
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

            if (resultText == "")
            {
                try
                {
                    if (rawResult.Contains("error_message"))
                    {
                        int index = rawResult.IndexOf(",\"error_message\":") + ",\"error_message\":".Length;
                        int firstQuote = -1;
                        int secondQuote = -1;
                        for (int i = index; i < rawResult.Length; i++)
                        {
                            if (firstQuote == -1 && rawResult[i] == '"')
                            {
                                firstQuote = i;
                            }
                            else if (firstQuote != -1 && secondQuote == -1 && rawResult[i] == '"')
                            {
                                secondQuote = i;
                                break;
                            }
                        }

                        resultText = rawResult.Substring(firstQuote, secondQuote - firstQuote).Replace('"'.ToString(), "");
                        var errorSplitted = resultText.Split(":");
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
                    resultText = "Error could not be parsed.";
                }
            }

            return resultText;
        }

        [HttpGet("GetUserChainProfile2", Name = "GetUserChainProfile2")]
        public UserChainProfile GetUserChainProfile2(string publicAddress)
        {
            UserChainProfile profile = new UserChainProfile();

            try
            {
                var hex = publicAddress;
                var publicKey = PublicKey.FromHexString(hex);
                var casperSdk = new NetCasperClient(Program._settings.NodeUrl);
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
        #region Bid Escrow

        [HttpGet("BidEscrowPostJobOffer", Name = "BidEscrowPostJobOffer")]
        public SimpleResponse BidEscrow_PostJobOffer(ulong expectedtimeframe, ulong budget, string userwallet)
        {
            try
            {
                PublicKey myAccountPK = PublicKey.FromHexString(userwallet);

                var wasmFile = "./wwwroot/wasms/proxy_caller.wasm";
                var wasmBytes = System.IO.File.ReadAllBytes(wasmFile);

                var header = new DeployHeader()
                {
                    Account = myAccountPK,
                    Timestamp = DateUtils.ToEpochTime(DateTime.UtcNow),
                    Ttl = 1800000,
                    ChainName = Program._settings.ChainName,
                    GasPrice = 1
                };
                var payment = new ModuleBytesDeployItem(100_000_000_000);

                //Job parameters which will be converted to bytes
                List<NamedArg> runtimeJobArgs = new List<NamedArg>();
                runtimeJobArgs.Add(new NamedArg("expected_timeframe", CLValue.U64(expectedtimeframe)));
                runtimeJobArgs.Add(new NamedArg("budget", CLValue.U512(budget)));
                // 10 $ OR MORE EQUIVALENT CSPR
                runtimeJobArgs.Add(new NamedArg("dos_fee", CLValue.U512(340_000_000_000)));

                List<CLValue> clVals = new List<CLValue>();

                NamedArgByteSerializer namedArgByteSerializer = new NamedArgByteSerializer();
                MemoryStream memoryStream = new MemoryStream();
                memoryStream.Write(BitConverter.GetBytes(runtimeJobArgs.Count));

                foreach (NamedArg runtimeArg in runtimeJobArgs)
                {
                    var bytes = namedArgByteSerializer.ToBytes(runtimeArg);

                    memoryStream.Write(bytes);   
                }

                foreach (var byt in memoryStream.ToArray())
                {
                    clVals.Add(CLValue.U8(byt));
                }

                //MAIN PROXY ARGS
                List<NamedArg> runtimeArgs = new List<NamedArg>(); 
                runtimeArgs.Add(new NamedArg("contract_package_hash", CLValue.ByteArray(Program._settings.BidEscrowContractPackageHash)));
                runtimeArgs.Add(new NamedArg("entry_point", CLValue.String("post_job_offer")));
                runtimeArgs.Add(new NamedArg("amount", CLValue.U512(340_000_000_000)));
                runtimeArgs.Add(new NamedArg("attached_value", CLValue.Option(CLValue.U512(340_000_000_000))));
                runtimeArgs.Add(new NamedArg("args", CLValue.List(clVals.ToArray())));

                var session = new ModuleBytesDeployItem(wasmBytes, runtimeArgs);

                var deploy = new Deploy(header, payment, session);

                //Return deploy object in JSON
                return new SimpleResponse { Success = true, Message = deploy.SerializeToJson() };

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                return new SimpleResponse { Success = false };
            }
        }

        [HttpGet("BidEscrowSubmitBid", Name = "BidEscrowSubmitBid")]
        public SimpleResponse BidEscrow_SubmitBid(uint jobofferid, ulong time, ulong userpayment, bool onboard, string userwallet)
        {
            try
            {
                PublicKey myAccountPK = PublicKey.FromHexString(userwallet);

                var wasmFile = "./wwwroot/wasms/proxy_caller.wasm";
                var wasmBytes = System.IO.File.ReadAllBytes(wasmFile);

                var header = new DeployHeader()
                {
                    Account = myAccountPK,
                    Timestamp = DateUtils.ToEpochTime(DateTime.UtcNow),
                    Ttl = 1800000,
                    ChainName = Program._settings.ChainName,
                    GasPrice = 1
                };
                var payment = new ModuleBytesDeployItem(150_000_000_000);

                List<NamedArg> runtimeBidArgs = new List<NamedArg>();
                runtimeBidArgs.Add(new NamedArg("job_offer_id", CLValue.U32(jobofferid)));
                runtimeBidArgs.Add(new NamedArg("time", CLValue.U64(time)));
                runtimeBidArgs.Add(new NamedArg("payment", CLValue.U512(userpayment)));
                runtimeBidArgs.Add(new NamedArg("reputation_stake", CLValue.U512(0)));
                runtimeBidArgs.Add(new NamedArg("onboard", CLValue.Bool(onboard)));
                runtimeBidArgs.Add(new NamedArg("cspr_stake", CLValue.Option(CLValue.U512(150_000_000_000))));

                List<CLValue> clVals = new List<CLValue>();

                NamedArgByteSerializer namedArgByteSerializer = new NamedArgByteSerializer();
                MemoryStream memoryStream = new MemoryStream();
                memoryStream.Write(BitConverter.GetBytes(runtimeBidArgs.Count));

                foreach (NamedArg runtimeArg in runtimeBidArgs)
                {
                    var bytes = namedArgByteSerializer.ToBytes(runtimeArg);

                    memoryStream.Write(bytes);
                }

                foreach (var byt in memoryStream.ToArray())
                {
                    clVals.Add(CLValue.U8(byt));
                }

                //MAIN PROXY ARGS
                List<NamedArg> runtimeArgs = new List<NamedArg>();
                runtimeArgs.Add(new NamedArg("contract_package_hash", CLValue.ByteArray(Program._settings.BidEscrowContractPackageHash)));
                runtimeArgs.Add(new NamedArg("entry_point", CLValue.String("submit_bid")));
                runtimeArgs.Add(new NamedArg("args", CLValue.List(clVals.ToArray())));
                runtimeArgs.Add(new NamedArg("attached_value", CLValue.U512(150_000_000_000)));
                runtimeArgs.Add(new NamedArg("amount", CLValue.U512(150_000_000_000)));

                var session = new ModuleBytesDeployItem(wasmBytes, runtimeBidArgs);

                var deploy = new Deploy(header, payment, session);

                //Return deploy object in JSON
                return new SimpleResponse { Success = true, Message = deploy.SerializeToJson() };

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                return new SimpleResponse { Success = false };
            }
        }

        [HttpGet("BidEscrowSubmitBidVA", Name = "BidEscrowSubmitBidVA")]
        public SimpleResponse BidEscrow_SubmitBid_Va(uint jobofferid, ulong time, ulong userpayment, ulong repstake, string userwallet)
        {
            try
            {
                PublicKey myAccountPK = PublicKey.FromHexString(userwallet);

                var namedArgs = new List<NamedArg>()
                {
                    new NamedArg("job_offer_id", CLValue.U32(jobofferid)),
                    new NamedArg("time", CLValue.U64(time)),
                    new NamedArg("payment", CLValue.U512(userpayment)),
                    new NamedArg("reputation_stake", CLValue.U512(repstake)),
                    new NamedArg("onboard", CLValue.Bool(false)),
                    new NamedArg("cspr_stake", CLValue.Option(CLValue.U512(150_000_000_000)))
                };

                //Create deploy object
                HashKey contractHash = new HashKey(Program._settings.BidEscrowContract);
                var deploy = DeployTemplates.ContractCall(contractHash,
                       "submit_bid",
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

        [HttpGet("BidEscrowCancelBid", Name = "BidEscrowCancelBid")]
        public SimpleResponse BidEscrow_CancelBid(uint bidid, string userwallet)
        {
            try
            {
                PublicKey myAccountPK = PublicKey.FromHexString(userwallet);

                var namedArgs = new List<NamedArg>()
                {
                    new NamedArg("bid_id", CLValue.U32(bidid))
                };

                //Create deploy object
                HashKey contractHash = new HashKey(Program._settings.BidEscrowContract);
                var deploy = DeployTemplates.ContractCall(contractHash,
                       "cancel_bid",
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

        [HttpGet("BidEscrowPickBid", Name = "BidEscrowPickBid")]
        public SimpleResponse BidEscrow_PickBid(uint jobid, uint bidid, string userwallet, long bidamount)
        {
            try
            {
                PublicKey myAccountPK = PublicKey.FromHexString(userwallet);

                var wasmFile = "./wwwroot/wasms/proxy_caller.wasm";
                var wasmBytes = System.IO.File.ReadAllBytes(wasmFile);

                var header = new DeployHeader()
                {
                    Account = myAccountPK,
                    Timestamp = DateUtils.ToEpochTime(DateTime.UtcNow),
                    Ttl = 1800000,
                    ChainName = Program._settings.ChainName,
                    GasPrice = 3
                };
                var payment = new ModuleBytesDeployItem(200_000_000_000);

                List<NamedArg> runtimeBidArgs = new List<NamedArg>();
                runtimeBidArgs.Add(new NamedArg("job_offer_id", CLValue.U32(jobid)));
                runtimeBidArgs.Add(new NamedArg("bid_id", CLValue.U32(bidid)));
                runtimeBidArgs.Add(new NamedArg("cspr_amount", CLValue.U512(bidamount)));

                List<CLValue> clVals = new List<CLValue>();

                NamedArgByteSerializer namedArgByteSerializer = new NamedArgByteSerializer();
                MemoryStream memoryStream = new MemoryStream();
                memoryStream.Write(BitConverter.GetBytes(runtimeBidArgs.Count));

                foreach (NamedArg runtimeArg in runtimeBidArgs)
                {
                    var bytes = namedArgByteSerializer.ToBytes(runtimeArg);

                    memoryStream.Write(bytes);
                }

                foreach (var byt in memoryStream.ToArray())
                {
                    clVals.Add(CLValue.U8(byt));
                }

                List<NamedArg> runtimeArgs = new List<NamedArg>();
                runtimeArgs.Add(new NamedArg("contract_package_hash", CLValue.ByteArray(Program._settings.BidEscrowContractPackageHash)));
                runtimeArgs.Add(new NamedArg("entry_point", CLValue.String("pick_bid")));
                runtimeArgs.Add(new NamedArg("amount", CLValue.U512(bidamount)));
                runtimeArgs.Add(new NamedArg("attached_value", CLValue.Option(CLValue.U512(bidamount))));
                runtimeArgs.Add(new NamedArg("args", CLValue.List(clVals.ToArray())));

                var session = new ModuleBytesDeployItem(wasmBytes, runtimeArgs);

                var deploy = new Deploy(header, payment, session);

                //Return deploy object in JSON
                return new SimpleResponse { Success = true, Message = deploy.SerializeToJson() };

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                return new SimpleResponse { Success = false };
            }
        }

        [HttpGet("BidEscrowSubmitJobProof", Name = "BidEscrowSubmitJobProof")]
        public SimpleResponse BidEscrow_SubmitJobProof(uint bidid, string documenthash, string userwallet)
        {
            try
            {
                CasperMiddlewareController cont = new CasperMiddlewareController();
                var job = cont.GetJobByBidId(Convert.ToInt32(bidid));
               
                PublicKey myAccountPK = PublicKey.FromHexString(userwallet);

                var namedArgs = new List<NamedArg>()
                {
                    new NamedArg("job_id", CLValue.U32(Convert.ToUInt32(job.data.job_id))),
                    new NamedArg("proof", CLValue.String(documenthash))
                };

                //Create deploy object
                HashKey contractHash = new HashKey(Program._settings.BidEscrowContract);
                var deploy = DeployTemplates.ContractCall(contractHash,
                       "submit_job_proof",
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

        [HttpGet("BidEscrowSubmitJobProofGracePeriod", Name = "BidEscrowSubmitJobProofGracePeriod")]
        public SimpleResponse BidEscrow_SubmitJobProofGracePeriod(uint jobid, string proof, uint repstake, bool onboard, string userwallet)
        {
            try
            {
                PublicKey myAccountPK = PublicKey.FromHexString(userwallet);
                var bidEscrowAddress = GlobalStateKey.FromString(Program._settings.BidEscrowContractPackageHash);

                var wasmFile = "./wwwroot/wasms/proxy_caller.wasm";
                var wasmBytes = System.IO.File.ReadAllBytes(wasmFile);

                var header = new DeployHeader()
                {
                    Account = myAccountPK,
                    Timestamp = DateUtils.ToEpochTime(DateTime.UtcNow),
                    Ttl = 1800000,
                    ChainName = Program._settings.ChainName,
                    GasPrice = 1
                };
                var payment = new ModuleBytesDeployItem(150_000_000_000);

                List<NamedArg> runtimeJobArgs = new List<NamedArg>();
                runtimeJobArgs.Add(new NamedArg("bid_escrow_address", CLValue.Key(bidEscrowAddress)));
                runtimeJobArgs.Add(new NamedArg("job_id", CLValue.U32(jobid)));
                runtimeJobArgs.Add(new NamedArg("proof", CLValue.String(proof)));
                runtimeJobArgs.Add(new NamedArg("reputation_stake", CLValue.U512(repstake)));
                runtimeJobArgs.Add(new NamedArg("onboard", CLValue.Bool(onboard)));

                List<CLValue> clVals = new List<CLValue>();

                NamedArgByteSerializer namedArgByteSerializer = new NamedArgByteSerializer();
                MemoryStream memoryStream = new MemoryStream();
                memoryStream.Write(BitConverter.GetBytes(runtimeJobArgs.Count));

                foreach (NamedArg runtimeArg in runtimeJobArgs)
                {
                    var bytes = namedArgByteSerializer.ToBytes(runtimeArg);

                    memoryStream.Write(bytes);
                }

                foreach (var byt in memoryStream.ToArray())
                {
                    clVals.Add(CLValue.U8(byt));
                }

                List<NamedArg> runtimeArgs = new List<NamedArg>();
                runtimeArgs.Add(new NamedArg("contract_package_hash", CLValue.ByteArray(Program._settings.BidEscrowContractPackageHash)));
                runtimeArgs.Add(new NamedArg("entry_point", CLValue.String("submit_job_proof_during_grace_period")));
                runtimeArgs.Add(new NamedArg("amount", CLValue.U512(150_000_000_000)));
                runtimeArgs.Add(new NamedArg("attached_value", CLValue.Option(CLValue.U512(150_000_000_000))));
                runtimeArgs.Add(new NamedArg("args", CLValue.List(clVals.ToArray())));

                var session = new ModuleBytesDeployItem(wasmBytes, runtimeArgs);

                var deploy = new Deploy(header, payment, session);

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

        #region Voters

        [HttpGet("SubmitVote", Name = "SubmitVote")]
        public SimpleResponse SubmitVote(Enums.VoteTypes votetype, byte isFormal, uint votingid, byte choice, int stake, string userwallet)
        {
            try
            {
                string contractAddress = "";
                if (votetype == VoteTypes.Simple)
                {
                    contractAddress = Program._settings.SimpleVoterContract;
                }
                else if (votetype == VoteTypes.Governance)
                {
                    contractAddress = Program._settings.RepoVoterContract;
                }
                else if (votetype == VoteTypes.Admin)
                {
                    contractAddress = Program._settings.AdminContract;
                }
                else if (votetype == VoteTypes.JobCompletion)
                {
                    contractAddress = Program._settings.BidEscrowContract;
                }
                else if (votetype == VoteTypes.VAOnboarding)
                {
                    contractAddress = Program._settings.OnboardingRequestContract;
                }
                else if (votetype == VoteTypes.KYC)
                {
                    contractAddress = Program._settings.KycVoterContract;
                }
                else if (votetype == VoteTypes.Reputation)
                {
                    contractAddress = Program._settings.ReputationVoterContract;
                }
                else if (votetype == VoteTypes.Slashing)
                {
                    contractAddress = Program._settings.SlashingVoterContract;
                }

                PublicKey myAccountPK = PublicKey.FromHexString(userwallet);

                var namedArgs = new List<NamedArg>()
                {
                    new NamedArg("voting_id", CLValue.U32(votingid)),
                    new NamedArg("voting_type", CLValue.U32(isFormal)),
                    new NamedArg("choice", CLValue.U32(choice)),
                    new NamedArg("stake", CLValue.U512(stake))
                };

                //Create deploy object
                HashKey contractHash = new HashKey(contractAddress);
                var deploy = DeployTemplates.ContractCall(contractHash,
                       "vote",
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

        [HttpGet("FinishVoting", Name = "FinishVoting")]
        public SimpleResponse FinishVoting(Enums.VoteTypes votetype, byte isFormal, uint votingid, string userwallet)
        {
            try
            {
                string contractAddress = "";
                if (votetype == VoteTypes.Simple)
                {
                    contractAddress = Program._settings.SimpleVoterContract;
                }
                else if (votetype == VoteTypes.Governance)
                {
                    contractAddress = Program._settings.RepoVoterContract;
                }
                else if (votetype == VoteTypes.Admin)
                {
                    contractAddress = Program._settings.AdminContract;
                }
                else if (votetype == VoteTypes.JobCompletion)
                {
                    contractAddress = Program._settings.BidEscrowContract;
                }
                else if (votetype == VoteTypes.VAOnboarding)
                {
                    contractAddress = Program._settings.OnboardingRequestContract;
                }
                else if (votetype == VoteTypes.KYC)
                {
                    contractAddress = Program._settings.KycVoterContract;
                }
                else if (votetype == VoteTypes.Reputation)
                {
                    contractAddress = Program._settings.ReputationVoterContract;
                }
                else if (votetype == VoteTypes.Slashing)
                {
                    contractAddress = Program._settings.SlashingVoterContract;
                }

                PublicKey myAccountPK = PublicKey.FromHexString(userwallet);

                var namedArgs = new List<NamedArg>()
                {
                    new NamedArg("voting_id", CLValue.U32(votingid)),
                    new NamedArg("voting_type", CLValue.U32(isFormal))
                };

                //Create deploy object
                HashKey contractHash = new HashKey(contractAddress);
                var deploy = DeployTemplates.ContractCall(contractHash,
                       "finish_voting",
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
        public SimpleResponse VaOnboarding_CreateVoting(string userwallet, string reason, string onboardwallet)
        {
            try
            {
                PublicKey myAccountPK = PublicKey.FromHexString(userwallet);

                PublicKey onboardUserWallet = PublicKey.FromHexString(onboardwallet);

                //IGNORE
                //var onboardingKeyHex = PublicKey.FromHexString(onboardwallet);
                //var onboardingKey = GlobalStateKey.FromString(onboardingKeyHex.GetAccountHash());

                var wasmFile = "./wwwroot/wasms/proxy_caller.wasm";
                var wasmBytes = System.IO.File.ReadAllBytes(wasmFile);

                var header = new DeployHeader()
                {
                    Account = myAccountPK,
                    Timestamp = DateUtils.ToEpochTime(DateTime.UtcNow),
                    Ttl = 1800000,
                    ChainName = Program._settings.ChainName,
                    GasPrice = 1
                };
                var payment = new ModuleBytesDeployItem(150_000_000_000);

                List<NamedArg> runtimeOnboardingArgs = new List<NamedArg>();
                runtimeOnboardingArgs.Add(new NamedArg("reason", CLValue.String(reason)));

                List<CLValue> clVals = new List<CLValue>();

                NamedArgByteSerializer namedArgByteSerializer = new NamedArgByteSerializer();
                MemoryStream memoryStream = new MemoryStream();
                memoryStream.Write(BitConverter.GetBytes(runtimeOnboardingArgs.Count));

                foreach (NamedArg runtimeArg in runtimeOnboardingArgs)
                {
                    var bytes = namedArgByteSerializer.ToBytes(runtimeArg);

                    memoryStream.Write(bytes);
                }

                foreach (var byt in memoryStream.ToArray())
                {
                    clVals.Add(CLValue.U8(byt));
                }

                //MAIN PROXY ARGS
                List<NamedArg> runtimeArgs = new List<NamedArg>();
                runtimeArgs.Add(new NamedArg("contract_package_hash", CLValue.ByteArray(Program._settings.VAOnboardingPackageHash)));
                runtimeArgs.Add(new NamedArg("entry_point", CLValue.String("create_voting")));
                runtimeArgs.Add(new NamedArg("amount", CLValue.U512(150_000_000_000)));
                runtimeArgs.Add(new NamedArg("attached_value", CLValue.Option(CLValue.U512(150_000_000_000))));
                runtimeArgs.Add(new NamedArg("args", CLValue.List(clVals.ToArray())));

                var session = new ModuleBytesDeployItem(wasmBytes, runtimeArgs);

                var deploy = new Deploy(header, payment, session);

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

                if (string.IsNullOrEmpty(repo))
                {
                    repo = Program._settings.RepoVoterContractPackageHash;
                }

                var repoKey = GlobalStateKey.FromString(repo);

                List<CLValue> valueBytes = new List<CLValue>();
                foreach (var byt in Encoding.ASCII.GetBytes(value))
                {
                    valueBytes.Add(CLValue.U8(byt));
                }

                var namedArgs = new List<NamedArg>()
                {
                    new NamedArg("variable_repo_to_edit", CLValue.Key(repoKey)),
                    new NamedArg("key", CLValue.String(key)),
                    new NamedArg("value", CLValue.List(valueBytes.ToArray())),
                    new NamedArg("activation_time", CLValue.Option(activationtime)),
                    new NamedArg("stake", CLValue.U512(stake))
                };

                //Create deploy object
                HashKey contractHash = new HashKey(Program._settings.RepoVoterContract);
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
                    new NamedArg("action", CLValue.U32(action)),
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
