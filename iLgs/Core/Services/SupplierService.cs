using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using iLgs.Core.ViewModels;

namespace iLgs.Core.Services
{
    public interface ISupplierService
    {
        List<SupplierViewModel> GetActiveSuppliers();
        Task<List<SupplierViewModel>> GetActiveSuppliersAsync();
        IQueryable<SupplierViewModel> GetSuppliersQueryable();
        Task<SupplierViewModel> GetByIdAsync(string id);
        Task<SupplierViewModel> SaveSupplierAsync(SupplierViewModel model);
    }

    public class SupplierService : ISupplierService
    {
        private readonly Models.AppManEntities _db;

        public SupplierService(Models.AppManEntities db)
        {
            _db = db;
        }

        public List<SupplierViewModel> GetActiveSuppliers()
        {
            return _db.Suppliers
                //.Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new SupplierViewModel
                {
                    Id = s.Id.ToString(),
                    Name = s.Name,
                    Address = s.Address,
                    TIN = s.TIN
                    //IsActive = s.IsActive
                })
                .ToList();
        }

        public async Task<List<SupplierViewModel>> GetActiveSuppliersAsync()
        {
            return await _db.Suppliers
                //.Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new SupplierViewModel
                {
                    Id = s.Id.ToString(),
                    Name = s.Name,
                    Address = s.Address,
                    TIN = s.TIN
                    //IsActive = s.IsActive
                })
                .ToListAsync();
        }

        public IQueryable<SupplierViewModel> GetSuppliersQueryable()
        {
            return _db.Suppliers
                .Select(s => new SupplierViewModel
                {
                    Id = s.Id.ToString(),
                    Name = s.Name,
                    Address = s.Address,
                    TIN = s.TIN,
                    //IsActive = s.IsActive,
                    //ActiveOrdersCount = s.Orders.Count(p => p.Status != "CANCELLED")
                    ActiveOrdersCount = s.Orders.Count(p => p.PostedDt != null)
                });
        }

        public async Task<SupplierViewModel> GetByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out Guid supId))
                return null;
            var s = await _db.Suppliers.FindAsync(supId);
            if (s == null) return null;

            return new SupplierViewModel
            {
                Id = s.Id.ToString(),
                Name = s.Name,
                Address = s.Address,
                TIN = s.TIN,
                //IsActive = s.IsActive
            };
        }

        public async Task<SupplierViewModel> SaveSupplierAsync(SupplierViewModel model)
        {
            Models.Supplier supplier;

            if (string.IsNullOrEmpty(model.Id))
            {
                supplier = new Models.Supplier
                {
                    Id = Guid.NewGuid(),
                    Name = model.Name,
                    Address = model.Address,
                    TIN = model.TIN,
                    //IsActive = model.IsActive
                };
                _db.Suppliers.Add(supplier);
            }
            else
            {
                supplier = await _db.Suppliers.FindAsync(Guid.Parse(model.Id));
                if (supplier == null) throw new InvalidOperationException("Supplier not found.");

                supplier.Name = model.Name;
                supplier.Address = model.Address;
                supplier.TIN = model.TIN;
                //supplier.IsActive = model.IsActive;
            }

            await _db.SaveChangesAsync();

            return new SupplierViewModel
            {
                Id = supplier.Id.ToString(),
                Name = supplier.Name,
                Address = supplier.Address,
                TIN = supplier.TIN,
                //IsActive = supplier.IsActive
                IsActive = true
            };
        }
    }
}