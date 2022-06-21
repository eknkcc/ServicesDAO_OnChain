using DAO_VotingEngine.Contexts;
using DAO_VotingEngine.Models;
using Helpers.Models.DtoModels.VoteDbDto;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Helpers.Constants.Enums;
using static DAO_VotingEngine.Mapping.AutoMapperBase;
using PagedList.Core;
using DAO_VotingEngine.Mapping;
using Helpers.Models.SharedModels;
using Helpers.Models.DtoModels.ReputationDbDto;

namespace DAO_VotingEngine.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class VoteController : Controller
    {
        [Route("Get")]
        [HttpGet]
        public IEnumerable<VoteDto> Get()
        {
            List<Vote> model = new List<Vote>();

            try
            {
                using (dao_votesdb_context db = new dao_votesdb_context())
                {
                    model = db.Votes.ToList();
                }
            }
            catch (Exception ex)
            {
                model = new List<Vote>();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<List<Vote>, List<VoteDto>>(model).ToArray();
        }

        [Route("GetId")]
        [HttpGet]
        public VoteDto GetId(int id)
        {
            Vote model = new Vote();

            try
            {
                using (dao_votesdb_context db = new dao_votesdb_context())
                {
                    model = db.Votes.Find(id);
                }
            }
            catch (Exception ex)
            {
                model = new Vote();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<Vote, VoteDto>(model);
        }

        [Route("Post")]
        [HttpPost]
        public VoteDto Post([FromBody] VoteDto model)
        {
            try
            {
                Vote item = _mapper.Map<VoteDto, Vote>(model);
                using (dao_votesdb_context db = new dao_votesdb_context())
                {
                    db.Votes.Add(item);
                    db.SaveChanges();
                }
                return _mapper.Map<Vote, VoteDto>(item);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return new VoteDto();
            }
        }

        [Route("PostMultiple")]
        [HttpPost]
        public List<VoteDto> PostMultiple([FromBody] List<VoteDto> model)
        {
            try
            {
                List<Vote> item = _mapper.Map<List<VoteDto>, List<Vote>>(model);
                using (dao_votesdb_context db = new dao_votesdb_context())
                {
                    db.Votes.AddRange(item);
                    db.SaveChanges();
                }
                return _mapper.Map<List<Vote>, List<VoteDto>>(item);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return new List<VoteDto>();
            }
        }

        [Route("Delete")]
        [HttpDelete]
        public bool Delete(int? ID)
        {
            try
            {
                using (dao_votesdb_context db = new dao_votesdb_context())
                {
                    Vote item = db.Votes.FirstOrDefault(s => s.VoteID == ID);
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
        public VoteDto Update([FromBody] VoteDto model)
        {
            try
            {
                Vote item = _mapper.Map<VoteDto, Vote>(model);
                using (dao_votesdb_context db = new dao_votesdb_context())
                {
                    db.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    db.SaveChanges();
                }
                return _mapper.Map<Vote, VoteDto>(item);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return new VoteDto();
            }
        }

        [Route("GetPaged")]
        [HttpGet]
        public PaginationEntity<VoteDto> GetPaged(int page = 1, int pageCount = 30)
        {
            PaginationEntity<VoteDto> res = new PaginationEntity<VoteDto>();

            try
            {
                using (dao_votesdb_context db = new dao_votesdb_context())
                {

                    IPagedList<VoteDto> lst = AutoMapperBase.ToMappedPagedList<Vote, VoteDto>(db.Votes.OrderByDescending(x => x.VoteID).ToPagedList(page, pageCount));

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

        [Route("GetAllVotesByVotingId")]
        [HttpGet]
        public List<VoteDto> GetAllVotesByVotingId(int votingid)
        {
            List<Vote> model = new List<Vote>();

            try
            {
                using (dao_votesdb_context db = new dao_votesdb_context())
                {
                    model = db.Votes.Where(x => x.VotingID == votingid).ToList();
                }
            }
            catch (Exception ex)
            {
                model = new List<Vote>();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<List<Vote>, List<VoteDto>>(model).ToList();
        }

        [Route("SubmitVote")]
        [HttpGet]
        public SimpleResponse SubmitVote(int VotingID, int UserID, StakeType Direction, double? ReputationStake)
        {
            SimpleResponse res = new SimpleResponse();

            try
            {
                using (dao_votesdb_context db = new dao_votesdb_context())
                {
                    Voting voteProcess = db.Votings.Find(VotingID);

                    //Check if user already voted on the same voting
                    if (db.Votes.Count(x => x.UserID == UserID && x.VotingID == voteProcess.VotingID) > 0)
                    {
                        return new SimpleResponse() { Success = false, Message = "User already voted on this voting." };
                    }

                    //Check if user has vote in informal
                    if (voteProcess.IsFormal)
                    {
                        if (db.Votings.Count(x => x.IsFormal == false && x.JobID == voteProcess.JobID) > 0)
                        {
                            //Get informal voting
                            var informalVoting = db.Votings.OrderByDescending(x => x.CreateDate).First(x => x.IsFormal == false && x.JobID == voteProcess.JobID);

                            //Get votes from informal voting
                            var reputationsJson = Helpers.Request.Get(Program._settings.Service_Reputation_Url + "/UserReputationStake/GetByProcessId?referenceProcessID=" + informalVoting.VotingID + "&reftype=" + StakeType.For);
                            var reputations = Helpers.Serializers.DeserializeJson<List<UserReputationStakeDto>>(reputationsJson);

                            if (reputations.Count(x => x.UserID == UserID) == 0)
                            {
                                return new SimpleResponse() { Success = false, Message = "User didn't vote on the informal voting." };
                            }
                        }
                    }

                    //Check if user staked reputation        
                    if (ReputationStake != null)
                    {
                        //Save vote into database
                        Vote vote = new Vote();
                        vote.Date = DateTime.Now;
                        vote.Direction = Direction;
                        vote.VotingID = VotingID;
                        vote.UserID = UserID;
                        db.Votes.Add(vote);
                        db.SaveChanges();

                        UserReputationStakeDto repModel = new UserReputationStakeDto();
                        repModel.ReferenceID = vote.VoteID;
                        repModel.ReferenceProcessID = vote.VotingID;
                        repModel.UserID = vote.UserID;
                        repModel.Amount = Convert.ToDouble(ReputationStake);
                        repModel.Type = Direction;
                        repModel.CreateDate = DateTime.Now;

                        var jsonResult = Helpers.Request.Post(Program._settings.Service_Reputation_Url + "/UserReputationStake/SubmitStake", Helpers.Serializers.SerializeJson(repModel));

                        SimpleResponse parsedResult = Helpers.Serializers.DeserializeJson<SimpleResponse>(jsonResult);

                        if (parsedResult.Success == false)
                        {
                            //If staking fails for some reasoın remove vote.
                            db.Votes.Remove(vote);
                            db.SaveChanges();
                        }
                        else
                        {
                            //If staking is successfull update voting model
                            voteProcess.VoteCount += 1;
                            if (repModel.Type == StakeType.For)
                            {
                                voteProcess.StakedFor += repModel.Amount;
                            }
                            else if (repModel.Type == StakeType.Against)
                            {
                                voteProcess.StakedAgainst += repModel.Amount;
                            }
                            db.SaveChanges();
                        }

                        //If this is an informal voting release stake immidiately.
                        if (voteProcess.IsFormal == false)
                        {
                            SimpleResponse releaseStakeResponse = Helpers.Serializers.DeserializeJson<SimpleResponse>(Helpers.Request.Get(Program._settings.Service_Reputation_Url + "/UserReputationStake/ReleaseStakesByType?referenceID=" + vote.VoteID + "&reftype=" + Helpers.Constants.Enums.StakeType.For + "&userid=" + UserID));
                        }

                        Program.monitizer.AddUserLog(UserID, UserLogType.Request, "User submitted vote. Voting # " + VotingID);

                        return parsedResult;
                    }
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return res;
        }


        [Route("GetAllVotesByUserId")]
        [HttpGet]
        public List<VoteDto> GetAllVotesByUserId(int userid)
        {
            List<Vote> model = new List<Vote>();

            try
            {
                using (dao_votesdb_context db = new dao_votesdb_context())
                {
                    model = db.Votes.Where(x => x.UserID == userid).ToList();
                }
            }
            catch (Exception ex)
            {
                model = new List<Vote>();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<List<Vote>, List<VoteDto>>(model).ToList();
        }
    }
}
