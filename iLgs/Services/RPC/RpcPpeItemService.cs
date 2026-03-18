using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.RPC
{
    public interface IRpcPpeItemService
    {
        IQueryable<RpcPpeItem> GetByRpcPpeId(Guid? rpcPpeId);
        ValueTask<RpcPpeItem> GetByIdAsync(Guid id);        
    }


    public class RpcPpeItemService : IRpcPpeItemService
    {
        protected readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<RpcPpeItem> _exceptionService;
        protected readonly IUserService _userService;

        public RpcPpeItemService(AppManEntities db)
        {
            _db = db;
            _exceptions = new CreateAndLogExceptions();
            _exceptionService = new ExceptionService<RpcPpeItem>();
            _userService = new UserService(_db);
        }

        public ValueTask<RpcPpeItem> GetByIdAsync(Guid id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.RpcPpeItems.Where(w => w.Id == id).FirstOrDefaultAsync();
            return data;
        });
        
        public IQueryable<RpcPpeItem> GetByRpcPpeId(Guid? rpcPpeId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.RpcPpeItems.AsNoTracking().Where(w => w.RpcPpeId == rpcPpeId).AsQueryable();
            return data;
        });                        
    }
}