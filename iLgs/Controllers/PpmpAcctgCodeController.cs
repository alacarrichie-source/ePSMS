using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("PPMP")]
    public class PpmpAcctgCodeController : BaseController
    {
        private readonly IUserService _userService;
        private string _menuId = "ppmp";

        private DbSet<PPMPAcctgCode> PPMPAcctgCodes
        {
            get { return _db.Set<PPMPAcctgCode>(); }
        }

        public PpmpAcctgCodeController()
        {
            _userService = new UserService(_db);
        }

        // GET: PpmpAcctgCode
        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.GetUserId();
            var access = await Access(userId, _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            ViewBag.Title = "PPMP Accounting Code Assignment";
            ViewBag.IsAdmin = await _userService.IsAdminAsync(userId);
            ViewBag.AllowAdd = access.AllowAdd;
            ViewBag.AllowEdit = access.AllowEdit;
            ViewBag.AllowDelete = access.AllowDelete;

            return View();
        }

        // READ: Starting from PPMPItems WHERE Type == 'M', grouped by Code, left joined with PPMPAcctgCodes and ItemTypes
        public async Task<ActionResult> Read([DataSourceRequest] DataSourceRequest request)
        {
            try
            {
                // 1. Fetch PPMP items where Type == "M"
                var rawPpmpItems = await _db.PPMPItems
                    .AsNoTracking()
                    .Where(x => x.Type == "M" && x.Code != null && x.Code != "")
                    .Select(x => new { x.Code, x.Description })
                    .ToListAsync();

                // 2. Group by Code to ensure strictly ONE logical row per Code
                var ppmpGroups = rawPpmpItems
                    .GroupBy(x => x.Code.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => new
                    {
                        Code = g.Key,
                        Description = g.Select(s => s.Description).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d)) ?? string.Empty
                    })
                    .ToList();

                // 3. Fetch existing mappings from PPMPAcctgCodes
                var mappings = await PPMPAcctgCodes
                    .AsNoTracking()
                    .ToListAsync();

                var mappingDict = mappings
                    .Where(m => !string.IsNullOrWhiteSpace(m.PPMPCode))
                    .GroupBy(m => m.PPMPCode.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                // 4. Preload ItemTypes for fast in-memory GSO Description lookup
                var itemTypes = await _db.ItemTypes
                    .AsNoTracking()
                    .Select(x => new { x.Id, Description = x.Code + x.GroupCode + " - " + x.Description })
                    .ToListAsync();

                var itemTypeDict = itemTypes.ToDictionary(x => x.Id, x => x.Description);

                // 5. Combine into ViewModel
                var data = new List<PpmpAcctgCodeVM>();
                foreach (var g in ppmpGroups)
                {
                    PPMPAcctgCode mapping = null;
                    if (mappingDict.ContainsKey(g.Code))
                    {
                        mapping = mappingDict[g.Code];
                    }

                    string gsoDesc = null;
                    if (mapping != null && mapping.GSOCodeId.HasValue && itemTypeDict.ContainsKey(mapping.GSOCodeId.Value))
                    {
                        gsoDesc = itemTypeDict[mapping.GSOCodeId.Value];
                    }

                    data.Add(new PpmpAcctgCodeVM
                    {
                        Id = mapping != null ? (Guid?)mapping.Id : null,
                        Code = g.Code,
                        Description = g.Description,
                        GSOCodeId = mapping != null ? mapping.GSOCodeId : null,
                        GSOCodeDescription = gsoDesc,
                        AcctgCode = mapping != null ? mapping.AcctgCode : null,
                        Remarks = mapping != null ? mapping.Remarks : null,
                        HasMapping = mapping != null
                    });
                }

                data = data.OrderBy(o => o.Code).ToList();

                return new JsonNetResult
                {
                    Data = data.ToDataSourceResult(request),
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                    Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
                };
            }
            catch (Exception ex)
            {
                return Json(new DataSourceResult { Errors = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // CREATE: Inserts into PPMPAcctgCodes only
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, PpmpAcctgCodeVM model)
        {
            try
            {
                var access = await Access(User.Identity.GetUserId(), _menuId);
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model == null)
                {
                    ModelState.AddModelError("", "No data provided.");
                }
                else
                {
                    var cleanCode = model.Code != null ? model.Code.Trim() : null;
                    var cleanAcctgCode = model.AcctgCode != null ? model.AcctgCode.Trim() : null;
                    var cleanRemarks = model.Remarks != null ? model.Remarks.Trim() : null;

                    if (string.IsNullOrWhiteSpace(cleanCode))
                    {
                        ModelState.AddModelError("Code", "PPMP Code is required.");
                    }

                    // Validate GSO Code
                    if (!model.GSOCodeId.HasValue || model.GSOCodeId.Value == Guid.Empty)
                    {
                        ModelState.AddModelError("GSOCodeId", "Please select a GSO Code.");
                    }
                    else
                    {
                        var validGsoCode = await _db.ItemTypes.AsNoTracking().AnyAsync(x => x.Id == model.GSOCodeId.Value);
                        if (!validGsoCode)
                        {
                            ModelState.AddModelError("GSOCodeId", "The selected GSO Code is invalid.");
                        }
                    }

                    if (string.IsNullOrWhiteSpace(cleanAcctgCode))
                    {
                        ModelState.AddModelError("AcctgCode", "Accounting Code is required.");
                    }

                    if (ModelState.IsValid)
                    {
                        // Validate server-side that PPMP Code exists in PPMPItems with Type == "M"
                        var existsInPpmp = await _db.PPMPItems
                            .AsNoTracking()
                            .AnyAsync(x => x.Type == "M" && x.Code == cleanCode);

                        if (!existsInPpmp)
                        {
                            ModelState.AddModelError("Code", "The selected PPMP Code does not exist or is not a valid 'M' group in PPMP.");
                        }

                        // Prevent duplicate mapping
                        var alreadyMapped = await PPMPAcctgCodes
                            .AsNoTracking()
                            .AnyAsync(x => x.PPMPCode == cleanCode);

                        if (alreadyMapped)
                        {
                            ModelState.AddModelError("Code", "An accounting code has already been assigned to this PPMP code.");
                        }

                        if (ModelState.IsValid)
                        {
                            string user = ControllerContext.HttpContext.User.Identity.Name;
                            DateTime date = DateTime.Now;

                            var entity = new PPMPAcctgCode
                            {
                                Id = Guid.NewGuid(),
                                PPMPCode = cleanCode,
                                GSOCodeId = model.GSOCodeId,
                                AcctgCode = cleanAcctgCode,
                                Remarks = cleanRemarks,
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date
                            };

                            PPMPAcctgCodes.Add(entity);
                            await _db.SaveChangesAsync();

                            model.Id = entity.Id;
                            model.Code = entity.PPMPCode;
                            model.GSOCodeId = entity.GSOCodeId;

                            var gsoItem = await _db.ItemTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.GSOCodeId.Value);
                            model.GSOCodeDescription = gsoItem != null ? gsoItem.Description : null;

                            model.AcctgCode = entity.AcctgCode;
                            model.Remarks = entity.Remarks;
                            model.HasMapping = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        // UPDATE: Updates PPMPAcctgCodes only
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, PpmpAcctgCodeVM model)
        {
            try
            {
                var access = await Access(User.Identity.GetUserId(), _menuId);
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model == null)
                {
                    ModelState.AddModelError("", "No data provided.");
                }
                else
                {
                    var cleanAcctgCode = model.AcctgCode != null ? model.AcctgCode.Trim() : null;
                    var cleanCode = model.Code != null ? model.Code.Trim() : null;
                    var cleanRemarks = model.Remarks != null ? model.Remarks.Trim() : null;

                    // Validate GSO Code
                    if (!model.GSOCodeId.HasValue || model.GSOCodeId.Value == Guid.Empty)
                    {
                        ModelState.AddModelError("GSOCodeId", "Please select a GSO Code.");
                    }
                    else
                    {
                        var validGsoCode = await _db.ItemTypes.AsNoTracking().AnyAsync(x => x.Id == model.GSOCodeId.Value);
                        if (!validGsoCode)
                        {
                            ModelState.AddModelError("GSOCodeId", "The selected GSO Code is invalid.");
                        }
                    }

                    if (string.IsNullOrWhiteSpace(cleanAcctgCode))
                    {
                        ModelState.AddModelError("AcctgCode", "Accounting Code is required.");
                    }

                    if (ModelState.IsValid)
                    {
                        PPMPAcctgCode entity = null;
                        if (model.Id.HasValue && model.Id.Value != Guid.Empty)
                        {
                            entity = await PPMPAcctgCodes.FirstOrDefaultAsync(x => x.Id == model.Id.Value);
                        }

                        if (entity == null && !string.IsNullOrWhiteSpace(cleanCode))
                        {
                            entity = await PPMPAcctgCodes.FirstOrDefaultAsync(x => x.PPMPCode == cleanCode);
                        }

                        if (entity == null)
                        {
                            ModelState.AddModelError("", "Accounting code assignment record not found.");
                        }
                        else
                        {
                            // Validate that underlying PPMP Code still exists as Type == "M"
                            var existsInPpmp = await _db.PPMPItems
                                .AsNoTracking()
                                .AnyAsync(x => x.Type == "M" && x.Code == entity.PPMPCode);

                            if (!existsInPpmp)
                            {
                                ModelState.AddModelError("Code", "The underlying PPMP Code no longer exists as a valid 'M' group in PPMP.");
                            }

                            if (ModelState.IsValid)
                            {
                                string user = ControllerContext.HttpContext.User.Identity.Name;
                                DateTime date = DateTime.Now;

                                entity.GSOCodeId = model.GSOCodeId;
                                entity.AcctgCode = cleanAcctgCode;
                                entity.Remarks = cleanRemarks;
                                entity.UpdatedBy = user;
                                entity.UpdatedDt = date;

                                await _db.SaveChangesAsync();

                                model.Id = entity.Id;
                                model.Code = entity.PPMPCode;
                                model.GSOCodeId = entity.GSOCodeId;

                                var gsoItem = await _db.ItemTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.GSOCodeId.Value);
                                model.GSOCodeDescription = gsoItem != null ? gsoItem.Description : null;

                                model.AcctgCode = entity.AcctgCode;
                                model.Remarks = entity.Remarks;
                                model.HasMapping = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        // DESTROY: Deletes only from PPMPAcctgCodes, never touches PPMPItems or ItemTypes
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Destroy([DataSourceRequest] DataSourceRequest request, PpmpAcctgCodeVM model)
        {
            try
            {
                var access = await Access(User.Identity.GetUserId(), _menuId);
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Access Denied!");
                }
                else if (model != null)
                {
                    PPMPAcctgCode entity = null;
                    if (model.Id.HasValue && model.Id.Value != Guid.Empty)
                    {
                        entity = await PPMPAcctgCodes.FirstOrDefaultAsync(x => x.Id == model.Id.Value);
                    }

                    if (entity == null && !string.IsNullOrWhiteSpace(model.Code))
                    {
                        var cleanCode = model.Code.Trim();
                        entity = await PPMPAcctgCodes.FirstOrDefaultAsync(x => x.PPMPCode == cleanCode);
                    }

                    if (entity == null)
                    {
                        ModelState.AddModelError("DeleteError", "Accounting code assignment not found or already removed.");
                    }
                    else
                    {
                        PPMPAcctgCodes.Remove(entity);
                        await _db.SaveChangesAsync();

                        // Reset model to unassigned state
                        model.Id = null;
                        model.GSOCodeId = null;
                        model.GSOCodeDescription = null;
                        model.AcctgCode = null;
                        model.Remarks = null;
                        model.HasMapping = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("DeleteError", ex.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        // LOOKUP: GSO Codes from ItemTypes master table
        public async Task<JsonResult> GetGSOCodes(string text)
        {
            try
            {
                var query = _db.ItemTypes.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    query = query.Where(x => x.Description.Contains(text) || x.Code.Contains(text));
                }

                var data = await query
                    .OrderBy(x => x.Description)
                    .Select(x => new
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Description = x.Code + x.GroupCode + " - " + x.Description
                    })
                    .ToListAsync();

                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }

        // LOOKUP: Accounting Codes from master tables
        public async Task<JsonResult> GetAccountingCodes(string text)
        {
            try
            {
                var query = _db.AccountCodeItems
                    .AsNoTracking()
                    .Where(w => !string.IsNullOrEmpty(w.Code));

                if (!string.IsNullOrWhiteSpace(text))
                {
                    query = query.Where(w => w.Code.Contains(text) || (w.ItemType != null && w.ItemType.Description.Contains(text)));
                }

                var list = await query
                    .Select(s => new
                    {
                        Code = s.Code,
                        Description = s.ItemType != null ? s.ItemType.Description : s.Code
                    })
                    .Distinct()
                    .ToListAsync();

                // Also include any distinct accounting codes already assigned in PPMPAcctgCodes
                var existingCodes = await PPMPAcctgCodes
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.AcctgCode))
                    .Select(x => new { Code = x.AcctgCode, Description = x.AcctgCode })
                    .Distinct()
                    .ToListAsync();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    existingCodes = existingCodes.Where(x => x.Code.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }

                var combined = list.Concat(existingCodes)
                    .GroupBy(x => x.Code.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => new { Code = g.Key, Description = g.First().Description })
                    .OrderBy(o => o.Code)
                    .ToList();

                return Json(combined, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }
    }
}
