using DAO_VotingEngine.Contexts;
using DAO_VotingEngine.Models;
using Helpers.Constants;
using Helpers.Models.DtoModels.ReputationDbDto;
using Helpers.Models.SharedModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Helpers;
using static Helpers.Constants.Enums;
using Helpers.Models.CasperServiceModels;
using Ubiety.Dns.Core.Common;

namespace DAO_VotingEngine
{
    public class TimedEvents
    {
        //Voting status control timer
        public static System.Timers.Timer votingStatusTimer;
        public static bool progress_CheckVotingStatusCasperChain = false;

        /// <summary>
        ///  Start timer controls of the application
        /// </summary>
        public static void StartTimers()
        {
            if (Program._settings.DaoBlockchain == null)
            {
                CheckVotingStatusOffchain(null, null);

                //Voting status timer
                votingStatusTimer = new System.Timers.Timer(10000);
                votingStatusTimer.Elapsed += CheckVotingStatusOffchain;
                votingStatusTimer.AutoReset = true;
                votingStatusTimer.Enabled = true;
            }
            else if (Program._settings.DaoBlockchain == Enums.Blockchain.Casper)
            {
                CheckVotingStatusCasperChain(null, null);

                //Voting status timer
                votingStatusTimer = new System.Timers.Timer(60000);
                votingStatusTimer.Elapsed += CheckVotingStatusCasperChain;
                votingStatusTimer.AutoReset = true;
                votingStatusTimer.Enabled = true;
            }
        }

        /// <summary>
        ///  Check voting status from centralized db
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private static void CheckVotingStatusOffchain(Object source, ElapsedEventArgs e)
        {
            try
            {
                using (dao_votesdb_context db = new dao_votesdb_context())
                {
                    //Get ended informal votings -> Start formal voting if quorum reached, else set voting status to Expired
                    var informalVotings = db.Votings.Where(x => x.IsFormal == false && x.EndDate < DateTime.Now && x.Status == Enums.VoteStatusTypes.Active).ToList();

                    foreach (var voting in informalVotings)
                    {
                        //Check if quorum is reached
                        var votes = db.Votes.Where(x => x.VotingID == voting.VotingID);
                        //Quorum reached -> Start formal voting
                        if (voting.QuorumCount != null && votes.Count() >= Convert.ToInt32(voting.QuorumCount))
                        {
                            voting.Status = Enums.VoteStatusTypes.Completed;
                            db.Entry(voting).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            db.SaveChanges();

                            Models.Voting formalVoting = new Models.Voting();
                            formalVoting.CreateDate = DateTime.Now;
                            formalVoting.StartDate = DateTime.Now;
                            TimeSpan ts = voting.EndDate - voting.StartDate;
                            formalVoting.EndDate = DateTime.Now.Add(ts);
                            formalVoting.IsFormal = true;
                            formalVoting.JobID = Convert.ToInt32(voting.JobID);
                            formalVoting.Status = Enums.VoteStatusTypes.Active;
                            formalVoting.Type = voting.Type;
                            formalVoting.PolicingRate = voting.PolicingRate;

                            if (voting.QuorumRatio != null && voting.QuorumRatio > 0)
                            {
                                formalVoting.QuorumCount = Convert.ToInt32(voting.QuorumRatio * Convert.ToDouble(db.Votes.Count(x => x.VotingID == voting.VotingID)));
                            }
                            else
                            {
                                formalVoting.QuorumCount = Convert.ToInt32(voting.QuorumRatio * 0.5);
                            }

                            formalVoting.EligibleUserCount = db.Votes.Count(x => x.VotingID == voting.VotingID);
                            formalVoting.QuorumRatio = voting.QuorumRatio;
                            formalVoting.StakedAgainst = 0;
                            formalVoting.StakedFor = 0;
                            db.Votings.Add(formalVoting);
                            db.SaveChanges();

                            //Send email notification to VAs
                            Helpers.Models.NotificationModels.SendEmailModel emailModel = new Helpers.Models.NotificationModels.SendEmailModel() { Subject = "Formal Voting Started For Job #" + voting.JobID, Content = "Formal voting process started for job #" + voting.JobID + "<br><br>Please submit your vote until " + formalVoting.EndDate.ToString(), TargetGroup = Enums.UserIdentityType.VotingAssociate };
                            Program.rabbitMq.Publish(Helpers.Constants.FeedNames.NotificationFeed, "email", Helpers.Serializers.Serialize(emailModel));

                        }
                        //Quorum isn't reached -> Set voting status to Expired
                        else
                        {
                            voting.Status = Enums.VoteStatusTypes.Expired;
                            db.Entry(voting).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            db.SaveChanges();
                        }

                        //Release staked reputations
                        var jsonResult = Helpers.Request.Get(Program._settings.Service_Reputation_Url + "/UserReputationStake/ReleaseStakes?referenceProcessID=" + voting.VotingID + "&reftype=" + Enums.StakeType.For);
                        SimpleResponse parsedResult = Helpers.Serializers.DeserializeJson<SimpleResponse>(jsonResult);
                    }

                    //Get ended formal votings -> Distribute or release reputations
                    var formalVotings = db.Votings.Where(x => x.IsFormal == true && x.EndDate < DateTime.Now && x.Status == Enums.VoteStatusTypes.Active).ToList();

                    foreach (var voting in formalVotings)
                    {
                        //Check if quorum is reached
                        var votes = db.Votes.Where(x => x.VotingID == voting.VotingID);
                        //Quorum reached -> Set voting status to completed and distribute reputations
                        if (voting.QuorumCount != null && votes.Count() >= Convert.ToInt32(voting.QuorumCount))
                        {
                            voting.Status = Enums.VoteStatusTypes.Completed;
                            db.Entry(voting).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            db.SaveChanges();

                            //Distribute staked reputations (Only for job completion votings)
                            if (voting.Type == Enums.VoteTypes.JobCompletion)
                            {
                                var jsonResult = Helpers.Request.Get(Program._settings.Service_Reputation_Url + "/UserReputationStake/DistributeStakes?votingId=" + voting.VotingID + "&jobId=" + voting.JobID + "&policingRate=" + voting.PolicingRate.ToString().Replace(",", "."));
                                SimpleResponse parsedResult = Helpers.Serializers.DeserializeJson<SimpleResponse>(jsonResult);
                            }
                            else
                            {
                                //Release staked reputations
                                var jsonResult = Helpers.Request.Get(Program._settings.Service_Reputation_Url + "/UserReputationStake/ReleaseStakes?referenceProcessID=" + voting.VotingID + "&reftype=" + Enums.StakeType.For);
                                SimpleResponse parsedResult = Helpers.Serializers.DeserializeJson<SimpleResponse>(jsonResult);
                            }
                        }
                        //Quorum isn't reached -> Set voting status to Expired
                        else
                        {
                            voting.Status = Enums.VoteStatusTypes.Expired;
                            db.Entry(voting).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            db.SaveChanges();

                            //Release staked reputations for voters
                            var jsonResult = Helpers.Request.Get(Program._settings.Service_Reputation_Url + "/UserReputationStake/ReleaseStakes?referenceProcessID=" + voting.VotingID + "&reftype=" + Enums.StakeType.For);
                            SimpleResponse parsedResult = Helpers.Serializers.DeserializeJson<SimpleResponse>(jsonResult);

                            //Release minted reputations for job doer
                            //var jsonResult2 = Helpers.Request.Get(Program._settings.Service_Reputation_Url + "/UserReputationStake/ReleaseStakes?referenceProcessID=" + voting.JobID+ "&reftype=" +Enums.StakeType.Mint);
                            //SimpleResponse parsedResult2 = Helpers.Serializers.DeserializeJson<SimpleResponse>(jsonResult2);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Program.monitizer.AddConsole("Exception in timer CheckVotingStatusOffchain. Ex: " + ex.Message);
            }
        }

        /// <summary>
        ///  Check voting status from casper blockchain
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private static void CheckVotingStatusCasperChain(Object source, ElapsedEventArgs e)
        {
            if (progress_CheckVotingStatusCasperChain == true) return;

            progress_CheckVotingStatusCasperChain = true;

            try
            {
                //Get voting data from chain and central db
                List<Models.Voting> dbVotings = new List<Models.Voting>();
                PaginatedResponse<Helpers.Models.CasperServiceModels.Voting> chainVotings = new PaginatedResponse<Helpers.Models.CasperServiceModels.Voting>();

                using (dao_votesdb_context db = new dao_votesdb_context())
                {
                    dbVotings = db.Votings.Where(x => x.Status == Enums.VoteStatusTypes.Active).ToList();
                }

                int informalWaitingCount = dbVotings.Count(x => x.IsFormal == false && x.DeployHash != null && x.BlockchainVotingID == null);
                int formalWaitingCount = dbVotings.Count(x => x.IsFormal == true && x.DeployHash != null && x.BlockchainVotingID == null);
                int informalFinishedCount = dbVotings.Count(x => x.IsFormal == false && x.BlockchainVotingID != null && x.EndDate < DateTime.Now);

                if (informalWaitingCount == 0 && formalWaitingCount == 0 && informalFinishedCount == 0)
                {
                    return;
                }

                chainVotings = Serializers.DeserializeJson<PaginatedResponse<Helpers.Models.CasperServiceModels.Voting>>(Request.Get(Program._settings.Service_CasperChain_Url + "/CasperMiddleware/GetVotings?page=1&page_size=100&has_ended=false&order_direction=DESC"));

                if(chainVotings.error != null)
                {
                    Program.monitizer.AddConsole("CheckVotingStatusCasperChain  CasperMiddleware GetVotings error: " + chainVotings.error);
                    return;
                }

                //Sync blockchain informal voting ids
                foreach (var item in dbVotings.Where(x => x.IsFormal == false && x.DeployHash != null && x.BlockchainVotingID == null))
                {
                    if (chainVotings.data.Count(x => x.deploy_hash == item.DeployHash) > 0)
                    {
                        using (dao_votesdb_context db = new dao_votesdb_context())
                        {
                            var chainVoting = chainVotings.data.First(x => x.deploy_hash == item.DeployHash);
                            var voting = db.Votings.Find(item.VotingID);
                            voting.BlockchainVotingID = chainVoting.voting_id;
                            voting.QuorumCount = chainVoting.informal_voting_quorum;
                            voting.CreateDate = DateTime.Now;
                            voting.StartDate = Convert.ToDateTime(chainVoting.informal_voting_starts_at);
                            voting.EndDate = Convert.ToDateTime(chainVoting.informal_voting_ends_at);
                            db.SaveChanges();
                        }
                    }
                }

                //Sync blockchain formal voting ids & deploy hash
                foreach (var item in dbVotings.Where(x => x.IsFormal == true && x.DeployHash == null && x.BlockchainVotingID == null))
                {
                    using (dao_votesdb_context db = new dao_votesdb_context())
                    {
                        var informalVoting = db.Votings.First(x => x.JobID == item.JobID && x.IsFormal == false && x.BlockchainVotingID != null);

                        if (chainVotings.data.Count(x => x.voting_id == informalVoting.BlockchainVotingID) > 0)
                        {
                            var chainVoting = chainVotings.data.First(x => x.voting_id == informalVoting.BlockchainVotingID);

                            var voting = db.Votings.Find(item.VotingID);
                            voting.BlockchainVotingID = chainVoting.voting_id;
                            voting.DeployHash = chainVoting.deploy_hash;
                            voting.QuorumCount = chainVoting.formal_voting_quorum;
                            voting.CreateDate = DateTime.Now;
                            voting.StartDate = Convert.ToDateTime(chainVoting.formal_voting_starts_at);
                            voting.EndDate = voting.StartDate.AddMilliseconds(Convert.ToInt64(chainVoting.formal_voting_ends_at));
                            db.SaveChanges();
                        }
                    }
                }

                //Start formal voting in central db if formal voting started onchain
                foreach (var informalVoting in dbVotings.Where(x => x.IsFormal == false && x.BlockchainVotingID != null))
                {
                    if (informalVoting.EndDate < DateTime.Now)
                    {
                        //Get all votes from chain and syncronize with central db (For doublecheck)
                        //Disabled for now because endpoints returns votes withouth knowing it belong to informal or not
                        //SyncronizeVotesFromChain(Enums.Blockchain.Casper, Convert.ToInt32(informalVoting.BlockchainVotingID), informalVoting.VotingID);

                        using (dao_votesdb_context db = new dao_votesdb_context())
                        {
                            //Quorum isn't reached -> Set voting status to Expired
                            if (db.Votes.Count(x => x.VotingID == informalVoting.VotingID) < informalVoting.QuorumCount)
                            {
                                var votingdb = db.Votings.Find(informalVoting.VotingID);
                                votingdb.Status = Enums.VoteStatusTypes.Expired;
                                db.Entry(votingdb).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                db.SaveChanges();

                                //Release staked reputations
                                Request.Get(Program._settings.Service_Reputation_Url + "/UserReputationStake/ReleaseStakes?referenceProcessID=" + informalVoting.VotingID + "&reftype=" + Enums.StakeType.For);

                                Program.monitizer.AddConsole("Informal voting finished without quorum #" + informalVoting.VotingID);
                            }
                        }

                        if (chainVotings.data.Count(x => x.voting_id == informalVoting.BlockchainVotingID) > 0)
                        {
                            using (dao_votesdb_context db = new dao_votesdb_context())
                            {
                                informalVoting.Status = Enums.VoteStatusTypes.Completed;
                                db.Entry(informalVoting).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                db.SaveChanges();

                                var chainVoting = chainVotings.data.First(x => x.voting_id == informalVoting.BlockchainVotingID);

                                Models.Voting formalVoting = new Models.Voting();
                                formalVoting.CreateDate = DateTime.Now;
                                formalVoting.StartDate = Convert.ToDateTime(chainVoting.formal_voting_starts_at);
                                formalVoting.EndDate = Convert.ToDateTime(chainVoting.formal_voting_ends_at);
                                formalVoting.IsFormal = true;
                                formalVoting.JobID = Convert.ToInt32(informalVoting.JobID);
                                formalVoting.Status = Enums.VoteStatusTypes.Active;
                                formalVoting.Type = informalVoting.Type;
                                formalVoting.PolicingRate = informalVoting.PolicingRate;
                                formalVoting.QuorumCount = Convert.ToInt32(chainVoting.formal_voting_quorum);
                                formalVoting.EligibleUserCount = db.Votes.Count(x => x.VotingID == informalVoting.VotingID);
                                formalVoting.QuorumRatio = informalVoting.QuorumRatio;
                                formalVoting.BlockchainVotingID = chainVoting.voting_id;
                                formalVoting.DeployHash = chainVoting.deploy_hash;
                                formalVoting.StakedAgainst = 0;
                                formalVoting.StakedFor = 0;
                                db.Votings.Add(formalVoting);
                                db.SaveChanges();

                                Program.monitizer.AddConsole("Formal voting started for job #" + formalVoting.JobID);

                                //Get all votes of formal voting from chain and syncronize with central db (For doublecheck)
                                //Disabled for now because endpoints returns votes withouth knowing it belong to informal or not
                                //SyncronizeVotesFromChain(Enums.Blockchain.Casper, Convert.ToInt32(formalVoting.BlockchainVotingID), formalVoting.VotingID);

                                //Release staked reputations
                                Helpers.Request.Get(Program._settings.Service_Reputation_Url + "/UserReputationStake/ReleaseStakes?referenceProcessID=" + informalVoting.VotingID + "&reftype=" + Enums.StakeType.For);

                                //Send email notification to VAs
                                Helpers.Models.NotificationModels.SendEmailModel emailModel = new Helpers.Models.NotificationModels.SendEmailModel() { Subject = "Formal Voting Started For Job #" + informalVoting.JobID, Content = "Formal voting process started for job #" + informalVoting.JobID + "<br><br>Please submit your vote until " + formalVoting.EndDate.ToString(), TargetGroup = Enums.UserIdentityType.VotingAssociate };
                                Program.rabbitMq.Publish(Helpers.Constants.FeedNames.NotificationFeed, "email", Helpers.Serializers.Serialize(emailModel));
                            }
                        }
                    }

                    
                }

                //End formal voting in central db if formal voting ended onchain
                if (dbVotings.Count(x => x.IsFormal == true && x.EndDate < DateTime.Now) > 0)
                {
                    var chainCompletedVotings = Serializers.DeserializeJson<List<Helpers.Models.CasperServiceModels.Voting>>(Request.Get(Program._settings.Service_CasperChain_Url + "/CasperMiddleware/GetVotings?page=1&page_size=50&has_ended=true&is_formal=true"));

                    foreach (var formalVoting in dbVotings.Where(x => x.IsFormal == true && x.EndDate < DateTime.Now))
                    {
                        if (chainCompletedVotings.Count(x => x.deploy_hash == formalVoting.DeployHash) > 0)
                        {
                            //Get all votes from chain and syncronize with central db (For doublecheck)
                            SyncronizeVotesFromChain(Enums.Blockchain.Casper, Convert.ToInt32(formalVoting.BlockchainVotingID), formalVoting.VotingID);

                            using (dao_votesdb_context db = new dao_votesdb_context())
                            {
                                //Quorum isn't reached -> Set voting status to Expired
                                if (db.Votes.Count(x => x.VotingID == formalVoting.VotingID) < formalVoting.QuorumCount)
                                {
                                    var votingdb = db.Votings.Find(formalVoting.VotingID);
                                    votingdb.Status = Enums.VoteStatusTypes.Expired;
                                    db.Entry(votingdb).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                    db.SaveChanges();
                                }
                                else
                                {
                                    var votingdb = db.Votings.Find(formalVoting.VotingID);
                                    votingdb.Status = Enums.VoteStatusTypes.Completed;
                                    db.Entry(votingdb).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                    db.SaveChanges();
                                }

                                //Release staked reputations
                                Helpers.Request.Get(Program._settings.Service_Reputation_Url + "/UserReputationStake/ReleaseStakes?referenceProcessID=" + formalVoting.VotingID + "&reftype=" + Enums.StakeType.For);

                                Program.monitizer.AddConsole("Formal voting completed for job #" + formalVoting.JobID);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddConsole("Exception in timer CheckVotingStatusCasperChain. Ex: " + ex.Message);
            }

            progress_CheckVotingStatusCasperChain = false;
        }

        private static void SyncronizeVotesFromChain(Enums.Blockchain blockchain, int voting_chain_id, int votingId)
        {
            try
            {
                if (blockchain == Enums.Blockchain.Casper)
                {
                    using (dao_votesdb_context db = new dao_votesdb_context())
                    {
                        var chainVotes = Serializers.DeserializeJson<List<Helpers.Models.CasperServiceModels.Vote>>(Request.Get(Program._settings.Service_CasperChain_Url + "/CasperMiddleware/GetVotesListbyVotingId?page=1&page_size=1000&voting_id=" + voting_chain_id));

                        var dbVotes = db.Votes.Where(x => x.VotingID == votingId).ToList();

                        foreach (var item in chainVotes)
                        {
                            var voting = db.Votings.Find(item);

                            if (dbVotes.Count(x => x.DeployHash == item.deploy_hash) == 0)
                            {
                                Models.Vote vote = new Models.Vote();
                                if (Convert.ToBoolean(item.is_in_favour) == true)
                                {
                                    vote.Direction = StakeType.For;
                                    voting.StakedFor += item.amount;
                                }
                                else
                                {
                                    vote.Direction = StakeType.Against;
                                    voting.StakedAgainst += item.amount;
                                }
                                vote.DeployHash = item.deploy_hash;
                                vote.Date = Convert.ToDateTime(item.timestamp);
                                vote.UserID = 0;
                                vote.VotingID = votingId;
                                vote.WalletAddress = item.address;
                                db.Votes.Add(vote);

                                db.SaveChanges();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
            }
        }
    }
}
