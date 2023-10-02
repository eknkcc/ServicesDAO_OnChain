using DAO_DbService.Contexts;
using DAO_DbService.Controllers;
using DAO_DbService.Models;
using Helpers.Constants;
using Helpers.Models.DtoModels.MainDbDto;
using Helpers.Models.DtoModels.ReputationDbDto;
using Helpers.Models.DtoModels.VoteDbDto;
using Helpers.Models.NotificationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using static Helpers.Constants.Enums;
using DAO_DbService.Mapping;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Helpers;
using Helpers.Models.CasperServiceModels;
using Microsoft.AspNetCore.Http;
using MySqlX.XDevAPI.Common;
using System.Security.Cryptography;
using System.Threading;
using static System.Reflection.Metadata.BlobBuilder;
using Microsoft.AspNetCore.Components.Authorization;

namespace DAO_DbService
{
    public class TimedEvents
    {
        /// <summary>
        /// Auction status control timer
        /// Checks the end date of the auction as follows:
        /// If internal auction end date reached without any internal bids -> Start public auction
        /// If public auction end date reached without any public bids -> Set auction and job status to Expired
        /// </summary>
        public static System.Timers.Timer auctionStatusTimer;

        /// <summary>
        ///  Job status control timer
        ///  Checks the results of voting as follows:
        ///  If formal voting ended as AGAINST -> Set job status to Failed
        ///  If formal voting ended as FOR -> Set job status to Completed
        /// </summary>
        public static System.Timers.Timer jobStatusTimer;

        /// <summary>
        ///  Syncronizes DAO settings from blockchain middleware
        /// </summary>
        public static System.Timers.Timer daoSettingsTimer;

        /// <summary>
        ///  Starts timer controls of the application
        /// </summary>
        public static void StartTimers()
        {

            if (Program._settings.DaoBlockchain == null)
            {
                CheckAuctionStatusOffChain(null, null);
                CheckJobStatus(null, null);

                //Auction status timer
                auctionStatusTimer = new System.Timers.Timer(10000);
                auctionStatusTimer.Elapsed += CheckAuctionStatusOffChain;
                auctionStatusTimer.AutoReset = true;
                auctionStatusTimer.Enabled = true;

                //Job status timer
                //It sends request to VotinEngine so it has longer interval
                jobStatusTimer = new System.Timers.Timer(60000);
                jobStatusTimer.Elapsed += CheckJobStatus;
                jobStatusTimer.AutoReset = true;
                jobStatusTimer.Enabled = true;
            }
            else if (Program._settings.DaoBlockchain == Enums.Blockchain.Casper)
            {
                CheckAuctionStatusOffChain(null, null);
                auctionStatusTimer = new System.Timers.Timer(10000);
                auctionStatusTimer.Elapsed += CheckAuctionStatusOffChain;
                auctionStatusTimer.AutoReset = true;
                auctionStatusTimer.Enabled = true;

                SyncDaoSettingsCasperChain(null, null);
                daoSettingsTimer = new System.Timers.Timer(300000);
                daoSettingsTimer.Elapsed += SyncDaoSettingsCasperChain;
                daoSettingsTimer.AutoReset = true;
                daoSettingsTimer.Enabled = true;

                SyncJobsCasperChain(null, null);
                jobStatusTimer = new System.Timers.Timer(60000);
                jobStatusTimer.Elapsed += SyncJobsCasperChain;
                jobStatusTimer.AutoReset = true;
                jobStatusTimer.Enabled = true;

                SyncBidsCasperChain(null, null);
                jobStatusTimer = new System.Timers.Timer(300000);
                jobStatusTimer.Elapsed += SyncBidsCasperChain;
                jobStatusTimer.AutoReset = true;
                jobStatusTimer.Enabled = true;

                CheckJobStatus(null, null);
                jobStatusTimer = new System.Timers.Timer(60000);
                jobStatusTimer.Elapsed += CheckJobStatus;
                jobStatusTimer.AutoReset = true;
                jobStatusTimer.Enabled = true;
            }
        }

        private static void SyncDaoSettingsCasperChain(Object source, ElapsedEventArgs e)
        {
            try
            {
                PaginatedResponse<Setting> settings = Serializers.DeserializeJson<PaginatedResponse<Setting>>(Request.Get(Program._settings.Service_CasperChain_Url + "/CasperMiddleware/GetSettings?page=1&page_size=100"));

                List<DaoSetting> dbSettings = new List<DaoSetting>();

                using (dao_maindb_context db = new dao_maindb_context())
                {
                    dbSettings = db.DaoSettings.ToList();
                }

                if (settings != null && settings.data != null)
                {
                    foreach (var setting in settings.data)
                    {
                        if (dbSettings.Count(x => x.Key == setting.name) == 0)
                        {
                            using (dao_maindb_context db = new dao_maindb_context())
                            {
                                db.DaoSettings.Add(new DaoSetting() { Key = setting.name, Value = setting.value, LastModified = DateTime.Now });
                                db.SaveChanges();
                            }
                        }
                        else
                        {
                            var dbSetting = dbSettings.First(x => x.Key == setting.name);
                            if (dbSetting.Value != setting.value)
                            {
                                using (dao_maindb_context db = new dao_maindb_context())
                                {
                                    var sts = db.DaoSettings.Find(dbSetting.DaoSettingID);
                                    sts.Value = setting.value;
                                    db.SaveChanges();
                                }
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddConsole("Exception in timer SyncDaoSettingsCasperChain. Ex: " + ex.Message);
            }
        }

        //WAITING FOR MIDDLEWARE ENDPOINTS
        private static void SyncJobsCasperChain(Object source, ElapsedEventArgs e)
        {
            try
            {
                //Get voting data from chain and central db
                List<JobPost> dbWaitingJobs = new List<JobPost>();
                List<JobPost> dbInternalBiddingJobs = new List<JobPost>();

                using (dao_maindb_context db = new dao_maindb_context())
                {
                    dbWaitingJobs = db.JobPosts.Where(x => x.Status == Enums.JobStatusTypes.InternalAuction && x.DeployHash != null && x.BlockchainJobPostID == null).ToList();
                    dbInternalBiddingJobs = db.JobPosts.Where(x => x.Status == Enums.JobStatusTypes.InternalAuction && x.DeployHash != null && x.BlockchainJobPostID != null).ToList();
                }

                if (dbWaitingJobs.Count > 0 || dbInternalBiddingJobs.Count > 0)
                {
                    PaginatedResponse<JobOfferDetailed> chainJobs = Serializers.DeserializeJson<PaginatedResponse<JobOfferDetailed>>(Request.Get(Program._settings.Service_CasperChain_Url + "/CasperMiddleware/GetJobOffers?page=1&page_size=100&order_direction=DESC"));

                    //Sync blockchainjob ids
                    foreach (var item in dbWaitingJobs)
                    {
                        if (chainJobs.data.Count(x => x.deploy_hash == item.DeployHash) > 0)
                        {
                            var chainJob = chainJobs.data.First(x => x.deploy_hash == item.DeployHash);

                            using (dao_maindb_context db = new dao_maindb_context())
                            {
                                var job = db.JobPosts.Find(item.JobID);
                                job.BlockchainJobPostID = chainJob.job_offer_id;
                                db.SaveChanges();

                                //Set auction end dates
                                int InternalAuctionTime = Convert.ToInt32(db.DaoSettings.First(x => x.Key == "InternalAuctionTime").Value);
                                int PublicAuctionTime = Convert.ToInt32(db.DaoSettings.First(x => x.Key == "PublicAuctionTime").Value);

                                DateTime internalAuctionEndDate = DateTime.Now.AddMilliseconds(InternalAuctionTime);
                                DateTime publicAuctionEndDate = DateTime.Now.AddMilliseconds(InternalAuctionTime + PublicAuctionTime);

                                Auction AuctionModel = new Auction()
                                {
                                    JobID = job.JobID,
                                    JobPosterUserID = job.UserID,
                                    CreateDate = DateTime.Now,
                                    Status = AuctionStatusTypes.InternalBidding,
                                    InternalAuctionEndDate = internalAuctionEndDate,
                                    PublicAuctionEndDate = publicAuctionEndDate,
                                    BlockchainAuctionID = chainJob.job_offer_id
                                };

                                db.Auctions.Add(AuctionModel);
                                db.SaveChanges();
                            }
                        }
                    }

                    //Sync auction process
                    foreach (var item in dbInternalBiddingJobs)
                    {
                        if (chainJobs.data.Count(x => x.deploy_hash == item.DeployHash) > 0)
                        {
                            var chainJob = chainJobs.data.First(x => x.deploy_hash == item.DeployHash);

                            //Public auction started on blockchain
                            if (chainJob.auction_type_id == 2)
                            {
                                using (dao_maindb_context db = new dao_maindb_context())
                                {
                                    var job = db.JobPosts.Find(item.JobID);
                                    job.Status = JobStatusTypes.PublicAuction;
                                    db.SaveChanges();

                                    var auction = db.Auctions.First(x => x.JobID == item.JobID);
                                    auction.Status = AuctionStatusTypes.PublicBidding;
                                    db.SaveChanges();

                                    //Send notification email to job poster
                                    var jobPoster = db.Users.Find(auction.JobPosterUserID);

                                    //Set email title and content
                                    string emailTitle = "Your job is in public bidding phase.";
                                    string emailContent = "Greetings, " + jobPoster.NameSurname.Split(' ')[0] + ", <br><br> Internal auction phase is finished for your job. There are no winning bids selected. <br><br> Your job will be in public bidding phase until " + Convert.ToDateTime(auction.PublicAuctionEndDate).ToString("MM.dd.yyyy HH:mm");
                                    //Send email
                                    SendEmailModel emailModel = new SendEmailModel() { Subject = emailTitle, Content = emailContent, To = new List<string> { jobPoster.Email } };
                                    Program.rabbitMq.Publish(Helpers.Constants.FeedNames.NotificationFeed, "email", Helpers.Serializers.Serialize(emailModel));
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Program.monitizer.AddConsole("Exception in timer SyncJobsCasperChain. Ex: " + ex.Message);
            }
        }

        private static void SyncBidsCasperChain(Object source, ElapsedEventArgs e)
        {
            try
            {
                //Get voting data from chain and central db
                List<JobPost> dbActiveJobs = new List<JobPost>();

                using (dao_maindb_context db = new dao_maindb_context())
                {
                    dbActiveJobs = db.JobPosts.Where(x => (x.Status == Enums.JobStatusTypes.InternalAuction || x.Status == Enums.JobStatusTypes.PublicAuction) && x.DeployHash != null && x.BlockchainJobPostID != null).ToList();
                }

                //Sync auction process
                foreach (var item in dbActiveJobs)
                {
                    using (dao_maindb_context db = new dao_maindb_context())
                    {
                        var auction = db.Auctions.First(x => x.JobID == item.JobID);
                        var dbWaitingBids = db.AuctionBids.Where(x => x.AuctionID == auction.AuctionID && (x.BlockchainBidID == null || x.BlockchainBidID == 0)).ToList();

                        if (dbWaitingBids.Count > 0)
                        {
                            PaginatedResponse<Bid> chainBids = Serializers.DeserializeJson<PaginatedResponse<Bid>>(Request.Get(Program._settings.Service_CasperChain_Url + "/CasperMiddleware/GetBids?jobid=" + item.BlockchainJobPostID + "&page=1&page_size=100&order_direction=DESC"));

                            foreach (var chainbid in chainBids.data.Where(x => x.job_offer_id == item.BlockchainJobPostID))
                            {
                                if (dbWaitingBids.Count(x => x.DeployHash == chainbid.deploy_hash) > 0)
                                {
                                    var bid = db.AuctionBids.First(x => x.DeployHash == chainbid.deploy_hash);
                                    bid.BlockchainBidID = chainbid.bid_id;
                                    db.SaveChanges();
                                }
                            }
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                Program.monitizer.AddConsole("Exception in timer SyncBidsCasperChain. Ex: " + ex.Message);
            }
        }


        /// <summary>
        ///  Checks auction status 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private static void CheckAuctionStatusOffChain(Object source, ElapsedEventArgs e)
        {
            try
            {
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    //Check if auction internal bidding ended -> Start public bidding
                    var publicAuctions = db.Auctions.Where(x => x.Status == Enums.AuctionStatusTypes.InternalBidding && x.InternalAuctionEndDate < DateTime.Now && x.WinnerAuctionBidID == null).ToList();

                    foreach (var auction in publicAuctions)
                    {
                        //string releaseResult = Helpers.Request.Get(Program._settings.Service_Reputation_Url + "/UserReputationStake/ReleaseStakes?referenceProcessID=" + auction.AuctionID + "&reftype=" + Enums.StakeType.Bid);

                        auction.Status = Enums.AuctionStatusTypes.PublicBidding;
                        db.Entry(auction).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        db.SaveChanges();

                        var job = db.JobPosts.Find(auction.JobID);
                        job.Status = Enums.JobStatusTypes.PublicAuction;
                        db.SaveChanges();

                        //Send notification email to job poster
                        var jobPoster = db.Users.Find(auction.JobPosterUserID);

                        //Set email title and content
                        string emailTitle = "Your job is in public bidding phase.";
                        string emailContent = "Greetings, " + jobPoster.NameSurname.Split(' ')[0] + ", <br><br> Internal auction phase is finished for your job. There are no winning bids selected. <br><br> Your job will be in public bidding phase until " + Convert.ToDateTime(auction.PublicAuctionEndDate).ToString("MM.dd.yyyy HH:mm");
                        //Send email
                        SendEmailModel emailModel = new SendEmailModel() { Subject = emailTitle, Content = emailContent, To = new List<string> { jobPoster.Email } };
                        Program.rabbitMq.Publish(Helpers.Constants.FeedNames.NotificationFeed, "email", Helpers.Serializers.Serialize(emailModel));
                    }


                    //Check if auction public bidding ended without any winner -> Set auction status to Expired
                    var expiredAuctions = db.Auctions.Where(x => x.Status == Enums.AuctionStatusTypes.PublicBidding && x.PublicAuctionEndDate < DateTime.Now).ToList();

                    foreach (var auction in expiredAuctions)
                    {
                        //No winners selected. Auction expired. -> Set auction and job status to Expired
                        if (auction.WinnerAuctionBidID == null)
                        {
                            string releaseResult = Helpers.Request.Get(Program._settings.Service_Reputation_Url + "/UserReputationStake/ReleaseStakes?referenceProcessID=" + auction.AuctionID + "&reftype=" + Enums.StakeType.Bid);

                            auction.Status = Enums.AuctionStatusTypes.Expired;
                            db.Entry(auction).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            db.SaveChanges();

                            var job = db.JobPosts.Find(auction.JobID);
                            job.Status = Enums.JobStatusTypes.Expired;
                            db.Entry(auction).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                            db.SaveChanges();

                            //Send notification email to job poster
                            var jobPoster = db.Users.Find(auction.JobPosterUserID);

                            //Set email title and content
                            string emailTitle = "Your job is expired.";
                            string emailContent = "Greetings, " + jobPoster.NameSurname.Split(' ')[0] + ", <br><br> Public auction phase is finished for your job. There are no winning bids selected. <br><br> Your job status is now expired and won't be listed in the active auctions anymore.";
                            //Send email
                            SendEmailModel emailModel = new SendEmailModel() { Subject = emailTitle, Content = emailContent, To = new List<string> { jobPoster.Email } };
                            Program.rabbitMq.Publish(Helpers.Constants.FeedNames.NotificationFeed, "email", Helpers.Serializers.Serialize(emailModel));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddConsole("Exception in timer CheckAuctionStatus. Ex: " + ex.Message);
            }
        }

        /// <summary>
        ///  Checks job status 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private static void CheckJobStatus(Object source, ElapsedEventArgs e)
        {
            //Check completed informal votings and update job status accordingly
            CheckCompletedInformalVotings();

            //Check completed formal votings and update job status accordingly
            bool onchain = true;
            if (Program._settings.DaoBlockchain == null)
            {
                onchain = false;
            }
            CheckCompletedFormalVotings(onchain);

            //Check job doer does not provided job evidence and started informal voting within expected time frame
            CheckJobFail();
        }

        /// <summary>
        ///  Checks completed informal votings and updates job status accordingly
        /// </summary>
        private static void CheckCompletedInformalVotings()
        {
            try
            {
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    //Check completed informal votings and update job status accordingly
                    var informalVotingJobs = db.JobPosts.Where(x => x.Status == Enums.JobStatusTypes.InformalVoting).ToList();

                    //Get completed informal votes from ApiGateway
                    string informalVotingsCompletedJson = Helpers.Request.Post(Program._settings.Voting_Engine_Url + "/Voting/GetCompletedVotingsByJobIds", Helpers.Serializers.SerializeJson(informalVotingJobs.Select(x => x.JobID)));

                    //Parse result
                    List<VotingDto> completedInformalModel = Helpers.Serializers.DeserializeJson<List<VotingDto>>(informalVotingsCompletedJson).Where(x => x.IsFormal == false).ToList();

                    foreach (var voting in completedInformalModel)
                    {
                        try
                        {
                            //Informal voting finished without quorum -> Set job status to Expired
                            if (voting.Status == Enums.VoteStatusTypes.Expired)
                            {
                                var job = db.JobPosts.Find(voting.JobID);
                                job.Status = Enums.JobStatusTypes.Expired;
                                db.SaveChanges();

                                //Send notification email to job poster and job doer
                                var jobPoster = db.Users.Find(job.UserID);
                                var jobDoer = db.Users.Find(job.JobDoerUserID);

                                //Set email title and content
                                string emailTitle = "Informal voting finished without quorum for job #" + job.JobID;
                                string emailContent = "Greetings, {name}, <br><br> Informal voting process for your job finished without the quorum. <br><br> Your job status is now expired. Please contact with system admin.";

                                //Send email to job poster
                                SendNotificationEmail(emailTitle, emailContent.Replace("{name}", jobPoster.NameSurname.Split(' ')[0]), jobPoster.Email);
                                //Send email to job doer
                                SendNotificationEmail(emailTitle, emailContent.Replace("{name}", jobDoer.NameSurname.Split(' ')[0]), jobDoer.Email);
                            }
                            //Informal voting completed -> Set job status according to vote result
                            else
                            {
                                var job = db.JobPosts.Find(voting.JobID);
                                job.Status = Enums.JobStatusTypes.FormalVoting;
                                db.SaveChanges();

                                //Send notification email to job poster and job doer
                                var jobPoster = db.Users.Find(job.UserID);
                                var jobDoer = db.Users.Find(job.JobDoerUserID);

                                //Set email title and content
                                string emailTitle = "Formal voting started for your job #" + job.JobID;
                                string emailContent = "Greetings, {name}, <br><br> Informal voting process for your job completed successfully. <br><br> Your job is now on formal voting.";

                                //Send email to job poster
                                SendNotificationEmail(emailTitle, emailContent.Replace("{name}", jobPoster.NameSurname.Split(' ')[0]), jobPoster.Email);
                                //Send email to job doer
                                SendNotificationEmail(emailTitle, emailContent.Replace("{name}", jobDoer.NameSurname.Split(' ')[0]), jobDoer.Email);
                            }
                        }
                        catch (Exception ex)
                        {
                            Program.monitizer.AddConsole("Exception in timer CheckCompletedInformalVotings. Ex: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddConsole("Exception in timer CheckCompletedInformalVotings. Ex: " + ex.Message);
            }
        }

        /// <summary>
        ///  Checks completed formal votings and updates job status accordingly
        /// </summary>
        private static void CheckCompletedFormalVotings(bool onchain = false)
        {
            try
            {
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    //Check completed formal votings and update job status accordingly
                    var formalVotingJobs = db.JobPosts.Where(x => x.Status == Enums.JobStatusTypes.FormalVoting).ToList();

                    //Get completed formal votes from ApiGateway
                    string formalVotingsCompletedJson = Helpers.Request.Post(Program._settings.Voting_Engine_Url + "/Voting/GetCompletedVotingsByJobIds", Helpers.Serializers.SerializeJson(formalVotingJobs.Select(x => x.JobID)));

                    //Parse result
                    List<VotingDto> completedFormalModel = Helpers.Serializers.DeserializeJson<List<VotingDto>>(formalVotingsCompletedJson).Where(x => x.IsFormal == true).ToList();

                    foreach (var voting in completedFormalModel)
                    {
                        try
                        {
                            //Formal voting finished without quorum -> Set job status to Expired
                            if (voting.Status == Enums.VoteStatusTypes.Expired)
                            {
                                var job = db.JobPosts.Find(voting.JobID);
                                job.Status = Enums.JobStatusTypes.Expired;
                                db.SaveChanges();
                            }
                            //Formal voting completed -> Set job status according to vote result
                            else if (voting.Status == Enums.VoteStatusTypes.Completed)
                            {
                                var job = db.JobPosts.Find(voting.JobID);

                                //Voting result AGAINST
                                if (voting.StakedFor < voting.StakedAgainst)
                                {
                                    job.Status = Enums.JobStatusTypes.Failed;
                                    db.SaveChanges();

                                    if (voting.Type == VoteTypes.JobCompletion)
                                    {
                                        //Send notification email to job poster and job doer
                                        var jobPoster = db.Users.Find(job.UserID);
                                        var jobDoer = db.Users.Find(job.JobDoerUserID);

                                        //Set email title and content
                                        string emailTitle = "Formal voting finished AGAINST for job #" + job.JobID;
                                        string emailContent = "Greetings, {name}, <br><br> We are sorry to give you the bad news. <br><br> Your job failed to pass formal voting. Job amount will be refunded to job poster.";

                                        //Send email to job poster
                                        SendNotificationEmail(emailTitle, emailContent.Replace("{name}", jobPoster.NameSurname.Split(' ')[0]), jobPoster.Email);
                                        //Send email to job doer
                                        SendNotificationEmail(emailTitle, emailContent.Replace("{name}", jobDoer.NameSurname.Split(' ')[0]), jobDoer.Email);
                                    }

                                    continue;
                                }

                                //Voting result FOR
                                job.Status = Enums.JobStatusTypes.Completed;
                                db.SaveChanges();

                                //if (onchain) continue;

                                //Job completion formal vote passed
                                if (voting.Type == VoteTypes.JobCompletion)
                                {
                                    var auction = db.Auctions.First(x => x.JobID == voting.JobID);
                                    var auctionWinnerBid = db.AuctionBids.First(x => x.AuctionBidID == auction.WinnerAuctionBidID);
                                    var user = db.Users.Find(auctionWinnerBid.UserID);

                                    bool vaOnboarded = false;

                                    //Get last DAO setting and check if new user should be onboarded as VA automatically
                                    if (db.PlatformSettings.OrderByDescending(x => x.PlatformSettingID).First().VAOnboardingSimpleVote == false)
                                    {
                                        //If job doer is Associate, change the user type to  VA
                                        if (user.UserType == Enums.UserIdentityType.Associate.ToString() && auctionWinnerBid.VaOnboarding == true)
                                        {
                                            user.DateBecameVA = DateTime.Now;
                                            user.UserType = Enums.UserIdentityType.VotingAssociate.ToString();
                                            db.SaveChanges();
                                            vaOnboarded = true;
                                        }
                                    }

                                    if (vaOnboarded || user.UserType != Enums.UserIdentityType.Associate.ToString())
                                    {
                                        CompleteJobVAOffChain(voting, job, auctionWinnerBid);
                                    }
                                    else
                                    {
                                        CompleteJobExternalOffChain(voting, job, auctionWinnerBid);
                                    }

                                    //Send notification email to job poster and job doer
                                    var jobPoster = db.Users.Find(job.UserID);
                                    var jobDoer = db.Users.Find(job.JobDoerUserID);

                                    //Set email title and content
                                    string emailTitle = "Formal voting finished successfully for job #" + job.JobID;
                                    string emailContent = "Greetings, {name}, <br><br> Congratulations, your job passed the formal voting and job is completed successfully. <br><br> Payment for the job will be visible on the 'Payment History' for job doer.";

                                    //Send email to job poster
                                    SendNotificationEmail(emailTitle, emailContent.Replace("{name}", jobPoster.NameSurname.Split(' ')[0]), jobPoster.Email);
                                    //Send email to job doer
                                    SendNotificationEmail(emailTitle, emailContent.Replace("{name}", jobDoer.NameSurname.Split(' ')[0]), jobDoer.Email);

                                    Program.monitizer.AddApplicationLog(LogTypes.ApplicationLog, "Job completed with vote #" + voting.VotingID);
                                }
                                //VA onboarding formal vote passed
                                else if (voting.Type == VoteTypes.VAOnboarding)
                                {
                                    string username = job.Title.Split("'")[1].Split("'")[0].Trim();
                                    var user = db.Users.First(x => x.UserName == username);
                                    user.UserType = UserIdentityType.VotingAssociate.ToString();
                                    user.DateBecameVA = DateTime.Now;
                                    db.SaveChanges();

                                    Program.monitizer.AddApplicationLog(LogTypes.ApplicationLog, "New user became VA with vote #" + voting.VotingID);
                                }
                                //Governance formal vote passed
                                else if (voting.Type == VoteTypes.Governance)
                                {
                                    string key = job.Title.Split("'")[1].Split("'")[0].Trim();
                                    string value = job.JobDescription.Split("New value:")[1].Trim();

                                    if (db.DaoSettings.Count(x => x.Key == key) > 0)
                                    {
                                        DaoSetting sts = db.DaoSettings.First(x => x.Key == key);
                                        sts.Value = value;
                                        sts.LastModified = DateTime.Now;
                                        db.SaveChanges();

                                        Program.monitizer.AddApplicationLog(LogTypes.ApplicationLog, "Dao settings changed with governance vote #" + voting.VotingID);
                                    }
                                    else
                                    {
                                        db.DaoSettings.Add(new DaoSetting() { Key = key, Value = value, LastModified = DateTime.Now });
                                        db.SaveChanges();
                                    }
                                }
                                //KYC formal vote passed
                                else if (voting.Type == VoteTypes.KYC)
                                {
                                    string username = job.Title.Split("'")[1].Split("'")[0].Trim();
                                    var user = db.Users.First(x => x.UserName == username);
                                    user.KYCStatus = true;
                                    db.SaveChanges();

                                    Program.monitizer.AddApplicationLog(LogTypes.ApplicationLog, "New user KYC completed with vote #" + voting.VotingID);
                                }
                                //Reputation formal vote passed
                                else if (voting.Type == VoteTypes.Reputation)
                                {

                                }
                                //Slashing formal vote passed
                                else if (voting.Type == VoteTypes.Slashing)
                                {

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Program.monitizer.AddConsole("Exception in timer CheckCompletedFormalVotings. Ex: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddConsole("Exception in timer CheckCompletedFormalVotings. Ex: " + ex.Message);
            }
        }

        private static void CompleteJobVAOffChain(VotingDto voting, JobPost job, AuctionBid auctionWinnerBid)
        {
            try
            {
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    DaoSettingController contr = new DaoSettingController();

                    //Get reputation stakes from reputation service
                    var reputationsJson = Helpers.Request.Get(Program._settings.Service_Reputation_Url + "/UserReputationStake/GetByProcessId?referenceProcessID=" + voting.VotingID + "&reftype=" + StakeType.For);
                    var reputations = Helpers.Serializers.DeserializeJson<List<UserReputationStakeDto>>(reputationsJson);

                    //Get reputations of voters who
                    var participatedUsers = reputations.Where(x => x.Type == Enums.StakeType.For || x.Type == Enums.StakeType.Against).ToList();
                    var allVAs = db.Users.Where(x => x.UserType == Enums.UserIdentityType.VotingAssociate.ToString()).Select(x => x.UserId);

                    //Add job doer to list
                    participatedUsers.Add(new UserReputationStakeDto() { UserID = job.JobDoerUserID });
                    var reputationsTotalJson = "";
                    var reputationsTotal = new List<UserReputationHistoryDto>();

                    List<int> userIds = new List<int>();
                    //Create Payment History model for dao members who participated into voting
                    bool distributeWithoutVote = Convert.ToBoolean(db.DaoSettings.OrderByDescending(x => x.DaoSettingID).First(x => x.Key == "DistributePaymentToNonVoters").Value);
                    if (distributeWithoutVote)
                    {
                        userIds = allVAs.ToList();
                        reputationsTotalJson = Helpers.Request.Post(Program._settings.Service_Reputation_Url + "/UserReputationHistory/GetLastReputationByUserIds", Helpers.Serializers.SerializeJson(allVAs.ToList()));
                        reputationsTotal = Helpers.Serializers.DeserializeJson<List<UserReputationHistoryDto>>(reputationsTotalJson);
                    }
                    else
                    {
                        userIds = participatedUsers.GroupBy(x => x.UserID).Select(x => x.Key).ToList();
                        reputationsTotalJson = Helpers.Request.Post(Program._settings.Service_Reputation_Url + "/UserReputationHistory/GetLastReputationByUserIds", Helpers.Serializers.SerializeJson(participatedUsers.Select(x => x.UserID)));
                        reputationsTotal = Helpers.Serializers.DeserializeJson<List<UserReputationHistoryDto>>(reputationsTotalJson);
                    }

                    double remainingJobAmount = auctionWinnerBid.Price;

                    // NOT AVAILABLE IN THE ONCHAIN VERSION
                    //Create governance payment
                    //
                    //if (lastSettings.GovernancePaymentRatio != null && lastSettings.GovernancePaymentRatio > 0)
                    //{
                    //    PaymentHistory paymentGovernance = new PaymentHistory
                    //    {
                    //        JobID = job.JobID,
                    //        Amount = remainingJobAmount * Convert.ToDouble(lastSettings.GovernancePaymentRatio),
                    //        CreateDate = DateTime.Now,
                    //        IBAN = "",
                    //        UserID = 0,
                    //        WalletAddress = lastSettings.GovernanceWallet,
                    //        Explanation = "Governance payment"
                    //    };

                    //    remainingJobAmount = remainingJobAmount - (remainingJobAmount * Convert.ToDouble(lastSettings.GovernancePaymentRatio));

                    //    db.PaymentHistories.Add(paymentGovernance);
                    //    db.SaveChanges();
                    //}

                    foreach (var userId in userIds)
                    {
                        if (reputationsTotal.Count(x => x.UserID == userId) == 0) continue;

                        double usersRepPerc = reputationsTotal.FirstOrDefault(x => x.UserID == userId).LastTotal / reputationsTotal.Sum(x => x.LastTotal);
                        double memberPayment = remainingJobAmount * usersRepPerc;

                        var daouser = db.Users.Find(userId);

                        PaymentHistory paymentDaoMember = new PaymentHistory
                        {
                            JobID = job.JobID,
                            Amount = memberPayment,
                            CreateDate = DateTime.Now,
                            IBAN = daouser.IBAN,
                            UserID = daouser.UserId,
                            WalletAddress = daouser.WalletAddress,
                            Explanation = userId == auctionWinnerBid.UserID ? "User received payment for job completion." : "User received payment for DAO policing."
                        };

                        db.PaymentHistories.Add(paymentDaoMember);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddConsole("Exception in timer CompleteJobVAOffChain. Ex: " + ex.Message);
            }

        }

        private static void CompleteJobExternalOffChain(VotingDto voting, JobPost job, AuctionBid auctionWinnerBid)
        {
            try
            {
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    //Get reputation stakes from reputation service
                    var reputationsJson = Helpers.Request.Get(Program._settings.Service_Reputation_Url + "/UserReputationStake/GetByProcessId?referenceProcessID=" + voting.VotingID + "&reftype=" + StakeType.For);
                    var reputations = Helpers.Serializers.DeserializeJson<List<UserReputationStakeDto>>(reputationsJson);

                    //Get reputations of voters
                    var participatedUsers = reputations.Where(x => x.Type == Enums.StakeType.For || x.Type == Enums.StakeType.Against).ToList();
                    //Add job doer to list
                    var reputationsTotalJson = Helpers.Request.Post(Program._settings.Service_Reputation_Url + "/UserReputationHistory/GetLastReputationByUserIds", Helpers.Serializers.SerializeJson(participatedUsers.Select(x => x.UserID)));
                    var reputationsTotal = Helpers.Serializers.DeserializeJson<List<UserReputationHistoryDto>>(reputationsTotalJson);

                    DaoSettingController contr = new DaoSettingController();

                    List<int> userIds = new List<int>();
                    //Create Payment History model for dao members who participated into voting
                    bool distributeWithoutVote = Convert.ToBoolean(db.DaoSettings.OrderByDescending(x => x.DaoSettingID).First(x => x.Key == "DistributePaymentToNonVoters").Value);
                    if (distributeWithoutVote)
                    {
                        userIds = db.Users.Where(x => x.UserType == Enums.UserIdentityType.VotingAssociate.ToString()).Select(x => x.UserId).ToList();
                    }
                    else
                    {
                        userIds = participatedUsers.GroupBy(x => x.UserID).Select(x => x.Key).ToList();
                    }

                    double remainingJobAmount = auctionWinnerBid.Price;

                    // NOT AVAILABLE IN THE ONCHAIN VERSION
                    //Create governance payment
                    //double remainingJobAmount = auctionWinnerBid.Price;
                    //if (lastSettings.GovernancePaymentRatio != null && lastSettings.GovernancePaymentRatio > 0)
                    //{
                    //    PaymentHistory paymentGovernance = new PaymentHistory
                    //    {
                    //        JobID = job.JobID,
                    //        Amount = remainingJobAmount * Convert.ToDouble(lastSettings.GovernancePaymentRatio),
                    //        CreateDate = DateTime.Now,
                    //        IBAN = "",
                    //        UserID = 0,
                    //        WalletAddress = lastSettings.GovernanceWallet,
                    //        Explanation = "Governance payment"
                    //    };

                    //    remainingJobAmount = remainingJobAmount - (remainingJobAmount * Convert.ToDouble(lastSettings.GovernancePaymentRatio));

                    //    db.PaymentHistories.Add(paymentGovernance);
                    //    db.SaveChanges();
                    //}

                    //Get default policing rate
                    double defaultPolicingRate = Convert.ToDouble(db.DaoSettings.OrderByDescending(x => x.DaoSettingID).First(x => x.Key == "DefaultPolicingRate").Value) / Convert.ToDouble(1000);

                    //Create Payment History model for dao members who participated into voting
                    foreach (var userId in userIds)
                    {
                        if (reputationsTotal.Count(x => x.UserID == userId) == 0) continue;

                        double usersRepPerc = reputationsTotal.FirstOrDefault(x => x.UserID == userId).LastTotal / reputationsTotal.Sum(x => x.LastTotal);

                        double memberPayment = remainingJobAmount * usersRepPerc * Convert.ToDouble(defaultPolicingRate);

                        var daouser = db.Users.Find(userId);

                        PaymentHistory paymentDaoMember = new PaymentHistory
                        {
                            JobID = job.JobID,
                            Amount = memberPayment,
                            CreateDate = DateTime.Now,
                            IBAN = daouser.IBAN,
                            UserID = daouser.UserId,
                            WalletAddress = daouser.WalletAddress,
                            Explanation = "User received payment for DAO policing."
                        };

                        db.PaymentHistories.Add(paymentDaoMember);
                        db.SaveChanges();
                    }


                    var jobdoeruser = db.Users.Find(job.JobDoerUserID);

                    //Create payment of the job doer
                    PaymentHistory paymentExternalMember = new PaymentHistory
                    {
                        JobID = job.JobID,
                        Amount = remainingJobAmount - (remainingJobAmount * defaultPolicingRate),
                        CreateDate = DateTime.Now,
                        IBAN = jobdoeruser.IBAN,
                        UserID = jobdoeruser.UserId,
                        WalletAddress = jobdoeruser.WalletAddress,
                        Explanation = "User received payment for job completion."
                    };

                    db.PaymentHistories.Add(paymentExternalMember);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddConsole("Exception in timer CompleteJobExternalOffChain. Ex: " + ex.Message);
            }

        }

        /// <summary>
        ///  Checks if job doer does not provided job evidence and started informal voting within expected time frame
        /// </summary>
        private static void CheckJobFail()
        {
            try
            {
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    //Get current active jobs
                    var activeJobs = db.JobPosts.Where(x => x.Status == Enums.JobStatusTypes.AuctionCompleted).ToList();

                    foreach (var job in activeJobs)
                    {
                        try
                        {
                            var auction = db.Auctions.Where(x => x.JobID == job.JobID).OrderByDescending(x => x.CreateDate).First();
                            var winnerBid = db.AuctionBids.First(x => x.AuctionBidID == auction.WinnerAuctionBidID);

                            int expectedDays = Convert.ToInt32(winnerBid.Time);

                            if (expectedDays > 0)
                            {
                                //Job doer didn't post valid evidence and started informal voting within expected time range -> Set job status to Failed
                                if (Convert.ToDateTime(auction.PublicAuctionEndDate).AddDays(expectedDays) < DateTime.Now)
                                {
                                    string releaseResult = Helpers.Request.Get(Program._settings.Service_Reputation_Url + "/UserReputationStake/ReleaseStakes?referenceProcessID=" + job.JobID + "&reftype=" + Enums.StakeType.Mint);

                                    job.Status = Enums.JobStatusTypes.Failed;
                                    db.SaveChanges();


                                    //Send notification email to job poster and job doer
                                    var jobPoster = db.Users.Find(job.UserID);
                                    var jobDoer = db.Users.Find(job.JobDoerUserID);

                                    //Set email title and content
                                    string emailTitle = "Job failed #" + job.JobID;
                                    string emailContent = "Greetings, {name}, <br><br> We are sorry to give you the bad news. <br><br> Your job is failed because job doer didn't post a valid evidence of work and started informal voting process within the expected time frame.";

                                    //Send email to job poster
                                    SendNotificationEmail(emailTitle, emailContent.Replace("{name}", jobPoster.NameSurname.Split(' ')[0]), jobPoster.Email);
                                    //Send email to job doer
                                    SendNotificationEmail(emailTitle, emailContent.Replace("{name}", jobDoer.NameSurname.Split(' ')[0]), jobDoer.Email);

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Program.monitizer.AddConsole("Exception in timer CheckJobFail. Ex: " + ex.Message);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddConsole("Exception in timer CheckJobFail. Ex: " + ex.Message);
            }
        }

        /// <summary>
        ///  Helper function for sending notification emails
        /// </summary>
        public static void SendNotificationEmail(string title, string content, string email)
        {
            SendEmailModel emailModel = new SendEmailModel() { Subject = title, Content = content, To = new List<string> { email } };
            Program.rabbitMq.Publish(Helpers.Constants.FeedNames.NotificationFeed, "email", Helpers.Serializers.Serialize(emailModel));
        }
    }
}
