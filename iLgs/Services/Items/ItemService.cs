using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Services.Items
{
    public interface IItemService
    {
        string NextStockNo(string psNo);
    }

    public class ItemService : IItemService
    {
        private readonly AppManEntities db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();

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