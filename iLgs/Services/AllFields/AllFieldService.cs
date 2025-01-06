using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Items;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.AllFields
{
    public interface IAllFieldService
    {
        IQueryable<AllField> GetAllByPsCardId(Guid? psCardId);
        IQueryable<AllField> GetAllByRisItemId(Guid? risItemId);
        //CategoryGroup GetCategoryGroup(string itemTypeCode, string itemCode);
        //string GetPartialField(string itemTypeCode, string itemCode);
        //string GetPartialItemField(string itemTypeCode, string itemCode);
        ValueTask<AllField> GetByIdAsync(Guid id);
        ValueTask<AllField> CreatePsCardFieldsAsync(PsCardVM model, string user, DateTime date);
        ValueTask<AllField> CreateRisFieldsAsync(RisItemEntryVM model, string user, DateTime date);
        ValueTask<AllField> UpdatePsCardFieldsAsync(PsCardVM model, string user, DateTime date);
        ValueTask<AllField> UpdateRisFieldsAsync(RisItemEntryVM model, string user, DateTime date);
        ValueTask<AllField> DeleteAsync(AllField model, string user, DateTime date);

        string GetRisDescription(RisItemEntryVM model);
        string GetCardStockNo(PsCardVM model);
        string GetRisStockNo(RisItemEntryVM model);
        string GetOrderStockNo(OrderItemVM model);        
        string GetCustodianStockNo(CustodianReportItem model);
        string GetCustodianStockNo(CustodianReportLandItem model);
        string GetCustodianStockNo(CustodianReportBldgItem model);
        string GetOrderPsNoDisplay(OrderItemVM model);
        void ValidateStockCardAllField(StockCardVM model);
        void ValidatePropertyCardAllField(PropertyCardVM model);
        void ValidateRisAllField(RisItemEntryVM model);
        string GetStockNo(AllField af, string itemTypeCode, string itemCode);        
        bool IsBrandRequired(Category c);
        //bool IsNoIcs(Guid? itemCodeId);
        AllField ChangeAllFieldCase(AllField allField);

        //ValueTask<ServiceResult<PsCardVM>> ValidatePsCardAllField(PsCardVM model);
    }

    public class AllFieldService : IAllFieldService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<AllField> _exceptionService = new ExceptionService<AllField>();
        private readonly IItemCodeService _itemCodeService;
        private IAllFieldsValidator _validator;

        public AllFieldService(AppManEntities db)
        {
            _db = db;
            _validator = new AllFieldsValidator(_db);
            _itemCodeService = new ItemCodeService(_db);
        }

        public IQueryable<AllField> GetAllByPsCardId(Guid? psCardId) => _exceptionService.TryCatch(() =>
        {
            var data = _db.AllFields.Where(w => w.Id == psCardId).AsNoTracking();
            return data;
        });

        public IQueryable<AllField> GetAllByRisItemId(Guid? risItemId) => _exceptionService.TryCatch(() =>
        {
            var data = _db.AllFields.Where(w => w.Id == risItemId).AsNoTracking();
            return data;
        });

        public ValueTask<AllField> GetByIdAsync(Guid id) => _exceptionService.TryCatch(async () =>
        {
            var data = await _db.AllFields.FindAsync(id);
            return data;
        });

        public void ValidatePsCardAllField(PsCardVM model)
        {
            var ex = new InvalidModelException();
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            _validator.ValidateAllFieldsPartial(model.AllField, partialView, ex);
            //_validator.ValidateAllFields(model.AllField, model.ItemTypeCode, model.ItemNo, ex);
            ex.ThrowIfContainsErrors();
        }

        public void ValidateStockCardAllField(StockCardVM model)
        {
            var ex = new InvalidModelException();
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            _validator.ValidateAllFieldsPartial(model.AllField, partialView, ex);
            //_validator.ValidateAllFields(model.AllField, model.ItemTypeCode, model.ItemNo, ex);           
            ex.ThrowIfContainsErrors();
        }

        public void ValidatePropertyCardAllField(PropertyCardVM model)
        {
            var ex = new InvalidModelException();
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            _validator.ValidateAllFieldsPartial(model.AllField, partialView, ex);
            //_validator.ValidateAllFields(model.AllField, model.ItemTypeCode, model.ItemNo, ex);
            ex.ThrowIfContainsErrors();
        }

        public void ValidateRisAllField(RisItemEntryVM model)
        {
            var ex = new InvalidModelException();
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            _validator.ValidateAllFieldsPartial(model.AllField, partialView, ex);
            //_validator.ValidateAllFields(model.AllField, model.PsType, model.ItemNo, ex);
            ex.ThrowIfContainsErrors();
        }

        public ValueTask<AllField> CreatePsCardFieldsAsync(PsCardVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            if (_db.AllFields.Any(a => a.Id == model.Id))
            {
                throw new RecordAlreadyExistsException(string.Format("Record already exists!"));
            }

            ValidatePsCardAllField(model);

            await CreateAsync(model.AllField, user, date);

            return model.AllField;
        });

        public ValueTask<AllField> CreateRisFieldsAsync(RisItemEntryVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            if (_db.AllFields.Any(a => a.Id == model.Id))
            {
                throw new RecordAlreadyExistsException(string.Format("Record already exists!"));
            }

            ValidateRisAllField(model);

            await CreateAsync(model.AllField, user, date);

            return model.AllField;
        });

        private async ValueTask CreateAsync(AllField model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            model = ChangeAllFieldCase(model);

            var entity = new AllField
            {
                Id = model.Id,
                AcqMode = model.AcqMode,
                AcqCost = model.AcqCost,
                InvDist = model.InvDist,
                GenericName = model.GenericName,
                DosageStrength = model.DosageStrength,
                DosageForm = model.DosageForm,
                DosageVolume = model.DosageVolume,
                Others = model.Others,
                Brand = model.Brand,
                Multipliers = model.Multipliers,
                Model_ = model.Model_,
                Dimension = model.Dimension,
                Size = model.Size,
                Capacity = model.Capacity,
                Weight = model.Weight,
                Materials = model.Materials,
                Color = model.Color,
                Type = model.Type,
                Area = model.Area,
                //Barangay = model.Barangay,
                //DateSale = model.DateSale,
                //DateDonation = model.DateDonation,
                //DateAcquisition = model.DateAcquisition,
                //DateConstruction = model.DateConstruction,
                //AreaSoldDonated = model.AreaSoldDonated,
                //PricePerSqm = model.PricePerSqm,
                //VendorDonor = model.VendorDonor,
                SerialNo = model.SerialNo,
                PropNo = model.PropNo,
                PlateNo = model.PlateNo,
                BodyNo = model.BodyNo,
                MVFileNo = model.MVFileNo,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.AllFields.Add(entity);
            await _db.SaveChangesAsync();
        }

        public ValueTask<AllField> UpdatePsCardFieldsAsync(PsCardVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            ValidatePsCardAllField(model);
            await UpdateAsync(model.AllField, user, date);
            return model.AllField;
        });

        public ValueTask<AllField> UpdateRisFieldsAsync(RisItemEntryVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            ValidateRisAllField(model);
            await UpdateAsync(model.AllField, user, date);
            return model.AllField;
        });

        private async ValueTask UpdateAsync(AllField model, string user, DateTime date)
        {
            var entity = await GetByIdAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            model = ChangeAllFieldCase(model);

            entity.AcqMode = model.AcqMode;
            entity.AcqCost = model.AcqCost;
            entity.InvDist = model.InvDist;
            entity.GenericName = model.GenericName;
            entity.DosageStrength = model.DosageStrength;
            entity.DosageForm = model.DosageForm;
            entity.DosageVolume = model.DosageVolume;
            entity.Others = model.Others;
            entity.Brand = model.Brand;
            entity.Multipliers = model.Multipliers;
            entity.Model_ = model.Model_;
            entity.Dimension = model.Dimension;
            entity.Size = model.Size;
            entity.Weight = model.Weight;
            entity.Capacity = model.Capacity;
            entity.Materials = model.Materials;
            entity.Color = model.Color;
            entity.Area = model.Area;

            //entity.AreaSoldDonated = model.AreaSoldDonated;
            //entity.PricePerSqm = model.PricePerSqm;
            //entity.Type = model.Type;            
            //entity.Barangay = model.Barangay;
            //entity.DateSale = model.DateSale;
            //entity.DateDonation = model.DateDonation;
            //entity.DateAcquisition = model.DateAcquisition;
            //entity.DateConstruction = model.DateConstruction;                       
            //entity.VendorDonor = model.VendorDonor;

            entity.SerialNo = model.SerialNo;
            entity.PropNo = model.PropNo;
            entity.PlateNo = model.PlateNo;
            entity.BodyNo = model.BodyNo;
            entity.MVFileNo = model.MVFileNo;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.AllFields.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public ValueTask<AllField> DeleteAsync(AllField model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await GetByIdAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.AllFields.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.AllFields.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });


        private string NextPropertyNo(string psNo)
        {
            string keyName = psNo;

            var data = _db.PsCards.Where(w => w.PsNo == psNo)
                .OrderByDescending(o => o.PsNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "001";
            }
            else
            {
                var sequence = (int.Parse(data.PsNo.Split('-')[1]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(3, '0');
            }
        }

        public string GetRisDescription(RisItemEntryVM model)
        {
            string description = "";
            //var af = model.AllField;
            //var group = AllFieldsUtil.GetCategoryGroup(model.PsType, model.ItemNo);
            //if (group == CategoryGroup.LAND)
            //{
            //    description += af.Area.ToString() + "sqm";
            //}
            //else if (group == CategoryGroup.OTHERS || group == CategoryGroup.OTHERS_A || group == CategoryGroup.OTHERS_B)
            //{
            //    description = (!string.IsNullOrWhiteSpace(af.Model_) ? $"{af.Model_}" : "") +
            //                   (!string.IsNullOrWhiteSpace(af.Dimension) ? $" {af.Dimension}" : "") +
            //                   (!string.IsNullOrWhiteSpace(af.Size) ? $" {af.Size}" : "") +
            //                   (!string.IsNullOrWhiteSpace(af.Weight) ? $" {af.Weight}" : "") +
            //                   (!string.IsNullOrWhiteSpace(af.Materials) ? $" {af.Materials}" : "") +
            //                   (!string.IsNullOrWhiteSpace(af.Capacity) ? $" {af.Capacity}" : "") +
            //                   (!string.IsNullOrWhiteSpace(af.Color) ? $" {af.Color}" : "") +
            //                   (!string.IsNullOrWhiteSpace(af.Type) ? $" {af.Type}" : "");
            //}
            //else if (group == CategoryGroup.DRUGS)
            //{
            //    description = (!string.IsNullOrWhiteSpace(af.GenericName) ? $"{af.GenericName}" : "") +
            //                    (!string.IsNullOrWhiteSpace(af.DosageStrength) ? $" {af.DosageStrength}" : "") +
            //                    (!string.IsNullOrWhiteSpace(af.DosageForm) ? $" {af.DosageForm}" : "") +
            //                    (!string.IsNullOrWhiteSpace(af.DosageVolume) ? $" {af.DosageVolume}" : "") +
            //                    (!string.IsNullOrWhiteSpace(af.Others) ? $" {af.Others}" : "") +
            //                    (!(af.Multipliers == null) ? $" {af.Multipliers}'s" : "");
            //}
            //else if (group == CategoryGroup.SERIAL 
            //    || group == CategoryGroup.SERIAL_A 
            //    || group == CategoryGroup.SERIAL_B
            //    || group == CategoryGroup.SERIAL_C
            //    || group == CategoryGroup.SERIAL_D)
            //{
            //    description = (!string.IsNullOrWhiteSpace(af.SerialNo) ? $"{af.SerialNo}" : "") +
            //                   (!string.IsNullOrWhiteSpace(af.PropNo) ? $" {af.PropNo}" : "") +
            //                   (!string.IsNullOrWhiteSpace(af.PlateNo) ? $" {af.PlateNo}" : "") +
            //                   (!string.IsNullOrWhiteSpace(af.BodyNo) ? $" {af.BodyNo}" : "") +
            //                   (!string.IsNullOrWhiteSpace(af.MVFileNo) ? $" {af.MVFileNo}" : "") +
            //                   (!string.IsNullOrWhiteSpace(af.Type) ? $" {af.Type}" : "");
            //}

            return description ?? "";
        }

        public bool IsNoIcs(Guid? itemCodeId)
        {
            return false;
            //return _db.ItemCodes.Where(w => w.Id == itemCodeId && (w.IsConsumable == "Y" || w.IsIncorporated == "Y" || w.ForDistribution == "Y")).Any();
        }

        public string GetRisStockNo(RisItemEntryVM model)
        {
            model.AllField = ChangeAllFieldCase(model.AllField);
            string stockNo = model.ItemCode.Trim();

            //if (!IsNoIcs(model.ItemCodeId))
            //{
            //    stockNo += GetStockNo(model.AllField, model.PsType, model.ItemCode);
            //}

            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            stockNo += GetPartialViewStockNo(model.AllField, partialView);

            return stockNo ?? "";
        }

        public string GetOrderStockNo(OrderItemVM model)
        {
            model.AllField = ChangeAllFieldCase(model.AllField);
            string stockNo = model.ItemCode.Trim();
            
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            stockNo += GetPartialViewStockNo(model.AllField, partialView);

            return stockNo ?? "";
        }

        public string GetOrderPsNoDisplay(OrderItemVM model)
        {
            string display = "";
            if (Enum.TryParse(model.PsType, out Category c))
            {
                if (c == CatLandsProp())
                {
                    display = $"{model.ItemCode}/{model.AllField.Area}SqM";
                }
                else
                {
                    display = $"{model.ItemCode}";
                }
            }
            return display;
        }

        public AllField ChangeAllFieldCase(AllField allField)
        {
            if (!string.IsNullOrWhiteSpace(allField.DosageStrength))
            {
                allField.DosageStrength = allField.DosageStrength.ToLower();
            }

            if (!string.IsNullOrWhiteSpace(allField.PlateNo))
            {
                allField.PlateNo = allField.PlateNo.ToUpper();
            }

            if (!string.IsNullOrWhiteSpace(allField.PlateNo))
            {
                allField.PlateNo = allField.PlateNo.ToUpper();
            }

            var properties = allField.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(string))
                {
                    if (property.Name != nameof(allField.DosageStrength) && property.Name != nameof(allField.PlateNo)
                         && property.Name != nameof(allField.InsertedBy) && property.Name != nameof(allField.UpdatedBy))
                    {
                        var value = (string)property.GetValue(allField);
                        if (value != null)
                        {
                            property.SetValue(allField, Utility.ToProperCase(value));
                        }
                    }
                }
            }

            return allField;
        }

        public string GetCardStockNo(PsCardVM model)
        {
            model.AllField = ChangeAllFieldCase(model.AllField);
            string stockNo = model.ItemCode.Trim();
            if (model.FromDonation == true)
            {
                stockNo = "FD" + stockNo;
            }
            //if (!IsNoIcs(model.ItemCodeId))
            //{
            //    stockNo += GetStockNo(model.AllField, model.ItemTypeCode, model.ItemCode);
            //}

            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            stockNo += GetPartialViewStockNo(model.AllField, partialView);

            return stockNo ?? "";
        }

        public string GetCustodianStockNo(CustodianReportItem model)
        {
            string stockNo = model.Item_Code.Trim();
            if (model.FromDonation == true)
            {
                stockNo = "FD" + stockNo;
            }

            //stockNo += GetStockNo(model.AllField, model.ItemType_Code, model.Item_Code);
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            stockNo += GetPartialViewStockNo(model.AllField, partialView);

            return stockNo ?? "";
        }

        public string GetCustodianStockNo(CustodianReportLandItem model)
        {
            string stockNo = model.Item_Code.Trim();
            if (model.FromDonation == true)
            {
                stockNo = "FD" + stockNo;
            }

            //stockNo += GetStockNo(model.AllField, model.ItemType_Code, model.Item_Code);
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            stockNo += GetPartialViewStockNo(model.AllField, partialView);

            return stockNo ?? "";
        }

        public string GetCustodianStockNo(CustodianReportBldgItem model)
        {
            string stockNo = model.Item_Code.Trim();
            if (model.FromDonation == true)
            {
                stockNo = "FD" + stockNo;
            }

            //stockNo += GetStockNo(model.AllField, model.ItemType_Code, model.Item_Code);
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            stockNo += GetPartialViewStockNo(model.AllField, partialView);

            return stockNo ?? "";
        }

        public bool IsBrandRequired(Category c)
        {
            return (c == CatMachineriesProp() || c == CatTransportationProp() || c == CatFurnituresProp() || c == CatOtherProperties()
                    || c == CatMedicalSupply() || c == CatAgriculturalSupply() || c == CatAnimalSupplies() || c == CatConstructionMaterialsSupply()
                    || c == CatOfficeSupplies() || c == CatAccountableFormsSupply() || c == CatNonAccountableFornsSupply() || c == CatMilitarySupply()
                    || c == CatOtherSupplies() || c == CatRepairSupply()) || c == CatDrugsSupply();
        }

        //string partialView = AllFieldsUtil.GetPartialView(itemCode);
        public string GetPartialViewStockNo(AllField af, string partialView)
        {
            string stockNo = "";
            if (partialView == "_FieldLand")
            {
                stockNo += ((af.Area != null) ? $"/{af.Area}sqm" : "");
            }
            else if (partialView == "_FieldDrugs" || partialView == "_FieldAlcohol")
            {
                if (!string.IsNullOrWhiteSpace(af.GenericName))
                {
                    if (af.GenericName.Equals("-"))
                    {
                        stockNo += "/xx";
                    }
                    else
                    {
                        var genName = Utility.ToProperCase(af.GenericName);
                        if (genName.Length >= 3)
                        {
                            stockNo += "/" + genName.Substring(0, 1) + genName.Substring(2, 1);
                        }
                        else
                        {
                            stockNo += "/" + genName.Substring(0, 1) + "X";
                        }
                    }
                }

                if (partialView == "_FieldAlcohol") // Alcohol
                {
                    if (!string.IsNullOrWhiteSpace(af.DosageVolume))
                    {
                        stockNo += $"/{af.DosageVolume}";
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(af.DosageStrength))
                    {
                        if (af.DosageStrength.Equals("-"))
                        {
                            stockNo += "/xx";
                        }
                        else
                        {
                            stockNo += "/" + af.DosageStrength.Replace(" ", "").Trim();
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(af.DosageForm))
                    {
                        if (af.DosageForm.Equals("-"))
                        {
                            stockNo += "/xx";
                        }
                        else
                        {
                            stockNo += "/" + af.DosageForm.PadRight(3, 'X').Substring(0, 3);
                        }
                    }
                }

                if (af.Multipliers.HasValue && af.Multipliers > 0)
                {
                    stockNo += $"/{af.Multipliers}'s";
                }
                else
                {
                    stockNo += "/xx";
                }

                if (!string.IsNullOrWhiteSpace(af.Brand))
                {
                    if (af.Brand.Equals("-"))
                    {
                        stockNo += "/xx";
                    }
                    else
                    {
                        stockNo += "/" + af.Brand.Replace(" ", "").Trim();
                    }
                }
            }
            else if (partialView.Contains("Brand"))
            {
                if (partialView == "_FieldBrand")
                {
                    stockNo += GetStockNoFromMultiples(af);
                }

                stockNo += GetStockNoFromBrand(af);

                // Vehicles
                if (partialView == "_FieldBrand_B")
                {
                    stockNo += GetStockNoFromModelV(af);
                }
                else
                {
                    stockNo += GetStockNoFromModel(af);
                }
            }
            else if (partialView == "_FieldMultiple")
            {
                stockNo += GetStockNoFromMultiples(af);
            }
            else if (partialView == "_FieldMultiple_A")
            {
                stockNo += GetStockNoFromMultiples(af);
                stockNo += GetStockNoFromBrand(af);
            }
            else if (partialView == "_FieldSerial")
            {
                // "Color", "Capacity", "Materials", "Weight", "Size", "Dimension", "Model_", "Brand", "PropNo", "SerialNo"                        

                stockNo += GetStockNoFromSerial(af);
                stockNo += GetStockNoFromMultiples(af);
                stockNo += GetStockNoFromBrand(af);
                stockNo += GetStockNoFromModel(af);
            }
            else if (partialView == "_FieldSerial_A")
            {
                // "Color", "Capacity", "Materials", "Weight", "Size", "Dimension", "Model_", "Brand", "MVFileNo", "Body", "Plate"                        
                stockNo += GetStockNoFromPlate(af);
                stockNo += GetStockNoFromMultiples(af);
                stockNo += GetStockNoFromBrand(af);
                stockNo += GetStockNoFromModel(af);
            }
            else if (partialView == "_FieldSerial_B")
            {
                stockNo += GetStockNoFromSerial(af);
                stockNo += GetStockNoFromMultiples(af);
            }
            else if (partialView == "_FieldSerial_C")
            {
                stockNo += GetStockNoFromPlate(af);
                stockNo += GetStockNoFromMultiples(af);
            }
            else if (partialView == "_FieldSerial_D")
            {
                stockNo += GetStockNoFromSerial(af);
                stockNo += GetStockNoFromBrand(af);
                stockNo += GetStockNoFromModel(af);
            }
            else if (partialView == "_FieldSerial_E")
            {
                stockNo += GetStockNoFromPlate(af);
            }
            else if (partialView == "_FieldSerial_F")
            {
                stockNo += GetStockNoFromSerial(af);
            }
            return stockNo;
        }

        private string GetStockNoFromSerial(AllField af)
        {
            string stockNo = "";
            if (!af.SerialNo.IsNullOrWhiteSpaceX())
            {
                stockNo += $"/{af.SerialNo}";
            }
            else
            {
                if (af.PropNo.IsNullOrWhiteSpaceX())
                {
                    stockNo += "/xx";
                }
                else
                {
                    stockNo += $"/{af.PropNo}";
                }
            }
            return stockNo;
        }

        private string GetStockNoFromPlate(AllField af)
        {
            string stockNo = "";
            if (!af.PlateNo.IsNullOrWhiteSpaceX())
            {
                stockNo += $"/{af.PlateNo}";
            }
            else
            {
                if (!af.BodyNo.IsNullOrWhiteSpaceX())
                {
                    stockNo += $"/{af.BodyNo}";
                }
                else
                {
                    if (!af.MVFileNo.IsNullOrWhiteSpaceX())
                    {
                        stockNo += $"/{af.MVFileNo}";
                    }
                }
            }
            return stockNo;
        }

        private string GetStockNoFromMultiples(AllField af)
        {
            string stockNo = "";
            if (af.Multipliers.HasValue && af.Multipliers > 0)
            {
                stockNo += $"/{af.Multipliers}'s";
            }
            else
            {
                stockNo += "/xx";
            }
            return stockNo;
        }

        private string GetStockNoFromBrand(AllField af)
        {
            string stockNo = "";
            if (af.Brand.IsNullOrWhiteSpaceX())
            {
                stockNo += "/xx";
            }
            else
            {
                stockNo += $"/{af.Brand}";
            }
            return stockNo;
        }

        private string GetStockNoFromModel(AllField af)
        {
            string stockNo = "";
            if (!af.Model_.IsNullOrWhiteSpaceX())
            {
                stockNo += $"/{Utility.ToProperCase(af.Model_)}";
            }
            else
            {
                if (!af.Dimension.IsNullOrWhiteSpaceX())
                {
                    stockNo += $"/{af.Dimension}";
                }
                else
                {
                    if (!af.Size.IsNullOrWhiteSpaceX())
                    {
                        stockNo += $"/{af.Dimension}";
                    }
                    else
                    {
                        if (!af.Weight.IsNullOrWhiteSpaceX())
                        {
                            stockNo += $"/{af.Weight}";
                        }
                        else
                        {
                            if (!af.Materials.IsNullOrWhiteSpaceX())
                            {
                                stockNo += $"/{af.Materials}";
                            }
                            else
                            {
                                if (!af.Capacity.IsNullOrWhiteSpaceX())
                                {
                                    stockNo += $"/{af.Capacity}";
                                }
                                else
                                {
                                    if (!af.Color.IsNullOrWhiteSpaceX())
                                    {
                                        stockNo += $"/{af.Color}";
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return stockNo;
        }

        private string GetStockNoFromModelV(AllField af)
        {
            string stockNo = "";
            if (!af.Model_.IsNullOrWhiteSpaceX())
            {
                stockNo += $"/{Utility.ToProperCase(af.Model_)}";
            }
            else
            {
                if (!af.Weight.IsNullOrWhiteSpaceX())
                {
                    stockNo += $"/{af.Weight}";
                }
                else
                {
                    if (!af.Color.IsNullOrWhiteSpaceX())
                    {
                        stockNo += $"/{af.Color}";
                    }
                }
            }
            return stockNo;
        }

        public string GetStockNo(AllField af, string itemTypeCode, string itemCode)
        {
            string stockNo = "";
            var group = AllFieldsUtil.GetCategoryGroup(itemTypeCode, itemCode);
            if (group == CategoryGroup.LAND)
            {
                stockNo += ((af.Area != null) ? $"/{af.Area}sqm" : "");
            }
            else if (group == CategoryGroup.DRUGS)
            {
                if (!string.IsNullOrWhiteSpace(af.GenericName))
                {
                    if (af.GenericName.Equals("-"))
                    {
                        stockNo += "/xx";
                    }
                    else
                    {
                        var genName = Utility.ToProperCase(af.GenericName);
                        if (genName.Length >= 3)
                        {
                            stockNo += "/" + genName.Substring(0, 1) + genName.Substring(2, 1);
                        }
                        else
                        {
                            stockNo += "/" + genName.Substring(0, 1) + "X";
                        }
                    }
                }

                if (itemCode.Contains("SC-5.1.")) // Alcohol
                {
                    if (!string.IsNullOrWhiteSpace(af.DosageVolume))
                    {
                        stockNo += $"/{af.DosageVolume}";
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(af.DosageStrength))
                    {
                        if (af.DosageStrength.Equals("-"))
                        {
                            stockNo += "/xx";
                        }
                        else
                        {
                            stockNo += "/" + af.DosageStrength.Replace(" ", "").Trim();
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(af.DosageForm))
                    {
                        if (af.DosageForm.Equals("-"))
                        {
                            stockNo += "/xx";
                        }
                        else
                        {
                            stockNo += "/" + af.DosageForm.PadRight(3, 'X').Substring(0, 3);
                        }
                    }
                }

                if (af.Multipliers.HasValue && af.Multipliers > 0)
                {
                    stockNo += $"/{af.Multipliers}'s";
                }
                else
                {
                    stockNo += "/xx";
                }

                if (!string.IsNullOrWhiteSpace(af.Brand))
                {
                    if (af.Brand.Equals("-"))
                    {
                        stockNo += "/xx";
                    }
                    else
                    {
                        stockNo += "/" + af.Brand.Replace(" ", "").Trim();
                    }
                }
            }
            else if (group == CategoryGroup.OTHERS || group == CategoryGroup.OTHERS_A || group == CategoryGroup.OTHERS_B)
            {
                if (group == CategoryGroup.OTHERS)
                {
                    if (af.Multipliers.HasValue && af.Multipliers > 0)
                    {
                        stockNo += $"/{af.Multipliers}'s";
                    }
                    else
                    {
                        stockNo += "/xx";
                    }
                }

                if (!string.IsNullOrWhiteSpace(af.Brand))
                {
                    if (af.Brand == "-")
                    {
                        stockNo += "/xx";
                    }
                    else
                    {
                        stockNo += $"/{af.Brand}";
                    }
                }
                else
                {
                    stockNo += "/xx";
                }

                // Vehicles
                if (group == CategoryGroup.OTHERS_B)
                {
                    if (af.Model_.IsNullOrWhiteSpaceX())
                    {
                        if (af.Weight.IsNullOrWhiteSpaceX())
                        {
                            if (!string.IsNullOrWhiteSpace(af.Color))
                            {
                                stockNo += $"/{af.Color}";
                            }
                        }
                        else
                        {
                            stockNo += $"/{af.Weight}";
                        }
                    }
                    else
                    {
                        stockNo += $"/{af.Model_}";
                    }
                }
                else
                {
                    if (af.Model_.IsNullOrWhiteSpaceX())
                    {
                        if (af.Dimension.IsNullOrWhiteSpaceX())
                        {
                            if (af.Size.IsNullOrWhiteSpaceX())
                            {
                                if (af.Weight.IsNullOrWhiteSpaceX())
                                {
                                    if (af.Materials.IsNullOrWhiteSpaceX())
                                    {
                                        if (af.Capacity.IsNullOrWhiteSpaceX())
                                        {
                                            if (!string.IsNullOrWhiteSpace(af.Color))
                                            {
                                                stockNo += $"/{af.Color}";
                                            }
                                        }
                                        else
                                        {
                                            stockNo += $"/{af.Capacity}";
                                        }
                                    }
                                    else
                                    {
                                        stockNo += $"/{af.Materials}";
                                    }
                                }
                                else
                                {
                                    stockNo += $"/{af.Weight}";
                                }
                            }
                            else
                            {
                                stockNo += $"/{af.Size}";
                            }
                        }
                        else
                        {
                            stockNo += $"/{af.Dimension}";
                        }
                    }
                    else
                    {
                        stockNo += $"/{af.Model_}";
                    }
                }
            }
            else if (group == CategoryGroup.SERIAL_A)
            {
                //  "PropNo", "SerialNo"
                if (string.IsNullOrWhiteSpace(af.SerialNo))
                {
                    if (string.IsNullOrWhiteSpace(af.PropNo))
                    {
                        stockNo += $"/xx";
                    }
                    else
                    {
                        stockNo += $"/{af.PropNo}";
                    }
                }
                else
                {
                    stockNo += $"/{af.SerialNo}";
                }
            }
            else if (group == CategoryGroup.SERIAL_B)
            {
                // "Brand", "MVFileNo", "BodyNo", "PlateNo", "PropNo", "SerialNo"
                if (string.IsNullOrWhiteSpace(af.SerialNo))
                {
                    if (string.IsNullOrWhiteSpace(af.PropNo))
                    {
                        if (string.IsNullOrWhiteSpace(af.PlateNo))
                        {
                            if (string.IsNullOrWhiteSpace(af.BodyNo))
                            {
                                if (!string.IsNullOrWhiteSpace(af.MVFileNo))
                                {
                                    stockNo += $"/{af.MVFileNo}";
                                }
                            }
                            else
                            {
                                stockNo += $"/{af.BodyNo}";
                            }
                        }
                        else
                        {
                            stockNo += $"/{af.PlateNo.ToUpper()}";
                        }
                    }
                    else
                    {
                        stockNo += $"/{af.PropNo}";
                    }
                }
                else
                {
                    stockNo += $"/{af.SerialNo}";
                }
            }
            else if (group == CategoryGroup.SERIAL_C)
            {
                // "Color", "Capacity", "Materials", "Weight", "Size", "Dimension", "Model_", "Brand", "PropNo", "SerialNo"                        
                if (string.IsNullOrWhiteSpace(af.SerialNo))
                {
                    if (string.IsNullOrWhiteSpace(af.PropNo))
                    {
                        if (string.IsNullOrWhiteSpace(af.Model_))
                        {
                            if (string.IsNullOrWhiteSpace(af.Dimension))
                            {
                                if (string.IsNullOrWhiteSpace(af.Size))
                                {
                                    if (string.IsNullOrWhiteSpace(af.Weight))
                                    {
                                        if (string.IsNullOrWhiteSpace(af.Materials))
                                        {
                                            if (string.IsNullOrWhiteSpace(af.Capacity))
                                            {
                                                if (!string.IsNullOrWhiteSpace(af.Color))
                                                {
                                                    stockNo += $"/{af.Color}";
                                                }
                                            }
                                            else
                                            {
                                                stockNo += $"/{af.Capacity}";
                                            }
                                        }
                                        else
                                        {
                                            stockNo += $"/{af.Materials}";
                                        }
                                    }
                                    else
                                    {
                                        stockNo += $"/{af.Weight}";
                                    }
                                }
                                else
                                {
                                    stockNo += $"/{af.Size}";
                                }
                            }
                            else
                            {
                                stockNo += $"/{af.Dimension}";
                            }
                        }
                        else
                        {
                            stockNo += $"/{Utility.ToProperCase(af.Model_)}";
                        }
                    }
                    else
                    {
                        stockNo += $"/{af.PropNo}";
                    }
                }
                else
                {
                    stockNo += $"/{af.SerialNo}";
                }
            }
            else if (group == CategoryGroup.SERIAL)
            {
                // "Color", "Capacity", "Materials", "Weight", "Size", "Dimension", "Model_", "Brand", "MVFileNo", "BodyNo", "PlateNo", "PropNo", "SerialNo"                        
                if (string.IsNullOrWhiteSpace(af.SerialNo))
                {
                    if (string.IsNullOrWhiteSpace(af.PropNo))
                    {
                        if (string.IsNullOrWhiteSpace(af.PlateNo))
                        {
                            if (string.IsNullOrWhiteSpace(af.BodyNo))
                            {
                                if (string.IsNullOrWhiteSpace(af.MVFileNo))
                                {
                                    if (string.IsNullOrWhiteSpace(af.Model_))
                                    {
                                        if (string.IsNullOrWhiteSpace(af.Dimension))
                                        {
                                            if (string.IsNullOrWhiteSpace(af.Size))
                                            {
                                                if (string.IsNullOrWhiteSpace(af.Weight))
                                                {
                                                    if (string.IsNullOrWhiteSpace(af.Materials))
                                                    {
                                                        if (string.IsNullOrWhiteSpace(af.Capacity))
                                                        {
                                                            if (!string.IsNullOrWhiteSpace(af.Color))
                                                            {
                                                                stockNo += $"/{af.Color}";
                                                            }
                                                        }
                                                        else
                                                        {
                                                            stockNo += $"/{af.Capacity}";
                                                        }
                                                    }
                                                    else
                                                    {
                                                        stockNo += $"/{af.Materials}";
                                                    }
                                                }
                                                else
                                                {
                                                    stockNo += $"/{af.Weight}";
                                                }
                                            }
                                            else
                                            {
                                                stockNo += $"/{af.Size}";
                                            }
                                        }
                                        else
                                        {
                                            stockNo += $"/{af.Dimension}";
                                        }
                                    }
                                    else
                                    {
                                        stockNo += $"/{Utility.ToProperCase(af.Model_)}";
                                    }
                                }
                                else
                                {
                                    stockNo += $"/{af.MVFileNo}";
                                }
                            }
                            else
                            {
                                stockNo += $"/{af.BodyNo}";
                            }
                        }
                        else
                        {
                            stockNo += $"/{af.PlateNo.ToUpper()}";
                        }
                    }
                    else
                    {
                        stockNo += $"/{af.PropNo}";
                    }
                }
                else
                {
                    stockNo += $"/{af.SerialNo}";
                }
            }
            return stockNo;
        }

    }
}