using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Agents.Services
{
    public interface IServiceAgent
    {
        IRisService Ris { get; }
        IRisItemService RisItems { get; }
        IRisItemExtnService RisItemExtns { get; }
        ICodextnService Codextns { get; }
        IPsCodeService PsCodes { get; }
    }
}