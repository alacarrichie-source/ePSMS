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

    public class ServiceAgent : IServiceAgent
    {
        public IRisService Ris { get; }
        public ICodextnService Codextn { get; }
        public IAirService Air { get; }
        public IAirInvoiceService AirInvoice { get; }

        public ServiceAgent(
            IRisService risService,
            ICodextnService codextnService,
            IAirService airService,
            IAirInvoiceService airInvoiceService)
        {
            Ris = risService;
            Codextn = codextnService;
            Air = airService;
            AirInvoice = airInvoiceService;
        }
    }

    //public class ServiceAgent : IServiceAgent
    //{
    //    private readonly AppManEntities _db = new AppManEntities();

    //    private IRisService _risService;
    //    private ICodextnService _codextnService;
    //    private IAirService _airService;
    //    private IAirInvoiceService _airInvoiceService;

    //    public ServiceAgent(AppManEntities db)
    //    {
    //        _db = db;
    //    }

    //    public IRisService Ris { get { return _risService = _risService ?? new RisService(_db); } }
    //    public ICodextnService Codextn { get { return _codextnService = _codextnService ?? new CodextnService(_db); } }
    //    public IAirService Air { get { return _airService = _airService ?? new AirService(_db); } }
    //    public IAirInvoiceService AirInvoice { get { return _airInvoiceService = _airInvoiceService ?? new AirInvoiceService(_db); } }
    //}
}