using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IPoIssuanceService
    {
        //ValueTask<IQueryable<PoIssuanceVM>> GetAllPostedPoWithPostedAir(string userId);
        ValueTask<RisIssuedVM> GeneratePAR(RisIssuedVM model, string user, DateTime date);
        //ValueTask<PoIssuanceVM> GetByOrderItemIdAsync(Guid? orderItemId);
        ValueTask PostAsync(Guid risIssuedId, string user, DateTime date);
        ValueTask UnpostAsync(Guid risIssuedId, string user, DateTime date);
        ValueTask<IQueryable<PoIssuanceVM>> GetAllPostedAirAsync(string userId);
        ValueTask<PoIssuanceVM> GetOrderItemByAirItemIdAsync(Guid? airItemId);
    }
}
