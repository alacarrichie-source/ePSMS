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
        IRisItemService RisItem { get; }
        //IRisItemExtnService RisItemExtn { get; }
        ICodextnService Codextn { get; }        
        IAirService Air { get; }
         
        IAirInvoiceService AirInvoice { get; }
    }
}