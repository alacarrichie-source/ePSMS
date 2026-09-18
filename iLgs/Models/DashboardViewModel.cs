using System;
using System.Collections.Generic;

namespace iLgs.Models
{
    public class DashboardViewModel
    {
        public DashboardViewModel()
        {
            UserDashboard = new UserDashboardVM();
            AdminDashboard = new AdminDashboardVM();

            // Retain backwards-compatibility
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

        // Role-Specific Workspaces
        public UserDashboardVM UserDashboard { get; set; }
        public AdminDashboardVM AdminDashboard { get; set; }

        public List<string> DepartmentList
        {
            get { return AdminDashboard != null ? AdminDashboard.DepartmentList : new List<string>(); }
            set { if (AdminDashboard != null) AdminDashboard.DepartmentList = value; }
        }

        // Backwards compatibility properties
        public DashboardKpiSummaryViewModel KpiSummary { get; set; }
        public List<DashboardActionItemViewModel> ActionRequiredItems { get; set; }
        public List<DashboardWorkflowStepViewModel> WorkflowPipeline { get; set; }
        public List<DashboardActivityViewModel> RecentActivities { get; set; }
        public List<DashboardQuickActionViewModel> QuickActions { get; set; }
    }

    #region Non-Admin Personal Workbench ViewModels

    public class UserDashboardVM
    {
        public UserDashboardVM()
        {
            PersonalKpis = new UserPersonalKpiVM();
            MyWorkload = new List<UserWorkloadMetricVM>();
            MyMonthlyActivity = new List<UserMonthlyActivityVM>();
            QuickActions = new List<DashboardQuickActionVM>();
            AttentionItems = new List<DashboardAttentionItemVM>();
            ContinueWorkItems = new List<DashboardWorkItemVM>();
            ModuleShortcuts = new List<DashboardModuleCardVM>();
            RecentWorkItems = new List<DashboardRecentItemVM>();
        }

        public string SearchPlaceholder { get; set; }
        public UserPersonalKpiVM PersonalKpis { get; set; }
        public List<UserWorkloadMetricVM> MyWorkload { get; set; }
        public List<UserMonthlyActivityVM> MyMonthlyActivity { get; set; }

        public List<DashboardQuickActionVM> QuickActions { get; set; }
        public List<DashboardAttentionItemVM> AttentionItems { get; set; }
        public List<DashboardAttentionItemVM> NeedsAttention
        {
            get { return AttentionItems; }
            set { AttentionItems = value; }
        }

        public List<DashboardWorkItemVM> ContinueWorkItems { get; set; }
        public List<DashboardWorkItemVM> ContinueWork
        {
            get { return ContinueWorkItems; }
            set { ContinueWorkItems = value; }
        }

        public List<DashboardModuleCardVM> ModuleShortcuts { get; set; }
        public List<DashboardModuleCardVM> Modules
        {
            get { return ModuleShortcuts; }
            set { ModuleShortcuts = value; }
        }

        public List<DashboardRecentItemVM> RecentWorkItems { get; set; }
        public List<DashboardRecentItemVM> RecentWork
        {
            get { return RecentWorkItems; }
            set { RecentWorkItems = value; }
        }
    }

    public class UserPersonalKpiVM
    {
        public int MyOpenWork { get; set; }
        public int MyDrafts { get; set; }
        public int MyInProgress { get; set; }
        public int NeedsMyAttention { get; set; }
        public int MyAccountabilityCount { get; set; }
        public int MyParCount { get; set; }
        public int MyIcsCount { get; set; }
        public int CompletedThisFiscalYear { get; set; }
    }

    public class UserWorkloadMetricVM
    {
        public string Module { get; set; }
        public int Count { get; set; }
        public string Color { get; set; }
        public string IconClass { get; set; }
        public string Url { get; set; }
    }

    public class UserMonthlyActivityVM
    {
        public int Month { get; set; }
        public string MonthName { get; set; }
        public int CompletedCount { get; set; }
    }

    public class DashboardQuickActionVM
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string IconClass { get; set; }
        public string ButtonText { get; set; }
        public string Category { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class DashboardAttentionItemVM
    {
        public Guid Id { get; set; }
        public string Reference { get; set; }
        public string Module { get; set; }
        public string Department { get; set; }
        public string ShortDescription { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public string ActionUrl { get; set; }
        public string ActionLabel { get; set; }
        public string UrgencyLevel { get; set; } // danger, warning, info
        public DateTime? Date { get; set; }
    }

    public class DashboardWorkItemVM
    {
        public Guid Id { get; set; }
        public string Reference { get; set; }
        public string Module { get; set; }
        public string Department { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string LastWorkedText { get; set; }
        public string ActionUrl { get; set; }
        public string Url
        {
            get { return ActionUrl; }
            set { ActionUrl = value; }
        }
        public DateTime? LastWorkedDate { get; set; }
        public DateTime? LastWorked
        {
            get { return LastWorkedDate; }
            set { LastWorkedDate = value; }
        }
        public string Stage { get; set; }
    }

    public class DashboardModuleCardVM
    {
        public string Title { get; set; }
        public string ModuleName { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string IconClass { get; set; }
        public string Category { get; set; }
    }

    public class DashboardRecentItemVM
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Reference
        {
            get { return !string.IsNullOrEmpty(Title) ? Title : Subtitle; }
            set { Title = value; }
        }
        public string Description
        {
            get { return !string.IsNullOrEmpty(Subtitle) ? Subtitle : Title; }
            set { Subtitle = value; }
        }
        public string Module { get; set; }
        public string IconClass { get; set; }
        public string Url { get; set; }
        public string TimeAgo { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime? ActivityDate
        {
            get { return Timestamp; }
            set { if (value.HasValue) Timestamp = value.Value; }
        }
    }

    #endregion

    #region Admin Operational Command Center ViewModels

    public class AdminDashboardVM
    {
        public AdminDashboardVM()
        {
            Filters = new AdminDashboardFilterVM();
            DepartmentList = new List<string>();
            ExecutiveKpis = new AdminExecutiveKpiVM();
            RequiresAttention = new List<DashboardAttentionItemVM>();
            OperationalSummary = new AdminOperationalSummaryVM();
            MonthlyProcurementActivity = new List<DashboardMonthlyVolumeVM>();
            StatusDistribution = new List<DashboardStatusDistributionVM>();
            OperationalBacklog = new List<DashboardBacklogItemVM>();
            MonthlyPoCommitment = new List<DashboardMonthlyVolumeVM>();
            WorkflowHealth = new List<DashboardWorkflowHealthVM>();
            RecentActivities = new List<DashboardActivityVM>();
            QuickModules = new List<DashboardModuleCardVM>();
            AdminTools = new List<DashboardModuleCardVM>();
        }

        public AdminDashboardFilterVM Filters { get; set; }
        public List<string> DepartmentList { get; set; }
        public AdminExecutiveKpiVM ExecutiveKpis { get; set; }
        public List<DashboardAttentionItemVM> RequiresAttention { get; set; }
        public List<DashboardAttentionItemVM> AttentionItems
        {
            get { return RequiresAttention; }
            set { RequiresAttention = value; }
        }
        public AdminOperationalSummaryVM OperationalSummary { get; set; }
        public List<DashboardMonthlyVolumeVM> MonthlyProcurementActivity { get; set; }
        public List<DashboardStatusDistributionVM> StatusDistribution { get; set; }
        public List<DashboardBacklogItemVM> OperationalBacklog { get; set; }
        public List<DashboardMonthlyVolumeVM> MonthlyPoCommitment { get; set; }
        public List<DashboardWorkflowHealthVM> WorkflowHealth { get; set; }
        public List<DashboardActivityVM> RecentActivities { get; set; }
        public List<DashboardActivityVM> RecentActivity
        {
            get { return RecentActivities; }
            set { RecentActivities = value; }
        }
        public List<DashboardModuleCardVM> QuickModules { get; set; }
        public List<DashboardModuleCardVM> CoreModules
        {
            get { return QuickModules; }
            set { QuickModules = value; }
        }
        public List<DashboardModuleCardVM> AdminTools { get; set; }

        public decimal TotalPropertyAcquisitionValue { get; set; }
        public decimal TotalSupplyInventoryValue { get; set; }
    }

    public class AdminExecutiveKpiVM
    {
        // 1. PR
        public int PrTotal { get; set; }
        public int PrActive { get; set; }
        public int PrDraft { get; set; }
        public int PrSubmitted { get; set; }
        public int PrReturned { get; set; }
        public int PrPosted { get; set; }

        // 2. PO
        public int PoTotal { get; set; }
        public int PoActive { get; set; }
        public int PoDraft { get; set; }
        public int PoPosted { get; set; }
        public decimal PoCommittedAmount { get; set; }

        // 3. AIR (Inspection)
        public int AirPendingInspection { get; set; }
        public int AirInspected { get; set; }
        public int AirTotal { get; set; }

        // 4. RIS Pending
        public int RisPendingTotal { get; set; }
        public int RisPendingApproval { get; set; }
        public int RisPendingIssuance { get; set; }
        public int RisPosted { get; set; }

        // 5. Asset Registry
        public int PropertyCardCount { get; set; }
        public int StockCardCount { get; set; }
        public int TotalRegistryCards { get; set; }

        // 6. Requires Attention
        public int ActionableExceptionsCount { get; set; }
    }

    public class DashboardMonthlyVolumeVM
    {
        public int Month { get; set; }
        public string MonthName { get; set; }
        public int PrCount { get; set; }
        public int PoCount { get; set; }
        public int AirCount { get; set; }
        public int RisCount { get; set; }
        public decimal PoAmount { get; set; }
    }

    public class DashboardStatusDistributionVM
    {
        public string Module { get; set; }
        public string Status { get; set; }
        public int Count { get; set; }
        public string Color { get; set; }
    }

    public class DashboardBacklogItemVM
    {
        public string Category { get; set; }
        public int Count { get; set; }
        public string Module { get; set; }
        public string ActionUrl { get; set; }
        public string UrgencyLevel { get; set; } // danger, warning, info
    }

    public class AdminDashboardFilterVM
    {
        public int FiscalYear { get; set; }
        public string Department { get; set; }
        public string Module { get; set; }
        public string Status { get; set; }
    }

    public class AdminOperationalSummaryVM
    {
        // Purchase Requests
        public int PrSubmitted { get; set; }
        public int PrReturned { get; set; }
        public int PrPosted { get; set; }
        public int PrDraft { get; set; }
        public int PrTotal { get; set; }

        // Purchase Orders
        public int PoDraft { get; set; }
        public int PoPosted { get; set; }
        public int PoTotal { get; set; }
        public decimal PoCommittedAmount { get; set; }

        // AIR
        public int AirPendingInspection { get; set; }
        public int AirInspected { get; set; }
        public int AirTotal { get; set; }

        // RIS
        public int RisPendingApproval { get; set; }
        public int RisPendingIssuance { get; set; }
        public int RisPosted { get; set; }
        public int RisTotal { get; set; }

        // Cards & Accountability
        public int StockCardCount { get; set; }
        public int PropertyCardCount { get; set; }
        public int ParCount { get; set; }
        public int IcsCount { get; set; }
    }

    public class DashboardWorkflowHealthVM
    {
        public DashboardWorkflowHealthVM()
        {
            Stages = new List<DashboardWorkflowHealthStageVM>();
        }

        public string ProcessTitle { get; set; }
        public string IconClass { get; set; }
        public List<DashboardWorkflowHealthStageVM> Stages { get; set; }
    }

    public class DashboardWorkflowHealthStageVM
    {
        public string StageName { get; set; }
        public int Count { get; set; }
        public string StatusClass { get; set; } // alert, active, success, default
        public string Url { get; set; }
    }

    public class DashboardActivityVM
    {
        public Guid Id { get; set; }
        public string DocumentType { get; set; }
        public string DocumentNo { get; set; }
        public string DocumentUrl { get; set; }
        public string Url
        {
            get { return DocumentUrl; }
            set { DocumentUrl = value; }
        }
        public string ActionText { get; set; }
        public string Action
        {
            get { return ActionText; }
            set { ActionText = value; }
        }
        public string UserName { get; set; }
        public string User
        {
            get { return UserName; }
            set { UserName = value; }
        }
        public string Department { get; set; }
        public string RemarksSnippet { get; set; }
        public string Remarks
        {
            get { return RemarksSnippet; }
            set { RemarksSnippet = value; }
        }
        public string RelativeTime { get; set; }
        public string TimeAgo
        {
            get { return RelativeTime; }
            set { RelativeTime = value; }
        }
        public DateTime Timestamp { get; set; }
    }

    #endregion

    #region Backwards Compatibility ViewModels

    public class DashboardKpiSummaryViewModel
    {
        public int PrTotal { get; set; }
        public int PrSubmitted { get; set; }
        public int PrReturned { get; set; }
        public int PrPosted { get; set; }
        public int PrDraft { get; set; }
        public int PoTotal { get; set; }
        public int PoPosted { get; set; }
        public int PoDraft { get; set; }
        public decimal PoCommittedAmount { get; set; }
        public int AirTotal { get; set; }
        public int AirPendingInspection { get; set; }
        public int AirInspected { get; set; }
        public int RisTotal { get; set; }
        public int RisPendingApproval { get; set; }
        public int RisPendingIssuance { get; set; }
        public int RisPosted { get; set; }
        public int StockCardCount { get; set; }
        public int PropertyCardCount { get; set; }
        public int ParCount { get; set; }
        public int IcsCount { get; set; }
    }

    public class DashboardActionItemViewModel
    {
        public Guid DocumentId { get; set; }
        public string DocumentType { get; set; }
        public string ReferenceNumber { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public string UrgencyLevel { get; set; } // danger, warning, info
        public string ActionUrl { get; set; }
        public string ActionButtonText { get; set; }
        public DateTime? TargetDate { get; set; }
    }

    public class DashboardWorkflowStepViewModel
    {
        public string StepCode { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int PendingCount { get; set; }
        public string Url { get; set; }
        public string IconClass { get; set; }
        public string Status { get; set; }
    }

    public class DashboardActivityViewModel
    {
        public Guid ActivityId { get; set; }
        public string DocumentType { get; set; }
        public string DocumentNo { get; set; }
        public string DocumentUrl { get; set; }
        public string Action { get; set; }
        public string User { get; set; }
        public string Department { get; set; }
        public DateTime Timestamp { get; set; }
        public string TimeAgo { get; set; }
        public string Remarks { get; set; }
    }

    public class DashboardQuickActionViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string IconClass { get; set; }
        public string ButtonText { get; set; }
        public string Category { get; set; }
        public bool IsPrimary { get; set; }
    }

    #endregion
}
