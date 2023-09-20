using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Interfaces;

namespace iLgs.Agents.Services
{
    public class ServiceAgent : IServiceAgent
    {
        private readonly AppManEntities _db = new AppManEntities();

        private IRisService _risService;
        private IRisItemService _risItemService;
        private IRisItemExtnService _risItemExtnService;
        private ICodextnService _codextnService;
        private IPsCodeService _psCodeService;

        public ServiceAgent(AppManEntities db)
        {
            _db = db;            
        }

        public IRisService Ris { get { return _risService = _risService ?? new RisService(_db); } }
        public IRisItemService RisItems { get { return _risItemService = _risItemService ?? new RisItemService(_db); } }
        public IRisItemExtnService RisItemExtns { get { return _risItemExtnService = _risItemExtnService ?? new RisItemExtnService(_db); } }
        public ICodextnService Codextns { get { return _codextnService = _codextnService ?? new CodextnService(_db); } }
        public IPsCodeService PsCodes { get { return _psCodeService = _psCodeService ?? new PsCodeService(_db); } }
    }
}