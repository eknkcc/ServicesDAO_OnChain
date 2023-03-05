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
    public class DaoSettingController : Controller
    {
        [Route("Get")]
        [HttpGet]
        public IEnumerable<DaoSettingDto> Get()
        {
            List<DaoSetting> model = new List<DaoSetting>();

            try
            {
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    model = db.DaoSettings.ToList();
                }
            }
            catch (Exception ex)
            {
                model = new List<DaoSetting>();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<List<DaoSetting>, List<DaoSettingDto>>(model).ToArray();
        }

        [Route("GetId")]
        [HttpGet]
        public DaoSettingDto GetId(int id)
        {
            DaoSetting model = new DaoSetting();

            try
            {
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    model = db.DaoSettings.Find(id);
                }
            }
            catch (Exception ex)
            {
                model = new DaoSetting();
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return _mapper.Map<DaoSetting, DaoSettingDto>(model);
        }

        [Route("Post")]
        [HttpPost]
        public DaoSettingDto Post([FromBody] DaoSettingDto model)
        {
            try
            {
                DaoSetting item = _mapper.Map<DaoSettingDto, DaoSetting>(model);
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    db.DaoSettings.Add(item);
                    db.SaveChanges();
                }
                return _mapper.Map<DaoSetting, DaoSettingDto>(item);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return new DaoSettingDto();
            }
        }

        [Route("PostMultiple")]
        [HttpPost]
        public List<DaoSettingDto> PostMultiple([FromBody] List<DaoSettingDto> model)
        {
            try
            {
                List<DaoSetting> item = _mapper.Map<List<DaoSettingDto>, List<DaoSetting>>(model);
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    db.DaoSettings.AddRange(item);
                    db.SaveChanges();
                }
                return _mapper.Map<List<DaoSetting>, List<DaoSettingDto>>(item);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return new List<DaoSettingDto>();
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
                    DaoSetting item = db.DaoSettings.FirstOrDefault(s => s.DaoSettingID == ID);
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
        public DaoSettingDto Update([FromBody] DaoSettingDto model)
        {
            try
            {
                DaoSetting item = _mapper.Map<DaoSettingDto, DaoSetting>(model);
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    db.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    db.SaveChanges();
                }
                return _mapper.Map<DaoSetting, DaoSettingDto>(item);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return new DaoSettingDto();
            }
        }

        [Route("GetPaged")]
        [HttpGet]
        public PaginationEntity<DaoSettingDto> GetPaged(int page = 1, int pageCount = 30)
        {
            PaginationEntity<DaoSettingDto> res = new PaginationEntity<DaoSettingDto>();

            try
            {
                using (dao_maindb_context db = new dao_maindb_context())
                {

                    IPagedList<DaoSettingDto> lst = AutoMapperBase.ToMappedPagedList<DaoSetting, DaoSettingDto>(db.DaoSettings.OrderByDescending(x => x.DaoSettingID).ToPagedList(page, pageCount));

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

        [Route("GetLatestSetting")]
        [HttpGet]
        public DaoSettingDto GetLatestSetting(string key)
        {
            try
            {
                using (dao_maindb_context db = new dao_maindb_context())
                {
                    if (db.DaoSettings.Count() > 0)
                    {
                        DaoSetting result = db.DaoSettings.Where(x => x.Key == key).OrderByDescending(x => x.DaoSettingID).First();

                        return _mapper.Map<DaoSetting, DaoSettingDto>(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
            }

            return null;
        }

        [Route("PostOrUpdate")]
        [HttpPost]
        public DaoSettingDto PostOrUpdate([FromBody] DaoSettingDto model)
        {
            try
            {
                DaoSetting item = _mapper.Map<DaoSettingDto, DaoSetting>(model);

                using (dao_maindb_context db = new dao_maindb_context())
                {
                    if (db.DaoSettings.Count(x => x.Key == model.Key) > 0)
                    {
                        item = db.DaoSettings.First(x => x.Key == model.Key);
                        item.Value = model.Value;
                        item.LastModified = DateTime.Now;
                        db.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        db.SaveChanges();
                    }
                    else
                    {
                        db.DaoSettings.Add(item);
                        db.SaveChanges();
                    }
                }

                return _mapper.Map<DaoSetting, DaoSettingDto>(item);
            }
            catch (Exception ex)
            {
                Program.monitizer.AddException(ex, LogTypes.ApplicationError, true);
                return new DaoSettingDto();
            }
        }

    }
}
