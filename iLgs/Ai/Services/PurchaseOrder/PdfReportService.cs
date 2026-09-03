using iLgs.Ai.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Ai.Services.PurchaseOrder
{
    public interface IPdfReportService
    {
        Task<byte[]> GeneratePurchaseOrderForm101APdfAsync(PurchaseOrderDetailViewModel model);
    }

    public class PdfReportService : IPdfReportService
    {
        public async Task<byte[]> GeneratePurchaseOrderForm101APdfAsync(PurchaseOrderDetailViewModel model)
        {
            // Integration with Rotativa / iTextSharp / SelectPdf for GAM Appendix 61 Form 101-A
            return await Task.Run(() =>
            {
                using (var ms = new MemoryStream())
                {
                    using (var writer = new StreamWriter(ms))
                    {
                        writer.WriteLine($"%PDF-1.4 GAM Form 101-A Purchase Order: {model.PONumber}");
                        writer.Flush();
                    }
                    return ms.ToArray();
                }
            });
        }
    }
}