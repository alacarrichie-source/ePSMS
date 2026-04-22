using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Items;
using iLgs.Services.PropertyCard;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace iLgs.Services.StockCards
{
    public interface IStockCardService : IPsCardService
    {
        new IQueryable<StockCardVM> GetAll(string userName);
        StockCardVM GetById(Guid? id);
        ValueTask<StockCardVM> CreateAsync(StockCardVM model, string user, DateTime date);
        ValueTask<StockCardVM> UpdateAsync(StockCardVM model, string user, DateTime date);
        ValueTask<StockCardVM> DeleteAsync(StockCardVM model, string user, DateTime date);

        ValueTask TransferAsync(PoTransferVM model, string user, DateTime date);
    }
    public class StockCardService : PsCardService, IStockCardService
    {
        private readonly IExceptionService<StockCardVM> _stockExceptionService;
        private readonly IStockCardValidator _validator;
        private readonly IItemCodeService _itemCodeService;
        private readonly IUserService _userService;

        public StockCardService(AppManEntities db)
            : base(db)
        {
            _stockExceptionService = new ExceptionService<StockCardVM>();
            _validator = new StockCardValidator(_db);
            _itemCodeService = new ItemCodeService(_db);
            _userService = new UserService(_db);
        }

        //public StockCardService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    ICreateAndLogExceptions exceptions,
        //    IExceptionService<PsCardVM> vmExceptionService,
        //    IExceptionService<PsCard> exceptionService,
        //    IAllFieldService allFieldService,
        //    IPsCardSharedService psCardSharedService,
        //    IPsCardItemService psCardItemService,
        //    IExceptionService<StockCardVM> stockExceptionService,
        //    IStockCardValidator validator,
        //    IItemCodeService itemCodeService,
        //    IUserService userService) 
        //    : base(db, appManEntitiesFactory, exceptions, vmExceptionService, exceptionService, allFieldService, psCardSharedService, psCardItemService) //, psCardItemIssuanceService)
        //{
        //    _stockExceptionService = stockExceptionService;
        //    _validator = validator;
        //    _itemCodeService = itemCodeService;
        //    _userService = userService;
        //}

        private Expression<Func<PsCard, StockCardVM>> Projection()
        {
            //var codeLookup = _db.ItemCodes.ToDictionary(x => x.Code, x => x.Description);
            return s => new StockCardVM
            {
                Id = s.Id,
                ItemCodeId = s.ItemCodeId,
                Item = s.ItemCode.Description,
                ItemNo = s.ItemCode.ItemNo,
                ItemCode = s.ItemCode.Code,
                ItemType = s.ItemCode.ItemType.Description,
                ItemTypeCode = s.ItemCode.ItemType.Code,
                PartialPage = s.ItemCode.PartialPage == null ? s.ItemCode.ItemType.PartialPage : s.ItemCode.PartialPage,
                CardCategory = s.CardCategory,
                Description = s.Description,
                SubAccountCode = s.SubAccountCode,
                //// Before 1st dot
                //SubAccount1 = _db.ItemCodes
                //    .Where(f => f.Code ==
                //        (SqlFunctions.CharIndex(".", s.ItemCode.Code) > 0
                //            ? DbFunctions.Left(s.ItemCode.Code, SqlFunctions.CharIndex(".", s.ItemCode.Code) - 1)
                //            : s.ItemCode.Code))
                //    .Select(f => f.Description)
                //    .FirstOrDefault(),

                //// Before 2nd dot
                //SubAccount2 = _db.ItemCodes
                //    .Where(f => f.Code ==
                //        (SqlFunctions.CharIndex(".", s.ItemCode.Code, SqlFunctions.CharIndex(".", s.ItemCode.Code) + 1) > 0
                //            ? DbFunctions.Left(
                //                s.ItemCode.Code,
                //                SqlFunctions.CharIndex(".", s.ItemCode.Code, SqlFunctions.CharIndex(".", s.ItemCode.Code) + 1) - 1
                //              )
                //            : s.ItemCode.Code))
                //    .Select(f => f.Description)
                //    .FirstOrDefault(),

                //// Before 3rd dot
                //SubAccount3 = _db.ItemCodes
                //    .Where(f => f.Code ==
                //        (SqlFunctions.CharIndex(".", s.ItemCode.Code, SqlFunctions.CharIndex(".", s.ItemCode.Code, SqlFunctions.CharIndex(".", s.ItemCode.Code) + 1) + 1) > 0
                //            ? DbFunctions.Left(
                //                s.ItemCode.Code,
                //                SqlFunctions.CharIndex(".", s.ItemCode.Code, SqlFunctions.CharIndex(".", s.ItemCode.Code, SqlFunctions.CharIndex(".", s.ItemCode.Code) + 1) + 1) - 1
                //              )
                //            : s.ItemCode.Code))
                //    .Select(f => f.Description)
                //    .FirstOrDefault(),

                //// Before 4th dot
                //SubAccount4 = _db.ItemCodes
                //    .Where(f => f.Code ==
                //        (SqlFunctions.CharIndex(".", s.ItemCode.Code,
                //                SqlFunctions.CharIndex(".", s.ItemCode.Code,
                //                    SqlFunctions.CharIndex(".", s.ItemCode.Code,
                //                        SqlFunctions.CharIndex(".", s.ItemCode.Code) + 1) + 1) + 1) > 0
                //            ? DbFunctions.Left(
                //                s.ItemCode.Code,
                //                SqlFunctions.CharIndex(".", s.ItemCode.Code,
                //                    SqlFunctions.CharIndex(".", s.ItemCode.Code,
                //                        SqlFunctions.CharIndex(".", s.ItemCode.Code,
                //                            SqlFunctions.CharIndex(".", s.ItemCode.Code) + 1) + 1) + 1) - 1
                //              )
                //            : s.ItemCode.Code))
                //    .Select(f => f.Description)
                //    .FirstOrDefault(),
                SubAccount1 = GetBeforeNthDot(s.ItemCode.Code, 1),
                SubAccount2 = GetBeforeNthDot(s.ItemCode.Code, 2),
                SubAccount3 = GetBeforeNthDot(s.ItemCode.Code, 3),
                SubAccount4 = GetBeforeNthDot(s.ItemCode.Code, 4),
                Fund = s.Fund,
                Unit = s.Unit,
                PsNo = s.PsNo,
                PsName = s.PsName,
                PrevPsNo = s.PrevPsNo,
                FromDonation = s.FromDonation,
                Amount = s.Amount,
                AllField = s.AllField,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                PostedBy = s.PostedBy,
                PostedDt = s.PostedDt,
                ItemCount = s.PsCardItems.Count(),
                NotPosted = s.PsCardItems.Count(c => c.PostedBy != "" && c.PostedBy != null)
            };
        }

        private string GetBeforeNthDot(string input, int n)
        {
            if (string.IsNullOrEmpty(input)) return input;
            int pos = -1;
            for (int i = 0; i < n; i++)
            {
                pos = input.IndexOf('.', pos + 1);
                if (pos == -1) return input; // no more dots
            }
            return input.Substring(0, pos);
        }

        public new IQueryable<StockCardVM> GetAll(string userName)
        {
            var data = _db.Database.SqlQuery<StockCardVM>("Exec Card_GetRecords 'S', {0}", userName).AsQueryable();

            return data;
        }

        public StockCardVM GetById(Guid? id)
        {
            var data = _db.PsCards.Where(w => w.Id == id).AsNoTracking()
                .Where(w => w.ItemCode.ItemType.Category == "S")
                .Select(s => new StockCardVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.ItemCode.Description,
                    ItemNo = s.ItemCode.ItemNo,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.ItemType.Description,
                    ItemTypeCode = s.ItemCode.ItemType.Code,
                    PartialPage = s.ItemCode.PartialPage == null ? s.ItemCode.ItemType.PartialPage : s.ItemCode.PartialPage,
                    CardCategory = s.CardCategory,
                    Description = s.Description,
                    SubAccountCode = s.SubAccountCode,
                    Fund = s.Fund,
                    Unit = s.Unit,
                    PsNo = s.PsNo,
                    PsName = s.PsName,
                    PrevPsNo = s.PrevPsNo,
                    FromDonation = s.FromDonation,
                    Amount = s.Amount,
                    AllField = s.AllField,
                    InsertedDt = s.InsertedDt
                }).ToList()
                .Select(s => new StockCardVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.Item,
                    ItemNo = s.ItemNo,
                    ItemCode = s.ItemCode,
                    ItemType = s.ItemType,
                    ItemTypeCode = s.ItemTypeCode,
                    PartialPage = s.PartialPage,
                    CardCategory = s.CardCategory,
                    Description = s.Description,
                    SubAccountCode = s.SubAccountCode,
                    SubAccount = _itemCodeService.GetSubAccounts(s.ItemCodeId), // Call the service on the in-memory data
                    Fund = s.Fund,
                    Unit = s.Unit,
                    PsNo = s.PsNo,
                    PsName = s.PsName,
                    PrevPsNo = s.PrevPsNo,
                    FromDonation = s.FromDonation,
                    Amount = s.Amount,
                    AllField = s.AllField,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefault();
            return data;
        }

        public ValueTask<StockCardVM> CreateAsync(StockCardVM model, string user, DateTime date) => _stockExceptionService.TryCatch(async () =>
        {
            var stockNo = await GetStockNoAsync(model);
            model.PsNo = stockNo;            
            await _validator.ValidateOnCreateAsync(model);

            model.Description = "Please see attachment.";
            model.AllField = _allFieldService.ChangeAllFieldCase(model.AllField);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;
            model.PsName = await GetPsNoAsync(model.PsNo);
            var entity = new PsCard
            {
                Id = model.Id,
                ItemCodeId = model.ItemCodeId,
                SubAccountCode = model.SubAccountCode,
                Fund = model.Fund,
                Description = model.Description,
                Unit = model.Unit,
                CardCategory = model.CardCategory,
                PsNo = model.PsNo,
                PsName = model.PsName,
                PrevPsNo = model.PrevPsNo,
                FromDonation = model.FromDonation,
                Amount = model.Amount,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            model.AllField.Id = model.Id;
            model.AllField.InsertedBy = user;
            model.AllField.InsertedDt = date;
            model.AllField.UpdatedBy = user;
            model.AllField.UpdatedDt = date;
            entity.AllField = model.AllField;

            _db.PsCards.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<StockCardVM> UpdateAsync(StockCardVM model, string user, DateTime date) => _stockExceptionService.TryCatch(async () =>
        {
            var stockNo = await GetStockNoAsync(model);
            model.PsNo = stockNo;
            await _validator.ValidateOnUpdateAsync(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCards.FindAsync(model.Id);

            ValidateUser(entity, model);
            await ValidateIfWithPostedItemAsync(model.Id);

            model.AllField = _allFieldService.ChangeAllFieldCase(model.AllField);
            model.PsName = await GetPsNoAsync(model.PsNo);

            entity.ItemCodeId = model.ItemCodeId;
            entity.SubAccountCode = model.SubAccountCode;
            entity.Fund = model.Fund;
            entity.Description = model.Description;
            entity.Unit = model.Unit;
            entity.CardCategory = model.CardCategory;
            entity.PsNo = model.PsNo;
            entity.PsName = model.PsName;
            entity.PrevPsNo = model.PrevPsNo;
            entity.FromDonation = model.FromDonation;
            entity.Amount = model.Amount;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;
            entity.AllField = model.AllField;

            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<StockCardVM> DeleteAsync(StockCardVM model, string user, DateTime date) => _stockExceptionService.TryCatch(async () =>
        {
            await _validator.ValidateOnDeleteAsync(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCards.FindAsync(model.Id);

            ValidateUser(entity, model);
            await ValidateIfWithPostedItemAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();

            _db.PsCards.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public async ValueTask TransferAsync(PoTransferVM model, string user, DateTime date)
        {
            var fund = model.Fund;
            var poNo = model.PoNo;
            var poDate = model.PoDate;
            var newFund = model.NewFund;
            var fromDonation = model.FromDonation;
            var psCardItems = await _db.PsCardItems.Include(i => i.PsCard.AllField).Where(w => w.PsCard.Fund == fund && w.PoNo == poNo && w.PoDate == poDate).ToListAsync();
            foreach(var psCardItem in psCardItems)
            {
                /*
                 * Check if new card, if eof() > create one, transfer old items to new card
                 */
                var newPsNo = psCardItem.PsCard.PsNo;

                if (fromDonation.Value == true)
                {
                    //if (!psCardItem.PsCard.FromDonation.HasValue || (psCardItem.PsCard.FromDonation.HasValue && psCardItem.PsCard.FromDonation.Value != true))
                    //{
                    //    newPsNo = $"FD{newPsNo}";
                    //}
                    if (newPsNo.Substring(0, 2) != "FD")
                    {
                        newPsNo = $"FD{newPsNo}";
                    }
                }
                else
                {
                    //if (psCardItem.PsCard.FromDonation.HasValue && psCardItem.PsCard.FromDonation.Value == true)
                    //{
                    //    newPsNo = newPsNo.Substring(2);
                    //}
                    if (newPsNo.Substring(0, 2) == "FD")
                    {
                        newPsNo = newPsNo.Substring(2);
                    }
                }

                //var newPsCard = await _db.PsCards.Where(w => w.PsNo == psCardItem.PsCard.PsNo && w.Fund == newFund && (w.FromDonation == fromDonation || (w.FromDonation == null && fromDonation == false))).SingleOrDefaultAsync();
                var newPsCard = await _db.PsCards.Where(w => w.PsNo == newPsNo && w.Fund == newFund).SingleOrDefaultAsync();

                if (newPsCard == null)
                {                    
                    newPsCard = new PsCard()
                    {
                        Id = Guid.NewGuid(),
                        ItemCodeId = psCardItem.PsCard.ItemCodeId,
                        SubAccountCode = psCardItem.PsCard.SubAccountCode,
                        Fund = newFund,
                        Description = psCardItem.PsCard.Description,
                        PsNo = newPsNo,
                        CardCategory = psCardItem.PsCard.CardCategory,
                        FromDonation = fromDonation,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    var af = psCardItem.PsCard.AllField;
                    var newAllField = new AllField()
                    {
                        Id = newPsCard.Id,
                        AcqMode = af.AcqMode,
                        InvDist = af.InvDist,
                        GenericName = af.GenericName,
                        DosageStrength = af.DosageStrength,
                        DosageForm = af.DosageForm,
                        DosageVolume = af.DosageVolume,
                        Others = af.Others,
                        Brand = af.Brand,
                        Multipliers = af.Multipliers,
                        Model_ = af.Model_,
                        Area = af.Area,
                        Barangay = af.Barangay,
                        DateSale = af.DateSale,
                        DateDonation = af.DateDonation,
                        DateAcquisition = af.DateAcquisition,
                        DateConstruction = af.DateConstruction,
                        AreaSoldDonated = af.AreaSoldDonated,
                        PricePerSqm = af.PricePerSqm,
                        AcqCost = af.AcqCost,
                        VendorDonor = af.VendorDonor,
                        Type = af.Type,
                        Dimension = af.Dimension,
                        Size = af.Size,
                        Weight = af.Weight,
                        Materials = af.Materials,
                        Capacity = af.Capacity,
                        Color = af.Color,
                        SerialNo = af.SerialNo,
                        PropNo = af.PropNo,
                        PlateNo = af.PlateNo,
                        BodyNo = af.BodyNo,
                        MVFileNo = af.MVFileNo,
                        InsertedBy = af.InsertedBy,
                        InsertedDt = af.InsertedDt,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };
                                                                    
                    newPsCard.AllField = newAllField;

                    _db.PsCards.Add(newPsCard);
                    await _db.SaveChangesAsync();
                }

                psCardItem.UpdatedBy = user;
                psCardItem.UpdatedDt = date;
                psCardItem.PsCardId = newPsCard.Id;
                psCardItem.InvDist = model.InvDist;
                await _db.SaveChangesAsync();
            }
        }

        private void ValidateUser(PsCard entity, StockCardVM model)
        {
            if (entity.InsertedBy != model.UpdatedBy)
            {
                var isAdmin = _userService.IsUserNameAdmin(model.UpdatedBy);
                if (!isAdmin)
                {
                    throw new RecordLockedException($"Record can only be updated by {entity.InsertedBy} or an Admin.");
                }
            }
        }

        private async Task ValidateIfWithPostedItemAsync(Guid? psCardId)
        {
            if (await _db.PsCardItems.AnyAsync(a => a.PsCardId == psCardId && a.PostedBy != null && a.PostedBy != ""))
            {
                throw new RecordLockedException("Posted items where found for this card. Cannot update.");
            }
        }
    }
}