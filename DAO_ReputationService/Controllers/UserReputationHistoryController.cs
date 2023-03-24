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
using Helpers.Models.DtoModels.ReputationDbDto;
using Helpers;
using System.Reflection.Metadata;
using Helpers.Models.CasperServiceModels;

namespace DAO_ReputationService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserReputationHistoryController : Controller
    {
        [Route("Get")]
        [HttpGet]
        public IEnumerable<UserReputationHistoryDto> Get()
        {
            List<UserReputationHistory> model = new List<UserReputationHistory>();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    model = db.UserReputationHistories.ToList();
                }
            }
            catch (Exception ex)
            {
                model = new List<UserReputationHistory>();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<List<UserReputationHistory>, List<UserReputationHistoryDto>>(model).ToArray();
        }

        [Route("GetId")]
        [HttpGet]
        public UserReputationHistoryDto GetId(int id)
        {
            UserReputationHistory model = new UserReputationHistory();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    model = db.UserReputationHistories.Find(id);
                }
            }
            catch (Exception ex)
            {
                model = new UserReputationHistory();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<UserReputationHistory, UserReputationHistoryDto>(model);
        }

        [Route("Post")]
        [HttpPost]
        public UserReputationHistoryDto Post([FromBody] UserReputationHistoryDto model)
        {
            try
            {
                UserReputationHistory item = _mapper.Map<UserReputationHistoryDto, UserReputationHistory>(model);
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    db.UserReputationHistories.Add(item);
                    db.SaveChanges();
                }
                return _mapper.Map<UserReputationHistory, UserReputationHistoryDto>(item);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return new UserReputationHistoryDto();
            }
        }

        [Route("PostMultiple")]
        [HttpPost]
        public List<UserReputationHistoryDto> PostMultiple([FromBody] List<UserReputationHistoryDto> model)
        {
            try
            {
                List<UserReputationHistory> item = _mapper.Map<List<UserReputationHistoryDto>, List<UserReputationHistory>>(model);
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    db.UserReputationHistories.AddRange(item);
                    db.SaveChanges();
                }
                return _mapper.Map<List<UserReputationHistory>, List<UserReputationHistoryDto>>(item);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return new List<UserReputationHistoryDto>();
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
                    UserReputationHistory item = db.UserReputationHistories.FirstOrDefault(s => s.UserReputationHistoryID == ID);
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
        public UserReputationHistoryDto Update([FromBody] UserReputationHistoryDto model)
        {
            try
            {
                UserReputationHistory item = _mapper.Map<UserReputationHistoryDto, UserReputationHistory>(model);
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    db.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    db.SaveChanges();
                }
                return _mapper.Map<UserReputationHistory, UserReputationHistoryDto>(item);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return new UserReputationHistoryDto();
            }
        }

        [Route("GetPaged")]
        [HttpGet]
        public PaginationEntity<UserReputationHistoryDto> GetPaged(int page = 1, int pageCount = 30)
        {
            PaginationEntity<UserReputationHistoryDto> res = new PaginationEntity<UserReputationHistoryDto>();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {

                    IPagedList<UserReputationHistoryDto> lst = AutoMapperBase.ToMappedPagedList<UserReputationHistory, UserReputationHistoryDto>(db.UserReputationHistories.OrderByDescending(x => x.UserReputationHistoryID).ToPagedList(page, pageCount));

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

        [Route("UserReputationHistorySearch")]
        [HttpGet]
        public IEnumerable<UserReputationHistoryDto> UserReputationHistorySearch(string query)
        {
            List<UserReputationHistory> res = new List<UserReputationHistory>();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    res = db.UserReputationHistories.Where(x => x.Explanation.Contains(query)).ToList();
                }

            }
            catch (Exception ex)
            {
                res = new List<UserReputationHistory>();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }
            return _mapper.Map<List<UserReputationHistory>, List<UserReputationHistoryDto>>(res).ToArray();
        }

        [Route("Search")]
        [HttpGet]
        public PaginationEntity<UserReputationHistoryDto> Search(string query, int page = 1, int pageCount = 30)
        {
            PaginationEntity<UserReputationHistoryDto> res = new PaginationEntity<UserReputationHistoryDto>();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    IPagedList<UserReputationHistoryDto> lst = AutoMapperBase.ToMappedPagedList<UserReputationHistory, UserReputationHistoryDto>(db.UserReputationHistories.Where(x => x.Explanation.Contains(query)).ToPagedList(page, pageCount));

                    res.Items = lst;
                    res.MetaData = new PaginationMetaData() { Count = lst.Count, FirstItemOnPage = lst.FirstItemOnPage, HasNextPage = lst.HasNextPage, HasPreviousPage = lst.HasPreviousPage, IsFirstPage = lst.IsFirstPage, IsLastPage = lst.IsLastPage, LastItemOnPage = lst.LastItemOnPage, PageCount = lst.PageCount, PageNumber = lst.PageNumber, PageSize = lst.PageSize, TotalItemCount = lst.TotalItemCount };
                }

                return res;
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return res;
            }

        }

        [Route("GetByUserId")]
        [HttpGet]
        public IEnumerable<UserReputationHistoryDto> GetByUserId(int userid, string address)
        {
            List<UserReputationHistory> model = new List<UserReputationHistory>();

            try
            {
                if (Program._settings.DaoBlockchain != null)
                {
                    var reputationChanges = Serializers.DeserializeJson<PaginatedResponse<Helpers.Models.CasperServiceModels.AggregatedReputationChange>>(Helpers.Request.Get(Program._settings.Service_CasperChain_Url + "/CasperMiddleware/GetReputationChangesList?address=" + address + "&page=1&page_size=100&order_direction=desc"));

                    if (reputationChanges.error == null && reputationChanges.data != null && reputationChanges.data.Count > 0)
                    {
                        foreach (var chainRepChange in reputationChanges.data)
                        {
                            UserReputationHistory repChange = new UserReputationHistory();
                            repChange.UserID = userid;
                            repChange.Title = "";
                            if (chainRepChange.voting_id != null && chainRepChange.voting_id != 0)
                            {
                                repChange.Explanation = "Result of the vote: " + chainRepChange.voting_id;
                            }
                            else
                            {
                                repChange.Explanation = "-";
                            }
                            repChange.EarnedAmount = Convert.ToDouble(chainRepChange.earned_amount);
                            repChange.LostAmount = Convert.ToDouble(chainRepChange.lost_amount);
                            repChange.StakedAmount = Convert.ToDouble(chainRepChange.staked_amount);
                            repChange.StakeReleasedAmount = Convert.ToDouble(chainRepChange.released_amount);
                            repChange.Date = Convert.ToDateTime(chainRepChange.timestamp);
                            model.Add(repChange);
                        }
                    }
                    else
                    {
                        UserReputationHistory repChange = new UserReputationHistory();
                        repChange.UserID = userid;
                        repChange.Title = "";
                        repChange.Explanation = "-";
                        repChange.EarnedAmount = 0;
                        repChange.LostAmount = 0;
                        repChange.StakedAmount = 0;
                        repChange.StakeReleasedAmount = 0;
                        repChange.Date = new DateTime();
                        model.Add(repChange);
                    }

                    SuccessResponse<TotalReputation> totalRep = Serializers.DeserializeJson<SuccessResponse<TotalReputation>>(Helpers.Request.Get(Program._settings.Service_CasperChain_Url + "/CasperMiddleware/GetTotalReputation?address=" + address));
                    if(totalRep.error == null)
                    {
                        model.First().LastStakedTotal = Convert.ToDouble(totalRep.data.staked_amount);
                        model.First().LastUsableTotal = Convert.ToDouble(totalRep.data.available_amount);
                        model.First().LastTotal = Convert.ToDouble(totalRep.data.available_amount) + Convert.ToDouble(totalRep.data.staked_amount);
                    }

                }
                else
                {
                    using (dao_reputationserv_context db = new dao_reputationserv_context())
                    {
                        model = db.UserReputationHistories.Where(x => x.UserID == userid).OrderByDescending(x => x.UserReputationHistoryID).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                model = new List<UserReputationHistory>();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<List<UserReputationHistory>, List<UserReputationHistoryDto>>(model).ToArray();
        }

        [Route("GetLastReputation")]
        [HttpGet]
        public UserReputationHistoryDto GetLastReputation(int userid)
        {
            UserReputationHistory model = new UserReputationHistory();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    //If user does not have reputation history. Initialize history for the user
                    if (db.UserReputationHistories.Count(x => x.UserID == userid) == 0)
                    {
                        db.UserReputationHistories.Add(new UserReputationHistory() { Date = DateTime.Now, Title = "Initial Reputation", Explanation = "Initial reputation record of the user.", EarnedAmount = 0, LastStakedTotal = 0, LastTotal = 0, LastUsableTotal = 0, LostAmount = 0, StakedAmount = 0, StakeReleasedAmount = 0, UserID = userid });
                        db.SaveChanges();
                    }

                    model = db.UserReputationHistories.Where(x => x.UserID == userid).OrderByDescending(x => x.UserReputationHistoryID).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                model = new UserReputationHistory();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<UserReputationHistory, UserReputationHistoryDto>(model);
        }

        [Route("GetLastReputationByUserIds")]
        [HttpPost]
        public List<UserReputationHistoryDto> GetLastReputationByUserIds(List<int> userids)
        {
            List<UserReputationHistory> model = new List<UserReputationHistory>();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    foreach (var userid in userids)
                    {
                        model.Add(db.UserReputationHistories.OrderByDescending(x => x.UserReputationHistoryID).FirstOrDefault(x => x.UserID == userid));
                    }
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<List<UserReputationHistory>, List<UserReputationHistoryDto>>(model);
        }

        [Route("GetByUserIdDate")]
        [HttpGet]
        public IEnumerable<UserReputationHistoryDto> GetByUserIdDate(int? userid, DateTime? start, DateTime? end)
        {
            List<UserReputationHistory> model = new List<UserReputationHistory>();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    model = db.UserReputationHistories.Where(x =>
                    (userid == null || x.UserID == userid) &&
                    (start == null || x.Date >= start) &&
                    (end == null || x.Date <= end)
                    )
                    .OrderByDescending(x => x.UserReputationHistoryID).ToList();
                }
            }
            catch (Exception ex)
            {
                model = new List<UserReputationHistory>();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<List<UserReputationHistory>, List<UserReputationHistoryDto>>(model).ToArray();
        }


        [Route("GetLastReputations")]
        [HttpGet]
        public List<UserReputationHistoryDto> GetLastReputations()
        {
            List<UserReputationHistory> model = new List<UserReputationHistory>();

            try
            {
                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    model = db.UserReputationHistories.OrderByDescending(x => x.UserReputationHistoryID).ToList().GroupBy(x => x.UserID).Select(x => x.First()).ToList();
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<List<UserReputationHistory>, List<UserReputationHistoryDto>>(model);
        }


        [Route("SyncReputationFromChain")]
        [HttpGet]
        public void SyncReputationFromChain(int votingId, int jobId)
        {
            try
            {
                List<UserReputationStake> stakes = new List<UserReputationStake>();

                using (dao_reputationserv_context db = new dao_reputationserv_context())
                {
                    //Get voter stakes
                    var voterStakes = db.UserReputationStakes.Where(x => x.ReferenceProcessID == votingId && (x.Type == StakeType.For || x.Type == StakeType.Against));
                    stakes.AddRange(voterStakes);

                    //Get job doer stake
                    var jobDoerStakes = db.UserReputationStakes.Where(x => x.ReferenceProcessID == jobId && x.Type == StakeType.Mint);
                    stakes.AddRange(jobDoerStakes);


                    foreach (var stake in stakes)
                    {
                        if (!string.IsNullOrEmpty(stake.WalletAddress))
                        {
                            var reputationChanges = Serializers.DeserializeJson<PaginatedResponse<AggregatedReputationChange>>(Helpers.Request.Get(Program._settings.Service_CasperChain_Url + "/CasperMiddleware/GetReputationChangesList?address=" + stake.WalletAddress + "&page=1&page_size=1&order_direction=desc"));

                            if (reputationChanges == null) return;

                            foreach (var repChange in reputationChanges.data)
                            {
                                var lastRepRecord = new UserReputationHistory();
                                if (db.UserReputationHistories.Count(x => x.UserID == stake.UserID) > 0) lastRepRecord = db.UserReputationHistories.OrderByDescending(x => x.UserReputationHistoryID).First(x => x.UserID == stake.UserID);

                                if ((jobDoerStakes.Contains(stake) && db.UserReputationHistories.Count(x => x.UserID == stake.UserID && x.Explanation.Contains("#" + jobId)) == 0)
                                      ||
                                     (voterStakes.Contains(stake) && db.UserReputationHistories.Count(x => x.UserID == stake.UserID && x.Explanation.Contains("#" + repChange.voting_id)) == 0)
                                   )
                                {
                                    UserReputationHistory hist = new UserReputationHistory();
                                    hist.Date = Convert.ToDateTime(repChange.timestamp);
                                    hist.UserID = stake.UserID;
                                    if (stake.Type == StakeType.For || stake.Type == StakeType.Against)
                                    {
                                        hist.Explanation = "User earned reputation from voting process #" + repChange.voting_id;
                                    }
                                    else
                                    {
                                        hist.Explanation = "User earned reputation from job #" + jobId;
                                    }
                                    hist.EarnedAmount = Convert.ToDouble(repChange.earned_amount);
                                    hist.LostAmount = Convert.ToDouble(repChange.lost_amount);
                                    hist.StakedAmount = Convert.ToDouble(repChange.staked_amount);
                                    hist.StakeReleasedAmount = Convert.ToDouble(repChange.released_amount);

                                    hist.LastTotal = lastRepRecord.LastTotal;
                                    hist.LastStakedTotal = lastRepRecord.LastStakedTotal;
                                    hist.LastUsableTotal = lastRepRecord.LastUsableTotal;

                                    if (hist.EarnedAmount > 0)
                                    {
                                        hist.Title = "Reputation Earned";
                                        hist.LastTotal += hist.EarnedAmount;
                                        hist.LastUsableTotal += hist.EarnedAmount;
                                    }
                                    if (hist.LostAmount > 0)
                                    {
                                        hist.Title = "Reputation Lost";
                                        hist.LastTotal -= hist.LostAmount;
                                        hist.LastUsableTotal -= hist.LostAmount;
                                    }
                                    if (hist.StakedAmount > 0)
                                    {
                                        hist.Title = "Staked";
                                        hist.LastStakedTotal += hist.StakedAmount;
                                        hist.LastUsableTotal -= hist.StakedAmount;
                                    }
                                    if (hist.StakeReleasedAmount > 0)
                                    {
                                        hist.Title = "Stake Released";
                                        hist.LastStakedTotal -= hist.StakeReleasedAmount;
                                        hist.LastUsableTotal += hist.StakeReleasedAmount;
                                    }

                                    db.UserReputationHistories.Add(hist);
                                    db.SaveChanges();
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return;
        }
    }
}
