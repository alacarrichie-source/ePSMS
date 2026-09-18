using iLgs.Ai.Services;
using iLgs.Models;
using iLgs.Services.Codes;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly AppManEntities _db;
        private readonly ICodextnService _codextnService;
        private readonly IUserService _userService;

        public DashboardService(AppManEntities db)
        {
            _db = db;
            _codextnService = new CodextnService(_db);
            _userService = new UserService(_db);
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(string userId, string userName)
        {
            var model = new DashboardViewModel
            {
                UserName = userName,
                FiscalYear = DateTime.Now.Year,
                CurrentDateText = DateTime.Now.ToString("dddd, MMMM d, yyyy")
            };

            // 1. Determine User Role and Department
            bool isAdmin = false;
            if (!string.IsNullOrEmpty(userName))
            {
                isAdmin = await _userService.IsUserNameAdminAsync(userName);
            }
            model.IsAdmin = isAdmin;

            UserProfile profile = null;
            if (!string.IsNullOrEmpty(userId))
            {
                profile = await _db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            }

            if (profile != null)
            {
                model.UserFullName = !string.IsNullOrWhiteSpace(profile.NameFull)
                    ? profile.NameFull
                    : (!string.IsNullOrWhiteSpace(profile.NameFirst) ? profile.NameFirst + " " + profile.NameLast : userName);
                model.DepartmentName = profile.Department;
            }
            else
            {
                model.UserFullName = userName;
                model.DepartmentName = isAdmin ? "Administrator / GSO" : "City Government Office";
            }

            // Authorized departments for user-level filtering
            var userDeptIds = new List<Guid>();
            if (!isAdmin && !string.IsNullOrEmpty(userId))
            {
                var userDepts = await _codextnService.GetUserDepartmentsAsync(userId);
                userDeptIds = await userDepts.Select(d => d.Id).ToListAsync();
            }

            // 2. Build Role-Specific Dashboard
            if (isAdmin)
            {
                await BuildAdminDashboardAsync(model);
            }
            else
            {
                await BuildUserDashboardAsync(model, userId, userName, profile != null ? profile.NameFull : null, userDeptIds);
            }

            // 3. Populate legacy properties for backwards-compatibility
            PopulateLegacyProperties(model, isAdmin, userDeptIds, userId, userName);

            return model;
        }

        #region Admin Operational Pulse Builder

        private async Task BuildAdminDashboardAsync(DashboardViewModel model)
        {
            var admin = model.AdminDashboard;
            int fy = model.FiscalYear > 0 ? model.FiscalYear : DateTime.Now.Year;
            admin.Filters.FiscalYear = fy;

            try
            {
                // 0. Populate Department List for Admin Filter Toolbar
                try
                {
                    var depts = await _db.UserProfiles.AsNoTracking()
                        .Where(u => !string.IsNullOrEmpty(u.Department))
                        .Select(u => u.Department)
                        .Distinct()
                        .OrderBy(d => d)
                        .ToListAsync();

                    admin.DepartmentList = depts ?? new List<string>();
                }
                catch
                {
                    admin.DepartmentList = new List<string>();
                }

                // A. Requires Attention (System-wide)
                var attention = new List<DashboardAttentionItemVM>();

                // 1. Returned PRs across all departments
                var returnedHistories = await _db.DocumentStatusHistories.AsNoTracking()
                    .Where(h => h.DocumentType == DocumentTypes.PurchaseRequest)
                    .GroupBy(h => h.DocumentId)
                    .Select(g => g.OrderByDescending(x => x.ChangedDt).FirstOrDefault())
                    .Where(h => h != null && h.ToStatus == PrStatuses.Returned)
                    .Take(4)
                    .ToListAsync();

                if (returnedHistories.Any())
                {
                    var returnedIds = returnedHistories.Select(h => h.DocumentId).ToList();
                    var returnedRequests = await _db.Requests.AsNoTracking()
                        .Where(r => returnedIds.Contains(r.Id))
                        .ToListAsync();

                    foreach (var hist in returnedHistories)
                    {
                        var req = returnedRequests.FirstOrDefault(r => r.Id == hist.DocumentId);
                        if (req != null)
                        {
                            attention.Add(new DashboardAttentionItemVM
                            {
                                Id = req.Id,
                                Reference = req.PrNo ?? req.CtrlNo ?? "PR",
                                Module = "Purchase Request",
                                Department = req.Department ?? "",
                                ShortDescription = req.Purpose,
                                Status = "Returned for Revision",
                                Reason = !string.IsNullOrWhiteSpace(hist.Remarks) ? hist.Remarks : "Correction required by reviewing officer.",
                                ActionUrl = "/Requests/Index",
                                ActionLabel = "Review PR",
                                UrgencyLevel = "danger",
                                Date = hist.ChangedDt
                            });
                        }
                    }
                }

                // 2. AIRs Awaiting Technical Inspection
                var pendingAirs = await _db.AIRs.AsNoTracking()
                    .Include(a => a.Order)
                    .Where(a => a.IsInspected != true)
                    .OrderByDescending(a => a.AIRDate ?? a.InsertedDt)
                    .Take(4)
                    .ToListAsync();

                foreach (var air in pendingAirs)
                {
                    attention.Add(new DashboardAttentionItemVM
                    {
                        Id = air.Id,
                        Reference = air.AIRNo ?? air.CtrlNo ?? "Pending AIR",
                        Module = "Inspection & Acceptance",
                        Department = air.Order != null ? (air.Order.Department ?? "") : "",
                        ShortDescription = "PO: " + (air.Order != null ? air.Order.PoNo : "N/A"),
                        Status = "Awaiting Inspection",
                        Reason = "Deliveries received; awaiting technical property inspection sign-off.",
                        ActionUrl = "/AIRs/Inspection",
                        ActionLabel = "Inspect AIR",
                        UrgencyLevel = "warning",
                        Date = air.AIRDate ?? air.InsertedDt
                    });
                }

                // 3. Draft PARs / ICSs needing completion or posting
                var draftPars = await _db.PARs.AsNoTracking()
                    .Where(p => string.IsNullOrEmpty(p.PostedBy))
                    .OrderByDescending(p => p.InsertedDt)
                    .Take(3)
                    .ToListAsync();

                foreach (var par in draftPars)
                {
                    attention.Add(new DashboardAttentionItemVM
                    {
                        Id = par.Id,
                        Reference = par.ParNo ?? "PAR Draft",
                        Module = "Property Acknowledgment (PAR)",
                        ShortDescription = "Custodian accountability receipt in draft status",
                        Status = "Draft",
                        Reason = "Accountability record prepared but not yet posted.",
                        ActionUrl = "/ParSet/Index",
                        ActionLabel = "Open PAR",
                        UrgencyLevel = "info",
                        Date = par.InsertedDt
                    });
                }

                admin.RequiresAttention = attention;

                // B. Operational Summary
                var summary = admin.OperationalSummary;

                // PR counts
                var allRequests = await _db.Requests.AsNoTracking()
                    .Select(r => new { r.Id, r.SubmittedBy, r.PostedBy, r.PostedDt })
                    .ToListAsync();

                summary.PrTotal = allRequests.Count;
                summary.PrSubmitted = allRequests.Count(r => !string.IsNullOrEmpty(r.SubmittedBy) && string.IsNullOrEmpty(r.PostedBy) && !r.PostedDt.HasValue);
                summary.PrDraft = allRequests.Count(r => string.IsNullOrEmpty(r.SubmittedBy) && string.IsNullOrEmpty(r.PostedBy) && !r.PostedDt.HasValue);
                summary.PrPosted = allRequests.Count(r => !string.IsNullOrEmpty(r.PostedBy) || r.PostedDt.HasValue);
                summary.PrReturned = returnedHistories.Count;

                // PO counts
                var orders = await _db.Orders.AsNoTracking()
                    .Select(o => new { o.Id, o.PostedBy, o.PostedDt })
                    .ToListAsync();

                summary.PoTotal = orders.Count;
                summary.PoPosted = orders.Count(o => o.PostedDt.HasValue || !string.IsNullOrEmpty(o.PostedBy));
                summary.PoDraft = orders.Count(o => !o.PostedDt.HasValue && string.IsNullOrEmpty(o.PostedBy));

                summary.PoCommittedAmount = await _db.OrderItems.AsNoTracking()
                    .Where(i => i.Amount.HasValue)
                    .SumAsync(i => (decimal?)i.Amount) ?? 0m;

                // AIR counts
                var airs = await _db.AIRs.AsNoTracking()
                    .Select(a => new { a.Id, a.IsInspected })
                    .ToListAsync();
                summary.AirTotal = airs.Count;
                summary.AirPendingInspection = airs.Count(a => a.IsInspected != true);
                summary.AirInspected = airs.Count(a => a.IsInspected == true);

                // RIS counts
                var risses = await _db.RISses.AsNoTracking()
                    .Select(r => new { r.Id, r.ApprovedBy, r.IssuedBy, r.PostedBy, r.PostedDt })
                    .ToListAsync();
                summary.RisTotal = risses.Count;
                summary.RisPendingApproval = risses.Count(r => string.IsNullOrEmpty(r.ApprovedBy));
                summary.RisPendingIssuance = risses.Count(r => !string.IsNullOrEmpty(r.ApprovedBy) && string.IsNullOrEmpty(r.IssuedBy));
                summary.RisPosted = risses.Count(r => !string.IsNullOrEmpty(r.PostedBy) || r.PostedDt.HasValue);

                // Cards & Accountability
                summary.StockCardCount = await _db.PsCards.AsNoTracking().CountAsync(c => c.CardCategory == "S");
                summary.PropertyCardCount = await _db.PsCards.AsNoTracking().CountAsync(c => c.CardCategory == "P");
                summary.ParCount = await _db.PARs.AsNoTracking().CountAsync();
                summary.IcsCount = await _db.IcsPars.AsNoTracking().CountAsync();

                // C. Executive KPI Strip
                admin.ExecutiveKpis = new AdminExecutiveKpiVM
                {
                    PrTotal = summary.PrTotal,
                    PrActive = summary.PrSubmitted + summary.PrDraft,
                    PrDraft = summary.PrDraft,
                    PrSubmitted = summary.PrSubmitted,
                    PrReturned = summary.PrReturned,
                    PrPosted = summary.PrPosted,

                    PoTotal = summary.PoTotal,
                    PoActive = summary.PoPosted,
                    PoDraft = summary.PoDraft,
                    PoPosted = summary.PoPosted,
                    PoCommittedAmount = summary.PoCommittedAmount,

                    AirPendingInspection = summary.AirPendingInspection,
                    AirInspected = summary.AirInspected,
                    AirTotal = summary.AirTotal,

                    RisPendingTotal = summary.RisPendingApproval + summary.RisPendingIssuance,
                    RisPendingApproval = summary.RisPendingApproval,
                    RisPendingIssuance = summary.RisPendingIssuance,
                    RisPosted = summary.RisPosted,

                    PropertyCardCount = summary.PropertyCardCount,
                    StockCardCount = summary.StockCardCount,
                    TotalRegistryCards = summary.PropertyCardCount + summary.StockCardCount,

                    ActionableExceptionsCount = summary.PrReturned + summary.AirPendingInspection + summary.RisPendingApproval + summary.RisPendingIssuance
                };

                // D. Monthly Procurement Activity & PO Commitment Trend (Grouped database query by month)
                var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

                // Group PRs by month for this FY
                var prMonthly = await _db.Requests.AsNoTracking()
                    .Where(r => (r.InsertedDt.HasValue && r.InsertedDt.Value.Year == fy) || (r.PrDate.HasValue && r.PrDate.Value.Year == fy))
                    .GroupBy(r => (r.InsertedDt ?? r.PrDate).Value.Month)
                    .Select(g => new { Month = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Group POs by month and sum amount for this FY
                var poMonthly = await _db.Orders.AsNoTracking()
                    .Where(o => (o.InsertedDt.HasValue && o.InsertedDt.Value.Year == fy) || (o.PoDate.HasValue && o.PoDate.Value.Year == fy))
                    .GroupBy(o => (o.InsertedDt ?? o.PoDate).Value.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        Count = g.Count(),
                        Amount = g.SelectMany(o => o.OrderItems).Where(i => i.Amount.HasValue).Sum(i => (decimal?)i.Amount) ?? 0m
                    })
                    .ToListAsync();

                // Group AIRs by month for this FY
                var airMonthly = await _db.AIRs.AsNoTracking()
                    .Where(a => (a.InsertedDt.HasValue && a.InsertedDt.Value.Year == fy) || (a.AIRDate.HasValue && a.AIRDate.Value.Year == fy))
                    .GroupBy(a => (a.InsertedDt ?? a.AIRDate).Value.Month)
                    .Select(g => new { Month = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Group RIS by month for this FY
                var risMonthly = await _db.RISses.AsNoTracking()
                    .Where(r => (r.InsertedDt.HasValue && r.InsertedDt.Value.Year == fy) || (r.RisDate.HasValue && r.RisDate.Value.Year == fy))
                    .GroupBy(r => (r.InsertedDt ?? r.RisDate).Value.Month)
                    .Select(g => new { Month = g.Key, Count = g.Count() })
                    .ToListAsync();

                var monthlyVolumes = new List<DashboardMonthlyVolumeVM>();
                var monthlyPoCommitments = new List<DashboardMonthlyVolumeVM>();

                for (int m = 1; m <= 12; m++)
                {
                    var prItem = prMonthly.FirstOrDefault(x => x.Month == m);
                    var poItem = poMonthly.FirstOrDefault(x => x.Month == m);
                    var airItem = airMonthly.FirstOrDefault(x => x.Month == m);
                    var risItem = risMonthly.FirstOrDefault(x => x.Month == m);

                    var vol = new DashboardMonthlyVolumeVM
                    {
                        Month = m,
                        MonthName = monthNames[m - 1],
                        PrCount = prItem != null ? prItem.Count : 0,
                        PoCount = poItem != null ? poItem.Count : 0,
                        AirCount = airItem != null ? airItem.Count : 0,
                        RisCount = risItem != null ? risItem.Count : 0,
                        PoAmount = poItem != null ? poItem.Amount : 0m
                    };

                    monthlyVolumes.Add(vol);
                    monthlyPoCommitments.Add(vol);
                }

                admin.MonthlyProcurementActivity = monthlyVolumes;
                admin.MonthlyPoCommitment = monthlyPoCommitments;

                // E. Status Distribution (Horizontal Stacked Bar Data)
                admin.StatusDistribution = new List<DashboardStatusDistributionVM>
                {
                    new DashboardStatusDistributionVM { Module = "PR", Status = "Draft", Count = summary.PrDraft, Color = "#94a3b8" },
                    new DashboardStatusDistributionVM { Module = "PR", Status = "Submitted", Count = summary.PrSubmitted, Color = "#0284c7" },
                    new DashboardStatusDistributionVM { Module = "PR", Status = "Returned", Count = summary.PrReturned, Color = "#dc2626" },
                    new DashboardStatusDistributionVM { Module = "PR", Status = "Posted", Count = summary.PrPosted, Color = "#16a34a" },

                    new DashboardStatusDistributionVM { Module = "PO", Status = "Draft", Count = summary.PoDraft, Color = "#94a3b8" },
                    new DashboardStatusDistributionVM { Module = "PO", Status = "Posted", Count = summary.PoPosted, Color = "#002576" },

                    new DashboardStatusDistributionVM { Module = "AIR", Status = "Awaiting Inspection", Count = summary.AirPendingInspection, Color = "#f59e0b" },
                    new DashboardStatusDistributionVM { Module = "AIR", Status = "Inspected", Count = summary.AirInspected, Color = "#16a34a" },

                    new DashboardStatusDistributionVM { Module = "RIS", Status = "Pending Approval", Count = summary.RisPendingApproval, Color = "#f59e0b" },
                    new DashboardStatusDistributionVM { Module = "RIS", Status = "Pending Issuance", Count = summary.RisPendingIssuance, Color = "#0284c7" },
                    new DashboardStatusDistributionVM { Module = "RIS", Status = "Posted", Count = summary.RisPosted, Color = "#16a34a" }
                };

                // F. Operational Backlog (Sorted highest to lowest)
                var backlogList = new List<DashboardBacklogItemVM>
                {
                    new DashboardBacklogItemVM { Category = "PR Returned for Revision", Count = summary.PrReturned, Module = "PR", ActionUrl = "/Requests", UrgencyLevel = "danger" },
                    new DashboardBacklogItemVM { Category = "Awaiting Technical Inspection", Count = summary.AirPendingInspection, Module = "AIR", ActionUrl = "/AIRs/Inspection", UrgencyLevel = "warning" },
                    new DashboardBacklogItemVM { Category = "RIS Pending Approval", Count = summary.RisPendingApproval, Module = "RIS", ActionUrl = "/RIS", UrgencyLevel = "warning" },
                    new DashboardBacklogItemVM { Category = "RIS Pending Stock Issuance", Count = summary.RisPendingIssuance, Module = "RIS", ActionUrl = "/RIS", UrgencyLevel = "info" },
                    new DashboardBacklogItemVM { Category = "Draft Accountability Receipts", Count = draftPars.Count, Module = "PAR", ActionUrl = "/ParSet", UrgencyLevel = "info" }
                };

                admin.OperationalBacklog = backlogList.OrderByDescending(b => b.Count).ToList();

                // G. Workflow Health Pipelines
                admin.WorkflowHealth = new List<DashboardWorkflowHealthVM>
                {
                    new DashboardWorkflowHealthVM
                    {
                        ProcessTitle = "Purchase Request Pipeline",
                        IconClass = "k-i-file-add",
                        Stages = new List<DashboardWorkflowHealthStageVM>
                        {
                            new DashboardWorkflowHealthStageVM { StageName = "Draft", Count = summary.PrDraft, StatusClass = "default", Url = "/Requests" },
                            new DashboardWorkflowHealthStageVM { StageName = "Submitted", Count = summary.PrSubmitted, StatusClass = "active", Url = "/Requests" },
                            new DashboardWorkflowHealthStageVM { StageName = "Returned", Count = summary.PrReturned, StatusClass = "alert", Url = "/Requests" },
                            new DashboardWorkflowHealthStageVM { StageName = "Posted", Count = summary.PrPosted, StatusClass = "success", Url = "/Requests" }
                        }
                    },
                    new DashboardWorkflowHealthVM
                    {
                        ProcessTitle = "Inspection & Acceptance (AIR)",
                        IconClass = "k-i-search",
                        Stages = new List<DashboardWorkflowHealthStageVM>
                        {
                            new DashboardWorkflowHealthStageVM { StageName = "Awaiting Inspection", Count = summary.AirPendingInspection, StatusClass = "alert", Url = "/AIRs/Inspection" },
                            new DashboardWorkflowHealthStageVM { StageName = "Inspected", Count = summary.AirInspected, StatusClass = "active", Url = "/AIRs/Inspection" },
                            new DashboardWorkflowHealthStageVM { StageName = "Completed", Count = summary.AirTotal - summary.AirPendingInspection, StatusClass = "success", Url = "/AIRs/Inspection" }
                        }
                    },
                    new DashboardWorkflowHealthVM
                    {
                        ProcessTitle = "Property Accountability (PAR / ICS)",
                        IconClass = "k-i-check-circle",
                        Stages = new List<DashboardWorkflowHealthStageVM>
                        {
                            new DashboardWorkflowHealthStageVM { StageName = "Active PARs", Count = summary.ParCount, StatusClass = "active", Url = "/ParSet" },
                            new DashboardWorkflowHealthStageVM { StageName = "Active ICSs", Count = summary.IcsCount, StatusClass = "active", Url = "/IcsSet" },
                            new DashboardWorkflowHealthStageVM { StageName = "Total Issued", Count = summary.ParCount + summary.IcsCount, StatusClass = "success", Url = "/CustodianReport" }
                        }
                    }
                };

                // H. Recent System Activity
                var histories = await _db.DocumentStatusHistories.AsNoTracking()
                    .OrderByDescending(h => h.ChangedDt)
                    .Take(12)
                    .ToListAsync();

                var now = DateTime.Now;
                admin.RecentActivities = histories.Select(h => new DashboardActivityVM
                {
                    Id = h.Id,
                    DocumentType = h.DocumentType,
                    DocumentNo = !string.IsNullOrWhiteSpace(h.DocumentNo) ? h.DocumentNo : "Document",
                    Action = !string.IsNullOrWhiteSpace(h.Action) ? h.Action : h.ToStatus,
                    User = h.ChangedBy ?? "System",
                    Department = "",
                    Timestamp = h.ChangedDt,
                    TimeAgo = FormatRelativeTime(h.ChangedDt, now),
                    Remarks = h.Remarks,
                    Url = ResolveDocumentUrl(h.DocumentType)
                }).ToList();

                // I. Quick Modules
                admin.QuickModules = GetOperationalModuleCards();

                // J. Administration Tools
                admin.AdminTools = GetAdminToolCards();
            }
            catch
            {
                // Graceful fallback
            }
        }

        #endregion

        #region Non-Admin Personal Workbench Builder

        private async Task BuildUserDashboardAsync(DashboardViewModel model, string userId, string userName, string userFullName, List<Guid> userDeptIds)
        {
            var userDash = model.UserDashboard;
            int fy = model.FiscalYear > 0 ? model.FiscalYear : DateTime.Now.Year;
            userDash.SearchPlaceholder = "Search PR, PO, AIR, RIS, Property No., PAR, ICS, item...";

            try
            {
                // A. Quick Actions for non-admin
                userDash.QuickActions = new List<DashboardQuickActionVM>
                {
                    new DashboardQuickActionVM
                    {
                        Title = "New Purchase Request",
                        Description = "Browse PPMP catalog and create a requisition",
                        Url = "/Procurement/Annual",
                        IconClass = "k-i-plus",
                        ButtonText = "+ New PR",
                        Category = "Procurement",
                        IsPrimary = true
                    },
                    new DashboardQuickActionVM
                    {
                        Title = "Requisition & Issuance",
                        Description = "Create or track supply issuances (RIS)",
                        Url = "/RIS",
                        IconClass = "k-i-file-add",
                        ButtonText = "+ New RIS",
                        Category = "Supplies",
                        IsPrimary = false
                    },
                    new DashboardQuickActionVM
                    {
                        Title = "Search Property",
                        Description = "Locate PPE property cards and accountable units",
                        Url = "/PropertyCard",
                        IconClass = "k-i-search",
                        ButtonText = "Search Property",
                        Category = "Property",
                        IsPrimary = false
                    },
                    new DashboardQuickActionVM
                    {
                        Title = "Search Stock",
                        Description = "Review consumable stock card balances",
                        Url = "/StockCard",
                        IconClass = "k-i-table",
                        ButtonText = "Search Stock",
                        Category = "Supplies",
                        IsPrimary = false
                    },
                    new DashboardQuickActionVM
                    {
                        Title = "My PAR / ICS",
                        Description = "Review property acknowledgments assigned to you",
                        Url = "/IcsSet",
                        IconClass = "k-i-user",
                        ButtonText = "My PAR / ICS",
                        Category = "Accountability",
                        IsPrimary = false
                    },
                    new DashboardQuickActionVM
                    {
                        Title = "Custodian Records",
                        Description = "Access employee property receipts & clearance",
                        Url = "/CustodianReport",
                        IconClass = "k-i-print",
                        ButtonText = "Custodian Records",
                        Category = "Accountability",
                        IsPrimary = false
                    }
                };

                // B. Needs Your Attention (Filtered strictly to this user)
                var attention = new List<DashboardAttentionItemVM>();

                var returnedHistories = await _db.DocumentStatusHistories.AsNoTracking()
                    .Where(h => h.DocumentType == DocumentTypes.PurchaseRequest)
                    .GroupBy(h => h.DocumentId)
                    .Select(g => g.OrderByDescending(x => x.ChangedDt).FirstOrDefault())
                    .Where(h => h != null && h.ToStatus == PrStatuses.Returned)
                    .Take(10)
                    .ToListAsync();

                int userReturnedCount = 0;
                if (returnedHistories.Any())
                {
                    var returnedIds = returnedHistories.Select(h => h.DocumentId).ToList();
                    var reqQuery = _db.Requests.AsNoTracking().Where(r => returnedIds.Contains(r.Id));

                    if (userDeptIds.Any())
                    {
                        reqQuery = reqQuery.Where(r => r.InsertedBy == userName || (r.DeptId.HasValue && userDeptIds.Contains(r.DeptId.Value)));
                    }
                    else
                    {
                        reqQuery = reqQuery.Where(r => r.InsertedBy == userName);
                    }

                    var userReturnedReqs = await reqQuery.ToListAsync();
                    userReturnedCount = userReturnedReqs.Count;

                    foreach (var hist in returnedHistories)
                    {
                        var req = userReturnedReqs.FirstOrDefault(r => r.Id == hist.DocumentId);
                        if (req != null)
                        {
                            attention.Add(new DashboardAttentionItemVM
                            {
                                Id = req.Id,
                                Reference = req.PrNo ?? req.CtrlNo ?? "PR",
                                Module = "Purchase Request",
                                Department = req.Department ?? "",
                                ShortDescription = req.Purpose,
                                Status = "Returned for Revision",
                                Reason = !string.IsNullOrWhiteSpace(hist.Remarks) ? hist.Remarks : "Returned by reviewer; action required.",
                                ActionUrl = "/Requests/Index",
                                ActionLabel = "Edit PR",
                                UrgencyLevel = "danger",
                                Date = hist.ChangedDt
                            });
                        }
                    }
                }

                userDash.AttentionItems = attention;

                // C. Continue Your Work (In-progress drafts for this user)
                var continueWork = new List<DashboardWorkItemVM>();

                var draftReqs = await _db.Requests.AsNoTracking()
                    .Where(r => string.IsNullOrEmpty(r.SubmittedBy) && string.IsNullOrEmpty(r.PostedBy))
                    .Where(r => r.InsertedBy == userName || (r.DeptId.HasValue && userDeptIds.Contains(r.DeptId.Value)))
                    .OrderByDescending(r => r.InsertedDt)
                    .Take(4)
                    .ToListAsync();

                foreach (var draft in draftReqs)
                {
                    continueWork.Add(new DashboardWorkItemVM
                    {
                        Id = draft.Id,
                        Reference = draft.PrNo ?? draft.CtrlNo ?? "Draft PR",
                        Module = "Purchase Request",
                        Status = "Draft",
                        Description = draft.Purpose ?? "Preparation in progress",
                        LastWorkedText = draft.InsertedDt.HasValue ? draft.InsertedDt.Value.ToString("MMM d, yyyy") : "Recent",
                        ActionUrl = "/Requests/Index",
                        LastWorkedDate = draft.InsertedDt,
                        Stage = "In Preparation"
                    });
                }

                // Add draft RIS if any
                var draftRisses = await _db.RISses.AsNoTracking()
                    .Where(r => string.IsNullOrEmpty(r.PostedBy) && !r.PostedDt.HasValue)
                    .Where(r => r.InsertedBy == userName)
                    .OrderByDescending(r => r.InsertedDt)
                    .Take(2)
                    .ToListAsync();

                foreach (var ris in draftRisses)
                {
                    continueWork.Add(new DashboardWorkItemVM
                    {
                        Id = ris.Id,
                        Reference = ris.RisNo ?? ris.CtrlNo ?? "Draft RIS",
                        Module = "Requisition & Issuance",
                        Status = "Draft",
                        Description = "Requisition slip in progress",
                        LastWorkedText = ris.InsertedDt.HasValue ? ris.InsertedDt.Value.ToString("MMM d, yyyy") : "Recent",
                        ActionUrl = "/RIS",
                        LastWorkedDate = ris.InsertedDt,
                        Stage = "Draft Requisition"
                    });
                }

                userDash.ContinueWorkItems = continueWork;

                // D. Personal KPIs
                // 1. My PRs
                var userPrs = await _db.Requests.AsNoTracking()
                    .Where(r => r.InsertedBy == userName || (r.DeptId.HasValue && userDeptIds.Contains(r.DeptId.Value)))
                    .Select(r => new { r.Id, r.SubmittedBy, r.PostedBy, r.PostedDt, r.InsertedDt })
                    .ToListAsync();

                int userOpenPrs = userPrs.Count(r => string.IsNullOrEmpty(r.PostedBy) && !r.PostedDt.HasValue);
                int userDraftPrs = userPrs.Count(r => string.IsNullOrEmpty(r.SubmittedBy) && string.IsNullOrEmpty(r.PostedBy));
                int userInProgressPrs = userPrs.Count(r => !string.IsNullOrEmpty(r.SubmittedBy) && string.IsNullOrEmpty(r.PostedBy));
                int userCompletedPrs = userPrs.Count(r => (!string.IsNullOrEmpty(r.PostedBy) || r.PostedDt.HasValue) &&
                    ((r.PostedDt.HasValue && r.PostedDt.Value.Year == fy) || (r.InsertedDt.HasValue && r.InsertedDt.Value.Year == fy)));

                // 2. My RISs
                var userRises = await _db.RISses.AsNoTracking()
                    .Where(r => r.InsertedBy == userName || r.RequestedBy == userName)
                    .Select(r => new { r.Id, r.PostedBy, r.PostedDt, r.InsertedDt })
                    .ToListAsync();

                int userOpenRises = userRises.Count(r => string.IsNullOrEmpty(r.PostedBy) && !r.PostedDt.HasValue);
                int userCompletedRises = userRises.Count(r => (!string.IsNullOrEmpty(r.PostedBy) || r.PostedDt.HasValue) &&
                    ((r.PostedDt.HasValue && r.PostedDt.Value.Year == fy) || (r.InsertedDt.HasValue && r.InsertedDt.Value.Year == fy)));

                // 3. User Accountability (PAR / ICS)
                var nameMatches = new List<string> { userName };
                if (!string.IsNullOrWhiteSpace(userFullName))
                {
                    nameMatches.Add(userFullName);
                }

                int userParCount = await _db.PARs.AsNoTracking()
                    .CountAsync(p => nameMatches.Contains(p.ReceivedBy) || nameMatches.Contains(p.InsertedBy));

                int userIcsCount = await _db.IcsPars.AsNoTracking()
                    .CountAsync(i => nameMatches.Contains(i.ReceivedBy) || nameMatches.Contains(i.InsertedBy));

                userDash.PersonalKpis = new UserPersonalKpiVM
                {
                    MyOpenWork = userOpenPrs + userOpenRises,
                    MyDrafts = userDraftPrs + draftRisses.Count,
                    MyInProgress = userInProgressPrs,
                    NeedsMyAttention = userReturnedCount,
                    MyAccountabilityCount = userParCount + userIcsCount,
                    MyParCount = userParCount,
                    MyIcsCount = userIcsCount,
                    CompletedThisFiscalYear = userCompletedPrs + userCompletedRises
                };

                // E. My Workload Distribution (Personal)
                userDash.MyWorkload = new List<UserWorkloadMetricVM>
                {
                    new UserWorkloadMetricVM { Module = "Purchase Requests", Count = userOpenPrs, Color = "#002576", IconClass = "k-i-file-add", Url = "/Requests" },
                    new UserWorkloadMetricVM { Module = "Requisitions (RIS)", Count = userOpenRises, Color = "#0284c7", IconClass = "k-i-check-circle", Url = "/RIS" },
                    new UserWorkloadMetricVM { Module = "PAR Items", Count = userParCount, Color = "#16a34a", IconClass = "k-i-user", Url = "/ParSet" },
                    new UserWorkloadMetricVM { Module = "ICS Items", Count = userIcsCount, Color = "#f59e0b", IconClass = "k-i-folder", Url = "/IcsSet" }
                };

                // F. My Monthly Activity (Personal for this FY)
                var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
                var userActivityMonths = await _db.DocumentStatusHistories.AsNoTracking()
                    .Where(h => h.ChangedBy == userName && h.ChangedDt.Year == fy)
                    .GroupBy(h => h.ChangedDt.Month)
                    .Select(g => new { Month = g.Key, Count = g.Count() })
                    .ToListAsync();

                var monthlyActivities = new List<UserMonthlyActivityVM>();
                for (int m = 1; m <= 12; m++)
                {
                    var match = userActivityMonths.FirstOrDefault(x => x.Month == m);
                    monthlyActivities.Add(new UserMonthlyActivityVM
                    {
                        Month = m,
                        MonthName = monthNames[m - 1],
                        CompletedCount = match != null ? match.Count : 0
                    });
                }
                userDash.MyMonthlyActivity = monthlyActivities;

                // G. Module Shortcuts
                userDash.ModuleShortcuts = GetOperationalModuleCards();

                // H. Recent Work
                var userHistories = await _db.DocumentStatusHistories.AsNoTracking()
                    .Where(h => h.ChangedBy == userName)
                    .OrderByDescending(h => h.ChangedDt)
                    .Take(6)
                    .ToListAsync();

                var now = DateTime.Now;
                userDash.RecentWorkItems = userHistories.Select(h => new DashboardRecentItemVM
                {
                    Id = h.Id,
                    Title = (!string.IsNullOrWhiteSpace(h.DocumentNo) ? h.DocumentNo : "Document"),
                    Subtitle = (!string.IsNullOrWhiteSpace(h.Action) ? h.Action : h.ToStatus) + (h.Remarks != null ? " - " + h.Remarks : ""),
                    Module = h.DocumentType ?? "Transaction",
                    Url = ResolveDocumentUrl(h.DocumentType),
                    TimeAgo = FormatRelativeTime(h.ChangedDt, now),
                    Timestamp = h.ChangedDt
                }).ToList();
            }
            catch
            {
                // Graceful fallback
            }
        }

        #endregion

        #region Helpers & Shared Data

        private static List<DashboardModuleCardVM> GetOperationalModuleCards()
        {
            return new List<DashboardModuleCardVM>
            {
                new DashboardModuleCardVM { Title = "Purchase Request", ModuleName = "PR", Description = "Create and track procurement requests", Url = "/Requests", IconClass = "k-i-file-add", Category = "Procurement" },
                new DashboardModuleCardVM { Title = "PPMP Catalog", ModuleName = "PPMP", Description = "Annual procurement plans and cart", Url = "/Procurement/Annual", IconClass = "k-i-cart", Category = "Procurement" },
                new DashboardModuleCardVM { Title = "Purchase Order", ModuleName = "PO", Description = "Review orders and supplier deliveries", Url = "/PurchaseOrder", IconClass = "k-i-file-txt", Category = "Procurement" },
                new DashboardModuleCardVM { Title = "Inspection & Acceptance", ModuleName = "AIR", Description = "Property delivery receipts and inspection", Url = "/AIRs/Inspection", IconClass = "k-i-search", Category = "Property" },
                new DashboardModuleCardVM { Title = "Requisition & Issuance", ModuleName = "RIS", Description = "Issue stock supplies to departments", Url = "/RIS", IconClass = "k-i-check-circle", Category = "Supplies" },
                new DashboardModuleCardVM { Title = "Property Cards", ModuleName = "PPE", Description = "Capital asset ledger and history", Url = "/PropertyCard", IconClass = "k-i-grid", Category = "Property" },
                new DashboardModuleCardVM { Title = "Stock Cards", ModuleName = "Stock", Description = "Consumable supply balances and bin cards", Url = "/StockCard", IconClass = "k-i-table", Category = "Supplies" },
                new DashboardModuleCardVM { Title = "Property Acknowledgment", ModuleName = "PAR", Description = "Capital property issuance receipts", Url = "/ParSet", IconClass = "k-i-user", Category = "Accountability" },
                new DashboardModuleCardVM { Title = "Inventory Custodian", ModuleName = "ICS", Description = "Semi-expendable property issuance", Url = "/IcsSet", IconClass = "k-i-folder", Category = "Accountability" },
                new DashboardModuleCardVM { Title = "Property Transfers", ModuleName = "Transfers", Description = "Inter-office accountability relocations", Url = "/PoTransfer", IconClass = "k-i-arrow-end", Category = "Accountability" },
                new DashboardModuleCardVM { Title = "Custodian Reports", ModuleName = "Reports", Description = "Individual employee property records", Url = "/CustodianReport", IconClass = "k-i-print", Category = "Reports" }
            };
        }

        private static List<DashboardModuleCardVM> GetAdminToolCards()
        {
            return new List<DashboardModuleCardVM>
            {
                new DashboardModuleCardVM { Title = "User Management", ModuleName = "Users", Description = "System user accounts and offices", Url = "/Users", IconClass = "k-i-user", Category = "Admin" },
                new DashboardModuleCardVM { Title = "Roles & Permissions", ModuleName = "Roles", Description = "Access control and role assignments", Url = "/Roles", IconClass = "k-i-lock", Category = "Admin" },
                new DashboardModuleCardVM { Title = "Item Code Management", ModuleName = "Items", Description = "Item classifications and item codes", Url = "/Items", IconClass = "k-i-list-unordered", Category = "Admin" },
                new DashboardModuleCardVM { Title = "Code Maintenance", ModuleName = "Codes", Description = "System lookup tables and constants", Url = "/Codes", IconClass = "k-i-gear", Category = "Admin" },
                new DashboardModuleCardVM { Title = "PPMP Accounting Code", ModuleName = "Accounting", Description = "GSO to Accounting item group mapping", Url = "/PpmpAcctgCode", IconClass = "k-i-connector", Category = "Admin" },
                new DashboardModuleCardVM { Title = "Audit Trail", ModuleName = "Audit", Description = "System mutation and transaction log", Url = "/AuditTrail", IconClass = "k-i-clock", Category = "Admin" },
                new DashboardModuleCardVM { Title = "System Reports", ModuleName = "Reports", Description = "Annual inventory & RPCI generation", Url = "/Report", IconClass = "k-i-chart-line", Category = "Admin" }
            };
        }

        private static string ResolveDocumentUrl(string documentType)
        {
            if (string.IsNullOrEmpty(documentType)) return "/";
            var dt = documentType.ToUpperInvariant();
            if (dt.Contains("REQUEST") || dt == "PR") return "/Requests";
            if (dt.Contains("ORDER") || dt == "PO") return "/PurchaseOrder";
            if (dt.Contains("AIR") || dt.Contains("INSPECTION")) return "/AIRs/Inspection";
            if (dt.Contains("RIS") || dt.Contains("REQUISITION")) return "/RIS";
            if (dt.Contains("PAR")) return "/ParSet";
            if (dt.Contains("ICS")) return "/IcsSet";
            if (dt.Contains("PROPERTY")) return "/PropertyCard";
            if (dt.Contains("STOCK")) return "/StockCard";
            return "/";
        }

        private static string FormatRelativeTime(DateTime dt, DateTime now)
        {
            var span = now - dt;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return string.Format("{0}m ago", (int)span.TotalMinutes);
            if (span.TotalHours < 24) return string.Format("{0}h ago", (int)span.TotalHours);
            if (span.TotalDays < 7) return string.Format("{0}d ago", (int)span.TotalDays);
            return dt.ToString("MMM d");
        }

        private void PopulateLegacyProperties(DashboardViewModel model, bool isAdmin, List<Guid> userDeptIds, string userId, string userName)
        {
            try
            {
                if (isAdmin)
                {
                    var s = model.AdminDashboard.OperationalSummary;
                    model.KpiSummary = new DashboardKpiSummaryViewModel
                    {
                        PrTotal = s.PrTotal,
                        PrSubmitted = s.PrSubmitted,
                        PrReturned = s.PrReturned,
                        PrPosted = s.PrPosted,
                        PrDraft = s.PrDraft,
                        PoTotal = s.PoTotal,
                        PoPosted = s.PoPosted,
                        PoDraft = s.PoDraft,
                        PoCommittedAmount = s.PoCommittedAmount,
                        AirTotal = s.AirTotal,
                        AirPendingInspection = s.AirPendingInspection,
                        AirInspected = s.AirInspected,
                        RisTotal = s.RisTotal,
                        RisPendingApproval = s.RisPendingApproval,
                        RisPendingIssuance = s.RisPendingIssuance,
                        RisPosted = s.RisPosted,
                        StockCardCount = s.StockCardCount,
                        PropertyCardCount = s.PropertyCardCount,
                        ParCount = s.ParCount,
                        IcsCount = s.IcsCount
                    };
                }
            }
            catch
            {
                // Graceful fallback
            }
        }

        #endregion
    }
}
