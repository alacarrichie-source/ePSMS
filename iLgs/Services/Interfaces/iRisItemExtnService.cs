using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IRisItemExtnService
    {
        IQueryable<RisItemExtnVM> GetAll();
        IQueryable<RisItemExtnVM> GetBatchInfo(Guid? risItemId, Guid? psCodeId);
        Task SaveAsync(Guid risItemId, List<RisItemExtnVM> risItemExtnList, string user, DateTime date);
    }
}
