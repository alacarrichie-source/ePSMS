using iLgs.Models;
using iLgs.Services.AIRs_;
using iLgs.Services.Codes;
using iLgs.Services.Requisition;

namespace iLgs.Agents.Services
{
    public interface IServiceAgent
    {
        IRisService Ris { get; }
        ICodextnService Codextn { get; }
        IAirService Air { get; }

        IAirInvoiceService AirInvoice { get; }
    }

    //public class ServiceAgent : IServiceAgent
    //{
    //    public IRisService Ris { get; }
    //    public ICodextnService Codextn { get; }
    //    public IAirService Air { get; }
    //    public IAirInvoiceService AirInvoice { get; }

    //    public ServiceAgent(
    //        IRisService risService,
    //        ICodextnService codextnService,
    //        IAirService airService,
    //        IAirInvoiceService airInvoiceService)
    //    {
    //        Ris = risService;
    //        Codextn = codextnService;
    //        Air = airService;
    //        AirInvoice = airInvoiceService;
    //    }
    //}

    public class ServiceAgent : IServiceAgent
    {
        private readonly AppManEntities _db;

        private IRisService _risService;
        private ICodextnService _codextnService;
        private IAirService _airService;
        private IAirInvoiceService _airInvoiceService;

        public ServiceAgent(AppManEntities db)
        {
            _db = db;
            _risService = new RisService(_db);
            _codextnService = new CodextnService(_db);
            _airService = new AirService(_db);
            _airInvoiceService = new AirInvoiceService(_db);
        }

        public IRisService Ris => _risService;
        public ICodextnService Codextn => _codextnService;
        public IAirService Air => _airService;
        public IAirInvoiceService AirInvoice => _airInvoiceService;
    }
}