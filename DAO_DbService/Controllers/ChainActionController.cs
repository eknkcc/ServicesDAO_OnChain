using DAO_DbService.Contexts;
using DAO_DbService.Models;
using Helpers.Models.DtoModels.MainDbDto;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Helpers.Constants.Enums;
using static DAO_DbService.Mapping.AutoMapperBase;
using Helpers.Models.SharedModels;
using DAO_DbService.Mapping;
using PagedList.Core;

namespace DAO_DbService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ChainActionController : Controller
    {
        [Route("Get")]
        [HttpGet]
        public IEnumerable<ChainActionDto> Get()
        {
            List<ChainAction> model = new List<ChainAction>();

            try
            {
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    model = db.ChainActions.ToList();
                }
            }
            catch (Exception ex)
            {
                model = new List<ChainAction>();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<List<ChainAction>, List<ChainActionDto>>(model).ToArray();
        }

        [Route("GetId")]
        [HttpGet]
        public ChainActionDto GetId(int id)
        {
            ChainAction model = new ChainAction();

            try
            {
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    model = db.ChainActions.Find(id);
                }
            }
            catch (Exception ex)
            {
                model = new ChainAction();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<ChainAction, ChainActionDto>(model);
        }

        [Route("Post")]
        [HttpPost]
        public ChainActionDto Post([FromBody] ChainActionDto model)
        {
            try
            {
                ChainAction item = _mapper.Map<ChainActionDto, ChainAction>(model);
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    db.ChainActions.Add(item);
                    db.SaveChanges();
                }
                return _mapper.Map<ChainAction, ChainActionDto>(item);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return new ChainActionDto();
            }
        }

        [Route("PostMultiple")]
        [HttpPost]
        public List<ChainActionDto> PostMultiple([FromBody] List<ChainActionDto> model)
        {
            try
            {
                List<ChainAction> item = _mapper.Map<List<ChainActionDto>, List<ChainAction>>(model);
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    db.ChainActions.AddRange(item);
                    db.SaveChanges();
                }
                return _mapper.Map<List<ChainAction>, List<ChainActionDto>>(item);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return new List<ChainActionDto>();
            }
        }

        [Route("Delete")]
        [HttpDelete]
        public bool Delete(int? ID)
        {
            try
            {
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    ChainAction item = db.ChainActions.FirstOrDefault(s => s.ChainActionId == ID);
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
        public ChainActionDto Update([FromBody] ChainActionDto model)
        {
            try
            {
                ChainAction item = _mapper.Map<ChainActionDto, ChainAction>(model);
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    db.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    db.SaveChanges();
                }
                return _mapper.Map<ChainAction, ChainActionDto>(item);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return new ChainActionDto();
            }
        }

        [Route("GetPaged")]
        [HttpGet]
        public PaginationEntity<ChainActionDto> GetPaged(int page = 1, int pageCount = 30)
        {
            PaginationEntity<ChainActionDto> res = new PaginationEntity<ChainActionDto>();

            try
            {
                using (dao_maindb_context db = new dao_maindb_context())
                {

                    IPagedList<ChainActionDto> lst = AutoMapperBase.ToMappedPagedList<ChainAction, ChainActionDto>(db.ChainActions.OrderByDescending(x => x.ChainActionId).ToPagedList(page, pageCount));

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

    }
}
