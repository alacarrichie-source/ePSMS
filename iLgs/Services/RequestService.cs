using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using iLgs.Models;
using System.Data.Entity;

namespace iLgs.Services
{
    public class RequestService : IRequestService
    {
        private readonly AppManEntities db = new AppManEntities();
        public RequestService(AppManEntities db)
        {
            this.db = db;        
        }
        public IQueryable<RequestVM> GetAll()
        {
            return db.Requests
                .Select(s => new RequestVM
                {
                    Id = s.Id,
                    PrNo = s.PrNo,
                    PrDate = s.PrDate,
                    Availability = s.Availability,
                    AvaialbilityDesig = s.AvaialbilityDesig,
                    ApprovedBy = s.ApprovedBy,
                    ApprovedDesig = s.ApprovedDesig,
                    SubmittedBy = s.SubmittedBy,
                    SubmittedDt = s.SubmittedDt,
                    IsWithPO = s.Orders.Any(),
                    // Transients from RIS
                    RisNo = s.RISs.RisNo,
                    Fund = s.RISs.Fund,
                    Department = s.RISs.Office,
                    Section = s.RISs.Division,
                    FPP = s.RISs.FPP,
                    Purpose = s.RISs.Purpose,
                    RequestedBy = s.RISs.RequestedBy,
                    RequestedDesig = s.RISs.ReceivedByDesignation,
                    RisDate = s.RISs.RisDate,
                    RisId = s.RisId                    
                })
                .AsQueryable();
        }

        public async Task<Request> GetByIdAsync(Guid? prId)
        {
            return await db.Requests.FindAsync(prId);
        }

        public async Task<bool> IsAnyPrNoAsync(Guid id, string prNo)
        {
            return await db.Requests.AnyAsync(a => a.Id != id && a.PrNo == prNo);
        }

        public async Task<bool> IsAnyRisNoAsync(Guid id, string risNo)
        {
            return await db.Requests.AnyAsync(a => a.Id != id && a.RISs.RisNo == risNo);
        }

        public async Task<Request> GetByPrNoAsync(string prNo)
        {
            return await db.Requests.Where(w => w.PrNo == prNo).FirstOrDefaultAsync();
        }

        public async Task<bool> IsPostedAsync(Guid? requestId)
        {
            var entity = await db.Requests.FindAsync(requestId);
            if (entity == null)
            {
                return false;
            }
            else
            {
                return !string.IsNullOrWhiteSpace(entity.SubmittedBy);
            }
        }

        public async Task<bool> IsWithPOAsync(Guid? requestId)
        {
            return await db.Orders.AnyAsync(a => a.PrId == requestId);
        }

        public async Task<bool> IsPoPostedAsync(Guid? requestId)
        {
            return await db.Orders.AnyAsync(a => a.PrId == requestId && !(a.PostedBy == "" || a.PostedBy == null));
        }

        public async Task<bool> IsWithInvalidUnitCostAsync(Guid? requestId)
        {
            return await db.RequestItems.AnyAsync(a => a.PrId == requestId && (a.UnitCost == null || a.UnitCost == 0));
        }

        public async Task<RequestVM> CreateAsync(RequestVM model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(model.PrNo))
            {
                model.PrNo = NextPrNo((DateTime)model.PrDate);
            }
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new Request()
            {
                Id = model.Id,
                RisId = model.RisId,
                PrNo = model.PrNo,
                PrDate = model.PrDate,
                Availability = model.Availability ?? "",
                AvaialbilityDesig = model.AvaialbilityDesig ?? "",
                ApprovedBy = model.ApprovedBy ?? "",
                ApprovedDesig = model.ApprovedDesig ?? "",
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt

                // Transients From RIS Removed
                //Fund = model.Fund,
                //Department = model.Department,
                //Section = model.Section,
                //PrNo = model.PrNo,
                //PrDate = model.PrDate,
                //FPP = model.FPP,
                //Purpose = model.Purpose,
                //RequestedBy = model.RequestedBy,
                //RequestedDesig = model.RequestedDesig,
            };

            // include items during add
            var risItems = db.RisItems.Include(i => i.RisItemExtns).Where(w => w.RisId == model.RisId).ToList();
            foreach (var risItem in risItems)
            {
                RequestItem requestItem = new RequestItem()
                {
                    Id = Guid.NewGuid(),
                    RisItemId = risItem.Id,
                    PrId = entity.Id,
                    Description = risItem.Description,
                    Qty = risItem.QtyRequest,
                    //UnitCost = risItem.UnitCost,
                    //Amount = risItem.TotalCost,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                foreach (var risItemExtn in risItem.RisItemExtns)
                {
                    RequestItemExtn requestItemExtn = new RequestItemExtn()
                    {
                        Id = Guid.NewGuid(),
                        RequestItemId = requestItem.Id,
                        ItemKey = risItemExtn.ItemKey,
                        ItemValue = risItemExtn.ItemValue,
                        Sequence = risItemExtn.Sequence,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };
                    requestItem.RequestItemExtns.Add(requestItemExtn);
                }

                entity.RequestItems.Add(requestItem);
            }

            db.Requests.Add(entity);
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<RequestVM> UpdateAsync(RequestVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.Requests.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            // if there's a change of requisition item
            if (entity.RisId != model.RisId)
            {
                var requestItems = db.RequestItems.Where(w => w.PrId == model.Id);
                await requestItems.ForEachAsync(f => {
                    f.UpdatedBy = model.UpdatedBy;
                    f.UpdatedDt = model.UpdatedDt;
                });
                await db.SaveChangesAsync();

                db.RequestItems.RemoveRange(requestItems);
                await db.SaveChangesAsync();
                
                // include items during add
                var risItems = db.RisItems.Include(i => i.RisItemExtns).Where(w => w.RisId == model.RisId).ToList();
                foreach (var risItem in risItems)
                {
                    RequestItem requestItem = new RequestItem()
                    {
                        Id = Guid.NewGuid(),
                        RisItemId = risItem.Id,
                        PrId = entity.Id,
                        Description = risItem.Description,
                        Qty = risItem.QtyRequest,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    foreach (var risItemExtn in risItem.RisItemExtns)
                    {
                        RequestItemExtn requestItemExtn = new RequestItemExtn()
                        {
                            Id = Guid.NewGuid(),
                            RequestItemId = requestItem.Id,
                            ItemKey = risItemExtn.ItemKey,
                            ItemValue = risItemExtn.ItemValue,
                            Sequence = risItemExtn.Sequence,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };
                        requestItem.RequestItemExtns.Add(requestItemExtn);
                    }

                    entity.RequestItems.Add(requestItem);
                }
            }

            entity.RisId = model.RisId;
            entity.PrNo = model.PrNo;
            entity.PrDate = model.PrDate;
            entity.Availability = model.Availability ?? "";
            entity.AvaialbilityDesig = model.AvaialbilityDesig ?? "";
            entity.ApprovedBy = model.ApprovedBy ?? "";
            entity.ApprovedDesig = model.ApprovedDesig ?? "";
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
            
            db.Requests.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<RequestVM> DeleteAsync(RequestVM model, string user, DateTime date)
        {

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.Requests.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.Requests.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.Requests.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        }


        public async Task PostAsync(Guid requestId, string user, DateTime date)
        {
            var entity = await db.Requests.FindAsync(requestId);
            if (entity != null)
            {
                entity.SubmittedBy = user;
                entity.SubmittedDt = date;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                db.Requests.Attach(entity);
                db.Entry(entity).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

        public async Task UnpostAsync(Guid requestId, string user, DateTime date)
        {
            var entity = await db.Requests.FindAsync(requestId);
            if (entity != null)
            {
                entity.SubmittedBy = null;
                entity.SubmittedDt = null;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                db.Requests.Attach(entity);
                db.Entry(entity).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

        private string NextPrNo(DateTime prDate)
        {
            string yyyy = prDate.Year.ToString().Trim();
            string mm = prDate.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = db.Requests.Where(w => w.PrDate.Value.Year == prDate.Year).OrderByDescending(o => o.PrNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.PrNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }
    }
}