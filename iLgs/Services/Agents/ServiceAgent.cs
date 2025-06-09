using iLgs.Models;
using iLgs.Services.AIRs_;
using iLgs.Services.Codes;
using iLgs.Services.Requisition;

namespace iLgs.Agents.Services
{
    public interface IServiceAgent
    {
        IRisService Ris { get; }
        //IRisItemService RisItem { get; }
        //IRisItemExtnService RisItemExtn { get; }
        ICodextnService Codextn { get; }
        IAirService Air { get; }

        IAirInvoiceService AirInvoice { get; }
    }

    public class ServiceAgent : IServiceAgent
    {
        private readonly AppManEntities _db = new AppManEntities();

        private IRisService _risService;
        //private IRisItemService _risItemService;
        //private IRisItemExtnService _risItemExtnService;
        private ICodextnService _codextnService;
        private IAirService _airService;
        private IAirInvoiceService _airInvoiceService;

        public ServiceAgent(AppManEntities db)
        {
            _db = db;            
        }

        public IRisService Ris { get { return _risService = _risService ?? new RisService(_db); } }
        //public IRisItemService RisItem { get { return _risItemService = _risItemService ?? new RisItemService(_db); } }
        //public IRisItemExtnService RisItemExtn { get { return _risItemExtnService = _risItemExtnService ?? new RisItemExtnService(_db); } }
        public ICodextnService Codextn { get { return _codextnService = _codextnService ?? new CodextnService(_db); } }
        public IAirService Air { get { return _airService = _airService ?? new AirService(_db); } }
        public IAirInvoiceService AirInvoice { get { return _airInvoiceService = _airInvoiceService ?? new AirInvoiceService(_db); } }
    }
}