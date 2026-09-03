using iLgs.Ai.Models;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Ai.Services.PurchaseOrder
{
    public interface ISupplierAiService
    {
        Task<List<SupplierDropdownViewModel>> GetActiveSuppliersAsync();
        Task<SupplierDropdownViewModel> GetSupplierByIdAsync(Guid id);
    }

    public class SupplierAiService : ISupplierAiService
    {
        private readonly AppManEntities _db;

        public SupplierAiService(AppManEntities db)
        {
            _db = db;
        }

        public async Task<List<SupplierDropdownViewModel>> GetActiveSuppliersAsync()
        {
            return await _db.Suppliers
                //.Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new SupplierDropdownViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    TIN = s.TIN,
                  //  DefaultPaymentTerms = s.DefaultPaymentTerms
                })
                .ToListAsync();
        }

        public async Task<SupplierDropdownViewModel> GetSupplierByIdAsync(Guid id)
        {
            var s = await _db.Suppliers.FindAsync(id);
            if (s == null) return null;

            return new SupplierDropdownViewModel
            {
                Id = s.Id,
                Name = s.Name,
                TIN = s.TIN,
                //DefaultPaymentTerms = s.DefaultPaymentTerms
            };
        }
    }
}