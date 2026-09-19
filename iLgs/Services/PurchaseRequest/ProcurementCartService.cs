using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using iLgs.Ai.Services;
using iLgs.Models;
using iLgs.Services.PPMP_;
using iLgs.Utilities;

namespace iLgs.Services.PurchaseRequest
{
    public class ProcurementCartService
    {
        public const string StatusActive = "ACTIVE";
        public const string StatusCompleted = "COMPLETED";
        public const string StatusAbandoned = "ABANDONED";
        public static string GetCartMode(ProcurementCart cart)
        {
            if (cart == null)
            {
                return CartModes.Normal;
            }
            if ((cart.RevisionUser != null && cart.RevisionUser.StartsWith("ADMIN_EDIT:")) ||
                (cart.ReviewComment != null && cart.ReviewComment.StartsWith("[CART_MODE:ADMIN_EDIT]")))
            {
                return CartModes.AdminEdit;
            }
            if (cart.IsRevision)
            {
                return CartModes.Revision;
            }
            return CartModes.Normal;
        }

        private readonly AppManEntities _db;
        private readonly IPPMPService _ppmpService;

        public ProcurementCartService(AppManEntities db)
        {
            _db = db;
            _ppmpService = new PPMPService(_db);
        }

                public async Task<ProcurementCart> GetActiveCartEntityAsync(string userId)
        {
            return await GetActiveCartEntityAsync(userId, CartModes.Normal, null);
        }

        public async Task<ProcurementCart> GetActiveCartEntityAsync(string userId, string preferredMode, Guid? requestId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(preferredMode))
            {
                preferredMode = CartModes.Normal;
            }

            var query = _db.ProcurementCarts
                .Include(c => c.ProcurementCartItems.Select(i => i.ProcurementCartSubItems))
                .Where(c => c.UserId == userId && c.Status == StatusActive);

            if (string.Equals(preferredMode, CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase))
            {
                if (!requestId.HasValue || requestId.Value == Guid.Empty)
                {
                    return null;
                }

                var adminQuery = query
                    .Where(c => (c.RevisionUser != null && c.RevisionUser.StartsWith("ADMIN_EDIT:")) ||
                                (c.ReviewComment != null && c.ReviewComment.StartsWith("[CART_MODE:ADMIN_EDIT]")))
                    .Where(c => c.RequestId == requestId.Value);

                return await adminQuery
                    .OrderByDescending(c => c.ProcurementCartItems.Any())
                    .ThenByDescending(c => c.UpdatedDt ?? c.InsertedDt)
                    .FirstOrDefaultAsync();
            }

            if (string.Equals(preferredMode, CartModes.Revision, StringComparison.OrdinalIgnoreCase))
            {
                if (!requestId.HasValue || requestId.Value == Guid.Empty)
                {
                    return null;
                }

                var revQuery = query
                    .Where(c => c.IsRevision &&
                                (c.RevisionUser == null || !c.RevisionUser.StartsWith("ADMIN_EDIT:")) &&
                                (c.ReviewComment == null || !c.ReviewComment.StartsWith("[CART_MODE:ADMIN_EDIT]")))
                    .Where(c => c.RequestId == requestId.Value);

                return await revQuery
                    .OrderByDescending(c => c.UpdatedDt ?? c.InsertedDt)
                    .FirstOrDefaultAsync();
            }

            if (string.Equals(preferredMode, CartModes.Normal, StringComparison.OrdinalIgnoreCase))
            {
                var normQuery = query
                    .Where(c => !c.IsRevision &&
                                (c.RevisionUser == null || !c.RevisionUser.StartsWith("ADMIN_EDIT:")) &&
                                (c.ReviewComment == null || !c.ReviewComment.StartsWith("[CART_MODE:ADMIN_EDIT]")));

                if (requestId.HasValue && requestId.Value != Guid.Empty)
                {
                    normQuery = normQuery.Where(c => c.RequestId == requestId.Value);
                }
                else
                {
                    normQuery = normQuery.Where(c => c.RequestId == null);
                }

                return await normQuery
                    .OrderByDescending(c => c.UpdatedDt ?? c.InsertedDt)
                    .FirstOrDefaultAsync();
            }

            return null;
        }

        public async Task<ProcurementCart> GetActiveWorkingNormalCartEntityAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await _db.ProcurementCarts
                .Include(c => c.ProcurementCartItems.Select(i => i.ProcurementCartSubItems))
                .Where(c => c.UserId == userId &&
                            c.Status == StatusActive &&
                            !c.IsRevision &&
                            (c.RevisionUser == null || !c.RevisionUser.StartsWith("ADMIN_EDIT:")) &&
                            (c.ReviewComment == null || !c.ReviewComment.StartsWith("[CART_MODE:ADMIN_EDIT]")))
                .OrderByDescending(c => c.ProcurementCartItems.Any())
                .ThenByDescending(c => !c.RequestId.HasValue)
                .ThenByDescending(c => c.UpdatedDt ?? c.InsertedDt)
                .FirstOrDefaultAsync();
        }

        public async Task<CartViewModel> GetActiveWorkingNormalCartViewModelAsync(string userId)
        {
            var entity = await GetActiveWorkingNormalCartEntityAsync(userId);
            return entity == null ? new CartViewModel() : MapEntityToViewModel(entity);
        }

        public async Task<int> GetWorkingNormalCartCountAsync(string userId)
        {
            var entity = await GetActiveWorkingNormalCartEntityAsync(userId);
            return entity == null || entity.ProcurementCartItems == null
                ? 0
                : entity.ProcurementCartItems.Count;
        }

        public async Task<ProcurementCart> GetActiveCartEntityByIdAsync(string userId, Guid cartId, string preferredMode, Guid? requestId)
        {
            if (string.IsNullOrWhiteSpace(userId) || cartId == Guid.Empty)
            {
                return null;
            }

            var entity = await _db.ProcurementCarts
                .Include(c => c.ProcurementCartItems.Select(i => i.ProcurementCartSubItems))
                .FirstOrDefaultAsync(c => c.Id == cartId &&
                                          c.UserId == userId &&
                                          c.Status == StatusActive);

            if (entity == null)
            {
                return null;
            }

            var expectedMode = string.IsNullOrWhiteSpace(preferredMode)
                ? CartModes.Normal
                : preferredMode;

            if (!string.Equals(GetCartMode(entity), expectedMode, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (requestId.HasValue && requestId.Value != Guid.Empty &&
                entity.RequestId != requestId.Value)
            {
                return null;
            }

            return entity;
        }

        public async Task<CartViewModel> GetActiveCartViewModelAsync(string userId)
        {
            return await GetActiveCartViewModelAsync(userId, CartModes.Normal, null);
        }

        public async Task<CartViewModel> GetActiveCartViewModelAsync(string userId, string preferredMode, Guid? requestId)
        {
            var entity = await GetActiveCartEntityAsync(userId, preferredMode, requestId);
            if (entity == null)
            {
                return new CartViewModel();
            }

            return MapEntityToViewModel(entity);
        }

 public async Task<int> GetCartCountAsync(string userId)
 {
 return await GetCartCountAsync(userId, CartModes.Normal, null);
 }

 public async Task<int> GetCartCountAsync(string userId, string preferredMode, Guid? requestId)
 {
 if (string.IsNullOrWhiteSpace(userId))
 {
 return 0;
 }

 var cart = await GetActiveCartEntityAsync(userId, preferredMode ?? CartModes.Normal, requestId);
 if (cart == null || cart.ProcurementCartItems == null)
 {
 return 0;
 }

 return cart.ProcurementCartItems.Count;
 }

        public CartViewModel MapEntityToViewModel(ProcurementCart entity)
        {
            if (entity == null)
            {
                return new CartViewModel();
            }

            var mode = GetCartMode(entity);
            var vm = new CartViewModel
            {
                CartId = entity.Id,
                DepartmentId = entity.DepartmentId,
                FiscalYear = entity.FiscalYear,
                RequestId = entity.RequestId,
                IsRevision = entity.IsRevision,
                CartMode = mode,
                SourceRequestId = entity.RequestId,
                Status = entity.Status,
                ReviewComment = entity.ReviewComment,
                RevisionNo = entity.RevisionNo.GetValueOrDefault(),
                RevisionUser = entity.RevisionUser,
                Items = new List<CartItemViewModel>()
            };

            if (string.Equals(mode, CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase) && entity.RequestId.HasValue)
            {
                var pr = _db.Requests.AsNoTracking().FirstOrDefault(r => r.Id == entity.RequestId.Value);
                if (pr != null)
                {
                    vm.AdminEditPrNo = pr.PrNo;
                    vm.AdminEditCtrlNo = pr.CtrlNo;
                    vm.AdminEditDepartment = pr.Department;
                    var hist = _db.DocumentStatusHistories.AsNoTracking()
                        .Where(h => h.DocumentType == DocumentTypes.PurchaseRequest && h.DocumentId == pr.Id)
                        .OrderByDescending(h => h.ChangedDt).ThenByDescending(h => h.Id)
                        .Select(h => h.ToStatus).FirstOrDefault();
                    vm.AdminEditStatus = hist ?? (!string.IsNullOrWhiteSpace(pr.PostedBy) ? PrStatuses.Posted : (!string.IsNullOrWhiteSpace(pr.SubmittedBy) ? PrStatuses.Submitted : PrStatuses.Draft));
                }
            }

            var orderedItems = entity.ProcurementCartItems
                .OrderBy(i => i.SortOrder.GetValueOrDefault())
                .ThenBy(i => i.ItemNo)
                .ToList();

            foreach (var item in orderedItems)
            {
                var itemVm = new CartItemViewModel
                {
                    Id = item.PpmpItemId,
                    RequestItemId = item.RequestItemId,
                    ItemNo = item.ItemNo,
                    Code = item.Code,
                    Description = item.Description,
                    TechnicalSpecifications = item.TechnicalSpecifications,
                    Unit = item.Unit,
                    UnitCost = item.UnitCost,
                    Quantity = item.Quantity,
                    SubItems = new List<CartSubItemViewModel>()
                };

                var orderedSubItems = item.ProcurementCartSubItems
                    .OrderBy(s => s.SortOrder.GetValueOrDefault())
                    .ThenBy(s => s.ItemNo)
                    .ToList();

                foreach (var subItem in orderedSubItems)
                {
                    itemVm.SubItems.Add(new CartSubItemViewModel
                    {
                        Id = subItem.RequestSubItemId ?? subItem.Id,
                        ParentItemId = item.PpmpItemId,
                        ItemNo = subItem.ItemNo,
                        Description = subItem.Description,
                        Unit = subItem.Unit,
                        Quantity = subItem.Quantity,
                        UnitCost = subItem.UnitCost.GetValueOrDefault()
                    });
                }

                vm.Items.Add(itemVm);
            }

            return vm;
        }

        public async Task<ProcurementCart> EnsureActiveCartAsync(string userId, Guid? departmentId, int? fiscalYear, string userName)
        {
 var cart = await GetActiveCartEntityAsync(userId, CartModes.Normal, null);
            var now = DateTime.Now;

            if (cart == null)
            {
                cart = new ProcurementCart
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    DepartmentId = departmentId,
                    FiscalYear = fiscalYear,
                    IsRevision = false,
                    Status = StatusActive,
                    InsertedBy = userName,
                    InsertedDt = now,
                    UpdatedBy = userName,
                    UpdatedDt = now
                };
                _db.ProcurementCarts.Add(cart);
                await _db.SaveChangesAsync();
            }
            else
            {
                bool modified = false;
                if (!cart.DepartmentId.HasValue && departmentId.HasValue)
                {
                    cart.DepartmentId = departmentId;
                    modified = true;
                }
                if (!cart.FiscalYear.HasValue && fiscalYear.HasValue)
                {
                    cart.FiscalYear = fiscalYear;
                    modified = true;
                }
                if (modified)
                {
                    cart.UpdatedBy = userName;
                    cart.UpdatedDt = now;
                    await _db.SaveChangesAsync();
                }
            }

            return cart;
        }

 public async Task<CartViewModel> AddItemAsync(string userId, Guid ppmpItemId, string userName, Guid? departmentId, int? fiscalYear)
 {
 return await AddItemAsync(userId, ppmpItemId, userName, departmentId, fiscalYear, CartModes.Normal, null);
 }

 public async Task<CartViewModel> AddItemAsync(string userId, Guid ppmpItemId, string userName, Guid? departmentId, int? fiscalYear, string preferredMode, Guid? requestId)
 {
 if (string.Equals(preferredMode, CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase))
 {
                throw new InvalidOperationException("New procurement items cannot be added while performing an administrative PR edit.");
 }

 ProcurementCart cart;
 if (string.Equals(preferredMode, CartModes.Revision, StringComparison.OrdinalIgnoreCase))
 {
 cart = await GetActiveCartEntityAsync(userId, CartModes.Revision, requestId);
 if (cart == null)
 {
                    throw new InvalidOperationException("Active revision cart not found.");
 }
 }
 else
 {
 cart = await GetActiveCartEntityAsync(userId, CartModes.Normal, null);
 if (cart == null)
 {
 cart = await EnsureActiveCartAsync(userId, departmentId, fiscalYear, userName);
 }
 }

 if (string.Equals(GetCartMode(cart), CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase))
 {
                throw new InvalidOperationException("New procurement items cannot be added while performing an administrative PR edit.");
 }
            var now = DateTime.Now;

            if (cart == null)
            {
                cart = await EnsureActiveCartAsync(userId, departmentId, fiscalYear, userName);
            }
            if (string.Equals(GetCartMode(cart), CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("New procurement items cannot be added while performing an administrative PR edit.");
            }

            if (cart.IsRevision)
            {
                if (!string.Equals(cart.RevisionUser, userName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("You are not authorized to edit this Purchase Request revision.");
                }
            }

            var ppmpItem = await _ppmpService.PPMPItem.GetByIdAsync(ppmpItemId);
            if (ppmpItem == null)
            {
                throw new InvalidOperationException("Procurement item was not found.");
            }

            if (ppmpItem.PPMP == null || !ppmpItem.PPMP.DeptId.HasValue || !ppmpItem.PPMP.ForYear.HasValue)
            {
                throw new InvalidOperationException("Procurement plan information for this item is incomplete.");
            }

            if (departmentId.HasValue && ppmpItem.PPMP.DeptId != departmentId.Value)
            {
                throw new InvalidOperationException("The item belongs to another department.");
            }

            if (fiscalYear.HasValue && ppmpItem.PPMP.ForYear != fiscalYear.Value)
            {
                throw new InvalidOperationException("The item belongs to another fiscal year.");
            }

            if (cart.ProcurementCartItems.Any())
            {
                if (cart.DepartmentId.HasValue && cart.DepartmentId.Value != ppmpItem.PPMP.DeptId.Value)
                {
                    throw new InvalidOperationException("Your cart belongs to another department. Empty it before changing procurement plans.");
                }
                if (cart.FiscalYear.HasValue && cart.FiscalYear.Value != ppmpItem.PPMP.ForYear.Value)
                {
                    throw new InvalidOperationException("Your cart belongs to another fiscal year. Empty it before changing procurement plans.");
                }
            }
            else
            {
                cart.DepartmentId = ppmpItem.PPMP.DeptId;
                cart.FiscalYear = ppmpItem.PPMP.ForYear;
            }

            if (cart.ProcurementCartItems.Any(i => i.PpmpItemId == ppmpItemId))
            {
                return MapEntityToViewModel(cart);
            }

            var ownUsage = 0;
            if ((cart.IsRevision || string.Equals(GetCartMode(cart), CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase)) && cart.RequestId.HasValue)
            {
                ownUsage = await _db.PPMPItemUsages
                    .Where(x => x.PrId == cart.RequestId.Value && x.PpmpItemId == ppmpItemId)
                    .Select(x => x.Qty)
                    .FirstOrDefaultAsync() ?? 0;
            }

            var availableQty = ppmpItem.QtyBal.GetValueOrDefault() + ownUsage;
            if (availableQty <= 0)
            {
                throw new InvalidOperationException("This item has no remaining quantity available.");
            }

            var nextSort = cart.ProcurementCartItems.Any()
                ? cart.ProcurementCartItems.Max(i => i.SortOrder.GetValueOrDefault()) + 1
                : 1;

            var newItem = new ProcurementCartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                PpmpItemId = ppmpItem.Id,
                Code = ppmpItem.Code,
                Description = ppmpItem.Description,
                TechnicalSpecifications = string.Empty,
                Unit = ppmpItem.Unit,
                UnitCost = ppmpItem.UnitCost,
                Quantity = availableQty,
                SortOrder = nextSort,
                ItemNo = nextSort.ToString()
            };

            cart.ProcurementCartItems.Add(newItem);
            cart.UpdatedBy = userName;
            cart.UpdatedDt = now;

            RenumberCartItems(cart);
            await _db.SaveChangesAsync();

            return MapEntityToViewModel(cart);
        }

        public async Task<CartItemViewModel> UpdateItemAsync(string userId, Guid ppmpItemId, int quantity, string description, string userName)
        {
            return await UpdateItemAsync(userId, ppmpItemId, quantity, description, userName, null, null);
        }

        public async Task<CartItemViewModel> UpdateItemAsync(string userId, Guid ppmpItemId, int quantity, string description, string userName, string preferredMode, Guid? requestId)
        {
            return await UpdateItemAsync(userId, ppmpItemId, quantity, description, userName, preferredMode, requestId, null, null);
        }

        public async Task<CartItemViewModel> UpdateItemAsync(string userId, Guid ppmpItemId, int quantity, string description, string userName, string preferredMode, Guid? requestId, string unit, decimal? unitCost)
        {
            ProcurementCart cart = null;
            if (!string.IsNullOrEmpty(preferredMode))
            {
                cart = await GetActiveCartEntityAsync(userId, preferredMode, requestId);
            }
            else
            {
                var targetItem = await _db.ProcurementCartItems
                    .Include(x => x.ProcurementCart.ProcurementCartItems.Select(s => s.ProcurementCartSubItems))
                    .FirstOrDefaultAsync(x => x.PpmpItemId == ppmpItemId && x.ProcurementCart.UserId == userId && x.ProcurementCart.Status == StatusActive);
                if (targetItem != null)
                {
                    cart = targetItem.ProcurementCart;
                }
                if (cart == null)
                {
                    cart = await GetActiveCartEntityAsync(userId, CartModes.Normal, null);
                }
            }
            if (cart == null)
            {
                throw new InvalidOperationException("Active procurement cart not found.");
            }

            if (cart.IsRevision && !string.Equals(cart.RevisionUser, userName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("You are not authorized to edit this Purchase Request revision.");
            }

            var item = cart.ProcurementCartItems.FirstOrDefault(i => i.PpmpItemId == ppmpItemId);
            if (item == null)
            {
                throw new InvalidOperationException("The cart item no longer exists.");
            }

            if (quantity <= 0)
            {
                throw new InvalidOperationException("Quantity must be at least 1.");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new InvalidOperationException("Description is required.");
            }

            var ppmpItem = await _ppmpService.PPMPItem.GetByIdAsync(ppmpItemId);
            if (ppmpItem == null)
            {
                throw new InvalidOperationException("The procurement item no longer exists.");
            }

            var ownUsage = 0;
            if ((cart.IsRevision || string.Equals(GetCartMode(cart), CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase)) && cart.RequestId.HasValue)
            {
                ownUsage = await _db.PPMPItemUsages
                    .Where(x => x.PrId == cart.RequestId.Value && x.PpmpItemId == ppmpItemId)
                    .Select(x => x.Qty)
                    .FirstOrDefaultAsync() ?? 0;
            }

            var availableQuantity = ppmpItem.QtyBal.GetValueOrDefault() + ownUsage;
            if (quantity > availableQuantity)
            {
                throw new InvalidOperationException(string.Format("Quantity cannot exceed the remaining quantity of {0}.", availableQuantity));
            }

            item.Description = description.Trim();
            item.Quantity = quantity;
            if (string.Equals(GetCartMode(cart), CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(unit))
                {
                    item.Unit = unit.Trim();
                }
                if (unitCost.HasValue && unitCost.Value >= 0)
                {
                    item.UnitCost = unitCost.Value;
                }
            }
            cart.UpdatedBy = userName;
            cart.UpdatedDt = DateTime.Now;

            await _db.SaveChangesAsync();

            var vm = MapEntityToViewModel(cart);
            return vm.Items.FirstOrDefault(i => i.Id == ppmpItemId);
        }

        public async Task<CartViewModel> RemoveItemAsync(string userId, Guid ppmpItemId, string userName)
        {
            return await RemoveItemAsync(userId, ppmpItemId, userName, null, null);
        }

        public async Task<CartViewModel> RemoveItemAsync(string userId, Guid ppmpItemId, string userName, string preferredMode, Guid? requestId)
        {
            ProcurementCart cart = null;
            if (!string.IsNullOrEmpty(preferredMode))
            {
                cart = await GetActiveCartEntityAsync(userId, preferredMode, requestId);
            }
            else
            {
                var targetItem = await _db.ProcurementCartItems
                    .Include(x => x.ProcurementCart.ProcurementCartItems.Select(s => s.ProcurementCartSubItems))
                    .FirstOrDefaultAsync(x => x.PpmpItemId == ppmpItemId && x.ProcurementCart.UserId == userId && x.ProcurementCart.Status == StatusActive);
                if (targetItem != null)
                {
                    cart = targetItem.ProcurementCart;
                }
                if (cart == null)
                {
                    cart = await GetActiveCartEntityAsync(userId, CartModes.Normal, null);
                }
            }
            if (cart == null)
            {
                return new CartViewModel();
            }

            if (cart.IsRevision && !string.Equals(cart.RevisionUser, userName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("You are not authorized to edit this Purchase Request revision.");
            }

            var item = cart.ProcurementCartItems.FirstOrDefault(i => i.PpmpItemId == ppmpItemId);
            if (item != null)
            {
                _db.ProcurementCartSubItems.RemoveRange(item.ProcurementCartSubItems.ToList());
                _db.ProcurementCartItems.Remove(item);
                cart.ProcurementCartItems.Remove(item);

                RenumberCartItems(cart);
                cart.UpdatedBy = userName;
                cart.UpdatedDt = DateTime.Now;

                await _db.SaveChangesAsync();
            }

            return MapEntityToViewModel(cart);
        }

        public async Task<CartSubItemViewModel> AddSubItemAsync(string userId, Guid parentItemId, CartSubItemViewModel subItem, string userName)
        {
            return await AddSubItemAsync(userId, parentItemId, subItem, userName, null, null);
        }

        public async Task<CartSubItemViewModel> AddSubItemAsync(string userId, Guid parentItemId, CartSubItemViewModel subItem, string userName, string preferredMode, Guid? requestId)
        {
            ProcurementCart cart = null;
            if (!string.IsNullOrEmpty(preferredMode))
            {
                cart = await GetActiveCartEntityAsync(userId, preferredMode, requestId);
            }
            else
            {
                var targetItem = await _db.ProcurementCartItems
                    .Include(x => x.ProcurementCart.ProcurementCartItems.Select(s => s.ProcurementCartSubItems))
                    .FirstOrDefaultAsync(x => x.PpmpItemId == parentItemId && x.ProcurementCart.UserId == userId && x.ProcurementCart.Status == StatusActive);
                if (targetItem != null)
                {
                    cart = targetItem.ProcurementCart;
                }
                if (cart == null)
                {
                    cart = await GetActiveCartEntityAsync(userId, CartModes.Normal, null);
                }
            }
            if (cart == null)
            {
                throw new InvalidOperationException("Active procurement cart not found.");
            }

            if (cart.IsRevision && !string.Equals(cart.RevisionUser, userName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("You are not authorized to edit this Purchase Request revision.");
            }

            var cartItem = cart.ProcurementCartItems.FirstOrDefault(i => i.PpmpItemId == parentItemId);
            if (cartItem == null)
            {
                throw new InvalidOperationException("The parent cart item no longer exists.");
            }

            if (string.IsNullOrWhiteSpace(subItem.Description) || string.IsNullOrWhiteSpace(subItem.Unit) || subItem.Quantity <= 0 || subItem.UnitCost < 0)
            {
                throw new InvalidOperationException("Sub-item requires a description, unit, positive quantity, and non-negative unit cost.");
            }

            var nextSort = cartItem.ProcurementCartSubItems.Any()
                ? cartItem.ProcurementCartSubItems.Max(s => s.SortOrder.GetValueOrDefault()) + 1
                : 1;

            var newSubItem = new ProcurementCartSubItem
            {
                Id = Guid.NewGuid(),
                CartItemId = cartItem.Id,
                RequestSubItemId = null,
                Description = subItem.Description.Trim(),
                Unit = subItem.Unit.Trim(),
                Quantity = subItem.Quantity,
                UnitCost = subItem.UnitCost,
                SortOrder = nextSort,
                ItemNo = string.Format("{0}.{1}", cartItem.ItemNo, nextSort)
            };

            cartItem.ProcurementCartSubItems.Add(newSubItem);
            cart.UpdatedBy = userName;
            cart.UpdatedDt = DateTime.Now;

            RenumberSubItems(cartItem);
            await _db.SaveChangesAsync();

            subItem.Id = newSubItem.Id;
            subItem.ParentItemId = parentItemId;
            subItem.ItemNo = newSubItem.ItemNo;
            return subItem;
        }

        public async Task<CartSubItemViewModel> UpdateSubItemAsync(string userId, Guid parentItemId, CartSubItemViewModel subItem, string userName)
        {
            return await UpdateSubItemAsync(userId, parentItemId, subItem, userName, null, null);
        }

        public async Task<CartSubItemViewModel> UpdateSubItemAsync(string userId, Guid parentItemId, CartSubItemViewModel subItem, string userName, string preferredMode, Guid? requestId)
        {
            ProcurementCart cart = null;
            if (!string.IsNullOrEmpty(preferredMode))
            {
                cart = await GetActiveCartEntityAsync(userId, preferredMode, requestId);
            }
            else
            {
                var targetItem = await _db.ProcurementCartItems
                    .Include(x => x.ProcurementCart.ProcurementCartItems.Select(s => s.ProcurementCartSubItems))
                    .FirstOrDefaultAsync(x => x.PpmpItemId == parentItemId && x.ProcurementCart.UserId == userId && x.ProcurementCart.Status == StatusActive);
                if (targetItem != null)
                {
                    cart = targetItem.ProcurementCart;
                }
                if (cart == null)
                {
                    cart = await GetActiveCartEntityAsync(userId, CartModes.Normal, null);
                }
            }
            if (cart == null)
            {
                throw new InvalidOperationException("Active procurement cart not found.");
            }

            if (cart.IsRevision && !string.Equals(cart.RevisionUser, userName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("You are not authorized to edit this Purchase Request revision.");
            }

            var cartItem = cart.ProcurementCartItems.FirstOrDefault(i => i.PpmpItemId == parentItemId);
            if (cartItem == null)
            {
                throw new InvalidOperationException("The parent cart item no longer exists.");
            }

            var existing = cartItem.ProcurementCartSubItems.FirstOrDefault(s => s.Id == subItem.Id);
            if (existing == null)
            {
                throw new InvalidOperationException("The cart sub-item no longer exists.");
            }

            if (string.IsNullOrWhiteSpace(subItem.Description) || string.IsNullOrWhiteSpace(subItem.Unit) || subItem.Quantity <= 0 || subItem.UnitCost < 0)
            {
                throw new InvalidOperationException("Sub-item requires a description, unit, positive quantity, and non-negative unit cost.");
            }

            existing.Description = subItem.Description.Trim();
            existing.Unit = subItem.Unit.Trim();
            existing.Quantity = subItem.Quantity;
            existing.UnitCost = subItem.UnitCost;
            cart.UpdatedBy = userName;
            cart.UpdatedDt = DateTime.Now;

            await _db.SaveChangesAsync();

            subItem.ParentItemId = parentItemId;
            subItem.ItemNo = existing.ItemNo;
            return subItem;
        }

        public async Task RemoveSubItemAsync(string userId, Guid parentItemId, Guid subItemId, string userName)
        {
            await RemoveSubItemAsync(userId, parentItemId, subItemId, userName, null, null);
        }

        public async Task RemoveSubItemAsync(string userId, Guid parentItemId, Guid subItemId, string userName, string preferredMode, Guid? requestId)
        {
            ProcurementCart cart = null;
            if (!string.IsNullOrEmpty(preferredMode))
            {
                cart = await GetActiveCartEntityAsync(userId, preferredMode, requestId);
            }
            else
            {
                var targetItem = await _db.ProcurementCartItems
                    .Include(x => x.ProcurementCart.ProcurementCartItems.Select(s => s.ProcurementCartSubItems))
                    .FirstOrDefaultAsync(x => x.PpmpItemId == parentItemId && x.ProcurementCart.UserId == userId && x.ProcurementCart.Status == StatusActive);
                if (targetItem != null)
                {
                    cart = targetItem.ProcurementCart;
                }
                if (cart == null)
                {
                    cart = await GetActiveCartEntityAsync(userId, CartModes.Normal, null);
                }
            }
            if (cart == null)
            {
                return;
            }

            if (cart.IsRevision && !string.Equals(cart.RevisionUser, userName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("You are not authorized to edit this Purchase Request revision.");
            }

            var cartItem = cart.ProcurementCartItems.FirstOrDefault(i => i.PpmpItemId == parentItemId);
            if (cartItem == null)
            {
                return;
            }

            var existing = cartItem.ProcurementCartSubItems.FirstOrDefault(s => s.Id == subItemId);
            if (existing != null)
            {
                _db.ProcurementCartSubItems.Remove(existing);
                cartItem.ProcurementCartSubItems.Remove(existing);

                RenumberSubItems(cartItem);
                cart.UpdatedBy = userName;
                cart.UpdatedDt = DateTime.Now;

                await _db.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(string userId, string userName)
        {
            await ClearCartAsync(userId, userName, CartModes.Normal, null);
        }

        public async Task ClearCartAsync(string userId, string userName, string preferredMode, Guid? requestId)
        {
            var cart = await GetActiveCartEntityAsync(userId, preferredMode ?? CartModes.Normal, requestId);
            if (cart == null)
            {
                return;
            }

            var items = cart.ProcurementCartItems.ToList();
            foreach (var item in items)
            {
                _db.ProcurementCartSubItems.RemoveRange(item.ProcurementCartSubItems.ToList());
            }
            _db.ProcurementCartItems.RemoveRange(items);
            cart.ProcurementCartItems.Clear();

            cart.DepartmentId = null;
            cart.FiscalYear = null;
            cart.RequestId = null;
            cart.IsRevision = false;
            cart.RevisionNo = null;
            cart.RevisionUser = null;
            cart.ReviewComment = null;
            cart.Status = StatusActive;
            cart.UpdatedBy = userName;
            cart.UpdatedDt = DateTime.Now;

            await _db.SaveChangesAsync();
        }

                public async Task AbandonCartAsync(Guid cartId, string userName)
        {
            var cart = await _db.ProcurementCarts.FirstOrDefaultAsync(c => c.Id == cartId);
            if (cart != null)
            {
                cart.Status = StatusAbandoned;
                cart.UpdatedBy = userName;
                cart.UpdatedDt = DateTime.Now;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<ProcurementCart> StartDraftCartAsync(Guid requestId, string userId, string userName, Access access)
        {
            if (!access.IsAllowed || !access.AllowEdit)
            {
                throw new InvalidOperationException("Draft edit access denied.");
            }

            var activeNormalCarts = await _db.ProcurementCarts
                .Include(c => c.ProcurementCartItems.Select(i => i.ProcurementCartSubItems))
                .Where(c => c.UserId == userId &&
                            c.Status == StatusActive &&
                            !c.IsRevision &&
                            (c.RevisionUser == null || !c.RevisionUser.StartsWith("ADMIN_EDIT:")) &&
                            (c.ReviewComment == null || !c.ReviewComment.StartsWith("[CART_MODE:ADMIN_EDIT]")))
                .OrderByDescending(c => c.UpdatedDt ?? c.InsertedDt)
                .ToListAsync();

            var activeDraftCarts = activeNormalCarts
                .Where(c => c.RequestId == requestId)
                .ToList();

            var existingCart = activeDraftCarts
                .FirstOrDefault(c => c.ProcurementCartItems != null && c.ProcurementCartItems.Any())
                ?? activeDraftCarts.FirstOrDefault();

            if (existingCart != null &&
                existingCart.ProcurementCartItems != null &&
                existingCart.ProcurementCartItems.Any())
            {
                var cartsToAbandon = activeNormalCarts
                    .Where(c => c.Id != existingCart.Id)
                    .ToList();

                foreach (var cartToAbandon in cartsToAbandon)
                {
                    cartToAbandon.Status = StatusAbandoned;
                    cartToAbandon.UpdatedBy = userName;
                    cartToAbandon.UpdatedDt = DateTime.Now;
                }

                if (cartsToAbandon.Any())
                {
                    await _db.SaveChangesAsync();
                }

                return existingCart;
            }

            var entity = await _db.Requests
                .Include(x => x.RequestItems.Select(i => i.RequestSubItems))
                .Include(x => x.RequestItems.Select(i => i.PPMPItem.PPMP))
                .FirstOrDefaultAsync(x => x.Id == requestId);

            if (entity == null)
            {
                throw new InvalidOperationException("The Purchase Request no longer exists.");
            }

            var latestStatus = await _db.DocumentStatusHistories
                .AsNoTracking()
                .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == requestId)
                .OrderByDescending(x => x.ChangedDt)
                .ThenByDescending(x => x.Id)
                .Select(x => x.ToStatus)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(latestStatus))
            {
                latestStatus = !string.IsNullOrWhiteSpace(entity.SubmittedBy)
                    ? PrStatuses.Submitted
                    : PrStatuses.Draft;
            }

            if (!string.Equals(latestStatus, PrStatuses.Draft, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only a Draft Purchase Request can be continued.");
            }

            if (!string.Equals(entity.InsertedBy, userName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only the original requester can continue this Draft Purchase Request.");
            }

            if (!entity.RequestItems.Any())
            {
                throw new InvalidOperationException("The Purchase Request has no items to continue.");
            }

            if (entity.RequestItems.Any(x => !x.PpmpItemId.HasValue || x.PPMPItem == null || x.PPMPItem.PPMP == null))
            {
                throw new InvalidOperationException("A Purchase Request item is no longer linked to the Annual Procurement Plan.");
            }

            if (entity.RequestItems.Any(x => x.Qty.GetValueOrDefault() <= 0 ||
                                             x.Qty.GetValueOrDefault() != Math.Truncate(x.Qty.GetValueOrDefault())))
            {
                throw new InvalidOperationException("The shared cart requires whole-number item quantities.");
            }

            var sourceItemCount = entity.RequestItems.Count;
            var sourceSubItemCount = entity.RequestItems.Sum(x => x.RequestSubItems.Count);
            var fiscalYear = entity.RequestItems.Select(x => x.PPMPItem.PPMP.ForYear).FirstOrDefault();
            var now = DateTime.Now;

            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    foreach (var stale in activeNormalCarts)
                    {
                        stale.Status = StatusAbandoned;
                        stale.UpdatedBy = userName;
                        stale.UpdatedDt = now;
                    }

                    var draftCart = new ProcurementCart
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        DepartmentId = entity.DeptId,
                        FiscalYear = fiscalYear,
                        RequestId = entity.Id,
                        IsRevision = false,
                        RevisionNo = null,
                        RevisionUser = null,
                        ReviewComment = null,
                        Status = StatusActive,
                        InsertedBy = userName,
                        InsertedDt = now,
                        UpdatedBy = userName,
                        UpdatedDt = now
                    };

                    var itemOrder = 1;
                    foreach (var reqItem in entity.RequestItems.OrderBy(x => x.ItemNoIndex))
                    {
                        var cartItem = new ProcurementCartItem
                        {
                            Id = Guid.NewGuid(),
                            CartId = draftCart.Id,
                            PpmpItemId = reqItem.PpmpItemId.Value,
                            RequestItemId = reqItem.Id,
                            ItemNo = itemOrder.ToString(),
                            Code = reqItem.PpmpCode,
                            Description = reqItem.Description,
                            TechnicalSpecifications = reqItem.OtherDesc,
                            Unit = reqItem.Unit,
                            Quantity = Convert.ToInt32(reqItem.Qty.GetValueOrDefault()),
                            UnitCost = reqItem.UnitCost,
                            SortOrder = itemOrder
                        };

                        var subOrder = 1;
                        foreach (var reqSubItem in reqItem.RequestSubItems.OrderBy(x => x.ItemNoIndex))
                        {
                            cartItem.ProcurementCartSubItems.Add(new ProcurementCartSubItem
                            {
                                Id = Guid.NewGuid(),
                                CartItemId = cartItem.Id,
                                RequestSubItemId = reqSubItem.Id,
                                ItemNo = string.Format("{0}.{1}", itemOrder, subOrder),
                                Description = reqSubItem.Description,
                                Unit = reqSubItem.Unit,
                                Quantity = reqSubItem.Qty.GetValueOrDefault(),
                                UnitCost = reqSubItem.UnitCost.GetValueOrDefault(),
                                SortOrder = subOrder
                            });
                            subOrder++;
                        }

                        draftCart.ProcurementCartItems.Add(cartItem);
                        itemOrder++;
                    }

                    _db.ProcurementCarts.Add(draftCart);
                    await _db.SaveChangesAsync();

                    var persistedItemCount = await _db.ProcurementCartItems.CountAsync(x => x.CartId == draftCart.Id);
                    if (persistedItemCount != sourceItemCount)
                    {
                        throw new InvalidOperationException("Draft cart initialization failed.");
                    }

                    if (sourceSubItemCount > 0)
                    {
                        var persistedSubItemCount = await _db.ProcurementCartSubItems
                            .CountAsync(x => x.ProcurementCartItem.CartId == draftCart.Id);
                        if (persistedSubItemCount != sourceSubItemCount)
                        {
                            throw new InvalidOperationException("Draft cart sub-item initialization failed.");
                        }
                    }

                    transaction.Commit();

                    var persistedCart = await GetActiveCartEntityByIdAsync(
                        userId,
                        draftCart.Id,
                        CartModes.Normal,
                        requestId);

                    return persistedCart ?? draftCart;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public async Task<ProcurementCart> StartAdminEditCartAsync(Guid requestId, string userId, string userName, Access access)
        {
            if (!access.IsAllowed || (!access.IsAdmin && !access.AllowEdit && !access.AllowPost))
            {
                throw new InvalidOperationException("Admin Edit access denied.");
            }

            var activeAdminCarts = await _db.ProcurementCarts
                .Include(c => c.ProcurementCartItems.Select(i => i.ProcurementCartSubItems))
                .Where(c => c.UserId == userId &&
                            c.RequestId == requestId &&
                            c.Status == StatusActive &&
                            ((c.RevisionUser != null && c.RevisionUser.StartsWith("ADMIN_EDIT:")) ||
                             (c.ReviewComment != null && c.ReviewComment.StartsWith("[CART_MODE:ADMIN_EDIT]"))))
                .OrderByDescending(c => c.UpdatedDt ?? c.InsertedDt)
                .ToListAsync();

            var existingCart = activeAdminCarts
                .FirstOrDefault(c => c.ProcurementCartItems != null && c.ProcurementCartItems.Any())
                ?? activeAdminCarts.FirstOrDefault();

            if (existingCart != null)
            {
                var prHasItems = await _db.RequestItems.AnyAsync(x => x.PrId == requestId);
                if (existingCart.ProcurementCartItems != null && existingCart.ProcurementCartItems.Any())
                {
                    var duplicateCarts = activeAdminCarts.Where(c => c.Id != existingCart.Id).ToList();
                    foreach (var duplicate in duplicateCarts)
                    {
                        duplicate.Status = StatusAbandoned;
                        duplicate.UpdatedBy = userName;
                        duplicate.UpdatedDt = DateTime.Now;
                    }

                    if (duplicateCarts.Any())
                    {
                        await _db.SaveChangesAsync();
                    }

                    return existingCart;
                }

                if (prHasItems)
                {
                    // All empty/stale ADMIN_EDIT carts are abandoned before rebuilding.
                    foreach (var staleCart in activeAdminCarts)
                    {
                        staleCart.Status = StatusAbandoned;
                        staleCart.UpdatedBy = userName;
                        staleCart.UpdatedDt = DateTime.Now;
                    }

                    await _db.SaveChangesAsync();
                }
                else
                {
                    return existingCart;
                }
            }

            var entity = await _db.Requests
                .Include(x => x.RequestItems.Select(i => i.RequestSubItems))
                .Include(x => x.RequestItems.Select(i => i.PPMPItem.PPMP))
                .FirstOrDefaultAsync(x => x.Id == requestId);

            if (entity == null)
            {
                throw new InvalidOperationException("The Purchase Request no longer exists.");
            }

            await new PurchaseRequestLifecycleService(_db).EnsureEditableAsync(requestId, allowAdminEdit: true);

            if (!entity.RequestItems.Any())
            {
                throw new InvalidOperationException("The Purchase Request has no items to edit.");
            }

            if (entity.RequestItems.Any(x => !x.PpmpItemId.HasValue || x.PPMPItem == null || x.PPMPItem.PPMP == null))
            {
                throw new InvalidOperationException("A Purchase Request item is no longer linked to the Annual Procurement Plan.");
            }

            if (entity.RequestItems.Any(x => x.Qty.GetValueOrDefault() <= 0 || x.Qty.GetValueOrDefault() != Math.Truncate(x.Qty.GetValueOrDefault())))
            {
                throw new InvalidOperationException("The shared cart requires whole-number item quantities.");
            }

            var sourceItemCount = entity.RequestItems.Count;
            var sourceSubItemCount = entity.RequestItems.Sum(x => x.RequestSubItems.Count);
            var fiscalYear = entity.RequestItems.Select(x => x.PPMPItem.PPMP.ForYear).FirstOrDefault();
            var now = DateTime.Now;

            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    // Build the complete entity graph FIRST, before calling DbSet.Add().
                    // EF6 discovers and marks the entire graph as Added when the root is added.
                    // Adding children after DbSet.Add() on a HashSet<T> navigation property
                    // may not reliably enter the Added state in the change tracker.
                    var adminCart = new ProcurementCart
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        DepartmentId = entity.DeptId,
                        FiscalYear = fiscalYear,
                        RequestId = entity.Id,
                        IsRevision = false,
                        RevisionNo = null,
                        RevisionUser = "ADMIN_EDIT:" + userName,
                        Status = StatusActive,
                        ReviewComment = "[CART_MODE:ADMIN_EDIT][INITIALIZED]",
                        InsertedBy = userName,
                        InsertedDt = now,
                        UpdatedBy = userName,
                        UpdatedDt = now
                    };

                    var itemOrder = 1;
                    foreach (var reqItem in entity.RequestItems.OrderBy(x => x.ItemNoIndex))
                    {
                        var cartItem = new ProcurementCartItem
                        {
                            Id = Guid.NewGuid(),
                            CartId = adminCart.Id,
                            PpmpItemId = reqItem.PpmpItemId.Value,
                            RequestItemId = reqItem.Id,
                            ItemNo = itemOrder.ToString(),
                            Code = reqItem.PpmpCode,
                            Description = reqItem.Description,
                            TechnicalSpecifications = reqItem.OtherDesc,
                            Unit = reqItem.Unit,
                            Quantity = Convert.ToInt32(reqItem.Qty.GetValueOrDefault()),
                            UnitCost = reqItem.UnitCost,
                            SortOrder = itemOrder
                        };

                        var subOrder = 1;
                        foreach (var reqSubItem in reqItem.RequestSubItems.OrderBy(x => x.ItemNoIndex))
                        {
                            var cartSubItem = new ProcurementCartSubItem
                            {
                                Id = Guid.NewGuid(),
                                CartItemId = cartItem.Id,
                                RequestSubItemId = reqSubItem.Id,
                                ItemNo = string.Format("{0}.{1}", itemOrder, subOrder),
                                Description = reqSubItem.Description,
                                Unit = reqSubItem.Unit,
                                Quantity = reqSubItem.Qty.GetValueOrDefault(),
                                UnitCost = reqSubItem.UnitCost.GetValueOrDefault(),
                                SortOrder = subOrder
                            };
                            cartItem.ProcurementCartSubItems.Add(cartSubItem);
                            subOrder++;
                        }

                        adminCart.ProcurementCartItems.Add(cartItem);
                        itemOrder++;
                    }

                    // Add the complete graph after all children are attached
                    _db.ProcurementCarts.Add(adminCart);

                    await _db.SaveChangesAsync();

                    // Verify persisted item count matches source
                    var persistedItemCount = await _db.ProcurementCartItems.CountAsync(x => x.CartId == adminCart.Id);
                    if (persistedItemCount != sourceItemCount)
                    {
                        throw new InvalidOperationException(string.Format(
                            "Admin Edit cart initialization failed. Expected {0} item(s), but {1} item(s) were persisted.",
                            sourceItemCount, persistedItemCount));
                    }

                    // Verify persisted sub-item count if source has sub-items
                    if (sourceSubItemCount > 0)
                    {
                        var persistedSubItemCount = await _db.ProcurementCartSubItems
                            .CountAsync(x => x.ProcurementCartItem.CartId == adminCart.Id);
                        if (persistedSubItemCount != sourceSubItemCount)
                        {
                            throw new InvalidOperationException(string.Format(
                                "Admin Edit cart initialization failed. Expected {0} sub-item(s), but {1} sub-item(s) were persisted.",
                                sourceSubItemCount, persistedSubItemCount));
                        }
                    }

                    var duplicateActiveCarts = await _db.ProcurementCarts
                        .Where(c => c.Id != adminCart.Id &&
                                    c.UserId == userId &&
                                    c.RequestId == requestId &&
                                    c.Status == StatusActive &&
                                    ((c.RevisionUser != null && c.RevisionUser.StartsWith("ADMIN_EDIT:")) ||
                                     (c.ReviewComment != null && c.ReviewComment.StartsWith("[CART_MODE:ADMIN_EDIT]"))))
                        .ToListAsync();

                    foreach (var duplicate in duplicateActiveCarts)
                    {
                        duplicate.Status = StatusAbandoned;
                        duplicate.UpdatedBy = userName;
                        duplicate.UpdatedDt = DateTime.Now;
                    }

                    if (duplicateActiveCarts.Any())
                    {
                        await _db.SaveChangesAsync();
                    }

                    transaction.Commit();

                    // Reload the exact cart by Id so the caller gets the same verified cart.
                    var persistedCart = await GetActiveCartEntityByIdAsync(
                        userId,
                        adminCart.Id,
                        CartModes.AdminEdit,
                        requestId);
                    return persistedCart ?? adminCart;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public async Task CompleteCartAsync(Guid cartId, string userName)
        {
            var cart = await _db.ProcurementCarts.FirstOrDefaultAsync(c => c.Id == cartId);
            if (cart != null)
            {
                cart.Status = StatusCompleted;
                cart.UpdatedBy = userName;
                cart.UpdatedDt = DateTime.Now;
                await _db.SaveChangesAsync();
            }
        }
        public async Task<ProcurementCart> StartRevisionCartAsync(Guid requestId, string userId, string userName, Access access)
        {
            if (!access.IsAllowed || !access.AllowEdit)
            {
                throw new InvalidOperationException("Revision access denied.");
            }

            // Fix: Only resume an existing Revision cart if it actually has items.
            // An empty stale cart must be abandoned and rebuilt from the source PR.
            var existingCart = await GetActiveCartEntityAsync(userId, CartModes.Revision, requestId);
            if (existingCart != null && existingCart.ProcurementCartItems != null && existingCart.ProcurementCartItems.Any())
            {
                return existingCart;
            }

            var entity = await _db.Requests
                .Include(x => x.RequestItems.Select(i => i.RequestSubItems))
                .Include(x => x.RequestItems.Select(i => i.PPMPItem.PPMP))
                .FirstOrDefaultAsync(x => x.Id == requestId);

            if (entity == null)
            {
                throw new InvalidOperationException("The Purchase Request no longer exists.");
            }

            var historyService = new DocumentHistoryService(_db);
            var latest = await historyService.GetLatestHistoryAsync(DocumentTypes.PurchaseRequest, requestId);
            var isReturned = latest != null && string.Equals(latest.ToStatus, PrStatuses.Returned, StringComparison.OrdinalIgnoreCase);
            var isRevising = latest != null && string.Equals(latest.ToStatus, PrStatuses.Revising, StringComparison.OrdinalIgnoreCase);

            if (!isReturned && !isRevising)
            {
                throw new InvalidOperationException("Only a Returned Purchase Request or an active revision can be opened.");
            }

            if (!string.Equals(entity.InsertedBy, userName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only the original requester can revise this Purchase Request.");
            }

            if (!entity.RequestItems.Any())
            {
                throw new InvalidOperationException("The Purchase Request has no items to revise.");
            }

            if (entity.RequestItems.Any(x => !x.PpmpItemId.HasValue || x.PPMPItem == null || x.PPMPItem.PPMP == null))
            {
                throw new InvalidOperationException("A Purchase Request item is no longer linked to the Annual Procurement Plan.");
            }

            if (entity.RequestItems.Any(x => x.Qty.GetValueOrDefault() <= 0 || x.Qty.GetValueOrDefault() != Math.Truncate(x.Qty.GetValueOrDefault())))
            {
                throw new InvalidOperationException("The shared cart requires whole-number item quantities.");
            }

            var revisionNo = await _db.DocumentStatusHistories.CountAsync(x =>
                x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == requestId && x.Action.StartsWith("Resubmitted")) + 1;

            var returnComment = await _db.DocumentStatusHistories.AsNoTracking()
                .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == requestId && x.ToStatus == PrStatuses.Returned)
                .OrderByDescending(x => x.ChangedDt).ThenByDescending(x => x.Id)
                .Select(x => x.Remarks).FirstOrDefaultAsync();

            var sourceItemCount = entity.RequestItems.Count;
            var sourceSubItemCount = entity.RequestItems.Sum(x => x.RequestSubItems.Count);
            var fiscalYear = entity.RequestItems.Select(x => x.PPMPItem.PPMP.ForYear).FirstOrDefault();
            var now = DateTime.Now;

            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    // Abandon stale empty cart if present
                    if (existingCart != null && !existingCart.ProcurementCartItems.Any())
                    {
                        existingCart.Status = StatusAbandoned;
                        existingCart.UpdatedBy = userName;
                        existingCart.UpdatedDt = now;
                    }

                    // Build the complete entity graph FIRST, before calling DbSet.Add().
                    var revisionCart = new ProcurementCart
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        DepartmentId = entity.DeptId,
                        FiscalYear = fiscalYear,
                        RequestId = entity.Id,
                        IsRevision = true,
                        RevisionNo = revisionNo,
                        RevisionUser = userName,
                        Status = StatusActive,
                        ReviewComment = returnComment,
                        InsertedBy = userName,
                        InsertedDt = now,
                        UpdatedBy = userName,
                        UpdatedDt = now
                    };

                    var itemOrder = 1;
                    foreach (var reqItem in entity.RequestItems.OrderBy(x => x.ItemNoIndex))
                    {
                        var cartItem = new ProcurementCartItem
                        {
                            Id = Guid.NewGuid(),
                            CartId = revisionCart.Id,
                            PpmpItemId = reqItem.PpmpItemId.Value,
                            RequestItemId = reqItem.Id,
                            ItemNo = itemOrder.ToString(),
                            Code = reqItem.PpmpCode,
                            Description = reqItem.Description,
                            TechnicalSpecifications = reqItem.OtherDesc,
                            Unit = reqItem.Unit,
                            Quantity = Convert.ToInt32(reqItem.Qty.GetValueOrDefault()),
                            UnitCost = reqItem.UnitCost,
                            SortOrder = itemOrder
                        };

                        var subOrder = 1;
                        foreach (var reqSubItem in reqItem.RequestSubItems.OrderBy(x => x.ItemNoIndex))
                        {
                            var cartSubItem = new ProcurementCartSubItem
                            {
                                Id = Guid.NewGuid(),
                                CartItemId = cartItem.Id,
                                RequestSubItemId = reqSubItem.Id,
                                ItemNo = string.Format("{0}.{1}", itemOrder, subOrder),
                                Description = reqSubItem.Description,
                                Unit = reqSubItem.Unit,
                                Quantity = reqSubItem.Qty.GetValueOrDefault(),
                                UnitCost = reqSubItem.UnitCost.GetValueOrDefault(),
                                SortOrder = subOrder
                            };
                            cartItem.ProcurementCartSubItems.Add(cartSubItem);
                            subOrder++;
                        }

                        revisionCart.ProcurementCartItems.Add(cartItem);
                        itemOrder++;
                    }

                    // Add the complete graph after all children are attached
                    _db.ProcurementCarts.Add(revisionCart);

                    if (isReturned)
                    {
                        historyService.AddStatusHistory(
                            DocumentTypes.PurchaseRequest,
                            entity.Id,
                            entity.PrNo ?? entity.CtrlNo,
                            PrStatuses.Returned,
                            PrStatuses.Revising,
                            "Revision Started",
                            string.Format("Purchase Request revision {0} started.", revisionNo),
                            userName);
                    }

                    await _db.SaveChangesAsync();

                    // Verify persisted item count matches source
                    var persistedItemCount = await _db.ProcurementCartItems.CountAsync(x => x.CartId == revisionCart.Id);
                    if (persistedItemCount != sourceItemCount)
                    {
                        throw new InvalidOperationException(string.Format(
                            "Revision cart initialization failed. Expected {0} item(s), but {1} item(s) were persisted.",
                            sourceItemCount, persistedItemCount));
                    }

                    // Verify persisted sub-item count if source has sub-items
                    if (sourceSubItemCount > 0)
                    {
                        var persistedSubItemCount = await _db.ProcurementCartSubItems
                            .CountAsync(x => x.ProcurementCartItem.CartId == revisionCart.Id);
                        if (persistedSubItemCount != sourceSubItemCount)
                        {
                            throw new InvalidOperationException(string.Format(
                                "Revision cart initialization failed. Expected {0} sub-item(s), but {1} sub-item(s) were persisted.",
                                sourceSubItemCount, persistedSubItemCount));
                        }
                    }

                    transaction.Commit();

                    // Reload from DB to ensure the returned entity matches persisted state
                    var persistedCart = await GetActiveCartEntityAsync(userId, CartModes.Revision, requestId);
                    return persistedCart ?? revisionCart;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private static void RenumberCartItems(ProcurementCart cart)
        {
            var ordered = cart.ProcurementCartItems
                .OrderBy(i => i.SortOrder.GetValueOrDefault())
                .ThenBy(i => i.ItemNo)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                var item = ordered[i];
                item.SortOrder = i + 1;
                item.ItemNo = (i + 1).ToString();
                RenumberSubItems(item);
            }
        }

        private static void RenumberSubItems(ProcurementCartItem item)
        {
            var orderedSubs = item.ProcurementCartSubItems
                .OrderBy(s => s.SortOrder.GetValueOrDefault())
                .ThenBy(s => s.ItemNo)
                .ToList();

            for (int i = 0; i < orderedSubs.Count; i++)
            {
                var sub = orderedSubs[i];
                sub.SortOrder = i + 1;
                sub.ItemNo = string.Format("{0}.{1}", item.ItemNo, i + 1);
            }
        }
    }
}
