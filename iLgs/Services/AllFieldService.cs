using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.CategoryEnum;

namespace iLgs.Services
{
    public interface IAllFieldService
    {
        IQueryable<AllField> GetAllByPsCardId(Guid? psCardId);
        IQueryable<AllField> GetAllByRisItemId(Guid? risItemId);
        ValueTask<AllField> GetByIdAsync(Guid id);
        ValueTask<AllField> CreatePsCardFieldsAsync(PsCardVM model, string user, DateTime date);
        ValueTask<AllField> CreateRisFieldsAsync(RisItemEntryVM model, string user, DateTime date);
        ValueTask<AllField> UpdatePsCardFieldsAsync(PsCardVM model, string user, DateTime date);
        ValueTask<AllField> UpdateRisFieldsAsync(RisItemEntryVM model, string user, DateTime date);
        ValueTask<AllField> DeleteAsync(AllField model, string user, DateTime date);
        //string GetDescription(AllField allFields);
        //string GetDescription(PsCardVM psCardVM);
        string GetRisDescription(RisItemEntryVM model);
        string GetCardStockNo(PsCardVM model);
        string GetRisStockNo(RisItemEntryVM model);
        void ValidatePsCardAllField(PsCardVM model);
        string GetStockNo(AllField af, string itemTypeCode, string itemCode);
        bool IsBrandRequired(Category c);
        bool IsNoIcs(Guid? itemCodeId);
    }

    public class AllFieldService : IAllFieldService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<AllField> _exceptionService = new ExceptionService<AllField>();

        public AllFieldService(AppManEntities db)
        {
            _db = db;
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
            var af = model.AllField;
            if (Enum.TryParse(model.ItemTypeCode, out Category c))
            {
                if (c == CatDrugs())
                {
                    if (string.IsNullOrWhiteSpace(af.GenericName))
                    {
                        throw new InvalidValueException("Generic Name is Required!");
                    }
                    if (string.IsNullOrWhiteSpace(af.Brand))
                    {
                        throw new InvalidValueException("Brand is Required!");
                    }
                    if (model.ItemNo.Substring(0, 4) == "5.1.") // Alcoh1ol
                    {
                        if (string.IsNullOrWhiteSpace(af.DosageVolume))
                        {
                            throw new InvalidValueException("Dosage Volume is Required!");
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(af.DosageStrength))
                        {
                            throw new InvalidValueException("Dosage Strength is Required!");
                        }
                        if (string.IsNullOrWhiteSpace(af.DosageForm))
                        {
                            throw new InvalidValueException("Dosage Form is Required!");
                        }
                    }
                }
                else if (c == CatMachineries() 
                    || c == CatTransportations() 
                    || c == CatFurnitures() 
                    || c == CatOtherProperties()
                    || c == CatMedicals() 
                    || c == CatAgriculturals() 
                    || c == CatAnimalSupplies() 
                    || c == CatConstructionMaterials()
                    || c == CatOfficeSupplies() 
                    || c == CatAccountableForms() 
                    || c == CatNonAccountableForns() 
                    || c == CatMilitaries()
                    || c == CatOtherSupplies())
                {
                    if (string.IsNullOrWhiteSpace(af.Brand))
                    {
                        throw new InvalidValueException("Brand is Required!");
                    }
                    if (string.IsNullOrWhiteSpace(af.Model_) && string.IsNullOrWhiteSpace(af.Size) && string.IsNullOrWhiteSpace(af.Dimension)
                        && string.IsNullOrWhiteSpace(af.Weight) && string.IsNullOrWhiteSpace(af.Materials) && string.IsNullOrWhiteSpace(af.Capacity)
                        && string.IsNullOrWhiteSpace(af.Color))
                    {
                        throw new InvalidValueException("Model or Dimension or Size or Weight or Materials or Capacity or Color is Required!");
                    }
                }
            }
        }

        public void ValidateRisAllField(RisItemEntryVM model)
        {
            var af = model.AllField;
            if (Enum.TryParse(model.PsType, out Category c))
            {
                if (c == CatDrugs())
                {
                    if (string.IsNullOrWhiteSpace(af.GenericName))
                    {
                        throw new InvalidValueException("Generic Name is Required!");
                    }
                    if (string.IsNullOrWhiteSpace(af.Brand))
                    {
                        throw new InvalidValueException("Brand is Required!");
                    }
                    if (model.ItemNo.Substring(0, 4) == "5.1.") // Alcoh1ol
                    {
                        if (string.IsNullOrWhiteSpace(af.DosageVolume))
                        {
                            throw new InvalidValueException("Dosage Volume is Required!");
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(af.DosageStrength))
                        {
                            throw new InvalidValueException("Dosage Strength is Required!");
                        }
                        if (string.IsNullOrWhiteSpace(af.DosageForm))
                        {
                            throw new InvalidValueException("Dosage Form is Required!");
                        }
                    }
                }
                else if (c == CatMachineries() || c == CatTransportations() || c == CatFurnitures() || c == CatOtherProperties()
                || c == CatMedicals() || c == CatAgriculturals() || c == CatAnimalSupplies() || c == CatConstructionMaterials()
                || c == CatOfficeSupplies() || c == CatAccountableForms() || c == CatNonAccountableForns() || c == CatMilitaries()
                || c == CatOtherSupplies())
                {
                    if (string.IsNullOrWhiteSpace(af.Brand))
                    {
                        throw new InvalidValueException("Brand is Required!");
                    }
                    if (string.IsNullOrWhiteSpace(af.Model_) && string.IsNullOrWhiteSpace(af.Size) && string.IsNullOrWhiteSpace(af.Dimension)
                        && string.IsNullOrWhiteSpace(af.Weight) && string.IsNullOrWhiteSpace(af.Materials) && string.IsNullOrWhiteSpace(af.Capacity)
                        && string.IsNullOrWhiteSpace(af.Color))
                    {
                        throw new InvalidValueException("Model or Dimension or Size or Weight or Materials or Capacity or Color is Required!");
                    }
                }
            }
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
            var af = model.AllField;
            if (Enum.TryParse(model.PsType, out Category c))
            {
                if (c == CatLands())
                {
                    description += af.Area.ToString() + "sqm";
                }
                else if (c == CatMachineries() 
                    || c == CatTransportations() 
                    || c == CatFurnitures() 
                    || c == CatOtherProperties()
                    || c == CatMedicals() 
                    || c == CatAgriculturals() 
                    || c == CatAnimalSupplies() 
                    || c == CatConstructionMaterials()
                    || c == CatOfficeSupplies() 
                    || c == CatAccountableForms() 
                    || c == CatNonAccountableForns() 
                    || c == CatMilitaries()
                    || c == CatOtherSupplies())
                {
                    description = (!string.IsNullOrWhiteSpace(af.Model_) ? $"{af.Model_}" : "") +
                                (!string.IsNullOrWhiteSpace(af.Dimension) ? $" {af.Dimension}" : "") +
                                (!string.IsNullOrWhiteSpace(af.Size) ? $" {af.Size}" : "") +
                                (!string.IsNullOrWhiteSpace(af.Weight) ? $" {af.Weight}" : "") +
                                (!string.IsNullOrWhiteSpace(af.Materials) ? $" {af.Materials}" : "") +
                                (!string.IsNullOrWhiteSpace(af.Capacity) ? $" {af.Capacity}" : "") +
                                (!string.IsNullOrWhiteSpace(af.Color) ? $" {af.Color}" : "") +
                                (!string.IsNullOrWhiteSpace(af.Type) ? $" {af.Type}" : "");
                }
                else if (c == CatDrugs())
                {
                    description = (!string.IsNullOrWhiteSpace(af.GenericName) ? $"{af.GenericName}" : "") +
                                (!string.IsNullOrWhiteSpace(af.DosageStrength) ? $" {af.DosageStrength}" : "") +
                                (!string.IsNullOrWhiteSpace(af.DosageForm) ? $" {af.DosageForm}" : "") +
                                (!string.IsNullOrWhiteSpace(af.DosageVolume) ? $" {af.DosageVolume}" : "") +
                                (!string.IsNullOrWhiteSpace(af.Others) ? $" {af.Others}" : "") +
                                (!(af.Multipliers == null) ? $" {af.Multipliers}'s" : "");
                }
                else if (c == CatRepairs())
                {
                    description = (!string.IsNullOrWhiteSpace(af.SerialNo) ? $"{af.SerialNo}" : "") +
                                (!string.IsNullOrWhiteSpace(af.PropNo) ? $" {af.PropNo}" : "") +
                                (!string.IsNullOrWhiteSpace(af.PlateNo) ? $" {af.PlateNo}" : "") +
                                (!string.IsNullOrWhiteSpace(af.BodyNo) ? $" {af.BodyNo}" : "") +
                                (!string.IsNullOrWhiteSpace(af.MVFileNo) ? $" {af.MVFileNo}" : "") +
                                (!string.IsNullOrWhiteSpace(af.Type) ? $" {af.Type}" : ""); 
                }
            }
            return description ?? "";
        }

        public bool IsNoIcs(Guid? itemCodeId)
        {
            return _db.ItemCodes.Where(w => w.Id == itemCodeId && (w.IsConsumable == "Y" || w.IsIncorporated == "Y" || w.ForDistribution == "Y")).Any();
        }

        public string GetRisStockNo(RisItemEntryVM model)
        {
            string stockNo = model.ItemCode.Trim();
            if (!IsNoIcs(model.ItemCodeId))
            {
                stockNo += GetStockNo(model.AllField, model.PsType, model.ItemCode);
            }
            return stockNo ?? "";
        }

        public string GetCardStockNo(PsCardVM model)
        {
            string stockNo = model.ItemCode.Trim();
            if (model.FromDonation == true)
            {
                stockNo = "FD" + stockNo;
            }
            if (!IsNoIcs(model.ItemCodeId))
            {
                stockNo += GetStockNo(model.AllField, model.ItemTypeCode, model.ItemCode);
            }
            return stockNo ?? "";
        }


        public bool IsBrandRequired(Category c)
        {
            return (c == CatMachineries() || c == CatTransportations() || c == CatFurnitures() || c == CatOtherProperties()
                    || c == CatMedicals() || c == CatAgriculturals() || c == CatAnimalSupplies() || c == CatConstructionMaterials()
                    || c == CatOfficeSupplies() || c == CatAccountableForms() || c == CatNonAccountableForns() || c == CatMilitaries()
                    || c == CatOtherSupplies() || c == CatRepairs()) || c == CatDrugs();
        }

        public string GetStockNo(AllField af, string itemTypeCode, string itemCode)
        {
            string stockNo = "";
            if (Enum.TryParse(itemTypeCode, out Category c))
            {
                if (c == CatLands())
                {
                    stockNo += ((af.Area != null) ? $"/{af.Area}sqm" : "");
                }
                else if (c == CatMachineries()
                    || c == CatTransportations()
                    || c == CatFurnitures()
                    || c == CatOtherProperties()
                    || c == CatMedicals()
                    || c == CatAgriculturals()
                    || c == CatAnimalSupplies()
                    || c == CatConstructionMaterials()
                    || c == CatOfficeSupplies()
                    || c == CatAccountableForms()
                    || c == CatNonAccountableForns()
                    || c == CatMilitaries()
                    || c == CatOtherSupplies())
                {
                    stockNo += (!string.IsNullOrWhiteSpace(af.Brand) ? $"/{af.Brand}" : "/xx");
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
                        stockNo += $"/{af.Model_}";
                    }

                }
                else if (c == CatRepairs())
                {
                    if (string.IsNullOrWhiteSpace(af.SerialNo))
                    {
                        if (string.IsNullOrWhiteSpace(af.PropNo))
                        {
                            stockNo += $"/NA";
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

                    if (itemCode.Equals("RSL-5.1") || itemCode.Contains("SL-5.2.") || itemCode.Contains("SL-6.")
                        || itemCode.Contains("SL-99."))
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
                            stockNo += $"/{af.PlateNo}";
                        }
                    }

                    if (itemCode.Contains("SL-1.2") || itemCode.Contains("SL-2.2") || itemCode.Contains("SL-3.2") || itemCode.Contains("SL-4.2")
                        || itemCode.Contains("SL-5.2.") || itemCode.Contains("SL-6.2.")
                        || itemCode.Contains("SL-7.2") || itemCode.Contains("SL-8.2") || itemCode.Contains("SL-99.2")
                        )
                    {
                        stockNo += (!string.IsNullOrWhiteSpace(af.Brand) ? $"/{af.Brand}" : "/xx");
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
                            stockNo += $"/{af.Model_}";
                        }
                    }
                }
                else if (c == CatDrugs())
                {
                    if (!string.IsNullOrWhiteSpace(af.GenericName))
                    {
                        if (af.GenericName.Length >= 3)
                        {
                            stockNo += "/" + af.GenericName.Substring(0, 1) + af.GenericName.Substring(2, 1);
                        }
                        else
                        {
                            stockNo += "/" + af.GenericName.Substring(0, 1) + "X";
                        }
                    }

                    if (itemCode.Contains("SC-5.1.")) // Alcohol
                    {
                        if (!string.IsNullOrWhiteSpace(af.DosageVolume))
                        {
                            stockNo += "/" + af.DosageVolume.Trim() + "'s";
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(af.DosageStrength))
                        {
                            stockNo += "/" + af.DosageStrength.Replace(" ", "").Trim();
                        }
                        if (!string.IsNullOrWhiteSpace(af.DosageForm))
                        {
                            stockNo += "/" + af.DosageForm.PadRight(3, 'X').Substring(0, 3);
                        }
                    }

                    if (af.Multipliers.HasValue)
                    {
                        stockNo += "/" + af.Multipliers.ToString().Trim() + "'s";
                    }

                    if (!string.IsNullOrWhiteSpace(af.Brand))
                    {
                        stockNo += "/" + af.Brand.Replace(" ", "").Trim();
                    }
                    else
                    {
                        stockNo += "/xx";
                    }
                }
            }
            return stockNo;
        }

    }
}