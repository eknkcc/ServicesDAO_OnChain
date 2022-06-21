using DAO_ReputationService.Contexts;
using DAO_ReputationService.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Helpers.Constants.Enums;
using static DAO_ReputationService.Mapping.AutoMapperBase;
using PagedList.Core;
using Helpers.Models.SharedModels;
using DAO_ReputationService.Mapping;
using Helpers.Constants;
using Helpers.Models.DtoModels.ReputationDbDto;

namespace DAO_ReputationService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserReputationStakeController : ControllerBase
    {

        [Route("Get")]
        [HttpGet]
        public IEnumerable<UserReputationStakeDto> Get()
        {
            List<UserReputationStake> model = new List<UserReputationStake>();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    model = db.UserReputationStakes.ToList();
                }
            }
            catch (Exception ex)
            {
                model = new List<UserReputationStake>();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<List<UserReputationStake>, List<UserReputationStakeDto>>(model).ToArray();
        }

        [Route("GetId")]
        [HttpGet]
        public UserReputationStakeDto GetId(int id)
        {
            UserReputationStake model = new UserReputationStake();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    model = db.UserReputationStakes.Find(id);
                }
            }
            catch (Exception ex)
            {
                model = new UserReputationStake();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<UserReputationStake, UserReputationStakeDto>(model);
        }

        [Route("Post")]
        [HttpPost]
        public UserReputationStakeDto Post([FromBody] UserReputationStakeDto model)
        {
            try
            {
                UserReputationStake item = _mapper.Map<UserReputationStakeDto, UserReputationStake>(model);
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    db.UserReputationStakes.Add(item);
                    db.SaveChanges();
                }
                return _mapper.Map<UserReputationStake, UserReputationStakeDto>(item);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return new UserReputationStakeDto();
            }
        }

        [Route("PostMultiple")]
        [HttpPost]
        public List<UserReputationStakeDto> PostMultiple([FromBody] List<UserReputationStakeDto> model)
        {
            try
            {
                List<UserReputationStake> item = _mapper.Map<List<UserReputationStakeDto>, List<UserReputationStake>>(model);
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    db.UserReputationStakes.AddRange(item);
                    db.SaveChanges();
                }
                return _mapper.Map<List<UserReputationStake>, List<UserReputationStakeDto>>(item);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return new List<UserReputationStakeDto>();
            }
        }

        [Route("Delete")]
        [HttpDelete]
        public bool Delete(int? ID)
        {
            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    UserReputationStake item = db.UserReputationStakes.FirstOrDefault(s => s.UserReputationStakeID == ID);
                    db.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
                    db.SaveChanges();
                }
                return true;
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return false;
            }
        }

        [Route("Update")]
        [HttpPut]
        public UserReputationStakeDto Update([FromBody] UserReputationStakeDto model)
        {
            try
            {
                UserReputationStake item = _mapper.Map<UserReputationStakeDto, UserReputationStake>(model);
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    db.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    db.SaveChanges();
                }
                Program.monitizer.AddUserLog(model.UserID,UserLogType.Request,"Reputation stake updated. User Reputaing Reputation #" + model.UserReputationStakeID);
                return _mapper.Map<UserReputationStake, UserReputationStakeDto>(item);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return new UserReputationStakeDto();
            }
        }

        [Route("GetPaged")]
        [HttpGet]
        public PaginationEntity<UserReputationStakeDto> GetPaged(int page = 1, int pageCount = 30)
        {
            PaginationEntity<UserReputationStakeDto> res = new PaginationEntity<UserReputationStakeDto>();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {

                    IPagedList<UserReputationStakeDto> lst = AutoMapperBase.ToMappedPagedList<UserReputationStake, UserReputationStakeDto>(db.UserReputationStakes.OrderByDescending(x => x.UserReputationStakeID).ToPagedList(page, pageCount));

                    res.Items = lst;
                    res.MetaData = new PaginationMetaData() { Count = lst.Count, FirstItemOnPage = lst.FirstItemOnPage, HasNextPage = lst.HasNextPage, HasPreviousPage = lst.HasPreviousPage, IsFirstPage = lst.IsFirstPage, IsLastPage = lst.IsLastPage, LastItemOnPage = lst.LastItemOnPage, PageCount = lst.PageCount, PageNumber = lst.PageNumber, PageSize = lst.PageSize, TotalItemCount = lst.TotalItemCount };



                    return res;
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return res;
        }

        [Route("GetByProcessId")]
        [HttpGet]
        public List<UserReputationStakeDto> GetByProcessId(int referenceProcessID, StakeType reftype)
        {
            List<UserReputationStake> model = new List<UserReputationStake>();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    //Stake for voting process
                    if (reftype == StakeType.Against || reftype == StakeType.For)
                    {
                        model = db.UserReputationStakes.Where(x => x.ReferenceProcessID == referenceProcessID && (x.Type == StakeType.Against || x.Type == StakeType.For)).ToList();
                    }
                    //Stake for auction process
                    else if (reftype == StakeType.Bid)
                    {
                        model = db.UserReputationStakes.Where(x => x.ReferenceProcessID == referenceProcessID && x.Type == StakeType.Bid).ToList();
                    }
                    //Stake for minting
                    else if (reftype == StakeType.Mint)
                    {
                        model = db.UserReputationStakes.Where(x => x.ReferenceProcessID == referenceProcessID && x.Type == StakeType.Mint).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                model = new List<UserReputationStake>();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<List<UserReputationStake>, List<UserReputationStakeDto>>(model).ToList();
        }

        [Route("GetByUserId")]
        [HttpGet]
        public List<UserReputationStakeDto> GetByUserId(int userid)
        {
            List<UserReputationStake> model = new List<UserReputationStake>();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    model = db.UserReputationStakes.Where(x => x.UserID == userid).ToList();
                }
            }
            catch (Exception ex)
            {
                model = new List<UserReputationStake>();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<List<UserReputationStake>, List<UserReputationStakeDto>>(model).ToList();
        }

        [Route("SubmitStake")]
        [HttpPost]
        public SimpleResponse SubmitStake([FromBody] UserReputationStake model)
        {
            SimpleResponse res = new SimpleResponse();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    //Check if user already staked reputation for same process
                    if (db.UserReputationStakes.Count(x => x.ReferenceProcessID == model.ReferenceProcessID && x.ReferenceID == model.ReferenceID && x.UserID == model.UserID && x.Status == ReputationStakeStatus.Staked) > 0)
                    {
                        return new SimpleResponse() { Success = false, Message = "User have already staked reputation for this process." };
                    }

                    //Check if user tries to submit negative stake
                    if (Math.Round(model.Amount, 5) <= 0)
                    {
                        return new SimpleResponse() { Success = false, Message = "Reputation stake must be greater than 0" };
                    }

                    //Get last user reputation record
                    UserReputationHistoryController cont = new UserReputationHistoryController();
                    UserReputationHistoryDto lastHst = cont.GetLastReputation(model.UserID);
                    
                    //Check if user have sufficient reputation unless it's a minting request
                    if (model.Type != StakeType.Mint && (lastHst == null || lastHst.LastUsableTotal < model.Amount))
                    {
                        return new SimpleResponse() { Success = false, Message = "User does not have sufficient reputation." };
                    }

                    //Add record to ReputationHistory
                    UserReputationHistoryDto repHst = new UserReputationHistoryDto();
                    repHst.Date = DateTime.Now;
                    repHst.EarnedAmount = 0;
                    repHst.StakedAmount = Math.Round(model.Amount, 5);
                    repHst.LostAmount = 0;
                    repHst.StakeReleasedAmount = 0;
                    repHst.LastStakedTotal = Math.Round(lastHst.LastStakedTotal + model.Amount, 5);
                    if(model.Type != StakeType.Mint)
                    {
                        repHst.LastUsableTotal = Math.Round(lastHst.LastUsableTotal - model.Amount, 5);
                    }
                    else
                    {
                        repHst.LastUsableTotal = Math.Round(lastHst.LastUsableTotal, 5);
                    }
                    repHst.LastTotal = Math.Round(lastHst.LastTotal, 5);
                    if(model.Type == StakeType.For || model.Type == StakeType.Against)
                    {
                        repHst.Title = "Vote Stake";
                        repHst.Explanation = "User staked reputation for voting process #" + model.ReferenceProcessID;
                    }
                    else if (model.Type == StakeType.Bid)
                    {
                        repHst.Title = "Bid Stake";
                        repHst.Explanation = "User staked reputation for auction process #" + model.ReferenceProcessID;
                    }
                    else if (model.Type == StakeType.Mint)
                    {
                        repHst.Title = "Minting";
                        repHst.Explanation = "User minted reputation for job #" + model.ReferenceProcessID;
                    }
                    repHst.UserID = model.UserID;
                    cont.Post(repHst);

                    model.CreateDate = DateTime.Now;
                    model.Status = ReputationStakeStatus.Staked;

                    db.UserReputationStakes.Add(model);
                    db.SaveChanges();

                    Program.monitizer.AddUserLog(model.UserID, UserLogType.Request, repHst.Explanation);

                    return new SimpleResponse() { Success = true, Message = "Stake successful." };
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return res;
        }

        /// <summary>
        ///  This method should be used in cases which staked reputation should be returned to the owners
        /// </summary>
        /// <param name="referenceID"></param>
        /// <returns></returns>
        [Route("ReleaseStakesByType")]
        [HttpGet]
        public SimpleResponse ReleaseStakesByType(int referenceID, StakeType reftype)
        {
            SimpleResponse res = new SimpleResponse();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    UserReputationStake stake = new UserReputationStake();
                    //Get staked reputations for voting (referenceID = VotingID)
                    if (reftype == StakeType.Against || reftype == StakeType.For)
                    {
                        stake = db.UserReputationStakes.FirstOrDefault(x => x.ReferenceID == referenceID && x.Status == ReputationStakeStatus.Staked && (x.Type == StakeType.Against || x.Type == StakeType.For));
                    }
                    //Get staked reputations for auction (referenceID = AuctionID)
                    else if (reftype == StakeType.Bid)
                    {
                        stake = db.UserReputationStakes.FirstOrDefault(x => x.ReferenceID == referenceID && x.Status == ReputationStakeStatus.Staked && x.Status == ReputationStakeStatus.Staked && x.Type == StakeType.Bid);
                    }
                    //Get minted pending reputations for the job (referenceID = JobId)
                    else if (reftype == StakeType.Mint)
                    {
                        stake = db.UserReputationStakes.FirstOrDefault(x => x.ReferenceID == referenceID && x.Status == ReputationStakeStatus.Staked && x.Status == ReputationStakeStatus.Staked && x.Type == StakeType.Mint);
                    }


                    stake.Status = ReputationStakeStatus.Released;
                    db.Entry(stake).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    db.SaveChanges();

                    //Get last user reputation record
                    UserReputationHistoryController cont = new UserReputationHistoryController();
                    UserReputationHistoryDto lastReputationHistory = cont.GetLastReputation(stake.UserID);

                    UserReputationHistory historyItem = new UserReputationHistory();
                    historyItem.Date = DateTime.Now;
                    historyItem.UserID = stake.UserID;
                    historyItem.EarnedAmount = 0;
                    historyItem.LostAmount = 0;
                    historyItem.StakedAmount = 0;
                    historyItem.StakeReleasedAmount = Math.Round(stake.Amount, 5);
                    historyItem.LastStakedTotal = Math.Round(lastReputationHistory.LastStakedTotal - stake.Amount, 5);
                    historyItem.LastTotal = Math.Round(lastReputationHistory.LastTotal, 5);

                    //If minted pending is released it won't affect usable total
                    if(reftype == StakeType.Mint)
                    {
                        historyItem.LastUsableTotal = Math.Round(lastReputationHistory.LastUsableTotal, 5);
                    }
                    else
                    {
                        historyItem.LastUsableTotal = Math.Round(lastReputationHistory.LastUsableTotal + stake.Amount, 5);
                    }

                    if (stake.Type == StakeType.For || stake.Type == StakeType.Against)
                    {
                        historyItem.Title = "Vote Stake Release";
                        historyItem.Explanation = "Staked reputation released for voting process #" + stake.ReferenceProcessID;
                    }
                    else if (stake.Type == StakeType.Bid)
                    {
                        historyItem.Title = "Bid Stake Release";
                        historyItem.Explanation = "Staked reputation released for auction process #" + stake.ReferenceProcessID;
                    }
                    else if (stake.Type == StakeType.Mint)
                    {
                        historyItem.Title = "Minting Stake Release";
                        historyItem.Explanation = "Staked minted reputation released for job #" + stake.ReferenceProcessID;
                    }
                    db.UserReputationHistories.Add(historyItem);
                    db.SaveChanges();

                    Program.monitizer.AddUserLog(stake.UserID,UserLogType.Request, historyItem.Explanation);
                    return new SimpleResponse() { Success = true, Message = "Release successful." };
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return res;
        }


        /// <summary>
        ///  This method should be used in cases which staked reputation should be returned to the owner
        /// </summary>
        /// <param name="referenceProcessID"></param>
        /// <returns></returns>
        [Route("ReleaseStakes")]
        [HttpGet]
        public SimpleResponse ReleaseStakes(int referenceProcessID, StakeType reftype)
        {
            SimpleResponse res = new SimpleResponse();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    List<UserReputationStake> stakes = new List<UserReputationStake>();
                    //Get staked reputations for voting
                    if (reftype == StakeType.Against || reftype == StakeType.For)
                    {
                        stakes = db.UserReputationStakes.Where(x => x.ReferenceProcessID == referenceProcessID && x.Status == ReputationStakeStatus.Staked && (x.Type == StakeType.Against || x.Type == StakeType.For)).ToList();
                    }
                    //Get staked reputations for auction
                    else if (reftype == StakeType.Bid)
                    {
                        stakes = db.UserReputationStakes.Where(x => x.ReferenceProcessID == referenceProcessID && x.Status == ReputationStakeStatus.Staked && x.Type == StakeType.Bid).ToList();
                    }   
                    //Get minted pending reputations for the job (referenceID = JobId)
                    else if (reftype == StakeType.Mint)
                    {
                        stakes = db.UserReputationStakes.Where(x => x.ReferenceProcessID == referenceProcessID && x.Status == ReputationStakeStatus.Staked && x.Type == StakeType.Mint).ToList();
                    }

                    foreach (var item in stakes)
                    {
                        ReleaseStakesByType(Convert.ToInt32(item.ReferenceID), item.Type);
                    }
                    Program.monitizer.AddApplicationLog(LogTypes.ApplicationLog, "Stakes released. referenceProcessID:"+ referenceProcessID);
                    return new SimpleResponse() { Success = true, Message = "Release successful." };
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return res;
        }

        /// <summary>
        ///  This method should be used in cases which staked reputation should be distributed according to voting results
        /// </summary>
        /// <param name="referenceProcessID"></param>
        /// <returns></returns>
        [Route("DistributeStakes")]
        [HttpGet]
        public SimpleResponse DistributeStakes(int votingId, int jobId, double policingRate)
        {
            SimpleResponse res = new SimpleResponse();

            UserReputationHistoryController cont = new UserReputationHistoryController();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    //Get all vote stakes of voting
                    var stakeList = db.UserReputationStakes.Where(x => x.ReferenceProcessID == votingId && x.Status == ReputationStakeStatus.Staked && (x.Type == StakeType.For || x.Type == StakeType.Against)).ToList();

                    //Get all reputations minted for the job
                    //Did not use "Staked" clause because when an expired voting is restarted minted stake is already released.
                    var mintList = db.UserReputationStakes.Where(x => x.ReferenceProcessID == jobId && x.Type == StakeType.Mint).ToList();


                    //Find winning side
                    Enums.StakeType winnerSide = Enums.StakeType.For;
                    double forReps = stakeList.Where(x => x.Type == Enums.StakeType.For).Sum(x => x.Amount);
                    double againstReps = stakeList.Where(x => x.Type == Enums.StakeType.Against).Sum(x => x.Amount);
                    if (againstReps > forReps)
                    {
                        winnerSide = Enums.StakeType.Against;
                    }

                    var winnersList = stakeList.Where(x => x.Type == winnerSide).ToList();
                    var losersList = stakeList.Where(x => x.Type != winnerSide).ToList();

                    double losingSideTotalStake = losersList.Sum(x => x.Amount);
                    double winnerSideTotalStake = winnersList.Sum(x => x.Amount);

                    //Distribute reputitation from votes
                    foreach (var stake in stakeList)
                    {
                        ReleaseStakesByType(Convert.ToInt32(stake.ReferenceID), stake.Type);

                        //User is in the winning side
                        if (winnersList.Count(x => x.UserID == stake.UserID) > 0)
                        {
                            double usersStakePerc = stake.Amount / winnerSideTotalStake;
                            double earnedReputation = losingSideTotalStake * usersStakePerc;

                            if(earnedReputation > 0)
                            {
                                //Get last user reputation record
                                UserReputationHistoryDto lastReputationHistory = cont.GetLastReputation(stake.UserID);

                                UserReputationHistory historyItem = new UserReputationHistory();
                                historyItem.Date = DateTime.Now;
                                historyItem.UserID = stake.UserID;
                                historyItem.EarnedAmount = Math.Round(earnedReputation, 5);
                                historyItem.LostAmount = 0;
                                historyItem.StakedAmount = 0;
                                historyItem.StakeReleasedAmount = 0;
                                historyItem.LastStakedTotal = Math.Round(lastReputationHistory.LastStakedTotal, 5);
                                historyItem.LastTotal = Math.Round(lastReputationHistory.LastTotal + earnedReputation, 5);
                                historyItem.LastUsableTotal = Math.Round(lastReputationHistory.LastUsableTotal + earnedReputation, 5);
                                historyItem.Title = "Reputation Earned";
                                historyItem.Explanation = "User earned reputation from voting process #" + votingId;
                                db.UserReputationHistories.Add(historyItem);
                            }

                        }
                        //User is in the losing side
                        else
                        {
                            //Get last user reputation record
                            UserReputationHistoryDto lastReputationHistory = cont.GetLastReputation(stake.UserID);

                            UserReputationHistory historyItem = new UserReputationHistory();
                            historyItem.Date = DateTime.Now;
                            historyItem.UserID = stake.UserID;
                            historyItem.EarnedAmount = 0;
                            historyItem.LostAmount = Math.Round(stake.Amount, 5);
                            historyItem.StakedAmount = 0;
                            historyItem.StakeReleasedAmount = 0;
                            historyItem.LastStakedTotal = Math.Round(lastReputationHistory.LastStakedTotal, 5);
                            historyItem.LastTotal = Math.Round(lastReputationHistory.LastTotal - stake.Amount, 5);
                            historyItem.LastUsableTotal = Math.Round(lastReputationHistory.LastUsableTotal - stake.Amount, 5);
                            historyItem.Explanation = "User lost reputation from voting process #" + votingId;
                            historyItem.Title = "Reputation Loss";

                            db.UserReputationHistories.Add(historyItem);
                        }

                        db.SaveChanges();
                    }

                    //Distribute or delete minted reputation according to voting result
                    foreach (var mintedStake in mintList)
                    {
                        ReleaseStakesByType(Convert.ToInt32(mintedStake.ReferenceID), mintedStake.Type);

                        //If voting result is FOR -> Job completed succesfully and minted reputations should be released and distributed
                        if(winnerSide == StakeType.For)
                        {
                            double votersEarnedFromMint = mintedStake.Amount * policingRate;
                            double jobDoerEarned = mintedStake.Amount - votersEarnedFromMint;

                            //Distribute job doers share
                            if (jobDoerEarned > 0)
                            {
                                //Get last user reputation record (ReferenceID = JobDoerUserID)
                                UserReputationHistoryDto lastReputationHistory = cont.GetLastReputation(Convert.ToInt32(mintedStake.UserID));

                                UserReputationHistory historyItem = new UserReputationHistory();
                                historyItem.Date = DateTime.Now;
                                historyItem.UserID = mintedStake.UserID;
                                historyItem.EarnedAmount = Math.Round(jobDoerEarned, 5);
                                historyItem.LostAmount = 0;
                                historyItem.StakedAmount = 0;
                                historyItem.StakeReleasedAmount = 0;
                                historyItem.LastStakedTotal = Math.Round(lastReputationHistory.LastStakedTotal, 5);
                                historyItem.LastTotal = Math.Round(lastReputationHistory.LastTotal + jobDoerEarned, 5);
                                historyItem.LastUsableTotal = Math.Round(lastReputationHistory.LastUsableTotal + jobDoerEarned, 5);
                                historyItem.Title = "Reputation Earned";
                                historyItem.Explanation = "User earned minted reputation from job #" + jobId;
                                db.UserReputationHistories.Add(historyItem);
                            }

                            //Distribute voters share
                            if (votersEarnedFromMint > 0)
                            {
                                foreach (var user in winnersList)
                                {
                                    double usersStakePerc = user.Amount / winnerSideTotalStake;
                                    double earnedReputation = votersEarnedFromMint * usersStakePerc;

                                    //Get last user reputation record
                                    UserReputationHistoryDto lastReputationHistory = cont.GetLastReputation(user.UserID);

                                    UserReputationHistory historyItem = new UserReputationHistory();
                                    historyItem.Date = DateTime.Now;
                                    historyItem.UserID = user.UserID;
                                    historyItem.EarnedAmount = Math.Round(earnedReputation, 5);
                                    historyItem.LostAmount = 0;
                                    historyItem.StakedAmount = 0;
                                    historyItem.StakeReleasedAmount = 0;
                                    historyItem.LastStakedTotal = Math.Round(lastReputationHistory.LastStakedTotal, 5);
                                    historyItem.LastTotal = Math.Round(lastReputationHistory.LastTotal + earnedReputation, 5);
                                    historyItem.LastUsableTotal = Math.Round(lastReputationHistory.LastUsableTotal + earnedReputation, 5);
                                    historyItem.Title = "Reputation Earned";
                                    historyItem.Explanation = "User earned minted repuatation from voting process #" + votingId;
                                    db.UserReputationHistories.Add(historyItem);
                                }
                            }
                        }

                        db.SaveChanges();
                    }
                    Program.monitizer.AddApplicationLog(LogTypes.ApplicationLog, "Stakes distributed. Job #:" + jobId);

                    return new SimpleResponse() { Success = true, Message = "Distribution successful." };
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return res;
        }
   
        [Route("DeleteByProcessId")]
        [HttpDelete]
        public bool DeleteByProcessId(int referenceProcessID, StakeType reftype)
        {
            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    List<UserReputationStake> query = new List<UserReputationStake>();

                    //Stake for voting process
                    if (reftype == StakeType.Against || reftype == StakeType.For)
                    {
                        query = db.UserReputationStakes.Where(x => x.ReferenceProcessID == referenceProcessID && (x.Type == StakeType.Against || x.Type == StakeType.For)).ToList();
                    }
                    //Stake for auction process
                    else if (reftype == StakeType.Bid)
                    {
                        query = db.UserReputationStakes.Where(x => x.ReferenceProcessID == referenceProcessID && x.Type == StakeType.Bid).ToList();
                    }
                    //Stake for minting
                    else if (reftype == StakeType.Mint)
                    {
                        query = db.UserReputationStakes.Where(x => x.ReferenceProcessID == referenceProcessID && x.Type == StakeType.Mint).ToList();
                    }

                    foreach (var item in query){
                        db.UserReputationStakes.Remove(item);
                    }

                    db.SaveChanges();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return false;
        }

   
    }
}
 