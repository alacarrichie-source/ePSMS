using ClosedXML.Excel;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.PPMP_
{
    public interface IPPMPItemService
    {
        IQueryable<PPMPItemVM> GetAll(Guid? ppmpId);
        IQueryable<PPMPItemVM> GetAvailableByRequestId(Guid? prId);
        IQueryable<PPMPItemVM> GetAllByYearDept(int? forYear, Guid? deptId);
        ValueTask<PPMPItemVM> GetByIdAsync(Guid? id);        
        //ValueTask<PPMPItemVM> CreateAsync(PPMPItemVM model, string user, DateTime date);
        //ValueTask<PPMPItemVM> UpdateAsync(PPMPItemVM model, string user, DateTime date);
        //ValueTask<PPMPItemVM> DeleteAsync(PPMPItemVM model, string user, DateTime date);

        ValueTask<PPMPUploadVM> SaveExcelAsync(IEnumerable<HttpPostedFileBase> files, PPMPUploadVM model, string user, DateTime date);

    }

    internal class PPMPItemService : BaseValidator, IPPMPItemService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<PPMPItemVM> _vmExceptionService;
        private readonly ICodextnService _codextnService;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IPPMPSharedService _ppmpSharedService;
        private readonly IExceptionService<PPMPUploadVM> _uploadExceptionService = new ExceptionService<PPMPUploadVM>();

        public PPMPItemService(AppManEntities db)
        {
            _db = db;
            _vmExceptionService = new ExceptionService<PPMPItemVM>();
            _codextnService = new CodextnService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<PPMPItemVM>(propertyName);
            _ppmpSharedService = new PPMPSharedService(_db);
        }

        private Expression<Func<PPMPItem, PPMPItemVM>> Projection()
        {
            return s => new PPMPItemVM
            {
                Id = s.Id,
                PpmpId = s.PpmpId,
                RecNo = s.RecNo,
                Type = s.Type,
                Code = s.Code,
                Description = s.Description,
                Unit = s.Unit,
                Qty = s.Qty,
                EstBudget = s.EstBudget,
                UnitCost = s.UnitCost,
                ProcMode = s.ProcMode,
                Jan = s.Jan,
                Feb = s.Feb,
                Mar = s.Mar,
                Apr = s.Apr,
                May = s.May,
                Jun = s.Jun,
                Jul = s.Jul,
                Aug = s.Aug,
                Sep = s.Sep,
                Oct = s.Oct,
                Nov = s.Nov,
                Dec = s.Dec,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                UpdatedBy = s.UpdatedBy,
                UpdatedDt = s.UpdatedDt
            };
        }

        public ValueTask<PPMPItemVM> GetByIdAsync(Guid? id) => _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.PPMPItems.AsNoTracking().Where(w => w.Id == id)
                .Select(Projection()).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<PPMPItemVM> GetAll(Guid? ppmpId) => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.PPMPItems.AsNoTracking().Where(w => w.PpmpId == ppmpId)
                .Select(Projection());
            return data;
        });

        public IQueryable<PPMPItemVM> GetAvailableByRequestId(Guid? prId) => _vmExceptionService.TryCatch(() =>
        {
            var request = _db.Requests.Find(prId);
            if (request == null || !request.InsertedDt.HasValue)
            {
                return Enumerable.Empty<PPMPItemVM>().AsQueryable();
            }

            var data = _db.PPMPItems.AsNoTracking().Where(w => w.PPMP.ForYear == request.InsertedDt.Value.Year 
                && w.PPMP.DeptId == request.DeptId && w.Type == "S" && !w.RequestItems.Any())
                .Select(Projection());
            return data;
        });

        public IQueryable<PPMPItemVM> GetAllByYearDept(int? forYear, Guid? deptId) => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.PPMPItems.AsNoTracking().Where(w => w.PPMP.ForYear == forYear && w.PPMP.DeptId == deptId && w.Type == "S")
                .Select(Projection());
            return data;
        });

        private async Task ValidateFieldsAsync(PPMPItemVM model)
        {
            _imex = new InvalidModelException();

            //if (Enum.TryParse(model.PsType, out Category c))
            //{
            //    if (_allFieldService.IsBrandRequired(c))
            //    {
            //        if (string.IsNullOrWhiteSpace(model.AllField.Brand))
            //        {
            //            _imex.UpsertDataList(_getDisplayName(nameof(model.AllField.Brand)), "Field is required.");
            //        }
            //    }                                   
            //}

            _imex.ThrowIfContainsErrors();
        }

        //public ValueTask<PPMPItemVM> CreateAsync(PPMPItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        //{
        //    ValidateIfNull(model);
        //    await _ppmpSharedService.ValidateStatusAsync((Guid)model.PpmpId);
        //    await ValidateFieldsAsync(model);            

        //    model.Id = Guid.NewGuid();
        //    model.InsertedBy = user;
        //    model.UpdatedBy = user;
        //    model.InsertedDt = date;
        //    model.UpdatedDt = date;


        //    await _db.SaveChangesAsync();

        //    return model;
        //});

        //public ValueTask<PPMPItemVM> UpdateAsync(PPMPItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        //{
        //    ValidateIfNull(model);
        //    await ValidateFieldsAsync(model);
        //    await _ppmpSharedService.ValidateStatusAsync((Guid)model.PpmpId);

        //    model.UpdatedBy = user;
        //    model.UpdatedDt = date;


        //    return model;
        //});

        //public ValueTask<PPMPItemVM> DeleteAsync(PPMPItemVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        //{
        //    ValidateIfNull(model);

        //    model.UpdatedBy = user;
        //    model.UpdatedDt = date;

        //    var entity = await _db.PPMPItems.FirstOrDefaultAsync(f => f.Id == model.Id);
        //    ValidateRecord(entity, model.Id);
        //    await _ppmpSharedService.ValidateStatusAsync((Guid)model.PpmpId);

        //    if (model.IsSetLot)

        //    _db.PPMPItems.Remove(entity);
        //    await _db.SaveChangesAsync();

        //    return model;
        //});

        //private void SetItemEntity(PPMPItem entity, PPMPItemVM model, Mode mode)
        //{
        //    if (mode == Mode.ADD)
        //    {
        //        model.Id = Guid.NewGuid();
        //        entity.InsertedBy = model.InsertedBy;
        //        entity.InsertedDt = model.InsertedDt;                
        //    }            
        //}        

        public ValueTask<PPMPUploadVM> SaveExcelAsync(IEnumerable<HttpPostedFileBase> files, PPMPUploadVM model, string user, DateTime date) => _uploadExceptionService.TryCatch(async () =>
        {
            if (files == null || !files.Any())
            {
                throw new RecordNotFoundException("No files to upload!");
            }

            if (await _ppmpSharedService.IsPostedAsync((Guid)model.PpmpId))
            {
                throw new RecordAlreadyPostedException("PPMP Number is already Posted. Cannot update.");
            }

            var ppmp = await _db.PPMPs.FindAsync(model.PpmpId);
            if (ppmp == null)
            {
                throw new NotFoundException("PPMP record not found.");
            }

            var recNo = 0;
            var ppmpItems = _db.PPMPItems.Where(w => w.PpmpId == model.PpmpId);

            if (ppmpItems.Any())
            {
                if (model.Delete?.ToUpper() == "Y")
                {
                    _db.PPMPItems.RemoveRange(ppmpItems);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    recNo = (int)ppmpItems.Max(m => m.RecNo) + 1;
                }
            }            

            foreach (var file in files)
            {
                var extn = System.IO.Path.GetExtension(file.FileName).Substring(1).ToLower();

                if (extn != "xlsx" && extn != "xls")
                {
                    throw new InvalidValueException("Invalid file type.");
                }

                using (var workbook = new XLWorkbook(file.InputStream))
                {
                    var worksheet = workbook.Worksheet(1);

                    var rows = worksheet.RowsUsed().Skip((int)model.StartRow - 1);

                    foreach (var row in rows)
                    {
                        var code = row.Cell(model.Code).GetString()?.Trim();

                        if (string.IsNullOrWhiteSpace(code))
                        {
                            continue;
                        }

                        var description = row.Cell(model.Description).GetString()?.Trim();
                        var qtyText = row.Cell(model.Qty).GetString()?.Trim();
                        var size = row.Cell(model.Size).GetString()?.Trim();
                        var budgetText = row.Cell(model.Budget).GetString()?.Trim();        
                        var mode = row.Cell(model.Mode).GetString()?.Trim();
                        var janText = row.Cell(model.Jan).GetString()?.Trim();
                        var febText = row.Cell(model.Feb).GetString()?.Trim();
                        var marText = row.Cell(model.Mar).GetString()?.Trim();
                        var aprText = row.Cell(model.Apr).GetString()?.Trim();
                        var mayText = row.Cell(model.May).GetString()?.Trim();
                        var junText = row.Cell(model.Jun).GetString()?.Trim();
                        var julText = row.Cell(model.Jul).GetString()?.Trim();
                        var augText = row.Cell(model.Aug).GetString()?.Trim();
                        var sepText = row.Cell(model.Sep).GetString()?.Trim();
                        var octText = row.Cell(model.Oct).GetString()?.Trim();
                        var novText = row.Cell(model.Nov).GetString()?.Trim();
                        var decText = row.Cell(model.Dec).GetString()?.Trim();

                        int? qty = null;
                        decimal? unitCost = null;
                        decimal? budget = null;
                        int? jan = null;
                        int? feb = null;
                        int? mar = null;
                        int? apr = null;
                        int? may = null;
                        int? jun = null;
                        int? jul = null;
                        int? aug = null;
                        int? sep = null;
                        int? oct = null;
                        int? nov = null;
                        int? dec = null;
                        string type = row.Cell(model.Code).Style.Font.Bold ? "M" : "S";                        

                        if (!string.IsNullOrWhiteSpace(qtyText))
                        {
                            if (int.TryParse(qtyText, out var parseInt))
                            {
                                qty = parseInt;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(budgetText))
                        {
                            if (decimal.TryParse(budgetText, out var parseDecimal))
                            {
                                budget = parseDecimal;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(janText))
                        {
                            if (int.TryParse(janText, out var parseInt))
                            {
                                jan = parseInt;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(febText))
                        {
                            if (int.TryParse(febText, out var parseInt))
                            {
                                feb = parseInt;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(marText))
                        {
                            if (int.TryParse(marText, out var parseInt))
                            {
                                mar = parseInt;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(aprText))
                        {
                            if (int.TryParse(aprText, out var parseInt))
                            {
                                apr = parseInt;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(mayText))
                        {
                            if (int.TryParse(mayText, out var parseInt))
                            {
                                may = parseInt;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(junText))
                        {
                            if (int.TryParse(junText, out var parseInt))
                            {
                                jun = parseInt;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(julText))
                        {
                            if (int.TryParse(julText, out var parseInt))
                            {
                                jul = parseInt;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(augText))
                        {
                            if (int.TryParse(augText, out var parseInt))
                            {
                                aug = parseInt;
                            }
                        }


                        if (!string.IsNullOrWhiteSpace(sepText))
                        {
                            if (int.TryParse(sepText, out var parseInt))
                            {
                                sep = parseInt;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(octText))
                        {
                            if (int.TryParse(octText, out var parseInt))
                            {
                                oct = parseInt;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(novText))
                        {
                            if (int.TryParse(novText, out var parseInt))
                            {
                                nov = parseInt;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(decText))
                        {
                            if (int.TryParse(decText, out var parseInt))
                            {
                                dec = parseInt;
                            }
                        }

                        recNo++;
                        if (budget.HasValue && qty.HasValue && qty.Value != 0)
                        {
                            unitCost = decimal.Round(
                                budget.Value / qty.Value,
                                2,
                                MidpointRounding.AwayFromZero);
                        }

                        var ppmpItem = new PPMPItem()
                        {
                            Id = Guid.NewGuid(),
                            RecNo= recNo,                            
                            PpmpId = model.PpmpId,
                            Type = type,
                            Code = code,
                            Description = description,
                            Qty = qty,
                            Unit = size ?? "",
                            EstBudget = budget,
                            ProcMode = mode,
                            UnitCost = unitCost,
                            Jan = jan,
                            Feb = feb,
                            Mar = mar,
                            Apr = apr,
                            May = may,
                            Jun = jun,
                            Jul = jul,
                            Aug = aug,
                            Sep = sep,
                            Oct = oct,
                            Nov = nov,
                            Dec = dec,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };

                        _db.PPMPItems.Add(ppmpItem);
                        await _db.SaveChangesAsync();                        
                    }
                }
            }
            return model;
        });

        private void ValidateIfNull(PPMPItemVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(PPMPItem entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }
    }
}