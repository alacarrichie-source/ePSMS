using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iLgs.Models;
using System.Threading.Tasks;
using System.Data.Entity;

namespace iLgs.Services.PurchaseRequest
{
    public interface IRequestItemService
    {
        IQueryable<RequestItemVM> GetByPrId(Guid? prId);
        Task<RequestItemVM> GetVmByIdAsync(Guid? id);
        Task<RequestItem> GetByIdAsync(Guid? id);

        Task<RequestItemVM> CreateAsync(RequestItemVM model, string user, DateTime date);
        Task<RequestItemVM> UpdateAsync(RequestItemVM model, string user, DateTime date);
        Task<RequestItemVM> DeleteAsync(RequestItemVM model, string user, DateTime date);
    }

    public class RequestItemService : IRequestItemService
    {
        private readonly AppManEntities _db;

        public RequestItemService(AppManEntities db)
        {
            _db = db;
        }

        public IQueryable<RequestItemVM> GetByPrId(Guid? prId)
        {
            var data = _db.RequestItems.AsNoTracking().Where(w => w.PrId == prId)
                .Select(s => new RequestItemVM
                {
                    Id = s.Id,
                    PrId = s.PrId,
                    RisItemId = s.RisItemId,
                    ItemCode = s.RisItem.ItemCode.Code,
                    ItemType = s.RisItem.ItemCode.Description,
                    Category = s.RisItem.ItemCode.ItemType.Category,
                    PsType = s.RisItem.ItemCode.ItemType.Code,
                    PsTypeDesc = s.RisItem.ItemCode.ItemType.Description,
                    PsNo = s.RisItem.PsNo,
                    PsNoDisplay = s.RisItem.PsNoDisplay,
                    Unit = s.RisItem.Unit,
                    ItemName = s.RisItem.ItemName,
                    Description = s.RisItem.Description,
                    Qty = s.Qty,
                    UnitCost = s.UnitCost,
                    TotalCost = s.TotalCost,
                    PriceRate = s.PriceRate,
                    InsertedDt = s.InsertedDt,
                    SetLotNo = s.RequestItemUnitGroupDescriptionItems.FirstOrDefault().RisItemUnitGroupDescriptionItem.RisItemUnitGroupDescription.RisItemUnitGroup.SetLotNo                    
                });
            return data;
        }

        public async Task<RequestItemVM> GetVmByIdAsync(Guid? id)
        {
            var data = await _db.RequestItems.AsNoTracking().Where(w => w.Id == id)
                .Select(s => new RequestItemVM
                {
                    Id = s.Id,
                    PrId = s.PrId,
                    RisItemId = s.RisItemId,
                    ItemCode = s.RisItem.ItemCode.Code,
                    ItemType = s.RisItem.ItemCode.Description,
                    Category = s.RisItem.ItemCode.ItemType.Category,
                    PsType = s.RisItem.ItemCode.ItemType.Code,
                    PsTypeDesc = s.RisItem.ItemCode.ItemType.Description,
                    PsNo = s.RisItem.PsNo,
                    PsNoDisplay = s.RisItem.PsNoDisplay,
                    Unit = s.RisItem.Unit,
                    ItemName = s.RisItem.ItemName,
                    Description = s.RisItem.Description,
                    Qty = s.Qty,
                    UnitCost = s.UnitCost,
                    TotalCost = s.TotalCost,
                    PriceRate = s.PriceRate,
                    InsertedDt = s.InsertedDt,
                    GridRequestItemExtns = "",
                    SetLotNo = s.RequestItemUnitGroupDescriptionItems.FirstOrDefault().RisItemUnitGroupDescriptionItem.RisItemUnitGroupDescription.RisItemUnitGroup.SetLotNo
                }).FirstOrDefaultAsync();
            return data;
        }

        public async Task<RequestItem> GetByIdAsync(Guid? id)
        {
            var data = await _db.RequestItems.FindAsync(id);                
            return data;
        }        

        public async Task<RequestItemVM> CreateAsync(RequestItemVM model, string user, DateTime date)
        {
            //model.Id = Guid.NewGuid(); // Id created in controller
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RequestItem entity = new RequestItem()
            {
                Id = model.Id,
                PrId = model.PrId,
                Qty = model.Qty,
                UnitCost = model.UnitCost,
                TotalCost = model.TotalCost,
                PriceRate = model.PriceRate,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.RequestItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<RequestItemVM> DeleteAsync(RequestItemVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RequestItem entity = await _db.RequestItems.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RequestItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RequestItems.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        }        

        public async Task<RequestItemVM> UpdateAsync(RequestItemVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RequestItem entity = await _db.RequestItems.FindAsync(model.Id);
            entity.Qty = model.Qty;
            entity.UnitCost = model.UnitCost;
            entity.TotalCost = model.TotalCost;
            entity.PriceRate = model.PriceRate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RequestItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
                        
            //var poItem = _db.OrderItems.Where(w => w.RequestItemId == model.Id).FirstOrDefault();
            //if (poItem != null)
            //{
            //    poItem.Qty = model.Qty;
            //    //poItem.Description = model.Description;
            //    _db.OrderItems.Attach(poItem);
            //    _db.Entry(poItem).State = EntityState.Modified;

            //    var airItem = _db.AIRItems.Where(w => w.OrderItemId == poItem.Id).FirstOrDefault();
            //    airItem.Qty = model.Qty;
            //    _db.AIRItems.Attach(airItem);
            //    _db.Entry(airItem).State = EntityState.Modified;
            //}            

            await _db.SaveChangesAsync();
            return model;
        }
    }
}