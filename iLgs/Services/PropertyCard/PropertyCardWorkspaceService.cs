using System;
using System.Data.Entity;
using System.Linq;
using iLgs.Models;
namespace iLgs.Services.PropertyCard
{
    // Read-only projections. All changes remain in the existing PropertyCard/StockCard services.
    public class PropertyCardWorkspaceService
    {
        private readonly AppManEntities _db;
        private readonly IPsCardItemService _items;
        public PropertyCardWorkspaceService(AppManEntities db, IPsCardItemService items)
        {
            _db = db;
            _items = items;
        }
        public PropertyCardWorkspaceVM Get(Guid id)
        {
            var card = _db.PsCards.AsNoTracking().Where(x => x.Id == id && x.CardCategory == "P")
                .Select(x => new PropertyCardVM {
                    Id = x.Id, PsNo = x.PsNo, Item = x.ItemCode.Description,
                    ItemType = x.ItemCode.ItemType.Description, ItemTypeCode = x.ItemCode.ItemType.Code,
                    Fund = x.Fund, Description = x.Description, CardCategory = x.CardCategory,
                    InsertedBy = x.InsertedBy, InsertedDt = x.InsertedDt,
                    SubAccount = _db.SubAccountViews.Where(a => a.Id == x.ItemCodeId).Select(a => a.SubAccount).FirstOrDefault()
                }).FirstOrDefault();
            if (card == null) return null;
            return new PropertyCardWorkspaceVM { Card = card, Position = Position(id) };
        }
        public PropertyCardPositionVM Position(Guid id)
        {
            // Use the existing transfer projection and its balance-value formula.
            // Only root transfers contribute original receipts.
            var position = _items.GetTransitByCardId(id, null).GroupBy(x => 1)
                .Select(g => new PropertyCardPositionVM {
                    Received = g.Sum(x => x.ParentId == null ? (x.Qty ?? 0) : 0),
                    TransferIn = g.Sum(x => x.TransferIn ?? 0),
                    Issued = g.Sum(x => x.QtyIss ?? 0),
                    TransferOut = g.Sum(x => x.TransferOut ?? 0),
                    Balance = g.Sum(x => x.QtyBal ?? 0),
                    BalanceValue = g.Sum(x => x.Amount ?? 0)
                }).FirstOrDefault() ?? new PropertyCardPositionVM();
            var acquisitions = _db.PsCardItems.AsNoTracking().Where(x => x.PsCardId == id);
            position.AcquisitionCount = acquisitions.Count();
            position.UnitCount = _db.PsCardItemExtns.Count(x => x.PsCardItem.PsCardId == id);
            var latest = acquisitions.OrderByDescending(x => x.PoDate).ThenByDescending(x => x.Id)
                .Select(x => new { x.PoNo, x.PoDate }).FirstOrDefault();
            if (latest != null) { position.LatestPo = latest.PoNo; position.LatestPoDate = latest.PoDate; }
            position.LatestAcquisitionDate = acquisitions.Select(x => x.AcqDate).Max();
            return position;
        }
        public IQueryable<PropertyCardUnitChoiceVM> Units(Guid id)
        {
            return _db.PsCardItemExtns.AsNoTracking().Where(x => x.PsCardItem.PsCardId == id && x.PsCardSubItemId == null)
                .Select(x => new PropertyCardUnitChoiceVM {
                    Id = x.Id, AcquisitionId = x.PsCardItemId, PoNo = x.PsCardItem.PoNo,
                    PropNo = x.PropNo, CustItemNo = x.CustItemNo,
                    Label = (x.PsCardItem.PoNo ?? "No PO") + " / " + (x.PropNo ?? x.CustItemNo ?? "Unnumbered unit"),
                    Location = x.Codextn.Description, Condition = x.Condition
                });
        }
        public IQueryable<PropertyCardHistoryVM> History(Guid id)
        {
            // Stored transaction records for both unit-level and acquisition-level events under this card.
            return from transaction in _db.PsCardItemTransactions.AsNoTracking()
                   join unit in _db.PsCardItemExtns on transaction.PsCardItemExtnId equals unit.Id into unitGroup
                   from unit in unitGroup.DefaultIfEmpty()
                   join item in _db.PsCardItems on transaction.PsCardItemId equals item.Id into itemGroup
                   from item in itemGroup.DefaultIfEmpty()
                   where (unit != null && unit.PsCardItem.PsCardId == id) || (item != null && item.PsCardId == id)
                   select new PropertyCardHistoryVM {
                       Id = transaction.Id,
                       PropNo = unit != null ? unit.PropNo : null,
                       PoNo = unit != null ? unit.PsCardItem.PoNo : (item != null ? item.PoNo : null),
                       Remarks = transaction.Remarks,
                       TransferId = transaction.PsCardItemTransferId,
                       IssuanceId = transaction.PsCardItemIssuanceId,
                       IcsParId = transaction.IcsParId,
                       InsertedBy = transaction.InsertedBy,
                       InsertedDt = transaction.InsertedDt,
                       UpdatedDt = transaction.UpdatedDt
                   };
        }
    }
}