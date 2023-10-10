using iLgs.Controllers;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.Interfaces
{
    public class PoIssuanceService : IPoIssuanceService
    {
        private readonly AppManEntities db = new AppManEntities();
        private IUserService userService;
        public PoIssuanceService(AppManEntities db)
        {
            this.db = db;
            this.userService = new UserService(db);
        }

        public async ValueTask<IQueryable<PoIssuanceVM>> GetAllPostedPoWithPostedAir(string userId)
        {
            var IsAdmin = await userService.IsAdmin(userId);
            var data = db.OrderItems
                    .Where(w => (w.Order.PostedDt != null && w.AIRItems.Any(a => a.AIR.PostedDt != null))
                        && (IsAdmin ||
                            db.Codextns.Any(x => x.CodeMast.Code == "DEPARTMENTS" 
                            && x.Description == w.Order.Request.RISs.Office
                            && x.DepartmentUsers.Any(a => a.UserId == userId))                        
                        )
                    )
                    .Select(s => new PoIssuanceVM
                    {
                        Id = s.Id,
                        RisItemId = s.RequestItem.RisItemId,
                        Department = s.Order.Request.RISs.Office,
                        PoNo = s.Order.PoNo,
                        PoDate = s.Order.PoDate,
                        PrNo = s.Order.Request.PrNo,
                        RisNo = s.Order.Request.RISs.RisNo,
                        Qty = (int?)s.Qty,
                        QtyIss = s.RequestItem.RisItem.RisIssueds.Sum(x => x.Qty) ?? 0,
                        Balance = (int?)s.Qty - (s.RequestItem.RisItem.RisIssueds.Sum(x => x.Qty) ?? 0),
                        StockNo = s.StockNo,
                        StockName = s.StockName,
                        UnitCost = s.UnitCost
                    })
                    .AsQueryable();
            return data;
        }

    }
}