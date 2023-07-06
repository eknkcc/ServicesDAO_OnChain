using DAO_WebPortal.Models;
using DAO_WebPortal.Providers;
using DAO_WebPortal.Resources;
using Helpers.Constants;
using Helpers.Models.DtoModels.MainDbDto;
using Helpers.Models.DtoModels.ReputationDbDto;
using Helpers.Models.DtoModels.VoteDbDto;
using Helpers.Models.SharedModels;
using Helpers.Models.WebsiteViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Helpers.Constants.Enums;
using Newtonsoft.Json;
using PagedList.Core;
using Helpers.Models.NotificationModels;
using Helpers.Models.KYCModels;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System.Threading;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json.Linq;
using Helpers.Models.CasperServiceModels;
using MySqlX.XDevAPI;

namespace DAO_WebPortal.Controllers
{
    [AuthorizeUser]
    public class HomeController : Controller
    {

        /// <summary>
        /// Dashboard Page
        /// </summary>
        /// <returns></returns>
        [Route("Home")]
        [Route("Dashboard")]
        public IActionResult Index()
        {
            ViewBag.Title = "Dashboard";


            try
            {
                //Get user type from session
                string userType = HttpContext.Session.GetString("UserType");

                //User type control for admin
                if (userType == Helpers.Constants.Enums.UserIdentityType.Admin.ToString())
                {
                    DashBoardViewModelAdmin dashModel = new DashBoardViewModelAdmin();
                    //Get dashboard data from ApiGateway
                    string dashboardJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Website/GetDashBoardAdmin?userid=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token"));
                    //Parse response
                    dashModel = Helpers.Serializers.DeserializeJson<DashBoardViewModelAdmin>(dashboardJson);
                    //Get coin price list
                    dashModel.CoinPrices = GetCandleSticks("cspr_usdt").Select(x => x.Close).ToList();

                    return View("Index_Admin", dashModel);
                }
                //User type control for associate
                if (userType == Helpers.Constants.Enums.UserIdentityType.Associate.ToString())
                {
                    DashBoardViewModelVA dashModel = new DashBoardViewModelVA();
                    //Get dashboard data from ApiGateway
                    string dashboardJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Website/GetDashBoardAssociate?userid=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token"));
                    //Parse response
                    dashModel = Helpers.Serializers.DeserializeJson<DashBoardViewModelVA>(dashboardJson);
                    //Get coin price list
                    dashModel.CoinPrices = GetCandleSticks("cspr_usdt").Select(x => x.Close).ToList();

                    return View("Index_Associate", dashModel);
                }
                //User type control for voting associate
                if (userType == Helpers.Constants.Enums.UserIdentityType.VotingAssociate.ToString())
                {
                    DashBoardViewModelVA dashModel = new DashBoardViewModelVA();
                    //Get dashboard data from ApiGateway
                    string dashboardJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Website/GetDashBoardVA?userid=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token"));
                    //Parse response
                    dashModel = Helpers.Serializers.DeserializeJson<DashBoardViewModelVA>(dashboardJson);
                    //Get coin price list
                    dashModel.CoinPrices = GetCandleSticks("cspr_usdt").Select(x => x.Close).ToList();

                    return View("Index_VotingAssociate", dashModel);
                }

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return View("../Public/Error.cshtml");
        }

        #region Job Post

        /// <summary>
        /// User's Job Page
        /// </summary>
        /// <returns></returns>
        [Route("My-Jobs")]
        public IActionResult My_Jobs(JobStatusTypes? status, string query)
        {
            ViewBag.Title = "My Jobs";

            MyJobsViewModel myJobsModel = new MyJobsViewModel();

            try
            {
                //Get jobs data from ApiGateway
                string jobsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Website/GetUserJobs?status=" + status + "&userid=" + HttpContext.Session.GetInt32("UserID") + "&query=" + query, HttpContext.Session.GetString("Token"));
                //Parse response
                myJobsModel = Helpers.Serializers.DeserializeJson<MyJobsViewModel>(jobsJson);
                myJobsModel.doerJobs = myJobsModel.doerJobs.OrderByDescending(x => x.CreateDate).ToList();
                myJobsModel.ownedJobs = myJobsModel.ownedJobs.OrderByDescending(x => x.CreateDate).ToList();
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return View(myJobsModel);
        }

        /// <summary>
        /// All Jobs Page
        /// </summary>
        /// <returns></returns>
        [Route("All-Jobs")]
        [Route("Home/All-Jobs")]
        public IActionResult All_Jobs(JobStatusTypes? status, string query, int page = 1, int pageCount = 50)
        {
            ViewBag.Title = "All Jobs";

            IPagedList<JobPostViewModel> pagedModel = new PagedList<JobPostViewModel>(null, 1, 1);

            try
            {
                //Get jobs data from ApiGateway
                string jobsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Website/GetAllJobs?status=" + status + "&userid=" + HttpContext.Session.GetInt32("UserID") + "&query=" + query + "&page=" + page + "&pageCount=" + pageCount, HttpContext.Session.GetString("Token"));
                //Parse response
                var jobsListPaged = Helpers.Serializers.DeserializeJson<PaginationEntity<JobPostViewModel>>(jobsJson);

                pagedModel = new StaticPagedList<JobPostViewModel>(
                    jobsListPaged.Items,
                    jobsListPaged.MetaData.PageNumber,
                    jobsListPaged.MetaData.PageSize,
                    jobsListPaged.MetaData.TotalItemCount
                    );

                //var result = JsonConvert.DeserializeObject<PagedList<JobPostViewModel>>(jobsJson);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return View(pagedModel);

        }

        /// <summary>
        /// New Job Page
        /// </summary>
        /// <returns></returns>
        [Route("New-Job")]
        public IActionResult New_Job()
        {
            ViewBag.Title = "Post A New Job";

            var candles = GetCandleSticks("cspr_usdt");
            if (candles.Count > 0)
            {
                var closeList = candles.Select(x => x.Close).ToList();
                HttpContext.Session.SetString("cspr_price", closeList[closeList.Count - 1].ToString());
            }
            
            // Check if user signed with traditional db account. If not opens signin popup.
            if (CheckDbSignIn() == "Unauthorized")
            {
                TempData["DbSignRequired"] = "true";
                TempData["ReloadPage"] = "true";
            }

            return View();
        }

        /// <summary>
        ///  New job post registration function
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="amount">Amount</param>
        /// <param name="time">Time</param>
        /// <param name="description">Description</param>
        /// <returns></returns>
        [HttpPost]
        [PreventDuplicateRequest]
        [ValidateAntiForgeryToken]
        [AuthorizeDbUser]
        public JsonResult New_Job_Post(string title, int amount, int cspramount, long time, string description, string tags, string codeurl, string documenturl, string signedDeployJson)
        {

            try
            {
                //User input controls
                SimpleResponse controlResult = UserInputControls.ControlPostJobOfferRequest(HttpContext.Session.GetString("KYCStatus"), time, amount);

                if (controlResult.Success == false) return base.Json(controlResult);

                int userid = Convert.ToInt32(HttpContext.Session.GetInt32("UserID"));
                string token = HttpContext.Session.GetString("Token");
                string ip = Utility.IpHelper.GetClientIpAddress(HttpContext);
                string port = Utility.IpHelper.GetClientPort(HttpContext);

                SimpleResponse res = StartJobFlow(title, description, time, amount, cspramount, tags, codeurl, documenturl, ChainActionTypes.Post_Job, signedDeployJson, userid, token, ip, port);

                return Json(res);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return Json(new SimpleResponse() { Success = false, Message = "An error occured while processing your request." });
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        }

        /// <summary>
        ///  Job Edit Page
        /// </summary>
        /// <param name="Job">Job Id</param>     
        /// <returns></returns>
        [Route("My-Job-Edit/{Job}")]
        public IActionResult My_Job_Edit(int Job)
        {
            ViewBag.Title = "Edit Job";

            // Check if user signed with traditional db account. If not opens signin popup.
            if (CheckDbSignIn() == "Unauthorized")
            {
                TempData["DbSignRequired"] = "true";
                TempData["ReloadPage"] = "true";
            }

            JobPostDto jobDetailModel = new JobPostDto();

            try
            {
                //Get model from ApiGateway
                string jobJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/GetId?id=" + Job, HttpContext.Session.GetString("Token"));
                //Parse response
                jobDetailModel = Helpers.Serializers.DeserializeJson<JobPostDto>(jobJson);

                //Check if user trying to edit job for another user
                if (jobDetailModel.UserID != Convert.ToInt32(HttpContext.Session.GetInt32("UserID")))
                {
                    return View("../Shared/Error.cshtml");
                }

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return View(jobDetailModel);
        }

        /// <summary>
        /// Edit user Job post 
        /// </summary>
        /// <param name="Model">JobPostDto Model</param>
        /// <returns></returns>
        [HttpPut]
        [AuthorizeDbUser]
        public JsonResult My_Job_Update(JobPostDto Model)
        {
            SimpleResponse result = new SimpleResponse();

            try
            {
                //Get model from ApiGateway
                string jobJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/GetId?id=" + Model.JobID, HttpContext.Session.GetString("Token"));
                //Parse response
                JobPostDto jobDetailModel = Helpers.Serializers.DeserializeJson<JobPostDto>(jobJson);

                //Check if user trying to edit job for another user
                if (jobDetailModel.UserID != Convert.ToInt32(HttpContext.Session.GetInt32("UserID")))
                {
                    Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "User tried to edit job that is not yours.", Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                    return Json(new SimpleResponse { Success = false, Message = Lang.UnauthorizedAccess });
                }

                //Put model to ApiGateway
                string updateResponseJson = Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/Update", Helpers.Serializers.SerializeJson(Model), HttpContext.Session.GetString("Token"));
                //Parse response
                JobPostDto model = Helpers.Serializers.DeserializeJson<JobPostDto>(updateResponseJson);

                if (model != null && model.JobID > 0)
                {
                    Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "User updated job.", Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                    result.Success = true;
                    result.Message = "Job updated succesfully.";

                    //Set server side toastr because page will be redirected
                    TempData["toastr-message"] = result.Message;
                    TempData["toastr-type"] = "success";

                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        }

        #region Generic Onchain Job Methods

        public SimpleResponse JobDbOperations_CreatePost(string title, string description, long time, int amount, int cspramount, string tags, string codeurl, string documenturl, int userid, string token)
        {
            try
            {
                //Create JobPost model
                JobPostDto jobPostModel = new JobPostDto()
                {
                    UserID = userid,
                    Amount = amount,
                    JobDescription = description,
                    Tags = tags,
                    CodeUrl = codeurl,
                    CreateDate = DateTime.Now,
                    LastUpdate = DateTime.Now,
                    Title = title,
                    TimeFrame = time.ToString(),
                    Status = Enums.JobStatusTypes.ChainApprovalPending,
                    DocumentUrl = documenturl,
                    CsprAmount = cspramount
                };

                //Post model to ApiGateway
                string jobPostResponseJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/Post", Helpers.Serializers.SerializeJson(jobPostModel), token);
                //Parse reponse
                jobPostModel = Helpers.Serializers.DeserializeJson<JobPostDto>(jobPostResponseJson);

                return new SimpleResponse { Success = true, Content = jobPostModel, Message = "" };
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                return new SimpleResponse { Success = false, Message = "An error occured while processing your request." };
            }
        }

        public SimpleResponse JobDbOperations_Complete(JobPostDto jobPostModel, string deployHash, string token, string ip, string port)
        {
            try
            {
                if (jobPostModel != null && jobPostModel.JobID > 0)
                {
                    //Set job status to informal voting
                    jobPostModel.Status = Enums.JobStatusTypes.InternalAuction;
                    jobPostModel.DeployHash = deployHash;

                    string updateResponseJson = Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/Update", Helpers.Serializers.SerializeJson(jobPostModel), token);

                    Program.monitizer.AddUserLog(jobPostModel.UserID, Helpers.Constants.Enums.UserLogType.Request, "User created a new job.", ip, port);

                    return new SimpleResponse { Success = true, Message = "Your request successfully submitted." };

                }

                return new SimpleResponse { Success = false, Message = "An error occured while creating the job post." };

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                return new SimpleResponse { Success = false, Message = "An error occured while processing your request." };
            }
        }

        public SimpleResponse JobDbOperations_Fail(JobPostDto jobPostModel, string token)
        {
            try
            {
                if (jobPostModel != null && jobPostModel.JobID > 0)
                {
                    //Set job status to informal voting
                    jobPostModel.Status = Enums.JobStatusTypes.ChainError;
                    string updateResponseJson = Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/Update", Helpers.Serializers.SerializeJson(jobPostModel), token);
                }

                return new SimpleResponse { Success = false, Message = "An error occured while creating the job post." };
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                return new SimpleResponse { Success = false, Message = "An error occured while processing your request." };
            }
        }

        public SimpleResponse StartJobFlow(string title, string description, long time, int jobamount, int cspramount, string tags, string codeurl, string documenturl, ChainActionTypes actionType, string signedDeployJson, int userid, string token, string ip, string port)
        {
            ChainActionDto chainAction = new ChainActionDto();

            try
            {
                //If platform uses blockchain, process onchain flow
                if (Program._settings.DaoBlockchain == Blockchain.Casper)
                {
                    chainAction = CreateChainActionRecord(signedDeployJson, HttpContext.Session.GetString("WalletAddress"), actionType);

                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;

                        SimpleResponse jobPostResponse = JobDbOperations_CreatePost(title, description, time, jobamount, cspramount, tags, codeurl, documenturl, userid, token);

                        ChainActionDto deployResult = new ChainActionDto();

                        if (jobPostResponse.Success == false || jobPostResponse.Content == null)
                        {
                            chainAction.Status = ChainActionStatus.Error.ToString();
                            chainAction.Result = "Create new post error.";
                            deployResult = chainAction;
                            ChainError(chainAction, null);
                        }
                        else
                        {
                            JobPostDto jobPost = (JobPostDto)jobPostResponse.Content;

                            deployResult = SendSignedDeploy(chainAction);

                            //Central db operations
                            if (!string.IsNullOrEmpty(deployResult.DeployHash) && deployResult.Status == Enums.ChainActionStatus.Completed.ToString())
                            {
                                JobDbOperations_Complete(jobPost, deployResult.DeployHash, token, ip, port);
                            }
                            else
                            {
                                JobDbOperations_Fail(jobPost, token);
                            }
                        }

                        Program.chainQue.RemoveAt(Program.chainQue.IndexOf(chainAction));
                        Program.chainQue.Add(deployResult);
                    }).Start();

                    //Set server side toastr because page will be redirected
                    TempData["toastr-message"] = "Your request successfully submitted. ";
                    TempData["toastr-type"] = "success";

                    return new SimpleResponse() { Success = true };
                }
                else
                {
                    //Central db operations
                    SimpleResponse jobPostResponse = JobDbOperations_CreatePost(title, description, time, jobamount, 0, tags, codeurl, documenturl, userid, token);
                    SimpleResponse dbResponse = JobDbOperations_Complete((JobPostDto)jobPostResponse.Content, "", token, ip, port);
                    return dbResponse;
                }
            }
            catch (Exception ex)
            {
                return ChainError(chainAction, ex);
            }
        }

        #endregion

        #endregion

        #region Job Post Detail & Forum

        /// <summary>
        /// Job Detail Page
        /// </summary>
        /// <param name="JobID">Job Id</param>
        /// <returns></returns>
        [Route("Job-Detail/{JobID}")]
        public IActionResult Job_Detail(int JobID)
        {
            ViewBag.Title = "Job Details";

            JobPostDetailModel model = new JobPostDetailModel();

            try
            {
                var userid = HttpContext.Session.GetInt32("UserID");

                //Get model from ApiGateway
                var url = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Website/GetJobDetail?jobid=" + JobID + "&userid=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token"));
                //Parse response
                model.JobPostWebsiteModel = Helpers.Serializers.DeserializeJson<JobPostViewModel>(url);

                //Get model from ApiGateway
                var url1 = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Website/GetJobComment?jobid=" + JobID + "&userid=" + userid, HttpContext.Session.GetString("Token"));
                //Parse response
                model.JobPostCommentModel = Helpers.Serializers.DeserializeJson<List<JobPostCommentModel>>(url1);

                //Get related auction if exists
                var auctionJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Auction/GetByJobId?jobid=" + JobID, HttpContext.Session.GetString("Token"));
                model.Auction = Helpers.Serializers.DeserializeJson<AuctionDto>(auctionJson);

                //Get winning bid if exists 
                if (model.Auction != null && model.Auction.WinnerAuctionBidID >= 0)
                {
                    var auctionBidJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/AuctionBid/GetId?id=" + model.Auction.WinnerAuctionBidID, HttpContext.Session.GetString("Token"));
                    model.WinnerBid = Helpers.Serializers.DeserializeJson<AuctionBidDto>(auctionBidJson);
                }

                //Get related voting if exists
                var votingJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Voting/Voting/GetByJobId?jobid=" + JobID, HttpContext.Session.GetString("Token"));
                model.JobPostWebsiteModel.Voting = Helpers.Serializers.DeserializeJson<List<VotingDto>>(votingJson);


                //Get review result comment if exists
                // try
                // {
                //     if(model.JobPostCommentModel.Count(x=>x.IsPinned == true && x.Comment.Contains("Recommendation:") && x.Comment.Contains("Pull Request Link:")) > 0)
                //     {
                //         JobPostCommentModel reviewResultComment = model.JobPostCommentModel.First(x=>x.IsPinned == true && x.Comment.Contains("Recommendation:") && x.Comment.Contains("Pull Request Link:"));
                //         string reviewLink = reviewResultComment.Comment.Split(Environment.NewLine)[1].Split(':')[1].Trim();
                //         string html = Markdown.Parse(markdownText);
                //     }
                // }
                // catch
                // {
                // }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return View(model);
        }

        /// <summary>
        /// Add new comment
        /// </summary>
        /// <param name="JobId">Job id</param>
        /// <param name="CommentId">Main comment id</param>
        /// <param name="Comment">Comment</param>
        /// <returns></returns>
        [HttpPost]
        [AuthorizeDbUser]
        public JsonResult AddNewComment(int JobId, int CommentId, string Comment)
        {
            SimpleResponse result = new SimpleResponse();

            try
            {
                //KYC Control
                if (Convert.ToBoolean(Program._settings.DaoSettings.First(x => x.Key == "ForumKycRequired").Value) && HttpContext.Session.GetString("KYCStatus") != "true")
                {
                    result.Success = false;
                    result.Message = "Please complete the KYC from User Profile to add a new comment";
                    return Json(result);
                }

                //Check if this job entered voting phase. If yes user can't post comment anymore
                var informalVotingJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Voting/Voting/GetInformalVotingByJobId?jobid=" + JobId, HttpContext.Session.GetString("Token"));
                var informalVoting = Helpers.Serializers.DeserializeJson<VotingDto>(informalVotingJson);
                if (informalVoting != null && informalVoting.VotingID > 0)
                {
                    result.Success = false;
                    result.Message = "This job is in voting process or completed. Posting comments are disabled.";
                    return Json(result);
                }

                //Create new comment model
                JobPostCommentDto model = new JobPostCommentDto()
                {
                    Comment = Comment,
                    Date = DateTime.Now,
                    JobID = JobId,
                    SubCommentID = CommentId,
                    UserID = Convert.ToInt32(HttpContext.Session.GetInt32("UserID")),
                    DownVote = 0,
                    UpVote = 0,
                };

                //If user posting comment to main topic
                if (CommentId == 0)
                {
                    //Get related auction if exists
                    AuctionDto auction = Helpers.Serializers.DeserializeJson<AuctionDto>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Auction/GetByJobId?jobid=" + JobId, HttpContext.Session.GetString("Token")));

                    //Check if auction have a winner bid
                    if (auction != null && auction.AuctionID > 0 && auction.WinnerAuctionBidID != null)
                    {
                        //Get winner bid
                        AuctionBidDto auctionbid = Helpers.Serializers.DeserializeJson<AuctionBidDto>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/AuctionBid/GetId?id=" + auction.WinnerAuctionBidID, HttpContext.Session.GetString("Token")));

                        //Set user comment as "Pinned" if the user is the owner of winner bid
                        //This indicates job doer posting a comment as "Work of evidence" so it will be pinned comment
                        if (auctionbid != null && auctionbid.AuctionBidID > 0 && auctionbid.UserId == model.UserID)
                        {
                            model.IsPinned = true;
                        }
                        else
                        {
                            model.IsPinned = false;
                        }
                    }
                }

                //Post model to ApiGateway
                model = Helpers.Serializers.DeserializeJson<JobPostCommentDto>(Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Db/JobPostComment/Post", Helpers.Serializers.SerializeJson(model), HttpContext.Session.GetString("Token")));

                if (model != null && model.JobPostCommentID > 0)
                {
                    Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "User commented.", Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                    result.Success = true;
                    result.Message = "Comment succesfully posted.";
                    result.Content = model;

                    //Set server side toastr because page will be redirected
                    TempData["toastr-message"] = result.Message;
                    TempData["toastr-type"] = "success";
                }

                return Json(result);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        }

        /// <summary>
        /// Delete user comment and comments upvote and downvote
        /// </summary>
        /// <param name="CommentId">JobPostCommentID</param>
        /// <returns></returns>
        [HttpDelete]
        [AuthorizeDbUser]
        public JsonResult DeleteComment(int CommentId)
        {
            SimpleResponse result = new SimpleResponse();

            try
            {
                JobPostCommentDto model = new JobPostCommentDto();

                //Get comment
                var modelJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/JobPostComment/GetId?id=" + CommentId, HttpContext.Session.GetString("Token"));
                //Parse result
                model = Helpers.Serializers.DeserializeJson<JobPostCommentDto>(modelJson);


                //Check if user trying to delete comment for another user
                if (model.UserID != Convert.ToInt32(HttpContext.Session.GetInt32("UserID")))
                {
                    return Json(new SimpleResponse { Success = false, Message = Lang.UnauthorizedAccess });
                }
                else
                {

                    //Check if this job entered voting phase. If yes user can't post comment anymore
                    var informalVotingJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Voting/Voting/GetInformalVotingByJobId?jobid=" + model.JobID, HttpContext.Session.GetString("Token"));
                    var informalVoting = Helpers.Serializers.DeserializeJson<VotingDto>(informalVotingJson);
                    if (informalVoting != null && informalVoting.VotingID > 0)
                    {
                        result.Success = false;
                        result.Message = "This job is in voting process or completed. Deleting comments are disabled.";
                        return Json(result);
                    }

                    model.Comment = "This comment is deleted by the owner.";

                    //Update comment as deleted        
                    var deleteModelJson = Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/JobPostComment/Update", Helpers.Serializers.SerializeJson(model), HttpContext.Session.GetString("Token"));
                    //Parse result
                    model = Helpers.Serializers.DeserializeJson<JobPostCommentDto>(deleteModelJson);

                    if (model != null && model.JobPostCommentID > 0)
                    {
                        Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "User deleted their own comment.", Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                        result.Success = true;
                        result.Message = "Comment succesfully deleted.";
                        result.Content = "";

                        //Set server side toastr because page will be redirected
                        TempData["toastr-message"] = result.Message;
                        TempData["toastr-type"] = "success";
                    }
                }


                return Json(result);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });

        }

        /// <summary>
        /// Comment upvote function
        /// </summary>
        /// <param name="JobPostCommentId">JobPostCommentId</param>
        /// <returns></returns>
        [HttpGet]
        [AuthorizeDbUser]
        public JsonResult UpVote(int JobPostCommentId)
        {

            SimpleResponse result = new SimpleResponse();

            try
            {
                var userid = HttpContext.Session.GetInt32("UserID");

                //Get model from ApiGateway
                var model = Helpers.Serializers.DeserializeJson<List<UserCommentVoteDto>>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/UserCommentVote/GetByCommentId?CommentId=" + JobPostCommentId, HttpContext.Session.GetString("Token")));

                //Get upvote comment count
                int UpCount = model.Count(x => x.IsUpVote == true);

                //Get downvote comment count
                int DownCount = model.Count(x => x.IsUpVote == false);

                int[] res = { 0, 0 };

                //If user upvoted same comment in the past
                if (model.Count(x => x.UserId == userid && x.IsUpVote == true) > 0)
                {
                    var CommentModel = model.First(x => x.UserId == userid);

                    //Delete model from ApiGateway
                    var deleteModel = Helpers.Request.Delete(Program._settings.Service_ApiGateway_Url + "/Db/UserCommentVote/Delete?ID=" + CommentModel.UserCommentVoteID, HttpContext.Session.GetString("Token"));

                    //Remove 1 point from upvote
                    res[0] = UpCount - 1;
                    res[1] = DownCount;
                    result.Success = true;
                    result.Message = "";
                    result.Content = res;
                }
                //If user downvoted same comment in the past
                else if (model.Count(x => x.UserId == userid && x.IsUpVote == false) > 0)
                {
                    //Set CommentModel.IsUpVote
                    var CommentModel = model.First(x => x.UserId == userid && x.IsUpVote == false);
                    CommentModel.IsUpVote = true;

                    //Put model to ApiGateway
                    var updateModel = Helpers.Serializers.DeserializeJson<UserCommentVoteDto>(Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/UserCommentVote/Update", Helpers.Serializers.SerializeJson(CommentModel), HttpContext.Session.GetString("Token")));

                    //Add 1 point to upvote
                    //Remove 1 point from downvote
                    res[0] = UpCount + 1;
                    res[1] = DownCount - 1;
                    result.Success = true;
                    result.Message = "";
                    result.Content = res;
                }
                else
                {
                    //Create model
                    UserCommentVoteDto CommentModel = new UserCommentVoteDto()
                    {
                        UserId = Convert.ToInt32(userid),
                        IsUpVote = true,
                        JobPostCommentID = JobPostCommentId,
                    };

                    //Post model to ApiGateway
                    var postModel = Helpers.Serializers.DeserializeJson<UserCommentVoteDto>(Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Db/UserCommentVote/Post", Helpers.Serializers.SerializeJson(CommentModel), HttpContext.Session.GetString("Token")));

                    //Add 1 point to upvote
                    res[0] = UpCount + 1;
                    res[1] = DownCount;
                    result.Success = true;
                    result.Message = "";
                    result.Content = res;

                    Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "User upvoted to Comment #" + JobPostCommentId, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        }

        /// <summary>
        /// Comment downvote function
        /// </summary>
        /// <param name="JobPostCommentId">JobPostCommentId</param>
        /// <returns></returns>
        [HttpGet]
        [AuthorizeDbUser]
        public JsonResult DownVote(int JobPostCommentId)
        {
            SimpleResponse result = new SimpleResponse();
            try
            {
                var userid = HttpContext.Session.GetInt32("UserID");

                //Get model from ApiGateway
                var model = Helpers.Serializers.DeserializeJson<List<UserCommentVoteDto>>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/UserCommentVote/GetByCommentId?CommentId=" + JobPostCommentId, HttpContext.Session.GetString("Token")));

                //Get upvote comment count
                int UpCount = model.Count(x => x.IsUpVote == true);

                //Get downvote comment count
                int DownCount = model.Count(x => x.IsUpVote == false);

                int[] res = { 0, 0 };

                //If user downvoted same comment in the past
                if (model.Count(x => x.UserId == userid && x.IsUpVote == false) > 0)
                {
                    var CommentModel = model.First(x => x.UserId == userid);

                    //Delete model from ApiGateway
                    var deleteModel = Helpers.Request.Delete(Program._settings.Service_ApiGateway_Url + "/Db/UserCommentVote/Delete?ID=" + CommentModel.UserCommentVoteID, HttpContext.Session.GetString("Token"));

                    //Remove 1 point from downvote
                    res[0] = UpCount;
                    res[1] = DownCount - 1;
                    result.Success = true;
                    result.Message = "";
                    result.Content = res;
                }
                //If user upvoted same comment in the past
                else if (model.Count(x => x.UserId == userid && x.IsUpVote == true) > 0)
                {
                    //Set CommentModel.IsUpVote
                    var CommentModel = model.First(x => x.UserId == userid && x.IsUpVote == true);
                    CommentModel.IsUpVote = false;

                    //Put model to ApiGateway
                    var updateModel = Helpers.Serializers.DeserializeJson<UserCommentVoteDto>(Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/UserCommentVote/Update", Helpers.Serializers.SerializeJson(CommentModel), HttpContext.Session.GetString("Token")));

                    //Add 1 point to downvote
                    //Remove 1 point from upvote
                    res[0] = UpCount - 1;
                    res[1] = DownCount + 1;
                    result.Success = true;
                    result.Message = "";
                    result.Content = res;
                }
                else
                {
                    //Create model
                    UserCommentVoteDto CommentModel = new UserCommentVoteDto()
                    {
                        UserId = Convert.ToInt32(userid),
                        IsUpVote = false,
                        JobPostCommentID = JobPostCommentId,
                    };

                    //Post model to ApiGateway
                    var postModel = Helpers.Serializers.DeserializeJson<UserCommentVoteDto>(Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Db/UserCommentVote/Post", Helpers.Serializers.SerializeJson(CommentModel), HttpContext.Session.GetString("Token")));

                    //Add 1 point to downvote
                    res[0] = UpCount;
                    res[1] = DownCount + 1;
                    result.Success = true;
                    result.Message = "";
                    result.Content = res;

                    Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "user downvoted to Comment #" + JobPostCommentId, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        }

        /// <summary>
        /// Inserts flag comment to the job
        /// </summary>
        /// <param name="jobid">Job id</param>
        /// <returns></returns>
        [HttpGet]
        [AuthorizeDbUser]
        public JsonResult FlagJob(int jobid, string flagreason)
        {
            SimpleResponse result = new SimpleResponse();

            try
            {
                //KYC Control
                if (Convert.ToBoolean(Program._settings.DaoSettings.First(x => x.Key == "ForumKycRequired").Value) && HttpContext.Session.GetString("KYCStatus") != "true")
                {
                    result.Success = false;
                    result.Message = "Please complete the KYC from User Profile";
                    return Json(result);
                }

                //Check if this job entered voting phase. If yes user can't flag anymore
                var informalVotingJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Voting/Voting/GetInformalVotingByJobId?jobid=" + jobid, HttpContext.Session.GetString("Token"));
                var informalVoting = Helpers.Serializers.DeserializeJson<VotingDto>(informalVotingJson);
                if (informalVoting != null && informalVoting.VotingID > 0)
                {
                    result.Success = false;
                    result.Message = "This job is in voting process. Flagging is disabled.";
                    return Json(result);
                }

                //Create new comment model
                JobPostCommentDto model = new JobPostCommentDto()
                {
                    Comment = flagreason,
                    Date = DateTime.Now,
                    JobID = jobid,
                    SubCommentID = 0,
                    UserID = Convert.ToInt32(HttpContext.Session.GetInt32("UserID")),
                    DownVote = 0,
                    UpVote = 0,
                    IsFlagged = true
                };

                //Post model to ApiGateway
                model = Helpers.Serializers.DeserializeJson<JobPostCommentDto>(Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Db/JobPostComment/Post", Helpers.Serializers.SerializeJson(model), HttpContext.Session.GetString("Token")));

                if (model != null && model.JobPostCommentID > 0)
                {
                    Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "User commented.", Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                    result.Success = true;
                    result.Message = "Job flagged succesfully.";
                    result.Content = model;

                    //Set server side toastr because page will be redirected
                    TempData["toastr-message"] = result.Message;
                    TempData["toastr-type"] = "success";
                }

                return Json(result);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });

        }

        /// <summary>
        /// Removes flag comment from the job
        /// </summary>
        /// <param name="jobid">Job id</param>
        /// <returns></returns>
        [HttpGet]
        [AuthorizeDbUser]
        public JsonResult RemoveFlag(int jobid)
        {
            SimpleResponse result = new SimpleResponse();

            try
            {
                //Post model to ApiGateway
                result = Helpers.Serializers.DeserializeJson<SimpleResponse>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Website/RemoveFlag?jobid=" + jobid + "&userid=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token")));

                //Set server side toastr because page will be redirected
                TempData["toastr-message"] = result.Message;
                TempData["toastr-type"] = "success";

                return Json(result);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });

        }

        /// <summary>
        ///  Creates a new job and restarts job flow
        /// </summary>
        /// <param name="jobid">Job id</param>
        /// <returns></returns>
        [HttpGet]
        [AuthorizeDbUser]
        public JsonResult RestartJob(int jobid)
        {
            SimpleResponse result = new SimpleResponse();

            try
            {
                //Get Model from ApiGateway          
                var jobJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/GetId?id=" + jobid, HttpContext.Session.GetString("Token"));

                //Parse result
                var JobModel = Helpers.Serializers.DeserializeJson<JobPostDto>(jobJson);

                //Set status to FailRestart
                JobModel.Status = JobStatusTypes.FailRestart;
                JobModel = Helpers.Serializers.DeserializeJson<JobPostDto>(Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/Update", Helpers.Serializers.SerializeJson(JobModel), HttpContext.Session.GetString("Token")));

                JobModel.Status = JobStatusTypes.InternalAuction;
                JobModel.CreateDate = DateTime.Now;
                JobModel.JobID = 0;

                //Post model to ApiGateway
                string jobPostResponseJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/Post", Helpers.Serializers.SerializeJson(JobModel), HttpContext.Session.GetString("Token"));

                //Parse reponse
                var model = Helpers.Serializers.DeserializeJson<JobPostDto>(jobPostResponseJson);

                if (model != null && model.JobID > 0)
                {

                    Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "User restarted job #" + jobid, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));
                    Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "User added a new job (Restart) #" + model.JobID, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                    //Set auction end dates
                    int InternalAuctionTime = Convert.ToInt32(Program._settings.DaoSettings.First(x => x.Key == "InternalAuctionTime").Value);
                    int PublicAuctionTime = Convert.ToInt32(Program._settings.DaoSettings.First(x => x.Key == "PublicAuctionTime").Value);

                    DateTime internalAuctionEndDate = DateTime.Now.AddMilliseconds(InternalAuctionTime);
                    DateTime publicAuctionEndDate = DateTime.Now.AddMilliseconds(InternalAuctionTime + PublicAuctionTime);

                    AuctionDto AuctionModel = new AuctionDto()
                    {
                        JobID = model.JobID,
                        JobPosterUserId = model.UserID,
                        CreateDate = DateTime.Now,
                        Status = AuctionStatusTypes.InternalBidding,
                        InternalAuctionEndDate = internalAuctionEndDate,
                        PublicAuctionEndDate = publicAuctionEndDate
                    };

                    //Post model to ApiGateway
                    //Add new auction
                    AuctionModel = Helpers.Serializers.DeserializeJson<AuctionDto>(Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Db/Auction/Post", Helpers.Serializers.SerializeJson(AuctionModel), HttpContext.Session.GetString("Token")));

                    if (AuctionModel != null && AuctionModel.AuctionID > 0)
                    {
                        result.Success = true;
                        result.Message = "Restart successful. New job created and internal auction process started for the job.";
                        result.Content = AuctionModel;
                    }

                    //Set server side toastr because page will be redirected
                    TempData["toastr-message"] = result.Message;
                    TempData["toastr-type"] = "success";

                    return Json(result);
                }

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });

        }

        #endregion

        #region Auctions

        /// <summary>
        /// Auctions Page
        /// </summary>
        /// <returns></returns>
        [Route("Auctions")]
        public IActionResult Auctions(string query)
        {
            ViewBag.Title = "Auctions";

            List<AuctionViewModel> auctionsModel = new List<AuctionViewModel>();

            try
            {
                //Get auctions from ApiGateway
                var auctionsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Website/GetAuctions?query=" + query, HttpContext.Session.GetString("Token"));
                //Parse response
                auctionsModel = Helpers.Serializers.DeserializeJson<List<AuctionViewModel>>(auctionsJson);

                //Get user's bid for all active auctions from ApiGateway
                var bidsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Website/GetUserBids?userid=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token"));
                //Parse response
                List<AuctionBidDto> bidsModel = Helpers.Serializers.DeserializeJson<List<AuctionBidDto>>(bidsJson);

                //Match users existing bids with auctions
                foreach (var bid in bidsModel)
                {
                    if (auctionsModel.Count(x => x.AuctionID == bid.AuctionID) > 0)
                    {
                        var auction = auctionsModel.First(x => x.AuctionID == bid.AuctionID);
                        auction.UsersBidId = bid.AuctionBidID;
                        auction.UsersChainBidId = bid.BlockchainBidID;
                    }
                }

                //Get user's available reputation and save it to session to show in vote modal
                if (Program._settings.DaoBlockchain == null)
                {
                    var reputationJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Reputation/UserReputationHistory/GetLastReputation?userid=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token"));
                    if (!string.IsNullOrEmpty(reputationJson))
                    {
                        HttpContext.Session.SetString("LastUsableReputation", Helpers.Serializers.DeserializeJson<UserReputationHistoryDto>(reputationJson).LastUsableTotal.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }
            return View(auctionsModel);
        }

        /// <summary>
        /// My Bids Page
        /// </summary>
        /// <returns></returns>
        [Route("My-Bids")]
        public IActionResult My_Bids()
        {
            ViewBag.Title = "My Bids";

            List<MyBidsViewModel> bidsModel = new List<MyBidsViewModel>();

            try
            {
                //Get auctions from ApiGateway
                var mybidsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Website/GetMyBids?userid=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token"));
                //Parse response
                bidsModel = Helpers.Serializers.DeserializeJson<List<MyBidsViewModel>>(mybidsJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return View(bidsModel);
        }

        /// <summary>
        /// Auction Detail Page
        /// </summary>
        /// <param name="AuctionID">Auction Id</param>
        /// <returns></returns>
        [Route("Auction-Detail/{AuctionID}")]
        public IActionResult Auction_Detail(int AuctionID)
        {
            ViewBag.Title = "Auction Details";

            AuctionDetailViewModel AuctionDetailModel = new AuctionDetailViewModel();

            try
            {
                //Get auction model from ApiGateway
                var auctionJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Auction/GetId?id=" + AuctionID, HttpContext.Session.GetString("Token"));
                //Parse response
                AuctionDetailModel.Auction = Helpers.Serializers.DeserializeJson<AuctionDto>(auctionJson);

                //If auction isn't completed only job poster and admin can see the bids
                if (HttpContext.Session.GetString("UserType") != Enums.UserIdentityType.Admin.ToString() &&
                    HttpContext.Session.GetInt32("UserID") != AuctionDetailModel.Auction.JobPosterUserId)
                {
                    if (AuctionDetailModel.Auction.Status == AuctionStatusTypes.PublicBidding || AuctionDetailModel.Auction.Status == AuctionStatusTypes.InternalBidding)
                    {
                        return RedirectToAction("Auctions");
                    }
                }

                //Get bids model from ApiGateway
                var auctionBidsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Website/GetAuctionBids?auctionid=" + AuctionID, HttpContext.Session.GetString("Token"));
                //Parse response
                AuctionDetailModel.BidItems = Helpers.Serializers.DeserializeJson<List<AuctionBidItemModel>>(auctionBidsJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }
            return View(AuctionDetailModel);
        }

        /// <summary>
        /// Add new bid for auction
        /// </summary>
        /// <param name="Model">AuctionBidDto Model</param>
        /// <returns></returns>
        [HttpPost]
        [AuthorizeDbUser]
        public JsonResult Auction_Bid_Add(AuctionBidDto Model)
        {
            SimpleResponse result = new SimpleResponse();

            try
            {
                //Get auction model from ApiGateway
                var auctionJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Auction/GetId?id=" + Model.AuctionID, HttpContext.Session.GetString("Token"));
                //Parse response
                AuctionDto auction = Helpers.Serializers.DeserializeJson<AuctionDto>(auctionJson);

                //Get bids model from ApiGateway
                var auctionBidsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/AuctionBid/GetByAuctionId?auctionid=" + auction.AuctionID, HttpContext.Session.GetString("Token"));
                //Parse response
                List<AuctionBidDto> bids = Helpers.Serializers.DeserializeJson<List<AuctionBidDto>>(auctionBidsJson);

                //Check inputs
                if (Model.Price <= 0 || string.IsNullOrEmpty(Model.Time))
                {
                    return Json(new SimpleResponse { Success = false, Message = "Please fill the necessary fields." });
                }

                if (string.IsNullOrEmpty(Model.ResumeLink) && auction.Status == Enums.AuctionStatusTypes.PublicBidding)
                {
                    return Json(new SimpleResponse { Success = false, Message = "Please fill the necessary fields." });
                }

                //Check if referrer exists
                if (!string.IsNullOrEmpty(Model.Referrer))
                {
                    string userRefJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetByUsername?username=" + Model.Referrer, HttpContext.Session.GetString("Token"));
                    var userRef = Helpers.Serializers.DeserializeJson<UserDto>(userRefJson);
                    if (userRef == null || userRef.UserId <= 0)
                    {
                        return Json(new SimpleResponse { Success = false, Message = "Referrer username could not be found." });
                    }
                }

                int time = 0;
                bool timeParsed = int.TryParse(Model.Time, out time);
                if (timeParsed == false || time <= 0)
                {
                    return Json(new SimpleResponse { Success = false, Message = "Timeframe must be positive integer." });
                }

                //Check if public user trying to submit bid for expired or completed auction
                if (auction.Status == Enums.AuctionStatusTypes.Completed || auction.Status == Enums.AuctionStatusTypes.Expired)
                {
                    return Json(new SimpleResponse { Success = false, Message = "You can't submit bid to a closed auction" });
                }

                //Check if user trying to submit bid for his/her own job
                if (auction.JobPosterUserId == Convert.ToInt32(HttpContext.Session.GetInt32("UserID")))
                {
                    return Json(new SimpleResponse { Success = false, Message = "You can't submit bid to your own job." });
                }

                //Check if public user trying to submit bid for internal auction
                if (auction.Status == Enums.AuctionStatusTypes.InternalBidding && HttpContext.Session.GetString("UserType") == Enums.UserIdentityType.Associate.ToString())
                {
                    return Json(new SimpleResponse { Success = false, Message = "This auction is opened for internal members." });
                }

                //Check if user already submitted bid to this auction
                if (bids.Count(x => x.UserId == Convert.ToInt32(HttpContext.Session.GetInt32("UserID"))) > 0)
                {
                    return Json(new SimpleResponse { Success = false, Message = "You already have an existing bid for this auction." });
                }

                Model.UserId = Convert.ToInt32(HttpContext.Session.GetInt32("UserID"));
                Model.CreateDate = DateTime.Now;

                if (Program._settings.DaoBlockchain == Blockchain.Casper)
                {
                    ChainActionDto chainAction = CreateChainActionRecord(Model.signedDeployJson, HttpContext.Session.GetString("WalletAddress"), ChainActionTypes.Submit_Bid);

                    int userid = Convert.ToInt32(HttpContext.Session.GetInt32("UserID"));
                    string token = HttpContext.Session.GetString("Token");
                    string ip = Utility.IpHelper.GetClientIpAddress(HttpContext);
                    string port = Utility.IpHelper.GetClientPort(HttpContext);

                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;

                        ChainActionDto deployResult = new ChainActionDto();

                        deployResult = SendSignedDeploy(chainAction);

                        //Central db operations
                        if (!string.IsNullOrEmpty(deployResult.DeployHash) && deployResult.Status == Enums.ChainActionStatus.Completed.ToString())
                        {
                            Model.DeployHash = deployResult.DeployHash;

                            //Post model to ApiGateway
                            Model = Helpers.Serializers.DeserializeJson<AuctionBidDto>(Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Db/AuctionBid/Post", Helpers.Serializers.SerializeJson(Model), token));

                            if (Model != null && Model.AuctionBidID > 0)
                            {
                                result.Success = true;
                                result.Message = "Bid succesffully submitted.";
                                result.Content = Model;
                            }

                            Program.monitizer.AddUserLog(userid, Helpers.Constants.Enums.UserLogType.Request, "The user has bid on the auction. Auction #" + Model.AuctionID, ip, port);

                            //Set server side toastr because page will be redirected
                            TempData["toastr-message"] = result.Message;
                            TempData["toastr-type"] = "success";

                        }

                        Program.chainQue.RemoveAt(Program.chainQue.IndexOf(chainAction));
                        Program.chainQue.Add(deployResult);
                    }).Start();

                    //Set server side toastr because page will be redirected
                    TempData["toastr-message"] = "Your request successfully submitted. ";
                    TempData["toastr-type"] = "success";

                    return Json(new SimpleResponse() { Success = true });
                }
                else
                {
                    //Post model to ApiGateway
                    Model = Helpers.Serializers.DeserializeJson<AuctionBidDto>(Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Db/AuctionBid/Post", Helpers.Serializers.SerializeJson(Model), HttpContext.Session.GetString("Token")));

                    if (Model != null && Model.AuctionBidID > 0)
                    {
                        result.Success = true;
                        result.Message = "Bid succesffully submitted.";
                        result.Content = Model;

                        //Stake bid if internal auction
                        if (auction.Status == Enums.AuctionStatusTypes.InternalBidding)
                        {
                            UserReputationStakeDto stake = new UserReputationStakeDto() { UserID = Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Amount = Model.ReputationStake, CreateDate = DateTime.Now, Type = StakeType.Bid, ReferenceID = Model.AuctionBidID, ReferenceProcessID = Model.AuctionID, Status = ReputationStakeStatus.Staked };

                            //Post model to ApiGateway
                            string reputationJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Reputation/UserReputationStake/SubmitStake", Helpers.Serializers.SerializeJson(stake), HttpContext.Session.GetString("Token"));

                            SimpleResponse reputationResponse = Helpers.Serializers.DeserializeJson<SimpleResponse>(reputationJson);

                            //Delete bid from db if reputation stake is unsuccesful
                            if (reputationResponse.Success == false)
                            {
                                var deleteModel = Helpers.Request.Delete(Program._settings.Service_ApiGateway_Url + "/Db/AuctionBid/Delete?ID=" + Model.AuctionBidID, HttpContext.Session.GetString("Token"));

                                return Json(reputationResponse);
                            }
                        }
                        else
                        {
                            Model.ReputationStake = 0;
                        }
                    }

                    Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "The user has bid on the auction. Auction #" + Model.AuctionID, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                    //Set server side toastr because page will be redirected
                    TempData["toastr-message"] = result.Message;
                    TempData["toastr-type"] = "success";

                    return Json(result);
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });

        }

        /// <summary>
        /// Delete Auction Bid
        /// </summary>
        /// <param name="id">Auction Bid ID</param>
        /// <returns></returns>
        [HttpPost]
        [AuthorizeDbUser]
        public JsonResult Auction_Bid_Delete(int id, string signedDeployJson)
        {
            SimpleResponse result = new SimpleResponse();

            try
            {
                //Get auction bid model from ApiGateway
                var auctionBidJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/AuctionBid/GetId?id=" + id, HttpContext.Session.GetString("Token"));
                //Parse response
                AuctionBidDto auctionBid = Helpers.Serializers.DeserializeJson<AuctionBidDto>(auctionBidJson);

                //Check if this bid belongs to user
                if (auctionBid.UserId != Convert.ToInt32(HttpContext.Session.GetInt32("UserID")))
                {
                    return Json(new SimpleResponse { Success = false, Message = Lang.UnauthorizedAccess });
                }

                if (Program._settings.DaoBlockchain == Blockchain.Casper)
                {
                    ChainActionDto chainAction = CreateChainActionRecord(signedDeployJson, HttpContext.Session.GetString("WalletAddress"), ChainActionTypes.Cancel_Bid);

                    int userid = Convert.ToInt32(HttpContext.Session.GetInt32("UserID"));
                    string token = HttpContext.Session.GetString("Token");
                    string ip = Utility.IpHelper.GetClientIpAddress(HttpContext);
                    string port = Utility.IpHelper.GetClientPort(HttpContext);

                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;

                        ChainActionDto deployResult = new ChainActionDto();

                        deployResult = SendSignedDeploy(chainAction);

                        //Central db operations
                        if (!string.IsNullOrEmpty(deployResult.DeployHash) && deployResult.Status == Enums.ChainActionStatus.Completed.ToString())
                        {

                            //Release staked reputation for the bid.
                            SimpleResponse releaseStakeResponse = Helpers.Serializers.DeserializeJson<SimpleResponse>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Reputation/UserReputationStake/ReleaseStakesByType?referenceID=" + id + "&reftype=" + Enums.StakeType.Bid, token));

                            //Post model to ApiGateway
                            var deleteBidResponse = Helpers.Serializers.DeserializeJson<bool>(Helpers.Request.Delete(Program._settings.Service_ApiGateway_Url + "/Db/AuctionBid/Delete?id=" + id, token));

                            if (deleteBidResponse)
                            {
                                result.Success = true;
                                result.Message = "Bid succesffully deleted.";

                                //Set server side toastr because page will be redirected
                                TempData["toastr-message"] = result.Message;
                                TempData["toastr-type"] = "success";

                                Program.monitizer.AddUserLog(userid, Helpers.Constants.Enums.UserLogType.Request, "The user deleted bid on the auction. Auction #" + auctionBid.AuctionID, ip, port);
                            }

                        }

                        Program.chainQue.RemoveAt(Program.chainQue.IndexOf(chainAction));
                        Program.chainQue.Add(deployResult);
                    }).Start();

                    //Set server side toastr because page will be redirected
                    TempData["toastr-message"] = "Your request successfully submitted. ";
                    TempData["toastr-type"] = "success";

                    return Json(new SimpleResponse() { Success = true });
                }
                else
                {
                    //Release staked reputation for the bid.
                    SimpleResponse releaseStakeResponse = Helpers.Serializers.DeserializeJson<SimpleResponse>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Reputation/UserReputationStake/ReleaseStakesByType?referenceID=" + id + "&reftype=" + Enums.StakeType.Bid, HttpContext.Session.GetString("Token")));

                    //Post model to ApiGateway
                    var deleteBidResponse = Helpers.Serializers.DeserializeJson<bool>(Helpers.Request.Delete(Program._settings.Service_ApiGateway_Url + "/Db/AuctionBid/Delete?id=" + id, HttpContext.Session.GetString("Token")));

                    if (deleteBidResponse)
                    {
                        result.Success = true;
                        result.Message = "Bid succesffully deleted.";

                        //Set server side toastr because page will be redirected
                        TempData["toastr-message"] = result.Message;
                        TempData["toastr-type"] = "success";

                        Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "The user deleted bid on the auction. Auction #" + auctionBid.AuctionID, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));
                    }

                    return Json(result);
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });

        }

        /// <summary>
        ///  This method chooses winner bid and changes the status of the auction as completed. 
        ///  Only job poster is authorized to call the method
        /// </summary>
        /// <param name="bidId"></param>
        /// <param name="auctionId"></param>
        /// <param name="jobid"></param>
        /// <returns></returns>
        [HttpPost]
        [AuthorizeDbUser]
        public JsonResult ChooseWinnerBid(int bidId, string signedDeployJson)
        {
            SimpleResponse result = new SimpleResponse();
            try
            {
                //Get auction bid model from ApiGateway
                var auctionBidJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/AuctionBid/GetId?id=" + bidId, HttpContext.Session.GetString("Token"));
                //Parse response
                AuctionBidDto auctionBid = Helpers.Serializers.DeserializeJson<AuctionBidDto>(auctionBidJson);

                //Get auction model from ApiGateway
                var auctionJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Auction/GetId?id=" + auctionBid.AuctionID, HttpContext.Session.GetString("Token"));
                //Parse response
                AuctionDto auction = Helpers.Serializers.DeserializeJson<AuctionDto>(auctionJson);

                //Get user model from ApiGateway
                var userJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetId?id=" + auctionBid.UserId, HttpContext.Session.GetString("Token"));
                //Parse response
                UserDto userModel = Helpers.Serializers.DeserializeJson<UserDto>(userJson);

                //Check if user is authorized to choose winner (Must be job poster)
                if (auction.JobPosterUserId != HttpContext.Session.GetInt32("UserID"))
                {
                    return Json(new SimpleResponse { Success = false, Message = Lang.UnauthorizedAccess });
                }

                if (Program._settings.DaoBlockchain == Blockchain.Casper)
                {
                    ChainActionDto chainAction = CreateChainActionRecord(signedDeployJson, HttpContext.Session.GetString("WalletAddress"), ChainActionTypes.Pick_Bid);

                    int userid = Convert.ToInt32(HttpContext.Session.GetInt32("UserID"));
                    string token = HttpContext.Session.GetString("Token");
                    string ip = Utility.IpHelper.GetClientIpAddress(HttpContext);
                    string port = Utility.IpHelper.GetClientPort(HttpContext);

                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;

                        ChainActionDto deployResult = new ChainActionDto();

                        deployResult = SendSignedDeploy(chainAction);

                        //Central db operations
                        if (!string.IsNullOrEmpty(deployResult.DeployHash) && deployResult.Status == Enums.ChainActionStatus.Completed.ToString())
                        {

                            //Post bid
                            bool bidChooseResult = Helpers.Serializers.DeserializeJson<bool>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Auction/SetWinnerBid?bidId=" + bidId, token));
                            if (bidChooseResult)
                            {
                                //Change job status to Auction Completed
                                JobPostDto jobStatusResult = Helpers.Serializers.DeserializeJson<JobPostDto>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/ChangeJobStatus?jobid=" + auction.JobID + "&status=" + Helpers.Constants.Enums.JobStatusTypes.AuctionCompleted, token));

                                if (jobStatusResult.JobID > 0)
                                {
                                    result.Success = true;
                                    result.Message = "Winner bid selected.";

                                    //Set server side toastr because page will be redirected
                                    TempData["toastr-message"] = result.Message;
                                    TempData["toastr-type"] = "success";

                                    Program.monitizer.AddUserLog(userid, Helpers.Constants.Enums.UserLogType.Request, "Job poster selected the winner bid. Job #" + auction.JobID, ip, port);

                                    //Send notification email to winner
                                    //Set email title and content
                                    string emailTitle = "You won the auction for job #" + auction.JobID;
                                    string emailContent = "Greetings, " + userModel.NameSurname.Split(' ')[0] + ", <br><br> You won the auction of '" + jobStatusResult.Title + "'.<br><br> Please post your job completion evidence as a comment to the related job and start informal voting process within expected timeframe";

                                    SendEmailModel emailModel = new SendEmailModel() { Subject = emailTitle, Content = emailContent, To = new List<string> { userModel.Email } };
                                    Program.rabbitMq.Publish(Helpers.Constants.FeedNames.NotificationFeed, "email", Helpers.Serializers.Serialize(emailModel));
                                }

                            }
                        }

                        Program.chainQue.RemoveAt(Program.chainQue.IndexOf(chainAction));
                        Program.chainQue.Add(deployResult);
                    }).Start();

                    //Set server side toastr because page will be redirected
                    TempData["toastr-message"] = "Your request successfully submitted. ";
                    TempData["toastr-type"] = "success";

                    return Json(new SimpleResponse() { Success = true });
                }
                else
                {
                    //Post bid
                    bool bidChooseResult = Helpers.Serializers.DeserializeJson<bool>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Auction/SetWinnerBid?bidId=" + bidId, HttpContext.Session.GetString("Token")));
                    if (bidChooseResult)
                    {
                        //Change job status to Auction Completed
                        JobPostDto jobStatusResult = Helpers.Serializers.DeserializeJson<JobPostDto>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/ChangeJobStatus?jobid=" + auction.JobID + "&status=" + Helpers.Constants.Enums.JobStatusTypes.AuctionCompleted, HttpContext.Session.GetString("Token")));

                        if (jobStatusResult.JobID > 0)
                        {
                            //Release staked reputations for auction
                            SimpleResponse stakeReleaseResponse = Helpers.Serializers.DeserializeJson<SimpleResponse>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Reputation/UserReputationStake/ReleaseStakes?referenceProcessID=" + auction.AuctionID + "&reftype=" + Helpers.Constants.Enums.StakeType.Bid, HttpContext.Session.GetString("Token")));

                            //Mint new reputation with (ReputationConversionRate(DAO Variable) * Bid Price)

                            double ReputationConversionRate = Convert.ToDouble(Program._settings.DaoSettings.First(x => x.Key == "ReputationConversionRate").Value) / Convert.ToDouble(1000);
                            double DefaultPolicingRate = Convert.ToDouble(Program._settings.DaoSettings.First(x => x.Key == "DefaultPolicingRate").Value) / Convert.ToDouble(1000);

                            UserReputationStakeDto stake = new UserReputationStakeDto() { UserID = auctionBid.UserId, Amount = auctionBid.Price * ReputationConversionRate, CreateDate = DateTime.Now, Type = StakeType.Mint, ReferenceID = auction.JobID, ReferenceProcessID = auction.JobID, Status = ReputationStakeStatus.Staked };

                            //If winner is external user and doesnt want to get onboarded as VA.
                            if (auctionBid.VaOnboarding == false && userModel.UserType == Enums.UserIdentityType.Associate.ToString())
                            {
                                stake.Amount = auctionBid.Price * DefaultPolicingRate * ReputationConversionRate;
                            }

                            //Post model to ApiGateway
                            string mintJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Reputation/UserReputationStake/SubmitStake", Helpers.Serializers.SerializeJson(stake), HttpContext.Session.GetString("Token"));
                            //Parse response
                            SimpleResponse mintReponse = Helpers.Serializers.DeserializeJson<SimpleResponse>(mintJson);

                            if (mintReponse.Success)
                            {
                                result.Success = true;
                                result.Message = "Winner bid selected.";

                                //Set server side toastr because page will be redirected
                                TempData["toastr-message"] = result.Message;
                                TempData["toastr-type"] = "success";

                                Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "Job poster selected the winner bid. Job #" + auction.JobID, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                                //Send notification email to winner
                                //Set email title and content
                                string emailTitle = "You won the auction for job #" + auction.JobID;
                                string emailContent = "Greetings, " + userModel.NameSurname.Split(' ')[0] + ", <br><br> You won the auction of '" + jobStatusResult.Title + "'.<br><br> Please post your job completion evidence as a comment to the related job and start informal voting process within expected timeframe";

                                SendEmailModel emailModel = new SendEmailModel() { Subject = emailTitle, Content = emailContent, To = new List<string> { userModel.Email } };
                                Program.rabbitMq.Publish(Helpers.Constants.FeedNames.NotificationFeed, "email", Helpers.Serializers.Serialize(emailModel));

                            }
                        }

                    }

                    return Json(result);
                }


                return Json(result);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        }

        /// <summary>
        /// Returns data for reputation pie chart in the payment policy confirmation.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ReputationPieChartModel GetVaReputationChart()
        {
            ReputationPieChartModel result = new ReputationPieChartModel();
            result.Labels = new List<string>();
            result.Values = new List<int>();

            try
            {
                //Get VA user ids
                string vaJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetUserIdsByType?type=" + Enums.UserIdentityType.VotingAssociate, HttpContext.Session.GetString("Token"));
                //Parse response
                List<int> vaIds = Helpers.Serializers.DeserializeJson<List<int>>(vaJson);

                //Get VA reputations
                var reputationsTotalJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Reputation/UserReputationHistory/GetLastReputationByUserIds", Helpers.Serializers.SerializeJson(vaIds), HttpContext.Session.GetString("Token"));
                var reputationsTotal = Helpers.Serializers.DeserializeJson<List<UserReputationHistoryDto>>(reputationsTotalJson);

                List<string> anonymizedReputations = new List<string>();
                foreach (var item in reputationsTotal)
                {
                    anonymizedReputations.Add(Utility.StringHelper.AnonymizeReputation(item.LastTotal));
                }

                foreach (var group in anonymizedReputations.GroupBy(x => x))
                {
                    result.Labels.Add(group.Key);
                    result.Values.Add(group.Count());
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, Enums.LogTypes.ApplicationError, true);
            }

            return result;
        }

        #endregion

        #region Voting

        /// <summary>
        /// Votes Page
        /// </summary>
        /// <returns></returns>
        [Route("Votings")]
        public IActionResult Votings()
        {
            ViewBag.Title = "Voting";

            List<VotingViewModel> votingsModel = new List<VotingViewModel>();
            try
            {
                //Get model from ApiGateway
                var votingJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Website/GetVotingsByStatus", HttpContext.Session.GetString("Token"));

                //Parse response
                votingsModel = Helpers.Serializers.DeserializeJson<List<VotingViewModel>>(votingJson);

                //Only get verified blockchain actions if platform works onchain
                if (!String.IsNullOrEmpty(Program._settings.DaoBlockchain.ToString()))
                {
                    votingsModel = votingsModel.Where(x => !string.IsNullOrEmpty(x.DeployHash) && x.BlockChainVotingId != null).ToList();
                }

                //Get user's votes
                string votesJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Voting/Vote/GetAllVotesByUserId?userid=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token"));
                List<VoteDto> votesModel = Helpers.Serializers.DeserializeJson<List<VoteDto>>(votesJson);

                foreach (var voting in votingsModel)
                {
                    if (votesModel.Count(x => x.VotingID == voting.VotingID) > 0)
                    {
                        var vote = votesModel.First(x => x.VotingID == voting.VotingID);
                        if (vote.Direction == Helpers.Constants.Enums.StakeType.For || vote.Direction == Helpers.Constants.Enums.StakeType.Against)
                        {
                            voting.UserVote = vote.Direction;
                        }
                    }

                    //For simple votes job poster and job doer is equal
                    if (voting.JobDoerUserID == 0) voting.JobDoerUserID = voting.JobOwnerUserID;
                }

                if (Program._settings.DaoBlockchain == null)
                {
                    //Get user's available reputation and save it to session to show in vote modal
                    var reputationJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Reputation/UserReputationHistory/GetLastReputation?userid=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token"));
                    if (!string.IsNullOrEmpty(reputationJson))
                    {
                        HttpContext.Session.SetString("LastUsableReputation", Helpers.Serializers.DeserializeJson<UserReputationHistoryDto>(reputationJson).LastUsableTotal.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }
            return View(votingsModel);
        }

        /// <summary>
        /// Vote Detail Page
        /// </summary>
        /// <param name="VoteID">Vote Id</param>
        /// <returns></returns>
        [Route("Vote-Detail/{VotingID}")]
        public IActionResult Vote_Detail(int VotingID)
        {
            ViewBag.Title = "Voting Details";

            VoteDetailViewModel voteDetailModel = new VoteDetailViewModel();

            try
            {
                //Get voting model from ApiGateway
                var votingJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Voting/Voting/GetId?id=" + VotingID, HttpContext.Session.GetString("Token"));
                var voting = Helpers.Serializers.DeserializeJson<VotingDto>(votingJson);

                //Get votes model from ApiGateway
                var votesJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Voting/Vote/GetAllVotesByVotingId?votingid=" + VotingID, HttpContext.Session.GetString("Token"));
                var votes = Helpers.Serializers.DeserializeJson<List<VoteDto>>(votesJson);

                //Only get verified blockchain actions if platform works onchain
                if (!String.IsNullOrEmpty(Program._settings.DaoBlockchain.ToString()))
                {
                    votes = votes.Where(x => !string.IsNullOrEmpty(x.DeployHash)).ToList();
                }

                //Get reputation stakes from reputation service
                var reputationsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Reputation/UserReputationStake/GetByProcessId?referenceProcessID=" + VotingID + "&reftype=" + StakeType.For, HttpContext.Session.GetString("Token"));
                var reputations = Helpers.Serializers.DeserializeJson<List<UserReputationStakeDto>>(reputationsJson).OrderByDescending(x => x.UserReputationStakeID);

                //Get usernames of voters
                var usernamesJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetUsernamesByUserIds", Helpers.Serializers.SerializeJson(votes.Select(x => x.UserID)), HttpContext.Session.GetString("Token"));
                var usernames = Helpers.Serializers.DeserializeJson<List<string>>(usernamesJson);

                //Get informal voting results
                if (voting.IsFormal)
                {
                    var informalVotingJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Voting/Voting/GetInformalVotingByJobId?jobid=" + voting.JobID, HttpContext.Session.GetString("Token"));
                    var informalVoting = Helpers.Serializers.DeserializeJson<VotingDto>(informalVotingJson);

                    //Get reputation stakes of informal voting from reputation service
                    var informalReputationsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Reputation/UserReputationStake/GetByProcessId?referenceProcessID=" + informalVoting.VotingID + "&reftype=" + StakeType.For, HttpContext.Session.GetString("Token"));
                    var informalReputations = Helpers.Serializers.DeserializeJson<List<UserReputationStakeDto>>(informalReputationsJson);

                    //Get total reputations staked for both sides in informal voting
                    voteDetailModel.InformalFor = informalReputations.Where(x => x.Type == StakeType.For).Sum(x => x.Amount);
                    voteDetailModel.InformalAgainst = informalReputations.Where(x => x.Type == StakeType.Against).Sum(x => x.Amount);
                }

                //Combine results into VoteItemModel
                List<VoteItemModel> voteItems = new List<VoteItemModel>();

                for (int i = 0; i < votes.Count; i++)
                {
                    VoteItemModel vt = new VoteItemModel();
                    vt.UserID = votes[i].UserID;
                    vt.Date = votes[i].Date;
                    vt.Direction = votes[i].Direction;
                    vt.VoteID = votes[i].VoteID;
                    vt.VotingID = votes[i].VotingID;
                    vt.UserName = usernames[i];
                    vt.DeployHash = votes[i].DeployHash;
                    if (reputations.Count(x => x.UserID == vt.UserID) > 0)
                    {
                        vt.ReputationStake = reputations.First(x => x.UserID == vt.UserID).Amount;
                    }
                    voteItems.Add(vt);
                }

                //Parse response
                voteDetailModel.VoteItems = voteItems;
                //Parse response
                voteDetailModel.Voting = Helpers.Serializers.DeserializeJson<VotingDto>(votingJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return View(voteDetailModel);
        }

        /// <summary>
        ///  Job Edit Page
        /// </summary>
        /// <param name="Job">Job Id</param>     
        /// <returns></returns>
        [Route("StartJobVoting")]
        [AuthorizeChainUser]
        [AuthorizeDbUser]
        [HttpPost]
        public IActionResult StartJobVoting(int jobid, int commentid, string signedDeployJson)
        {
            SimpleResponse res = new SimpleResponse();

            try
            {
                //Get related auction if exists
                var auctionJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Auction/GetByJobId?jobid=" + jobid, HttpContext.Session.GetString("Token"));
                AuctionDto auction = Helpers.Serializers.DeserializeJson<AuctionDto>(auctionJson);

                if (auction == null || auction.AuctionID <= 0 || auction.Status != AuctionStatusTypes.Completed)
                {
                    return Json(new SimpleResponse { Success = false, Message = "Could not found completed auction for this job." });
                }

                //Get winner bid
                AuctionBidDto winnerBid = Helpers.Serializers.DeserializeJson<AuctionBidDto>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/AuctionBid/GetId?id=" + auction.WinnerAuctionBidID, HttpContext.Session.GetString("Token")));

                if (winnerBid.UserId != HttpContext.Session.GetInt32("UserID"))
                {
                    Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "User tried to start informal voting for job that is not yours. Job #" + auction.JobID, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                    return Json(new SimpleResponse { Success = false, Message = "User is not authorized to start informal voting for this job." });
                }

                int userid = Convert.ToInt32(HttpContext.Session.GetInt32("UserID"));
                string token = HttpContext.Session.GetString("Token");
                string ip = Utility.IpHelper.GetClientIpAddress(HttpContext);
                string port = Utility.IpHelper.GetClientPort(HttpContext);

                if (Program._settings.DaoBlockchain == Blockchain.Casper)
                {
                    ChainActionDto chainAction = CreateChainActionRecord(signedDeployJson, HttpContext.Session.GetString("WalletAddress"), ChainActionTypes.Submit_JobProof);

                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;

                        ChainActionDto deployResult = new ChainActionDto();

                        deployResult = SendSignedDeploy(chainAction);

                        //Central db operations
                        if (!string.IsNullOrEmpty(deployResult.DeployHash) && deployResult.Status == Enums.ChainActionStatus.Completed.ToString())
                        {
                            CompleteSubmitJobProof(winnerBid, jobid, deployResult.DeployHash, userid, token, ip, port);
                        }
                        else
                        {
                            //Delete comment if blockchain approval failed        
                            var deleteModelJson = Helpers.Request.Delete(Program._settings.Service_ApiGateway_Url + "/Db/JobPostComment/Delete?ID=" + commentid, token);
                        }

                        Program.chainQue.RemoveAt(Program.chainQue.IndexOf(chainAction));
                        Program.chainQue.Add(deployResult);
                    }).Start();

                    //Set server side toastr because page will be redirected
                    TempData["toastr-message"] = "Your request successfully submitted. ";
                    TempData["toastr-type"] = "success";

                    return Json(new SimpleResponse() { Success = true });
                }
                else
                {
                    res = CompleteSubmitJobProof(winnerBid, jobid, "", userid, token, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                    return Json(res);
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                //Delete comment if blockchain approval failed        
                var deleteModelJson = Helpers.Request.Delete(Program._settings.Service_ApiGateway_Url + "/Db/JobPostComment/Delete?ID=" + commentid, HttpContext.Session.GetString("Token"));
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        }

        public SimpleResponse CompleteSubmitJobProof(AuctionBidDto winnerBid, int jobid, string deployhash, int userid, string token, string ip, string port)
        {
            SimpleResponse res = new SimpleResponse();

            //Get user model from ApiGateway
            var userJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetId?id=" + winnerBid.UserId, token);
            //Parse response
            UserDto userModel = Helpers.Serializers.DeserializeJson<UserDto>(userJson);

            double DefaultPolicingRate = Convert.ToDouble(Program._settings.DaoSettings.First(x => x.Key == "DefaultPolicingRate").Value) / Convert.ToDouble(1000);
            double InformalQuorumRatio = Convert.ToDouble(Program._settings.DaoSettings.First(x => x.Key == "InformalQuorumRatio").Value) / Convert.ToDouble(1000);
            int InformalVotingTime = Convert.ToInt32(Program._settings.DaoSettings.First(x => x.Key == "InformalVotingTime").Value);

            //Start informal voting
            VotingDto informalVoting = new VotingDto();
            informalVoting.JobID = jobid;
            informalVoting.StartDate = DateTime.Now;
            if (winnerBid.VaOnboarding == false && userModel.UserType == Enums.UserIdentityType.Associate.ToString())
            {
                //Job doer wont earn any reputations
                informalVoting.PolicingRate = 1;
            }
            else
            {
                //Job doer will earn reputation and will be onboarded as VA
                informalVoting.PolicingRate = DefaultPolicingRate;
            }
            informalVoting.QuorumRatio = InformalQuorumRatio;
            informalVoting.Type = Enums.VoteTypes.JobCompletion;
            informalVoting.EndDate = DateTime.Now.AddMilliseconds(InformalVotingTime);
            informalVoting.DeployHash = deployhash;

            //Get related job post
            var jobJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/GetId?id=" + jobid, token);
            JobPostDto job = Helpers.Serializers.DeserializeJson<JobPostDto>(jobJson);

            //Get total dao member count
            int daoMemberCount = Convert.ToInt32(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetCount?type=" + UserIdentityType.VotingAssociate, token));
            //Eligible user count = VA Count
            informalVoting.EligibleUserCount = daoMemberCount;
            //Quorum count is calculated with total user count - 2(job poster, job doer)
            informalVoting.QuorumCount = Convert.ToInt32(Math.Ceiling(InformalQuorumRatio * Convert.ToDouble(informalVoting.EligibleUserCount)));

            string jsonResult = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Voting/Voting/StartInformalVoting", Helpers.Serializers.SerializeJson(informalVoting), token);
            res = Helpers.Serializers.DeserializeJson<SimpleResponse>(jsonResult);
            res.Content = null;

            //Change job status 
            Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/ChangeJobStatus?jobid=" + jobid + "&status=" + JobStatusTypes.InformalVoting, token);

            //Set server side toastr because page will be redirected
            TempData["toastr-message"] = res.Message;
            TempData["toastr-type"] = "success";

            Program.monitizer.AddUserLog(userid, Helpers.Constants.Enums.UserLogType.Request, "User started informal voting . Job #" + jobid, ip, port);

            //Send email notification to VAs
            SendEmailModel emailModel = new SendEmailModel() { Subject = "Informal Voting Started For Job #" + jobid, Content = "Informal voting process started for job #" + jobid + "<br><br>Please submit your vote until " + informalVoting.EndDate.ToString(), TargetGroup = Enums.UserIdentityType.VotingAssociate };
            Program.rabbitMq.Publish(Helpers.Constants.FeedNames.NotificationFeed, "email", Helpers.Serializers.Serialize(emailModel));

            return res;
        }

        /// <summary>
        ///  User submit vote action.
        /// </summary>
        /// <param name="VotingID"></param>
        /// <param name="Direction"></param>
        /// <param name="ReputationStake"></param>
        /// <returns></returns>
        [HttpPost]
        [AuthorizeChainUser]
        public JsonResult SubmitVote(int VotingID, StakeType Direction, double? ReputationStake, string signedDeployJson)
        {
            SimpleResponse result = new SimpleResponse();

            try
            {
                //Get voting model from ApiGateway
                var votingJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Voting/Voting/GetId?id=" + VotingID, HttpContext.Session.GetString("Token"));
                //Parse response
                VotingDto voting = Helpers.Serializers.DeserializeJson<VotingDto>(votingJson);

                //Get job post model from ApiGateway
                var jobJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/GetId?id=" + voting.JobID, HttpContext.Session.GetString("Token"));
                //Parse response
                JobPostDto job = Helpers.Serializers.DeserializeJson<JobPostDto>(jobJson);

                //Check if public user trying to submit bid for expired or completed auction
                if (voting.Status != Enums.VoteStatusTypes.Active)
                {
                    Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "User tried to submit vote for closed job. Voting #" + VotingID, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                    return Json(new SimpleResponse { Success = false, Message = "You can't submit vote to a closed voting." });
                }

                //Check if user trying to submit bid for his/her own job
                if (job.JobDoerUserID == Convert.ToInt32(HttpContext.Session.GetInt32("UserID")))
                {
                    Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "User tried to submit vote for her/his own job. Voting #" + VotingID, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                    return Json(new SimpleResponse { Success = false, Message = "You can't submit vote to your own job." });
                }

                //Check if user trying to submit bid with 0 reputation
                if (ReputationStake == null || ReputationStake <= 0)
                {
                    return Json(new SimpleResponse { Success = false, Message = "You must stake reputation greater than 0 for this voting type." });
                }


                if (Program._settings.DaoBlockchain == Blockchain.Casper)
                {
                    ChainActionDto chainAction = CreateChainActionRecord(signedDeployJson, HttpContext.Session.GetString("WalletAddress"), ChainActionTypes.Submit_Vote);

                    int userid = Convert.ToInt32(HttpContext.Session.GetInt32("UserID"));
                    string token = HttpContext.Session.GetString("Token");

                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;

                        ChainActionDto deployResult = new ChainActionDto();

                        deployResult = SendSignedDeploy(chainAction);

                        //Central db operations
                        if (!string.IsNullOrEmpty(deployResult.DeployHash) && deployResult.Status == Enums.ChainActionStatus.Completed.ToString())
                        {

                            string jsonResponse = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Voting/Vote/SubmitVote?VotingID=" + VotingID + "&UserID=" + userid + "&Direction=" + Direction + "&ReputationStake=" + ReputationStake.ToString().Replace(",", ".") + "&DeployHash=" + deployResult.DeployHash + "&WalletAddress=" + HttpContext.Session.GetString("WalletAddress"), token);
                            SimpleResponse votePostResponse = Helpers.Serializers.DeserializeJson<SimpleResponse>(jsonResponse);

                            //Set server side toastr because page will be redirected
                            TempData["toastr-message"] = result.Message;
                            TempData["toastr-type"] = "success";

                            Program.monitizer.AddUserLog(userid, Helpers.Constants.Enums.UserLogType.Request, "User voted job. Voting #" + VotingID, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));
                        }

                        Program.chainQue.RemoveAt(Program.chainQue.IndexOf(chainAction));
                        Program.chainQue.Add(deployResult);
                    }).Start();

                    //Set server side toastr because page will be redirected
                    TempData["toastr-message"] = "Your request successfully submitted. ";
                    TempData["toastr-type"] = "success";

                    return Json(new SimpleResponse() { Success = true });
                }
                else
                {
                    //Post model to ApiGateway
                    string jsonResponse = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Voting/Vote/SubmitVote?VotingID=" + VotingID + "&UserID=" + Convert.ToInt32(HttpContext.Session.GetInt32("UserID")) + "&Direction=" + Direction + "&ReputationStake=" + ReputationStake.ToString().Replace(",", "."), HttpContext.Session.GetString("Token"));
                    result = Helpers.Serializers.DeserializeJson<SimpleResponse>(jsonResponse);

                    if (result.Success)
                    {
                        //Set server side toastr because page will be redirected
                        TempData["toastr-message"] = result.Message;
                        TempData["toastr-type"] = "success";

                        Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "User voted job. Voting #" + VotingID, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));
                    }

                    return Json(result);
                }

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });

        }


        [HttpPost]
        [AuthorizeChainUser]
        public JsonResult FinishVoting(int VotingID, string signedDeployJson)
        {
            SimpleResponse result = new SimpleResponse();

            try
            {

                if (Program._settings.DaoBlockchain == Blockchain.Casper)
                {
                    ChainActionDto chainAction = CreateChainActionRecord(signedDeployJson, HttpContext.Session.GetString("WalletAddress"), ChainActionTypes.Finish_Voting);

                    int userid = Convert.ToInt32(HttpContext.Session.GetInt32("UserID"));
                    string token = HttpContext.Session.GetString("Token");

                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;

                        ChainActionDto deployResult = new ChainActionDto();

                        deployResult = SendSignedDeploy(chainAction);

                        //Central db operations
                        if (!string.IsNullOrEmpty(deployResult.DeployHash) && deployResult.Status == Enums.ChainActionStatus.Completed.ToString())
                        {

                            string jsonResponse = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Voting/Voting/GetId?id=" + VotingID, token);
                            VotingDto voting = Helpers.Serializers.DeserializeJson<VotingDto>(jsonResponse);
                            voting.Status = VoteStatusTypes.BlockchainFinish;

                            string jsonResponse2 = Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Voting/Voting/Update", Helpers.Serializers.SerializeJson(voting), token);

                            //Set server side toastr because page will be redirected
                            TempData["toastr-message"] = result.Message;
                            TempData["toastr-type"] = "success";

                            Program.monitizer.AddUserLog(userid, Helpers.Constants.Enums.UserLogType.Request, "User finished voting. Voting #" + VotingID, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));
                        }

                        Program.chainQue.RemoveAt(Program.chainQue.IndexOf(chainAction));
                        Program.chainQue.Add(deployResult);
                    }).Start();

                    //Set server side toastr because page will be redirected
                    TempData["toastr-message"] = "Your request successfully submitted. ";
                    TempData["toastr-type"] = "success";

                    return Json(new SimpleResponse() { Success = true });
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });

        }


        /// <summary>
        /// New Simple Vote Page
        /// </summary>
        /// <returns></returns>
        [Route("New-Vote")]
        public IActionResult New_Vote()
        {
            ViewBag.Title = "Start A New Vote";

            return View();
        }

        #region OLD METHODS
        ///// <summary>
        /////  New simple vote post function
        ///// </summary>
        ///// <param name="title">Title</param>
        ///// <param name="description">Description</param>
        ///// <returns></returns>
        //[HttpPost]
        //[AuthorizeChainUser]
        //public JsonResult New_SimpleVote_Post(NewVoteModel model)
        //{
        //    SimpleResponse result = new SimpleResponse();

        //    try
        //    {
        //        if (model.type == "va")
        //        {
        //            string userJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetByUsername?username=" + model.vausername, HttpContext.Session.GetString("Token"));
        //            var userObj = Helpers.Serializers.DeserializeJson<UserDto>(userJson);

        //            if (userObj == null || userObj.UserId <= 0)
        //            {
        //                result.Success = false;
        //                result.Message = "Could not find a user with this username.";
        //                return Json(result);
        //            }

        //            model.title = "New VA Onboarding (" + model.vausername + ")";
        //            model.description = model.vausername + " has indicated his interest and willingness to serve as a VA. In accordance with DAO policy, " + model.vausername + " is herewith proposed as a voting associate.";
        //        }

        //        if (model.type == "governance")
        //        {
        //            model.title = "Governance Vote (DAO Variables)";
        //            model.description = "Variables listed below will be applied to DAO" + Environment.NewLine + Environment.NewLine + JsonConvert.SerializeObject(model.settings, Formatting.Indented);
        //        }

        //        //Empty fields control
        //        if (string.IsNullOrEmpty(model.title) || string.IsNullOrEmpty(model.description))
        //        {
        //            result.Success = false;
        //            result.Message = "You must fill all the fields.";
        //            return Json(result);
        //        }

        //        //Create JobPost model
        //        JobPostDto jobPostModel = new JobPostDto() { UserID = Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Amount = 0, JobDescription = model.description, CreateDate = DateTime.Now, LastUpdate = DateTime.Now, Title = model.title, Status = Enums.JobStatusTypes.InformalVoting };
        //        //Post model to ApiGateway
        //        string jobPostResponseJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/Post", Helpers.Serializers.SerializeJson(jobPostModel), HttpContext.Session.GetString("Token"));
        //        //Parse reponse
        //        jobPostModel = Helpers.Serializers.DeserializeJson<JobPostDto>(jobPostResponseJson);

        //        if (jobPostModel != null && jobPostModel.JobID > 0)
        //        {
        //            //Start informal voting
        //            VotingDto informalVoting = new VotingDto();
        //            informalVoting.JobID = jobPostModel.JobID;
        //            informalVoting.StartDate = DateTime.Now;
        //            informalVoting.PolicingRate = Program._settings.DefaultPolicingRate;
        //            informalVoting.QuorumRatio = Program._settings.QuorumRatio;

        //            if (model.type == "governance")
        //            {
        //                informalVoting.Type = Enums.VoteTypes.Governance;
        //            }
        //            else
        //            {
        //                informalVoting.Type = Enums.VoteTypes.Simple;
        //            }

        //            informalVoting.EndDate = DateTime.Now.AddDays(Program._settings.SimpleVotingTime);

        //            if (Program._settings.SimpleVotingTimeType == "week")
        //            {
        //                informalVoting.EndDate = DateTime.Now.AddDays(Program._settings.SimpleVotingTime * 7);
        //            }
        //            else if (Program._settings.SimpleVotingTimeType == "minute")
        //            {
        //                informalVoting.EndDate = DateTime.Now.AddMinutes(Program._settings.SimpleVotingTime);
        //            }

        //            //Get total dao member count
        //            int daoMemberCount = Convert.ToInt32(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetCount?type=" + UserIdentityType.VotingAssociate, HttpContext.Session.GetString("Token")));
        //            //Eligible user count = VA Count - 1 (Job Doer)
        //            informalVoting.EligibleUserCount = daoMemberCount - 1;
        //            //Quorum count is calculated with total user count - 2(job poster, job doer)
        //            informalVoting.QuorumCount = Convert.ToInt32(Program._settings.QuorumRatio * Convert.ToDouble(informalVoting.EligibleUserCount));

        //            string jsonResult = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Voting/Voting/StartInformalVoting", Helpers.Serializers.SerializeJson(informalVoting), HttpContext.Session.GetString("Token"));
        //            result = Helpers.Serializers.DeserializeJson<SimpleResponse>(jsonResult);
        //            result.Content = null;

        //            Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "User started new simple vote.", Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

        //            //Set server side toastr because page will be redirected
        //            TempData["toastr-message"] = result.Message;
        //            TempData["toastr-type"] = "success";

        //            //Send email notification to VAs
        //            SendEmailModel emailModel = new SendEmailModel() { Subject = "New Simple Vote Submitted #" + jobPostModel.JobID, Content = "New simple vote started. Title:" + jobPostModel.Title + "<br><br>Please submit your vote until " + informalVoting.EndDate.ToString(), TargetGroup = Enums.UserIdentityType.VotingAssociate };
        //            Program.rabbitMq.Publish(Helpers.Constants.FeedNames.NotificationFeed, "email", Helpers.Serializers.Serialize(emailModel));

        //            return Json(result);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
        //    }

        //    return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        //}
        #endregion

        /// <summary>
        ///  New Simple vote post function
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [AuthorizeChainUser]
        public JsonResult New_Vote_Simple(NewVoteSimpleModel model)
        {
            try
            {
                //User input controls
                SimpleResponse controlResult = UserInputControls.ControlSimpleVoteRequest(model.documenthash);

                if (controlResult.Success == false) return base.Json(controlResult);

                int userid = Convert.ToInt32(HttpContext.Session.GetInt32("UserID"));
                string token = HttpContext.Session.GetString("Token");
                string ip = Utility.IpHelper.GetClientIpAddress(HttpContext);
                string port = Utility.IpHelper.GetClientPort(HttpContext);

                string title = "Simple Vote: " + model.title;
                string description = model.description;

                SimpleResponse res = StartVoteFlow(title, description, VoteTypes.Simple, ChainActionTypes.Simple_Vote, model.signedDeployJson, userid, token, ip, port);

                return Json(res);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return Json(new SimpleResponse() { Success = false, Message = "An error occured while processing your request." });
            }
        }

        /// <summary>
        ///  New VA Onboarding vote post function
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [AuthorizeChainUser]
        public JsonResult New_Vote_VaOnboarding(NewVoteVaOnboardingModel model)
        {
            try
            {
                //User input controls
                SimpleResponse controlResult = UserInputControls.ControlVaOnboardingVoteRequest(model.newvausername, model.newvaaddress, model.reason, HttpContext.Session.GetString("Token"));

                if (controlResult.Success == false) return base.Json(controlResult);

                int userid = Convert.ToInt32(HttpContext.Session.GetInt32("UserID"));
                string token = HttpContext.Session.GetString("Token");
                string ip = Utility.IpHelper.GetClientIpAddress(HttpContext);
                string port = Utility.IpHelper.GetClientPort(HttpContext);

                string title = "VA Onboarding vote for user '" + model.newvausername + "'";
                string description = "VA Onboarding vote for user '" + model.newvausername + "' <br><br> Reason: " + model.reason;

                SimpleResponse res = StartVoteFlow(title, description, VoteTypes.VAOnboarding, ChainActionTypes.VA_Onboarding_Vote, model.signedDeployJson, userid, token, ip, port);

                return Json(res);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return Json(new SimpleResponse() { Success = false, Message = "An error occured while processing your request." });
            }
        }

        /// <summary>
        ///  New Governance/Repo vote post function
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [AuthorizeChainUser]
        public JsonResult New_Vote_Governance(NewVoteGovernanceModel model)
        {
            try
            {
                //User input controls
                SimpleResponse controlResult = UserInputControls.ControlGovernanceVoteRequest(model.key, model.value);

                if (controlResult.Success == false) return base.Json(controlResult);

                int userid = Convert.ToInt32(HttpContext.Session.GetInt32("UserID"));
                string token = HttpContext.Session.GetString("Token");
                string ip = Utility.IpHelper.GetClientIpAddress(HttpContext);
                string port = Utility.IpHelper.GetClientPort(HttpContext);

                string title = "Governance vote for variable: '" + model.key + "'";
                string description = "Governance vote for variable: '" + model.key + "' <br><br> New value: " + model.value;

                SimpleResponse res = StartVoteFlow(title, description, VoteTypes.Governance, ChainActionTypes.Governance_Vote, model.signedDeployJson, userid, token, ip, port);

                return Json(res);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return Json(new SimpleResponse() { Success = false, Message = "An error occured while processing your request." });
            }
        }

        /// <summary>
        ///  New KYC vote post function
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [AuthorizeChainUser]
        public JsonResult New_Vote_KYC(NewVoteKYCModel model)
        {
            try
            {
                //User input controls
                SimpleResponse controlResult = UserInputControls.ControlKYCVoteRequest(model.kycUserName, HttpContext.Session.GetString("Token"));

                if (controlResult.Success == false) return base.Json(controlResult);

                int userid = Convert.ToInt32(HttpContext.Session.GetInt32("UserID"));
                string token = HttpContext.Session.GetString("Token");
                string ip = Utility.IpHelper.GetClientIpAddress(HttpContext);
                string port = Utility.IpHelper.GetClientPort(HttpContext);

                string title = "KYC vote for user '" + model.kycUserName + "'";
                string description = "KYC vote for user '" + model.kycUserName + "' <br><br> Document verification id: " + ((UserKYCDto)((dynamic)controlResult.Content).kyc).VerificationId;

                SimpleResponse res = StartVoteFlow(title, description, VoteTypes.KYC, ChainActionTypes.KYC_Vote, model.signedDeployJson, userid, token, ip, port);

                return Json(res);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return Json(new SimpleResponse() { Success = false, Message = "An error occured while processing your request." });
            }
        }

        /// <summary>
        ///  New Reputation vote post function
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [AuthorizeChainUser]
        public JsonResult New_Vote_Reputation(NewVoteReputationModel model)
        {
            try
            {
                //User input controls
                SimpleResponse controlResult = UserInputControls.ControlReputationVoteRequest(model.amount, model.documenthash, model.action, model.repusername, model.subjectaddress, HttpContext.Session.GetString("Token"));

                if (controlResult.Success == false) return base.Json(controlResult);

                int userid = Convert.ToInt32(HttpContext.Session.GetInt32("UserID"));
                string token = HttpContext.Session.GetString("Token");
                string ip = Utility.IpHelper.GetClientIpAddress(HttpContext);
                string port = Utility.IpHelper.GetClientPort(HttpContext);

                model.subjectaddress = Utility.StringHelper.ShortenWallet(((UserDto)((dynamic)controlResult.Content).user).WalletAddress);

                string title = "Reputation vote for account '" + model.subjectaddress + "'";
                string description = "Reputation vote details <br><br> Account: " + model.subjectaddress + " <br> Action: " + model.action + " <br> Amount: " + model.amount + " <br> Document Hash: " + model.documenthash + " <br> Stake: " + model.stake;

                SimpleResponse res = StartVoteFlow(title, description, VoteTypes.Reputation, ChainActionTypes.Reputation_Vote, model.signedDeployJson, userid, token, ip, port);

                return Json(res);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return Json(new SimpleResponse() { Success = false, Message = "An error occured while processing your request." });
            }
        }

        /// <summary>
        ///  New Slashing vote post function
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [AuthorizeChainUser]
        public JsonResult New_Vote_Slashing(NewVoteSlashingModel model)
        {
            try
            {
                //User input controls
                SimpleResponse controlResult = UserInputControls.ControlSlashingVoteRequest(model.addresstoslash, model.slashusername, HttpContext.Session.GetString("Token"));

                if (controlResult.Success == false) return base.Json(controlResult);

                int userid = Convert.ToInt32(HttpContext.Session.GetInt32("UserID"));
                string token = HttpContext.Session.GetString("Token");
                string ip = Utility.IpHelper.GetClientIpAddress(HttpContext);
                string port = Utility.IpHelper.GetClientPort(HttpContext);


                string title = "Slashing vote for account '" + model.addresstoslash + "'";
                string description = "Slashing vote details <br><br> Account: " + model.addresstoslash + " <br> Slash Ratio: " + model.slashratio + " <br> Stake: " + model.stake;

                SimpleResponse res = StartVoteFlow(title, description, VoteTypes.Slashing, ChainActionTypes.Slashing_Vote, model.signedDeployJson, userid, token, ip, port);

                return Json(res);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return Json(new SimpleResponse() { Success = false, Message = "An error occured while processing your request." });
            }
        }

        #region Generic Onchain Vote Methods

        public SimpleResponse VoteDbOperations_CreatePost(string title, string description, int userid, string token)
        {
            try
            {
                //Create JobPost model
                JobPostDto jobPostModel = new JobPostDto()
                {
                    UserID = userid,
                    Amount = 0,
                    JobDescription = description,
                    CreateDate = DateTime.Now,
                    LastUpdate = DateTime.Now,
                    Title = title,
                    Status = Enums.JobStatusTypes.ChainApprovalPending
                };
                //Post model to ApiGateway
                string jobPostResponseJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/Post", Helpers.Serializers.SerializeJson(jobPostModel), token);
                //Parse reponse
                jobPostModel = Helpers.Serializers.DeserializeJson<JobPostDto>(jobPostResponseJson);

                return new SimpleResponse { Success = true, Content = jobPostModel, Message = "" };
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                return new SimpleResponse { Success = false, Message = "An error occured while processing your request." };
            }
        }

        public SimpleResponse VoteDbOperations_Complete(VoteTypes voteType, JobPostDto jobPostModel, string deployHash, int userid, string token, string ip, string port)
        {
            try
            {
                if (jobPostModel != null && jobPostModel.JobID > 0)
                {

                    double DefaultPolicingRate = Convert.ToDouble(Program._settings.DaoSettings.First(x => x.Key == "DefaultPolicingRate").Value) / Convert.ToDouble(1000);
                    double InformalQuorumRatio = Convert.ToDouble(Program._settings.DaoSettings.First(x => x.Key == "InformalQuorumRatio").Value) / Convert.ToDouble(1000);
                    int InformalVotingTime = Convert.ToInt32(Program._settings.DaoSettings.First(x => x.Key == "InformalVotingTime").Value);


                    //Start informal voting
                    VotingDto informalVoting = new VotingDto();
                    informalVoting.JobID = jobPostModel.JobID;
                    informalVoting.StartDate = DateTime.Now;
                    informalVoting.PolicingRate = DefaultPolicingRate;
                    informalVoting.QuorumRatio = InformalQuorumRatio;
                    informalVoting.Type = voteType;
                    informalVoting.DeployHash = deployHash;
                    informalVoting.EndDate = DateTime.Now.AddMilliseconds(InformalVotingTime);

                    //Get total dao member count
                    int daoMemberCount = Convert.ToInt32(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetCount?type=" + UserIdentityType.VotingAssociate, token));
                    //Eligible user count = VA Count - 1 (Job Doer)
                    informalVoting.EligibleUserCount = daoMemberCount - 1;
                    //Quorum count is calculated with total user count - 2(job poster, job doer)
                    informalVoting.QuorumCount = Convert.ToInt32(InformalQuorumRatio * Convert.ToDouble(informalVoting.EligibleUserCount));

                    string jsonResult = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Voting/Voting/StartInformalVoting", Helpers.Serializers.SerializeJson(informalVoting), token);
                    var voteResult = Helpers.Serializers.DeserializeJson<SimpleResponse>(jsonResult);

                    if (voteResult.Success)
                    {
                        //Set job status to informal voting
                        jobPostModel.Status = Enums.JobStatusTypes.InformalVoting;
                        string updateResponseJson = Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/Update", Helpers.Serializers.SerializeJson(jobPostModel), token);

                        Program.monitizer.AddUserLog(userid, Helpers.Constants.Enums.UserLogType.Request, "User started new simple vote.", ip, port);

                        //Send email notification to VAs
                        SendEmailModel emailModel = new SendEmailModel() { Subject = "New Vote Submitted #" + jobPostModel.JobID, Content = "New vote started. Title:" + jobPostModel.Title + "<br><br>Please submit your vote until " + informalVoting.EndDate.ToString(), TargetGroup = Enums.UserIdentityType.VotingAssociate };
                        Program.rabbitMq.Publish(Helpers.Constants.FeedNames.NotificationFeed, "email", Helpers.Serializers.Serialize(emailModel));


                        return new SimpleResponse { Success = true, Message = "Your request successfully submitted." };
                    }
                    else
                    {
                        return new SimpleResponse { Success = false, Message = voteResult.Message };
                    }

                }

                return new SimpleResponse { Success = false, Message = "An error occured while creating the job post." };

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                return new SimpleResponse { Success = false, Message = "An error occured while processing your request." };
            }
        }

        public SimpleResponse VoteDbOperations_Fail(JobPostDto jobPostModel, string token)
        {
            try
            {
                if (jobPostModel != null && jobPostModel.JobID > 0)
                {
                    //Set job status to error
                    jobPostModel.Status = Enums.JobStatusTypes.ChainError;
                    string updateResponseJson = Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/Update", Helpers.Serializers.SerializeJson(jobPostModel), token);
                }

                return new SimpleResponse { Success = false, Message = "An error occured while creating the job post." };
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                return new SimpleResponse { Success = false, Message = "An error occured while processing your request." };
            }
        }

        public SimpleResponse StartVoteFlow(string title, string description, VoteTypes voteType, ChainActionTypes actionType, string signedDeployJson, int userid, string token, string ip, string port)
        {
            ChainActionDto chainAction = new ChainActionDto();

            try
            {
                //If platform uses blockchain, process onchain flow
                if (Program._settings.DaoBlockchain == Blockchain.Casper)
                {
                    chainAction = CreateChainActionRecord(signedDeployJson, HttpContext.Session.GetString("WalletAddress"), actionType);

                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;

                        SimpleResponse votePostResponse = VoteDbOperations_CreatePost(title, description, userid, token);

                        ChainActionDto deployResult = new ChainActionDto();

                        if (votePostResponse.Success == false || votePostResponse.Content == null)
                        {
                            chainAction.Status = ChainActionStatus.Error.ToString();
                            chainAction.Result = "Create new post error.";
                            deployResult = chainAction;
                            ChainError(chainAction, null);
                        }
                        else
                        {
                            JobPostDto jobPost = (JobPostDto)votePostResponse.Content;

                            deployResult = SendSignedDeploy(chainAction);

                            //Central db operations
                            if (!string.IsNullOrEmpty(deployResult.DeployHash) && deployResult.Status == Enums.ChainActionStatus.Completed.ToString())
                            {
                                VoteDbOperations_Complete(voteType, jobPost, deployResult.DeployHash, userid, token, ip, port);
                            }
                            else
                            {
                                VoteDbOperations_Fail(jobPost, token);
                            }
                        }

                        Program.chainQue.RemoveAt(Program.chainQue.IndexOf(chainAction));
                        Program.chainQue.Add(deployResult);
                    }).Start();

                    //Set server side toastr because page will be redirected
                    TempData["toastr-message"] = "Your request successfully submitted. ";
                    TempData["toastr-type"] = "success";

                    return new SimpleResponse() { Success = true };
                }
                else
                {
                    //Central db operations
                    SimpleResponse jobPostResponse = VoteDbOperations_CreatePost("Simple Vote: " + title, description, userid, token);
                    SimpleResponse dbResponse = VoteDbOperations_Complete(voteType, (JobPostDto)jobPostResponse.Content, "", userid, token, ip, port);
                    return dbResponse;
                }
            }
            catch (Exception ex)
            {
                return ChainError(chainAction, ex);
            }
        }

        #endregion

        #endregion

        #region Reputation

        /// <summary>
        /// User Reputation History Page
        /// </summary>
        /// <returns></returns>
        [Route("Reputation-History")]
        public IActionResult Reputation_History()
        {
            ViewBag.Title = "Reputation History";

            List<UserReputationHistoryDto> ReputationHistoryModel = new List<UserReputationHistoryDto>();
            try
            {
                //Get model from ApiGateway
                if (Program._settings.DaoBlockchain != null)
                {
                    var url = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Reputation/UserReputationHistory/GetByUserId?address=" + HttpContext.Session.GetString("WalletAddress"), HttpContext.Session.GetString("Token"));
                    //Parse response
                    ReputationHistoryModel = Helpers.Serializers.DeserializeJson<List<UserReputationHistoryDto>>(url);
                    ReputationHistoryModel = ReputationHistoryModel.OrderByDescending(x => x.Date).ToList();
                    //HttpContext.Session.SetString("Reputation", (ReputationHistoryModel.First().StakeReleasedAmount + ReputationHistoryModel.First().StakedAmount).ToString());
                }
                else
                {
                    var url = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Reputation/UserReputationHistory/GetByUserId?userid=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token"));
                    //Parse response
                    ReputationHistoryModel = Helpers.Serializers.DeserializeJson<List<UserReputationHistoryDto>>(url);
                }


            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }
            return View(ReputationHistoryModel);
        }

        /// <summary>
        ///  Exports reputation history to csv
        /// </summary>
        /// <returns></returns>
        public async Task<FileResult> ExportReputationHistoryCsv(DateTime? start, DateTime? end)
        {
            try
            {
                //Get reputation history data from ApiGateway
                string repsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Reputation/UserReputationHistory/GetByUserIdDate?userid=" + HttpContext.Session.GetInt32("UserID") + "&start=" + start.ToString() + "&end=" + end.ToString(), HttpContext.Session.GetString("Token"));
                //Parse response
                List<UserReputationHistoryDto> model = Helpers.Serializers.DeserializeJson<List<UserReputationHistoryDto>>(repsJson);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Date;Title;Explanation;Earned Amount;Lost Amount; Staked Total; Usable Total; Cumulative Total");
                foreach (var item in model.OrderByDescending(x => x.Date))
                {
                    sb.AppendLine(item.Date + ";" + item.Title + ";" + item.Explanation + ";" + item.EarnedAmount + ";" + item.LostAmount + ";" + item.LastStakedTotal + ";" + item.LastUsableTotal + ";" + item.LastTotal);
                }

                byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString()); ;

                return File(fileBytes, "text/csv", "CRDAO Reputation History.csv");
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return File(new List<byte>().ToArray(), "text/csv", "CRDAO Reputation History.csv");
        }

        #endregion

        #region User Views & Methods
        /// <summary>
        /// User Profile Page
        /// </summary>
        /// <returns></returns>
        [Route("User-Profile")]
        [AuthorizeDbUser]
        public IActionResult User_Profile()
        {
            ViewBag.Title = "User Profile";

            UserDto profileModel = new UserDto();

            try
            {
                //Get model from ApiGateway
                var json = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetId?id=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token"));

                //Parse response
                profileModel = Helpers.Serializers.DeserializeJson<UserDto>(json);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return View(profileModel);
        }

        /// <summary>
        ///  Profile save changes action
        /// </summary>
        /// <param name="image"></param>
        /// <param name="File"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("ProfileUpdate")]
        [AuthorizeDbUser]

        public JsonResult ProfileUpdate(string image, string ibanAddress, IFormFile File)
        {

            try
            {
                //Get user
                UserDto modeluser = Helpers.Serializers.DeserializeJson<UserDto>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetId?id=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token")));

                if (modeluser != null && modeluser.UserId > 0)
                {
                    //If custom image uploaded
                    if (File != null)
                    {

                        var file = File;
                        var ext = (Path.GetExtension(file.FileName).ToLower());

                        //File extension control
                        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".gif")
                        {
                            return Json(new SimpleResponse { Success = false, Message = "Please upload a supported format. (.png, .jpg, .gif)" });
                        }


                        using (var ms = new MemoryStream())
                        {
                            File.CopyTo(ms);
                            var fileBytes = ms.ToArray();
                            string s = ResizeImage(fileBytes, 150, 150);
                            modeluser.ProfileImage = s;
                        }
                    }
                    else
                    {
                        modeluser.ProfileImage = Path.GetFileName(image);
                    }

                    modeluser.IBAN = ibanAddress;

                    //It will be set from succesful onchain login
                    //modeluser.WalletAddress = walletAddress;

                    //Update user  
                    var updatemodel = Helpers.Serializers.DeserializeJson<UserDto>(Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/Users/Update", Helpers.Serializers.SerializeJson(modeluser), HttpContext.Session.GetString("Token")));

                    if (updatemodel != null && updatemodel.UserId > 0)
                    {
                        Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "User updated their profile photo.", Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                        HttpContext.Session.SetString("ProfileImage", modeluser.ProfileImage);


                        TempData["toastr-message"] = "Save changes successful.";
                        TempData["toastr-type"] = "success";


                        return Json(new SimpleResponse { Success = true, Message = "Save changes successful." });
                    }
                }

                return Json(new SimpleResponse { Success = false, Message = "Profile Updated Failed." });
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        }

        /// <summary>
        ///  Update user's wallet info
        /// </summary>
        /// <param name="wallet">Wallet address</param>
        /// <returns></returns>
        [HttpGet]
        [Route("WalletUpdate")]
        [AuthorizeDbUser]

        public JsonResult WalletUpdate(string walletAddress)
        {

            try
            {
                //Get user
                UserDto modeluser = Helpers.Serializers.DeserializeJson<UserDto>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetId?id=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token")));

                if (modeluser != null && modeluser.UserId > 0)
                {
                    modeluser.WalletAddress = walletAddress;

                    //Update user  
                    var updatemodel = Helpers.Serializers.DeserializeJson<UserDto>(Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/Users/Update", Helpers.Serializers.SerializeJson(modeluser), HttpContext.Session.GetString("Token")));

                    if (updatemodel != null && updatemodel.UserId > 0)
                    {
                        TempData["toastr-message"] = "Save changes successful.";
                        TempData["toastr-type"] = "success";

                        HttpContext.Session.SetString("WalletAddress", walletAddress);

                        Program.monitizer.AddUserLog(modeluser.UserId, UserLogType.Request, "User updated the wallet address. OldValue:" + modeluser.WalletAddress + " || New Value:" + walletAddress, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                        UserChainProfile chainProfile = null;

                        if (Program._settings.DaoBlockchain == Blockchain.Casper)
                        {
                            chainProfile = new CasperChainController().GetUserChainProfile(walletAddress);
                        }

                        if (chainProfile != null)
                        {
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
                            HttpContext.Session.SetString("Reputation", chainProfile.Reputation.ToString());
                        }

                        return Json(new SimpleResponse { Success = true, Message = "Save changes successful." });
                    }
                }

                return Json(new SimpleResponse { Success = false, Message = "Profile Updated Failed." });
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        }

        /// <summary>
        ///  Resize uploaded profile image
        /// </summary>
        /// <param name="data">Image as byte array</param>
        /// <param name="w">Expected width</param>
        /// <param name="h">Expected height</param>
        /// <returns></returns>
        public static string ResizeImage(byte[] data, double w, double h)
        {
            using (var ms = new MemoryStream(data))
            {
                var image = Image.FromStream(ms);

                var ratioX = (double)w / image.Width;
                var ratioY = (double)h / image.Height;
                var ratio = Math.Min(ratioX, ratioY);

                var width = (int)(image.Width * ratio);
                var height = (int)(image.Height * ratio);

                var newImage = new Bitmap(width, height);
                Graphics.FromImage(newImage).DrawImage(image, 0, 0, width, height);
                Bitmap bmp = new Bitmap(newImage);

                System.IO.MemoryStream ms2 = new MemoryStream();
                bmp.Save(ms2, ImageFormat.Jpeg);
                byte[] byteImage = ms2.ToArray();

                return Convert.ToBase64String(byteImage);
            }
        }

        /// <summary>
        /// User KYC Verification Page
        /// </summary>
        /// <returns></returns>

        [Route("KYC-Verification")]
        [AuthorizeDbUser]
        public IActionResult KYC_Verification()
        {
            KYCViewModel model = new KYCViewModel();

            try
            {
                model.Countries = Helpers.Serializers.DeserializeJson<List<KYCCountries>>(Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Identity/Kyc/GetKycCountries", "", HttpContext.Session.GetString("Token")));

                if (model.Countries == null)
                    model.Countries = new List<KYCCountries>();

                model.Status = Helpers.Serializers.DeserializeJson<UserKYCDto>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Identity/Kyc/GetKycStatus?id=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token")));

                if (model.Status == null)
                    model.Status = new UserKYCDto();

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError);
                return View(new KYCViewModel());
            }

            return View(model);
        }

        /// <summary>
        ///  Submits form data for the KYC verification
        /// </summary>
        /// <param>User information</param>
        /// <returns>Generic Simple Response class</returns>
        [Route("UploadKYCDoc")]
        [AuthorizeDbUser]
        public JsonResult UploadKYCDoc(KYCFileUpload File)
        {
            SimpleResponse model = new SimpleResponse();
            try
            {
                //Send files to Identity server          

                model = Helpers.Request.Upload(Program._settings.Service_ApiGateway_Url + "/Identity/Kyc/SubmitKYCFile?Type=" + File.Type + "&Name=" + File.Name + "&Surname=" + File.Surname + "&Dob=" + File.DoB + "&Email=" + File.Email + "&Country=" + File.Country + "&DocumentNumber=" + File.DocumentNumber + "&IssueDate=" + File.IssueDate + "&ExpiryDate=" + File.ExpiryDate + "&UserID=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token"), File.UploadedFile1, File.UploadedFile2);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError);
                return Json(new SimpleResponse());
            }
            return Json(model);
        }

        /// <summary>
        ///  Public user pay dos fee action
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("PayDosFee/{JobId}")]
        public JsonResult PayDosFee(int JobId)
        {
            SimpleResponse result = new SimpleResponse();

            try
            {
                //Get Model from ApiGateway          
                var url = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/GetId?id=" + JobId, HttpContext.Session.GetString("Token"));
                //Parse result
                var JobModel = Helpers.Serializers.DeserializeJson<JobPostDto>(url);
                //Set JobPost Model
                JobModel.Status = Helpers.Constants.Enums.JobStatusTypes.InternalAuction;

                //Update Model 
                JobModel = Helpers.Serializers.DeserializeJson<JobPostDto>(Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/Update", Helpers.Serializers.SerializeJson(JobModel), HttpContext.Session.GetString("Token")));

                //Set Auction model

                //Set auction end dates
                int InternalAuctionTime = Convert.ToInt32(Program._settings.DaoSettings.First(x => x.Key == "InternalAuctionTime").Value);
                int PublicAuctionTime = Convert.ToInt32(Program._settings.DaoSettings.First(x => x.Key == "PublicAuctionTime").Value);

                DateTime internalAuctionEndDate = DateTime.Now.AddMilliseconds(InternalAuctionTime);
                DateTime publicAuctionEndDate = DateTime.Now.AddMilliseconds(InternalAuctionTime + PublicAuctionTime);


                AuctionDto AuctionModel = new AuctionDto()
                {
                    JobID = JobId,
                    JobPosterUserId = JobModel.UserID,
                    CreateDate = DateTime.Now,
                    Status = AuctionStatusTypes.InternalBidding,
                    InternalAuctionEndDate = internalAuctionEndDate,
                    PublicAuctionEndDate = publicAuctionEndDate
                };

                //Check existing auction related with this job
                var AuctionModelByJobid = Helpers.Serializers.DeserializeJson<AuctionDto>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Auction/GetByJobId?jobid=" + JobId, HttpContext.Session.GetString("Token")));
                if (AuctionModelByJobid != null && AuctionModelByJobid.AuctionID > 0)
                {
                    result.Success = false;
                    result.Message = "There is an existing auction related with this job.";
                    result.Content = AuctionModel;

                    return Json(result);
                }

                //Post model to ApiGateway
                //Add new auction
                AuctionModel = Helpers.Serializers.DeserializeJson<AuctionDto>(Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Db/Auction/Post", Helpers.Serializers.SerializeJson(AuctionModel), HttpContext.Session.GetString("Token")));

                if (AuctionModel != null && AuctionModel.AuctionID > 0)
                {
                    result.Success = true;
                    result.Message = "DoS fee successfully paid. Internal auction process started for the job.";
                    result.Content = AuctionModel;

                    Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "User paid DoS fee. Job # " + JobModel.JobID, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                }

                return Json(result);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        }

        #endregion

        #region Payment History

        /// <summary>
        ///  Payment History view
        /// </summary>
        /// <returns></returns>
        [Route("Payment-History")]
        public IActionResult Payment_History()
        {
            ViewBag.Title = "Payment History";

            PaymentHistoryViewModel model = new PaymentHistoryViewModel();

            try
            {
                //Get payment history data from ApiGateway
                string paymentsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Website/PaymentHistoryByUserId?userid=" + HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("Token"));
                //Parse response
                model = Helpers.Serializers.DeserializeJson<PaymentHistoryViewModel>(paymentsJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return View(model);
        }


        /// <summary>
        ///  All Payment History view
        /// </summary>
        /// <returns></returns>
        [Route("Payment-History-Admin")]
        public IActionResult Payment_History_Admin()
        {
            ViewBag.Title = "Payment History";

            PaymentHistoryViewModel model = new PaymentHistoryViewModel();

            try
            {
                //Get payment history data from ApiGateway
                string paymentsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Website/AllPaymentHistory", HttpContext.Session.GetString("Token"));
                //Parse response
                model = Helpers.Serializers.DeserializeJson<PaymentHistoryViewModel>(paymentsJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return View(model);
        }


        /// <summary>
        ///  Exports payment history to csv
        /// </summary>
        /// <returns></returns>
        public async Task<FileResult> ExportPaymentHistoryCsv(DateTime? start, DateTime? end)
        {
            try
            {
                //Get payment history data from ApiGateway
                string url = Program._settings.Service_ApiGateway_Url + "/Db/PaymentHistory/ExportPaymentHistoryByDate?userid=" + HttpContext.Session.GetInt32("UserID") + "&start=" + start.ToString() + "&end=" + end.ToString();
                //Get all users payments if user type is admin
                if (HttpContext.Session.GetString("UserType") == Enums.UserIdentityType.Admin.ToString())
                {
                    url = Program._settings.Service_ApiGateway_Url + "/Db/PaymentHistory/ExportPaymentHistoryByDate?start=" + start.ToString() + "&end=" + end.ToString();
                }
                string paymentsJson = Helpers.Request.Get(url, HttpContext.Session.GetString("Token"));
                //Parse response
                List<PaymentExport> model = Helpers.Serializers.DeserializeJson<List<PaymentExport>>(paymentsJson);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("JobID;Job Name;Job Post Date;Payment Date;Job Poster;Job Doer;Bid Price;Payment User;Payment Amount");
                foreach (var item in model.OrderByDescending(x => x.paymentHistory.CreateDate))
                {
                    sb.AppendLine(item.job.JobID + ";" + item.job.Title + ";" + item.job.CreateDate + ";" + item.paymentHistory.CreateDate + ";" + item.JobPosterUsername + ";" + item.JobDoerUsername + ";" + item.winnerBid.Price + ";" + item.PaymentUsername + ";" + item.paymentHistory.Amount);
                }

                byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString()); ;

                return File(fileBytes, "text/csv", "CRDAO Payment History.csv");
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return File(new List<byte>().ToArray(), "text/csv", "CRDAO Payment History.csv");
        }


        #endregion

        #region Admin Views & Methods

        /// <summary>
        ///  Approves job with "AdminApprovalPending" status
        /// </summary>
        /// <param name="JobId"></param>
        /// <returns></returns>
        [AuthorizeAdmin]
        [HttpGet]
        public JsonResult AdminJobApproval(int JobId)
        {
            SimpleResponse result = ApproveJob(JobId);

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        }

        public SimpleResponse ApproveJob(int JobId)
        {
            SimpleResponse result = new SimpleResponse();

            try
            {
                //Get Model from ApiGateway          
                var jobJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/GetId?id=" + JobId, HttpContext.Session.GetString("Token"));
                //Parse result
                var JobModel = Helpers.Serializers.DeserializeJson<JobPostDto>(jobJson);

                //Get job poster user object 
                var userJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetId?id=" + JobModel.UserID, HttpContext.Session.GetString("Token"));
                //Parse result
                var userModel = Helpers.Serializers.DeserializeJson<UserDto>(userJson);

                ////If job poster is admin or VA start auction immediately
                //if (userModel.UserType == UserIdentityType.VotingAssociate.ToString() || userModel.UserType == UserIdentityType.Admin.ToString())
                //{
                //Set JobPost Model
                JobModel.Status = Helpers.Constants.Enums.JobStatusTypes.InternalAuction;

                //Set Auction model

                //Set auction end dates
                int InternalAuctionTime = Convert.ToInt32(Program._settings.DaoSettings.First(x => x.Key == "InternalAuctionTime").Value);
                int PublicAuctionTime = Convert.ToInt32(Program._settings.DaoSettings.First(x => x.Key == "PublicAuctionTime").Value);

                DateTime internalAuctionEndDate = DateTime.Now.AddMilliseconds(InternalAuctionTime);
                DateTime publicAuctionEndDate = DateTime.Now.AddMilliseconds(InternalAuctionTime + PublicAuctionTime);

                AuctionDto AuctionModel = new AuctionDto()
                {
                    JobID = JobId,
                    JobPosterUserId = JobModel.UserID,
                    CreateDate = DateTime.Now,
                    Status = AuctionStatusTypes.InternalBidding,
                    InternalAuctionEndDate = internalAuctionEndDate,
                    PublicAuctionEndDate = publicAuctionEndDate
                };

                //Check existing auction related with this job
                var AuctionModelByJobid = Helpers.Serializers.DeserializeJson<AuctionDto>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Auction/GetByJobId?jobid=" + JobId, HttpContext.Session.GetString("Token")));
                if (AuctionModelByJobid != null && AuctionModelByJobid.AuctionID > 0)
                {
                    result.Success = false;
                    result.Message = "There is an existing auction related with this job.";
                    result.Content = AuctionModel;

                    return result;
                }

                //Post model to ApiGateway
                //Add new auction
                AuctionModel = Helpers.Serializers.DeserializeJson<AuctionDto>(Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Db/Auction/Post", Helpers.Serializers.SerializeJson(AuctionModel), HttpContext.Session.GetString("Token")));

                if (AuctionModel != null && AuctionModel.AuctionID > 0)
                {
                    result.Success = true;
                    result.Message = "Job approved. Internal auction process started for the job.";
                    result.Content = AuctionModel;
                }
                //}
                ////If job poster is not admin or VA check for KYC
                //else
                //{
                //    if (userModel.KYCStatus == false)
                //    {
                //        JobModel.Status = Helpers.Constants.Enums.JobStatusTypes.KYCPending;
                //        result.Success = true;
                //        result.Message = "Job approved. User needs to complete KYC.";
                //    }
                //    else
                //    {
                //        JobModel.Status = Helpers.Constants.Enums.JobStatusTypes.DoSFeePending;
                //        result.Success = true;
                //        result.Message = "Job approved. User needs to pay DoS fee.";
                //    }
                //}

                //Update job status 
                JobModel = Helpers.Serializers.DeserializeJson<JobPostDto>(Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/Update", Helpers.Serializers.SerializeJson(JobModel), HttpContext.Session.GetString("Token")));

                Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "Admin approved the job.Job #" + JobId, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                //Send notification email to job poster

                //Set email title and content
                string emailTitle = "Your job is approved by system administrator (Job #" + JobId + ")";
                string emailContent = "";

                if (JobModel.Status == Enums.JobStatusTypes.InternalAuction)
                {
                    emailContent = "Greetings, " + userModel.NameSurname.Split(' ')[0] + ", <br><br> Your job is approved by system administrator<br><br> Internal auction process started for the job.";
                }
                else if (JobModel.Status == Enums.JobStatusTypes.KYCPending)
                {
                    emailContent = "Greetings, " + userModel.NameSurname.Split(' ')[0] + ", <br><br> Your job is approved by system administrator<br><br> You have to complete KYC before auction phase. Please complete your KYC from job detail.";
                }
                else if (JobModel.Status == Enums.JobStatusTypes.DoSFeePending)
                {
                    emailContent = "Greetings, " + userModel.NameSurname.Split(' ')[0] + ", <br><br> Your job is approved by system administrator<br><br> Please pay the Dos fee to start internal auction process.";
                }

                SendEmailModel emailModel = new SendEmailModel() { Subject = emailTitle, Content = emailContent, To = new List<string> { userModel.Email } };
                Program.rabbitMq.Publish(Helpers.Constants.FeedNames.NotificationFeed, "email", Helpers.Serializers.Serialize(emailModel));

                //Set server side toastr because page will be redirected
                TempData["toastr-message"] = result.Message;
                TempData["toastr-type"] = "success";

                return result;

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return result;

        }

        /// <summary>
        ///  Disapproves job with "AdminApprovalPending" status
        /// </summary>
        /// <param name="JobId"></param>
        /// <returns></returns>
        [AuthorizeAdmin]
        [HttpGet]
        public JsonResult AdminJobDisapproval(int JobId)
        {
            SimpleResponse result = new SimpleResponse();

            try
            {
                //Get Model from ApiGateway          
                var jobJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/GetId?id=" + JobId, HttpContext.Session.GetString("Token"));
                //Parse result
                var JobModel = Helpers.Serializers.DeserializeJson<JobPostDto>(jobJson);

                //Get job poster user object 
                var userJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetId?id=" + JobModel.UserID, HttpContext.Session.GetString("Token"));
                //Parse result
                var userModel = Helpers.Serializers.DeserializeJson<UserDto>(userJson);

                //Set JobPost Model
                JobModel.Status = Helpers.Constants.Enums.JobStatusTypes.Rejected;

                //Update job status 
                JobModel = Helpers.Serializers.DeserializeJson<JobPostDto>(Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/Update", Helpers.Serializers.SerializeJson(JobModel), HttpContext.Session.GetString("Token")));
                if (JobModel.JobID > 0 && JobModel.Status == JobStatusTypes.Rejected)
                {
                    result.Success = true;
                    result.Message = "Job disapproved.";
                }

                //Send notification email to job poster

                //Set email title and content
                string emailTitle = "Your job is disapproved by system administrator (Job #" + JobId + ")";
                string emailContent = "Greetings, " + userModel.NameSurname.Split(' ')[0] + ", <br><br> Your job is disapproved by system administrator.<br><br> Please read the job posting rules and contact with system administrator.";

                SendEmailModel emailModel = new SendEmailModel() { Subject = emailTitle, Content = emailContent, To = new List<string> { userModel.Email } };
                Program.rabbitMq.Publish(Helpers.Constants.FeedNames.NotificationFeed, "email", Helpers.Serializers.Serialize(emailModel));


                Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "Admin disapproved the job.Job #" + JobId, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

                //Set server side toastr because page will be redirected
                TempData["toastr-message"] = result.Message;
                TempData["toastr-type"] = "success";

                return Json(result);

            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        }

        /// <summary>
        ///  DAO Variables save changes
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [AuthorizeAdmin]
        [Route("DaoVariablesPost")]
        public JsonResult DaoVariablesPost(DaoSettingDto model)
        {
            SimpleResponse result = new SimpleResponse();

            try
            {
                model.LastModified = DateTime.Now;

                //Post model to ApiGateway
                string jsonResult = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Db/DaoSetting/PostOrUpdate", Helpers.Serializers.SerializeJson(model), HttpContext.Session.GetString("Token"));
                //Parse result
                DaoSettingDto resultParsed = Helpers.Serializers.DeserializeJson<DaoSettingDto>(jsonResult);

                if (resultParsed.DaoSettingID > 0)
                {
                    result.Success = true;
                    result.Message = "DAO setting changed successfully.";

                    //Set server side toastr because page will be redirected
                    TempData["toastr-message"] = result.Message;
                    TempData["toastr-type"] = "success";

                    Startup.LoadDaoSettings(null, null);
                }
                else
                {
                    result.Success = false;
                    result.Message = "Error occured while changing DAO variables.";
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        }

        /// <summary>
        ///  This view shows users of the DAO
        /// </summary>
        /// <returns></returns>
        [Route("Users-List")]
        [Route("Home/Users-List")]
        [AuthorizeAdmin]
        public IActionResult Users_List(int page = 1, int pageCount = 20)
        {
            ViewBag.Title = "Users List";

            IPagedList<UserDto> pagedModel = new PagedList<UserDto>(null, 1, 1);

            try
            {
                //Get users data from ApiGateway
                string usersJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/GetPaged?page=" + page + "&pageCount=" + pageCount, HttpContext.Session.GetString("Token"));
                //Parse response
                var jobsListPaged = Helpers.Serializers.DeserializeJson<PaginationEntity<UserDto>>(usersJson);

                pagedModel = new StaticPagedList<UserDto>(
                    jobsListPaged.Items,
                    jobsListPaged.MetaData.PageNumber,
                    jobsListPaged.MetaData.PageSize,
                    jobsListPaged.MetaData.TotalItemCount
                    );
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }
            return View(pagedModel);
        }

        /// <summary>
        ///  Finds user info from query
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AuthorizeAdmin]
        public JsonResult UserSearch(string searchText)
        {
            List<UserDto> userList = new List<UserDto>();
            try
            {
                string usersJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Users/UserSearch?query=" + searchText, HttpContext.Session.GetString("Token"));
                //Parse response
                userList = Helpers.Serializers.DeserializeJson<List<UserDto>>(usersJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(userList);
        }

        /// <summary>
        ///  This view shows reputation logs of the DAO
        /// </summary>
        /// <returns></returns>
        [Route("Reputation-Logs")]
        [Route("Home/Reputation-Logs")]
        [AuthorizeAdmin]
        public IActionResult Reputation_Logs(int page = 1, int pageCount = 20)
        {
            ViewBag.Title = "Reputation Logs";

            IPagedList<ReputationLogsDto> pagedModel = new PagedList<ReputationLogsDto>(null, 1, 1);

            try
            {
                //Get jobs data from ApiGateway
                string usersJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Reputation/UserReputationHistory/GetPaged?page=" + page + "&pageCount=" + pageCount, HttpContext.Session.GetString("Token"));
                //Parse response
                var jobsListPaged = Helpers.Serializers.DeserializeJson<PaginationEntity<ReputationLogsDto>>(usersJson);

                pagedModel = new StaticPagedList<ReputationLogsDto>(
                    jobsListPaged.Items,
                    jobsListPaged.MetaData.PageNumber,
                    jobsListPaged.MetaData.PageSize,
                    jobsListPaged.MetaData.TotalItemCount
                    );
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }
            return View(pagedModel);
        }

        /// <summary>
        ///  Finds user reputations from user id
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AuthorizeAdmin]
        public JsonResult ReputationSearch(string searchText)
        {
            List<ReputationLogsDto> repList = new List<ReputationLogsDto>();
            try
            {
                string repJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Reputation/UserReputationHistory/GetByUserId?userid=" + searchText, HttpContext.Session.GetString("Token"));
                //Parse response
                repList = Helpers.Serializers.DeserializeJson<List<ReputationLogsDto>>(repJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(repList);
        }

        /// <summary>
        ///  This view shows application logs of the DAO
        /// </summary>
        /// <returns></returns>
        [Route("Application-Logs")]
        [Route("Home/Application-Logs")]
        [AuthorizeAdmin]
        public IActionResult Application_Logs(int page = 1, int pageCount = 20)
        {
            ViewBag.Title = "Application Logs";

            IPagedList<Helpers.Models.DtoModels.LogDbDto.ApplicationLogDto> pagedModel = new PagedList<Helpers.Models.DtoModels.LogDbDto.ApplicationLogDto>(null, 1, 1);

            try
            {
                //Get jobs data from ApiGateway
                string usersJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Log/ApplicationLog/GetPaged?page=" + page + "&pageCount=" + pageCount, HttpContext.Session.GetString("Token"));
                //Parse response
                var jobsListPaged = Helpers.Serializers.DeserializeJson<PaginationEntity<Helpers.Models.DtoModels.LogDbDto.ApplicationLogDto>>(usersJson);

                pagedModel = new StaticPagedList<Helpers.Models.DtoModels.LogDbDto.ApplicationLogDto>(
                    jobsListPaged.Items,
                    jobsListPaged.MetaData.PageNumber,
                    jobsListPaged.MetaData.PageSize,
                    jobsListPaged.MetaData.TotalItemCount
                    );
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }
            return View(pagedModel);
        }

        /// <summary>
        ///  Approves job with "AdminApprovalPending" status
        /// </summary>
        /// <param name="JobId"></param>
        /// <returns></returns>
        //[AuthorizeAdmin]
        //[HttpGet]
        //public JsonResult RestartVoting(int votingid)
        //{
        //    SimpleResponse result = new SimpleResponse();

        //    try
        //    {
        //        //Get response from ApiGateway
        //        string jsonResponse = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Voting/Voting/RestartVoting?votingid=" + votingid, HttpContext.Session.GetString("Token"));
        //        result = Helpers.Serializers.DeserializeJson<SimpleResponse>(jsonResponse);
        //        result.Content = null;

        //        //Change job status
        //        VotingDto voteModel = Helpers.Serializers.DeserializeJson<VotingDto>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Voting/Voting/GetId?id=" + votingid, HttpContext.Session.GetString("Token")));
        //        JobPostDto jobModel = Helpers.Serializers.DeserializeJson<JobPostDto>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/GetId?id=" + voteModel.JobID, HttpContext.Session.GetString("Token")));
        //        jobModel.Status = JobStatusTypes.InformalVoting;
        //        jobModel = Helpers.Serializers.DeserializeJson<JobPostDto>(Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/Update", Helpers.Serializers.SerializeJson(jobModel), HttpContext.Session.GetString("Token")));

        //        //Set server side toastr because page will be redirected                                
        //        TempData["toastr-message"] = result.Message;
        //        TempData["toastr-type"] = "success";

        //        Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "Voting restarted by admin user. Voting #" + votingid, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

        //        return Json(result);

        //    }
        //    catch (Exception ex)
        //    {
        //        Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
        //    }

        //    return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        //}

        /// <summary>
        ///  Approves job with "AdminApprovalPending" status
        /// </summary>
        /// <param name="JobId"></param>
        /// <returns></returns>
        //[AuthorizeAdmin]
        //[HttpGet]
        //public JsonResult RestartAuction(int auctionid)
        //{
        //    SimpleResponse result = new SimpleResponse();

        //    try
        //    {
        //        //Get auction model from ApiGateway
        //        var auctionJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Auction/GetId?id=" + auctionid, HttpContext.Session.GetString("Token"));
        //        //Parse response
        //        var auction = Helpers.Serializers.DeserializeJson<AuctionDto>(auctionJson);

        //        if (auction.Status != AuctionStatusTypes.Expired)
        //        {
        //            result.Message = "Only expired auctions can be restarted";
        //            result.Success = false;
        //        }

        //        auction.Status = AuctionStatusTypes.InternalBidding;

        //        //Set auction end dates
        //        int InternalAuctionTime = Convert.ToInt32(Program._settings.DaoSettings.First(x => x.Key == "InternalAuctionTime").Value);
        //        int PublicAuctionTime = Convert.ToInt32(Program._settings.DaoSettings.First(x => x.Key == "PublicAuctionTime").Value);

        //        DateTime internalAuctionEndDate = DateTime.Now.AddSeconds(InternalAuctionTime);
        //        DateTime publicAuctionEndDate = DateTime.Now.AddSeconds(InternalAuctionTime + PublicAuctionTime);

        //        auction.InternalAuctionEndDate = internalAuctionEndDate;
        //        auction.PublicAuctionEndDate = publicAuctionEndDate;

        //        var auctionUpdateJson = Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/Auction/Update", Helpers.Serializers.SerializeJson(auction), HttpContext.Session.GetString("Token"));
        //        auction = Helpers.Serializers.DeserializeJson<AuctionDto>(auctionUpdateJson);

        //        if (auction.AuctionID > 0)
        //        {
        //            result.Message = "Auction restarted succesfully.";
        //            result.Success = true;

        //            //Change job status
        //            JobPostDto jobModel = Helpers.Serializers.DeserializeJson<JobPostDto>(Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/GetId?id=" + auction.JobID, HttpContext.Session.GetString("Token")));
        //            jobModel.Status = JobStatusTypes.InternalAuction;
        //            jobModel = Helpers.Serializers.DeserializeJson<JobPostDto>(Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/Db/JobPost/Update", Helpers.Serializers.SerializeJson(jobModel), HttpContext.Session.GetString("Token")));
        //        }

        //        //Set server side toastr because page will be redirected
        //        TempData["toastr-message"] = result.Message;
        //        TempData["toastr-type"] = "success";

        //        Program.monitizer.AddUserLog(Convert.ToInt32(HttpContext.Session.GetInt32("UserID")), Helpers.Constants.Enums.UserLogType.Request, "Auction restarted by admin user. Auction #" + auctionid, Utility.IpHelper.GetClientIpAddress(HttpContext), Utility.IpHelper.GetClientPort(HttpContext));

        //        return Json(result);

        //    }
        //    catch (Exception ex)
        //    {
        //        Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
        //    }

        //    return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        //}

        [HttpPost]
        [AuthorizeAdmin]
        public JsonResult ChangePaymentStatusMulti(List<int> idList)
        {
            SimpleResponse result = new SimpleResponse();

            try
            {
                //Post model to ApiGateway
                string jsonResult = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/Db/PaymentHistory/ChangeStatusMulti?status=" + Enums.PaymentType.Completed, Helpers.Serializers.SerializeJson(idList), HttpContext.Session.GetString("Token"));
                //Parse result
                List<PaymentHistoryDto> resultParsed = Helpers.Serializers.DeserializeJson<List<PaymentHistoryDto>>(jsonResult);

                if (resultParsed != null)
                {
                    result.Success = true;
                    result.Message = "Status changed successfully.";

                    //Set server side toastr because page will be redirected
                    TempData["toastr-message"] = result.Message;
                    TempData["toastr-type"] = "success";

                    Startup.LoadDaoSettings(null, null);
                }
                else
                {
                    result.Success = false;
                    result.Message = "Error occured while changing Status variables.";

                    TempData["toastr-message"] = result.Message;
                    TempData["toastr-type"] = "warning";
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return Json(new SimpleResponse { Success = false, Message = Lang.ErrorNote });
        }

        /// <summary>
        ///  Export completed jobs as CSV
        /// </summary>
        /// <returns></returns>
        [AuthorizeAdmin]
        public IActionResult ExportJobs()
        {
            try
            {
                //Get payment history data from ApiGateway
                string url = Program._settings.Service_ApiGateway_Url + "/Db/Website/ExportCompletedJobs";
                string jobsCsv = Helpers.Request.Get(url, HttpContext.Session.GetString("Token"));

                byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(jobsCsv);

                return File(fileBytes, "text/csv", "CRDAO Completed.csv");
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return File(new List<byte>().ToArray(), "text/csv", "CRDAO Completed Jobs.csv");
        }

        #endregion

        #region  VA Directory
        /// <summary>
        ///  VA Directory view
        /// </summary>
        /// <returns></returns>
        [Route("VA-Directory")]
        public IActionResult VA_Directory()
        {
            ViewBag.Title = "VA Directory";

            List<VADirectoryViewModel> model = new List<VADirectoryViewModel>();

            try
            {
                //Get VA Directory data from ApiGateway
                string paymentsJson = Helpers.Request.Get(Program._settings.Service_ApiGateway_Url + "/Db/Website/GetVADirectory", HttpContext.Session.GetString("Token"));
                //Parse response
                model = Helpers.Serializers.DeserializeJson<List<VADirectoryViewModel>>(paymentsJson);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return View(model);
        }
        #endregion

        #region DAO Variables
        /// <summary>
        ///  This view shows global parameters of the DAO
        /// </summary>
        /// <returns></returns>
        [Route("Dao-Variables")]
        public IActionResult Dao_Variables()
        {
            ViewBag.Title = "DAO Variables";

            return View();
        }
        #endregion

        #region Utility

        public string CheckOnchainSignIn()
        {
            if (HttpContext.Session.GetInt32("UserID") == null || HttpContext.Session.GetInt32("UserID") <= 0)
            {
                return "Unauthorized";
            }

            return "";
        }

        public string CheckDbSignIn()
        {
            if (HttpContext.Session.GetInt32("UserID") == null || HttpContext.Session.GetInt32("UserID") <= 0)
            {
                return "Unauthorized";
            }

            return "";
        }

        public List<CandleStick> GetCandleSticks(string symbol)
        {
            List<CandleStick> res = new List<CandleStick>();

            try
            {
                //Get candle data from GateIO Api
                var jsonResult = Helpers.Request.Get("https://data.gateapi.io/api2/1/candlestick2/" + symbol + "?group_sec=3600&range_hour=72");
                dynamic result = JsonConvert.DeserializeObject(jsonResult);
                foreach (dynamic item in result.data)
                {
                    res.Add(new CandleStick() { Date = item[0], Volume = item[1], Open = item[2], High = item[3], Low = item[4], Close = item[5] });
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return res;
        }

        [HttpGet]
        public JsonResult SetCookie(string src)
        {
            CookieOptions cookies = new CookieOptions();
            cookies.Expires = DateTime.Now.AddDays(90);
            Response.Cookies.Append("theme", src, cookies);
            return Json("");
        }

        [HttpGet]
        public ActionResult GetProfileImage()
        {
            try
            {
                if (!string.IsNullOrEmpty(HttpContext.Session.GetString("ProfileImage")))
                {
                    string image = HttpContext.Session.GetString("ProfileImage");

                    //User's profile image is one of the stock images
                    if (image.Length < 50)
                    {
                        byte[] img = System.IO.File.ReadAllBytes("./wwwroot/Home/images/avatars/" + image);
                        return this.File(img, "image/png", "image.png");
                    }
                    //User's profile image is custom uploaded image
                    else
                    {
                        var arr = Convert.FromBase64String(image);
                        return this.File(arr, "image/png", "image.png");
                    }
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            //Return default profile image
            byte[] defaultImage = System.IO.File.ReadAllBytes("./wwwroot/Home/images/avatars/default.png");
            return this.File(defaultImage, "image/png", "image.png");
        }
        #endregion

        #region Casper Chain Action Methods

        public ChainActionDto CreateChainActionRecord(string signedDeployJson, string walletAddress, ChainActionTypes voteType)
        {
            ChainActionDto chainAction = new ChainActionDto();

            Program.monitizer.AddApplicationLog(LogTypes.ChainLog, "Sending Deploy: " + signedDeployJson);

            int userid = 0;
            if(HttpContext.Session.GetInt32("UserID") != null)
            {
                userid = Convert.ToInt32(HttpContext.Session.GetInt32("UserID"));
            }

            chainAction = new ChainActionDto() { ActionType = voteType.ToString(), CreateDate = DateTime.Now, WalletAddress = walletAddress, DeployJson = signedDeployJson, Status = Enums.ChainActionStatus.InProgress.ToString(), UserId = userid };
            var chainQuePostJson = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/ChainAction/Post", Helpers.Serializers.SerializeJson(chainAction));
            chainAction = Helpers.Serializers.DeserializeJson<ChainActionDto>(chainQuePostJson);
            if (chainAction != null && chainAction.ChainActionId > 0)
            {
                Program.chainQue.Add(chainAction);
            }

            return chainAction;
        }

        public ChainActionDto SendSignedDeploy(ChainActionDto chainAction)
        {
            var chainActionResult = Helpers.Request.Post(Program._settings.Service_ApiGateway_Url + "/CasperChainService/Contracts/SendSignedDeploy", Helpers.Serializers.SerializeJson(chainAction));
            var chainActionResultModel = Helpers.Serializers.DeserializeJson<ChainActionDto>(chainActionResult);

            if (chainActionResultModel != null && !string.IsNullOrEmpty(chainActionResultModel.Status))
            {
                chainAction = chainActionResultModel;
                var updateJson = Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/ChainAction/Update", Helpers.Serializers.SerializeJson(chainAction));
            }

            return chainAction;
        }

        public SimpleResponse ChainError(ChainActionDto chainAction, Exception? ex)
        {
            try
            {
                chainAction.Status = Enums.ChainActionStatus.Error.ToString();
                var updateJson = Helpers.Request.Put(Program._settings.Service_ApiGateway_Url + "/ChainAction/Update", Helpers.Serializers.SerializeJson(chainAction));

                if (ex != null)
                {
                    Program.monitizer.AddException(ex, LogTypes.ApplicationError, false);
                }
            }
            catch
            {

            }

            return new SimpleResponse { Success = false, Message = "An error occured while sending the deploy to the chain. Please check chain logs for details." };
        }

        #endregion

    }
}
