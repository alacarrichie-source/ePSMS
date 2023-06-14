using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface ICodextnService
    {
        IQueryable<CodextnVM> GetByMastCode(string mastCode);
        IQueryable<CodextnVM> GetByMastId(Guid mastId);
        Task<CodextnVM> CreateAsync(CodextnVM model, string user, DateTime date);
        Task<CodextnVM> UpdateAsync(CodextnVM model, string user, DateTime date);
        Task<CodextnVM> DeleteAsync(CodextnVM model, string user, DateTime date);        
    }
}
