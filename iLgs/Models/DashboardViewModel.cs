using System;
using System.Collections.Generic;

namespace iLgs.Models
{
    public class DashboardViewModel
    {
        public DashboardViewModel()
        {
            KpiSummary = new DashboardKpiSummaryViewModel();
            ActionRequiredItems = new List<DashboardActionItemViewModel>();
            WorkflowPipeline = new List<DashboardWorkflowStepViewModel>();
            RecentActivities = new List<DashboardActivityViewModel>();
            QuickActions = new List<DashboardQuickActionViewModel>();
        }

        public string UserName { get; set; }
        public string UserFullName { get; set; }
        public string DepartmentName { get; set; }
        public bool IsAdmin { get; set; }
        public int FiscalYear { get; set; }
        public string CurrentDateText { get; set; }
        public int CartItemCount { get; set; }

        public DashboardKpiSummaryViewModel KpiSummary { get; set; }
        public List<DashboardActionItemViewModel> ActionRequiredItems { get; set; }
        public List<DashboardWorkflowStepViewModel> WorkflowPipeline { get; set; }
        public List<DashboardActivityViewModel> RecentActivities { get; set; }
        public List<DashboardQuickActionViewModel> QuickActions { get; set; }
    }

    public class DashboardKpiSummaryViewModel
    {
        // Purchase Requests (PR)
        public int PrTotal { get; set; }
        public int PrDraft { get; set; }
        public int PrSubmitted { get; set; }
        public int PrReturned { get; set; }
        public int PrPosted { get; set; }

        // Purchase Orders (PO)
        public int PoTotal { get; set; }
        public int PoDraft { get; set; }
        public int PoPosted { get; set; }
        public decimal PoCommittedAmount { get; set; }

        // Inspection & Acceptance (AIR)
        public int AirPendingInspection { get; set; }
        public int AirInspected { get; set; }
        public int AirTotal { get; set; }

        // Requisition & Issuance (RIS)
        public int RisPendingApproval { get; set; }
        public int RisPendingIssuance { get; set; }
        public int RisPosted { get; set; }
        public int RisTotal { get; set; }

        // Property & Inventory Cards
        public int StockCardCount { get; set; }
        public int PropertyCardCount { get; set; }
        public int ParCount { get; set; }
        public int IcsCount { get; set; }
    }

    public class DashboardActionItemViewModel
    {
        public Guid Id { get; set; }
        public string DocumentType { get; set; } // PR, PO, AIR, RIS
        public string DocumentNo { get; set; }
        public string Department { get; set; }
        public string Description { get; set; }
        public string Remarks { get; set; }
        public string UrgencyLevel { get; set; } // danger, warning, info
        public string Status { get; set; }
        public string ActionUrl { get; set; }
        public string ActionLabel { get; set; }
        public DateTime? Date { get; set; }
        public decimal? Amount { get; set; }
    }

    public class DashboardWorkflowStepViewModel
    {
        public int StepNumber { get; set; }
        public string Title { get; set; }
        public int Count { get; set; }
        public string Subtitle { get; set; }
        public string Url { get; set; }
        public string IconClass { get; set; }
        public string BadgeClass { get; set; }
    }

    public class DashboardActivityViewModel
    {
        public Guid Id { get; set; }
        public string DocumentType { get; set; } // PR, PO, AIR, RIS, PAR, ICS
        public string DocumentNo { get; set; }
        public string Action { get; set; }
        public string User { get; set; }
        public string Department { get; set; }
        public DateTime Timestamp { get; set; }
        public string TimeAgo { get; set; }
        public string Remarks { get; set; }
        public string Url { get; set; }
    }

    public class DashboardQuickActionViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string IconClass { get; set; }
        public string Category { get; set; }
        public string BadgeText { get; set; }
        public bool IsPrimary { get; set; }
    }
}
