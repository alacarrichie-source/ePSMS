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
        ValueTask<IQueryable<PoIssuanceVM>> GetAllAsync(string userId);
        ValueTask<PoIssuanceVM> GetByIdAsync(Guid? id);
        //ValueTask<IQueryable<PoIssuanceVM>> GetAllPostedAirAsync(string userId);
        //ValueTask<PoIssuanceVM> GetOrderItemByAirItemIdAsync(Guid? airItemId);
        //ValueTask<RisIssuedVM> GeneratePAR(RisIssuedVM model, string user, DateTime date);
        ValueTask PostAsync(Guid psCardItemIssuanceId, string user, DateTime date);
        ValueTask UnpostAsync(Guid psCardItemIssuanceId, string user, DateTime date);        
    }
}
