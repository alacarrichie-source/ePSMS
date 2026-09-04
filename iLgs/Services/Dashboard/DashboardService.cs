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

            // 1. Determine User Role and Assigned Department(s)
            bool isAdmin = false;
            if (!string.IsNullOrEmpty(userId))
            {
                isAdmin = await _userService.IsAdminAsync(userId);
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

            // Get authorized departments for non-admin filtering
            var userDeptIds = new List<Guid>();
            if (!isAdmin && !string.IsNullOrEmpty(userId))
            {
                var userDepts = await _codextnService.GetUserDepartmentsAsync(userId);
                userDeptIds = await userDepts.Select(d => d.Id).ToListAsync();
            }

            // 2. Aggregate Key Performance Indicators (KPIs)
            await PopulateKpiMetricsAsync(model, isAdmin, userDeptIds);

            // 3. Populate Action Required Backlog
            await PopulateActionRequiredItemsAsync(model, isAdmin, userDeptIds, userId, userName);

            // 4. Populate Document Workflow Pipeline
            await PopulateWorkflowPipelineAsync(model);

            // 5. Populate Recent Activities Feed
            await PopulateRecentActivitiesAsync(model);

            // 6. Populate Quick Actions
            PopulateQuickActions(model, isAdmin);

            return model;
        }

        private async Task PopulateKpiMetricsAsync(DashboardViewModel model, bool isAdmin, List<Guid> userDeptIds)
        {
            try
            {
                // Purchase Requests
                IQueryable<Request> reqQuery = _db.Requests.AsNoTracking();
                if (!isAdmin && userDeptIds.Any())
                {
                    reqQuery = reqQuery.Where(r => r.DeptId.HasValue && userDeptIds.Contains(r.DeptId.Value));
                }

                var allRequests = await reqQuery
                    .Select(r => new { r.Id, r.SubmittedBy, r.PostedBy, r.PostedDt })
                    .ToListAsync();

                var reqIds = allRequests.Select(r => r.Id).ToList();

                // Fetch latest PR histories for Returned status identification
                var returnedPrIds = new HashSet<Guid>();
                if (reqIds.Any())
                {
                    var prHistories = await _db.DocumentStatusHistories.AsNoTracking()
                        .Where(h => h.DocumentType == DocumentTypes.PurchaseRequest && reqIds.Contains(h.DocumentId))
                        .GroupBy(h => h.DocumentId)
                        .Select(g => g.OrderByDescending(x => x.ChangedDt).FirstOrDefault())
                        .ToListAsync();

                    foreach (var h in prHistories.Where(h => h != null && h.ToStatus == PrStatuses.Returned))
                    {
                        returnedPrIds.Add(h.DocumentId);
                    }
                }

                model.KpiSummary.PrTotal = allRequests.Count;
                model.KpiSummary.PrPosted = allRequests.Count(r => r.PostedDt.HasValue || !string.IsNullOrEmpty(r.PostedBy));
                model.KpiSummary.PrReturned = returnedPrIds.Count;
                model.KpiSummary.PrSubmitted = allRequests.Count(r => !returnedPrIds.Contains(r.Id) &&
                                                                      !string.IsNullOrEmpty(r.SubmittedBy) &&
                                                                      !r.PostedDt.HasValue &&
                                                                      string.IsNullOrEmpty(r.PostedBy));
                model.KpiSummary.PrDraft = allRequests.Count(r => !returnedPrIds.Contains(r.Id) &&
                                                                   string.IsNullOrEmpty(r.SubmittedBy) &&
                                                                   !r.PostedDt.HasValue &&
                                                                   string.IsNullOrEmpty(r.PostedBy));

                // Purchase Orders
                var orders = await _db.Orders.AsNoTracking()
                    .Select(o => new { o.Id, o.PostedBy, o.PostedDt })
                    .ToListAsync();

                model.KpiSummary.PoTotal = orders.Count;
                model.KpiSummary.PoPosted = orders.Count(o => o.PostedDt.HasValue || !string.IsNullOrEmpty(o.PostedBy));
                model.KpiSummary.PoDraft = orders.Count(o => !o.PostedDt.HasValue && string.IsNullOrEmpty(o.PostedBy));

                // Committed Amount
                var totalAmount = await _db.OrderItems.AsNoTracking()
                    .Where(i => i.Amount.HasValue)
                    .SumAsync(i => (decimal?)i.Amount) ?? 0m;
                model.KpiSummary.PoCommittedAmount = totalAmount;

                // AIR (Inspection & Acceptance)
                var airs = await _db.AIRs.AsNoTracking()
                    .Select(a => new { a.Id, a.IsInspected, a.PostedBy })
                    .ToListAsync();

                model.KpiSummary.AirTotal = airs.Count;
                model.KpiSummary.AirPendingInspection = airs.Count(a => a.IsInspected != true);
                model.KpiSummary.AirInspected = airs.Count(a => a.IsInspected == true);

                // RIS (Requisitions)
                var risses = await _db.RISses.AsNoTracking()
                    .Select(r => new { r.Id, r.ApprovedBy, r.IssuedBy, r.PostedBy, r.PostedDt })
                    .ToListAsync();

                model.KpiSummary.RisTotal = risses.Count;
                model.KpiSummary.RisPendingApproval = risses.Count(r => string.IsNullOrEmpty(r.ApprovedBy));
                model.KpiSummary.RisPendingIssuance = risses.Count(r => !string.IsNullOrEmpty(r.ApprovedBy) && string.IsNullOrEmpty(r.IssuedBy));
                model.KpiSummary.RisPosted = risses.Count(r => !string.IsNullOrEmpty(r.PostedBy) || r.PostedDt.HasValue);

                // Inventory & Cards
                model.KpiSummary.StockCardCount = await _db.PsCards.AsNoTracking().CountAsync();
                model.KpiSummary.PropertyCardCount = await _db.PropertyCardItems.AsNoTracking().CountAsync();
                model.KpiSummary.ParCount = await _db.PARs.AsNoTracking().CountAsync();
                model.KpiSummary.IcsCount = await _db.IcsPars.AsNoTracking().CountAsync();
            }
            catch
            {
                // Fallback graceful zero counts on error
            }
        }

        private async Task PopulateActionRequiredItemsAsync(DashboardViewModel model, bool isAdmin, List<Guid> userDeptIds, string userId, string userName)
        {
            try
            {
                var actionItems = new List<DashboardActionItemViewModel>();

                // 1. Returned PRs (Always top priority for departments & requesters)
                var returnedHistories = await _db.DocumentStatusHistories.AsNoTracking()
                    .Where(h => h.DocumentType == DocumentTypes.PurchaseRequest)
                    .GroupBy(h => h.DocumentId)
                    .Select(g => g.OrderByDescending(x => x.ChangedDt).FirstOrDefault())
                    .Where(h => h != null && h.ToStatus == PrStatuses.Returned)
                    .Take(5)
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
                            actionItems.Add(new DashboardActionItemViewModel
                            {
                                Id = req.Id,
                                DocumentType = "PR",
                                DocumentNo = req.PrNo ?? req.CtrlNo ?? "Draft PR",
                                Department = req.Department,
                                Description = req.Purpose,
                                Remarks = !string.IsNullOrWhiteSpace(hist.Remarks)
                                    ? hist.Remarks
                                    : "Returned by reviewer for revision.",
                                UrgencyLevel = "danger",
                                Status = "Returned",
                                ActionUrl = "/Requests/Index",
                                ActionLabel = "Revise PR",
                                Date = hist.ChangedDt
                            });
                        }
                    }
                }

                // 2. Submitted PRs Awaiting Review (Priority for GSO / Admins)
                if (isAdmin)
                {
                    var submittedReqs = await _db.Requests.AsNoTracking()
                        .Where(r => !string.IsNullOrEmpty(r.SubmittedBy) && string.IsNullOrEmpty(r.PostedBy) && !r.PostedDt.HasValue)
                        .OrderByDescending(r => r.SubmittedDt ?? r.InsertedDt)
                        .Take(5)
                        .ToListAsync();

                    foreach (var req in submittedReqs)
                    {
                        if (!actionItems.Any(a => a.Id == req.Id))
                        {
                            actionItems.Add(new DashboardActionItemViewModel
                            {
                                Id = req.Id,
                                DocumentType = "PR",
                                DocumentNo = req.PrNo ?? req.CtrlNo ?? "Pending Review",
                                Department = req.Department,
                                Description = req.Purpose,
                                Remarks = "Submitted by " + (req.SubmittedBy ?? "department") + " - awaiting review & posting.",
                                UrgencyLevel = "warning",
                                Status = "For Review",
                                ActionUrl = "/Requests/Index",
                                ActionLabel = "Review PR",
                                Date = req.SubmittedDt ?? req.InsertedDt
                            });
                        }
                    }
                }

                // 3. AIRs Awaiting Inspection (Priority for Inspection Officers / GSO)
                var pendingAirs = await _db.AIRs.AsNoTracking()
                    .Include(a => a.Order)
                    .Where(a => a.IsInspected != true)
                    .OrderByDescending(a => a.AIRDate ?? a.InsertedDt)
                    .Take(4)
                    .ToListAsync();

                foreach (var air in pendingAirs)
                {
                    actionItems.Add(new DashboardActionItemViewModel
                    {
                        Id = air.Id,
                        DocumentType = "AIR",
                        DocumentNo = air.AIRNo ?? air.CtrlNo ?? "Pending AIR",
                        Department = air.Order != null ? air.Order.Department : "General Services",
                        Description = "PO: " + (air.Order != null ? air.Order.PoNo : "N/A") + (air.InvoiceNo != null ? " | Invoice: " + air.InvoiceNo : ""),
                        Remarks = "Deliveries received, awaiting technical property inspection sign-off.",
                        UrgencyLevel = "info",
                        Status = "Pending Inspection",
                        ActionUrl = "/AIRs/Inspection",
                        ActionLabel = "Inspect",
                        Date = air.AIRDate ?? air.InsertedDt
                    });
                }

                // 4. In-progress Draft PRs for department users (if few items)
                if (!isAdmin && actionItems.Count < 5)
                {
                    var draftReqs = await _db.Requests.AsNoTracking()
                        .Where(r => string.IsNullOrEmpty(r.SubmittedBy) && string.IsNullOrEmpty(r.PostedBy))
                        .Where(r => r.InsertedBy == userName || (r.DeptId.HasValue && userDeptIds.Contains(r.DeptId.Value)))
                        .OrderByDescending(r => r.InsertedDt)
                        .Take(3)
                        .ToListAsync();

                    foreach (var draft in draftReqs)
                    {
                        if (!actionItems.Any(a => a.Id == draft.Id))
                        {
                            actionItems.Add(new DashboardActionItemViewModel
                            {
                                Id = draft.Id,
                                DocumentType = "PR",
                                DocumentNo = draft.PrNo ?? draft.CtrlNo ?? "Draft",
                                Department = draft.Department,
                                Description = draft.Purpose,
                                Remarks = "Draft PR in progress. Ready for item addition and submission.",
                                UrgencyLevel = "info",
                                Status = "Draft",
                                ActionUrl = "/Requests/Index",
                                ActionLabel = "Continue",
                                Date = draft.InsertedDt
                            });
                        }
                    }
                }

                model.ActionRequiredItems = actionItems.Take(6).ToList();
            }
            catch
            {
                // Graceful fallback
            }
        }

        private async Task PopulateWorkflowPipelineAsync(DashboardViewModel model)
        {
            try
            {
                int ppmpCount = await _db.PPMPItems.AsNoTracking().CountAsync();

                model.WorkflowPipeline = new List<DashboardWorkflowStepViewModel>
                {
                    new DashboardWorkflowStepViewModel
                    {
                        StepNumber = 1,
                        Title = "1. PPMP Catalog",
                        Count = ppmpCount,
                        Subtitle = "Approved Items",
                        Url = "/Procurement/Annual",
                        IconClass = "k-i-cart",
                        BadgeClass = "pipeline-ppmp"
                    },
                    new DashboardWorkflowStepViewModel
                    {
                        StepNumber = 2,
                        Title = "2. Purchase Request",
                        Count = model.KpiSummary.PrSubmitted,
                        Subtitle = "In Review / Evaluation",
                        Url = "/Requests",
                        IconClass = "k-i-file-add",
                        BadgeClass = "pipeline-pr"
                    },
                    new DashboardWorkflowStepViewModel
                    {
                        StepNumber = 3,
                        Title = "3. Purchase Order",
                        Count = model.KpiSummary.PoDraft + model.KpiSummary.PoPosted,
                        Subtitle = "Consolidated Orders",
                        Url = "/PurchaseOrder",
                        IconClass = "k-i-file-txt",
                        BadgeClass = "pipeline-po"
                    },
                    new DashboardWorkflowStepViewModel
                    {
                        StepNumber = 4,
                        Title = "4. AIR Inspection",
                        Count = model.KpiSummary.AirPendingInspection,
                        Subtitle = "Awaiting Inspection",
                        Url = "/AIRs/Inspection",
                        IconClass = "k-i-search",
                        BadgeClass = "pipeline-air"
                    },
                    new DashboardWorkflowStepViewModel
                    {
                        StepNumber = 5,
                        Title = "5. Property Issuance",
                        Count = model.KpiSummary.RisPendingIssuance,
                        Subtitle = "Ready for Issuance",
                        Url = "/RIS",
                        IconClass = "k-i-check-circle",
                        BadgeClass = "pipeline-ris"
                    }
                };
            }
            catch
            {
                // Graceful fallback
            }
        }

        private async Task PopulateRecentActivitiesAsync(DashboardViewModel model)
        {
            try
            {
                var histories = await _db.DocumentStatusHistories.AsNoTracking()
                    .OrderByDescending(h => h.ChangedDt)
                    .Take(8)
                    .ToListAsync();

                var activities = new List<DashboardActivityViewModel>();
                var now = DateTime.Now;

                foreach (var h in histories)
                {
                    string timeAgo = FormatRelativeTime(h.ChangedDt, now);
                    string url = "/Home";

                    switch (h.DocumentType)
                    {
                        case DocumentTypes.PurchaseRequest:
                            url = "/Requests";
                            break;
                        case DocumentTypes.PurchaseOrder:
                            url = "/PurchaseOrder";
                            break;
                        case DocumentTypes.AcceptanceInspectionReport:
                            url = "/AIRs/Inspection";
                            break;
                        case DocumentTypes.RequisitionIssuanceSlip:
                            url = "/RIS";
                            break;
                        case DocumentTypes.PropertyAcknowledgementReceipt:
                            url = "/ParSet";
                            break;
                        case DocumentTypes.InventoryCustodianSlip:
                            url = "/IcsSet";
                            break;
                    }

                    activities.Add(new DashboardActivityViewModel
                    {
                        Id = h.Id,
                        DocumentType = h.DocumentType,
                        DocumentNo = !string.IsNullOrWhiteSpace(h.DocumentNo) ? h.DocumentNo : "Document",
                        Action = !string.IsNullOrWhiteSpace(h.Action) ? h.Action : h.ToStatus,
                        User = h.ChangedBy,
                        Timestamp = h.ChangedDt,
                        TimeAgo = timeAgo,
                        Remarks = h.Remarks,
                        Url = url
                    });
                }

                model.RecentActivities = activities;
            }
            catch
            {
                // Graceful fallback
            }
        }

        private void PopulateQuickActions(DashboardViewModel model, bool isAdmin)
        {
            var actions = new List<DashboardQuickActionViewModel>
            {
                new DashboardQuickActionViewModel
                {
                    Title = "Create Purchase Request",
                    Description = "Browse approved PPMP catalog and add to cart",
                    Url = "/Procurement/Annual",
                    IconClass = "k-i-cart",
                    Category = "Procurement",
                    IsPrimary = true
                },
                new DashboardQuickActionViewModel
                {
                    Title = "Purchase Requests Directory",
                    Description = "View, track, and manage all Purchase Requests",
                    Url = "/Requests",
                    IconClass = "k-i-file-add",
                    Category = "Procurement",
                    IsPrimary = false
                }
            };

            if (isAdmin)
            {
                actions.Add(new DashboardQuickActionViewModel
                {
                    Title = "Create Consolidated PO",
                    Description = "Combine approved department PRs into a PO",
                    Url = "/PurchaseOrder/Create",
                    IconClass = "k-i-plus-circle",
                    Category = "Procurement",
                    IsPrimary = false
                });

                actions.Add(new DashboardQuickActionViewModel
                {
                    Title = "Purchase Orders Master",
                    Description = "Review active orders and supplier transmissions",
                    Url = "/PurchaseOrder",
                    IconClass = "k-i-file-txt",
                    Category = "Procurement",
                    IsPrimary = false
                });

                actions.Add(new DashboardQuickActionViewModel
                {
                    Title = "Inspection & Acceptance (AIR)",
                    Description = "Record received supplier deliveries and inspection",
                    Url = "/AIRs/Inspection",
                    IconClass = "k-i-search",
                    Category = "Property",
                    IsPrimary = false
                });
            }

            actions.Add(new DashboardQuickActionViewModel
            {
                Title = "Requisition & Issuance (RIS)",
                Description = "Issue available supplies from inventory to end-users",
                Url = "/RIS",
                IconClass = "k-i-check-circle",
                Category = "Property",
                IsPrimary = false
            });

            actions.Add(new DashboardQuickActionViewModel
            {
                Title = "Stock Cards Registry",
                Description = "Monitor consumable supply card balances",
                Url = "/StockCard",
                IconClass = "k-i-table",
                Category = "Inventory",
                IsPrimary = false
            });

            actions.Add(new DashboardQuickActionViewModel
            {
                Title = "Property Cards (PPE)",
                Description = "Track capital property accountability and ledger",
                Url = "/PropertyCard",
                IconClass = "k-i-grid",
                Category = "Inventory",
                IsPrimary = false
            });

            actions.Add(new DashboardQuickActionViewModel
            {
                Title = "Custodian Accountability Reports",
                Description = "Generate PAR / ICS accountability and RPCI summaries",
                Url = "/CustodianReport",
                IconClass = "k-i-print",
                Category = "Reports",
                IsPrimary = false
            });

            model.QuickActions = actions;
        }

        private static string FormatRelativeTime(DateTime timestamp, DateTime now)
        {
            var span = now - timestamp;
            if (span.TotalMinutes < 1)
                return "Just now";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 2)
                return "Yesterday";
            if (span.TotalDays < 7)
                return $"{(int)span.TotalDays}d ago";

            return timestamp.ToString("MMM d, yyyy");
        }
    }
}
