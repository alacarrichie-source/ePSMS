using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Services.Items
{
    public interface IItemService
    {
        //IQueryable<PsCode> GetAll();
        //IQueryable<PsCodeVM> GetMaintenanceView();
        //Task<PsCode> GetByIdAsync(Guid psId);
        //Task<PsCode> GetByPsNoAsync(string psNo);
        //Task<bool> GetAnyPsNoAsync(Guid id, string psNo);

        //Task<PsCode> CreateAsync(PsCode model, string user, DateTime date);
        //Task<PsCode> UpdateAsync(PsCode model, string user, DateTime date);
        //Task<PsCode> DeleteAsync(PsCode model, string user, DateTime date);
        
        string NextStockNo(string psNo);
    }

    public class ItemService : IItemService
    {
        private readonly AppManEntities db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<ItemCodeVM> _VmExceptionService = new ExceptionService<ItemCodeVM>();

        public ItemService(AppManEntities db)
        {
            this.db = db;
        }

        public string NextStockNo(string psNo)
        {
            string keyName = psNo;

            var data = db.PsStocks.Where(w => w.PsCode.PsNo == psNo)
                .OrderByDescending(o => o.StockNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "001";
            }
            else
            {
                var sequence = (int.Parse(data.StockNo.Split('-')[1]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(3, '0');
            }
        }                
    }
}