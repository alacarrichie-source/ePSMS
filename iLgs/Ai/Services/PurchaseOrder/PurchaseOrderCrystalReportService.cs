using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using iLgs.Reports.DataSets;

namespace iLgs.Ai.Service.PurchaseOrder
{
    /// <summary>
    /// Shared Crystal Report service.
    ///
    /// Both printing paths must first produce POGroupReportPayload:
    ///
    /// 1. Wizard JSON -> POGroupReportPayload
    /// 2. Saved database PO -> POGroupReportPayload
    ///
    /// After that, both paths use the same DataSet and the same .rpt file.
    /// </summary>
    public class PurchaseOrderCrystalReportService
    {
        // ================================================================
        // PUBLIC ENTRY POINTS
        // ================================================================

        /// <summary>
        /// Use this for the Step 4 / Wizard payload.
        /// </summary>
        public byte[] ExportWizardPayloadPdf(
            IEnumerable<POGroupReportPayload> wizardPayload,
            string reportPath)
        {
            return ExportPdf(wizardPayload, reportPath);
        }

        /// <summary>
        /// Use this after an actual/saved PO has been converted to the
        /// same POGroupReportPayload structure.
        /// </summary>
        public byte[] ExportActualDataPdf(
            POGroupReportPayload actualDataPayload,
            string reportPath)
        {
            if (actualDataPayload == null)
                throw new ArgumentNullException("actualDataPayload");

            return ExportPdf(
                new List<POGroupReportPayload> { actualDataPayload },
                reportPath);
        }

        /// <summary>
        /// Shared Crystal export method used by both Wizard and saved PO.
        /// </summary>
        public byte[] ExportPdf(
            IEnumerable<POGroupReportPayload> payload,
            string reportPath)
        {
            if (payload == null)
                throw new ArgumentNullException("payload");

            if (string.IsNullOrWhiteSpace(reportPath))
                throw new ArgumentException(
                    "Report path is required.",
                    "reportPath");

            if (!File.Exists(reportPath))
                throw new FileNotFoundException(
                    "Crystal Report file was not found.",
                    reportPath);

            PurchaseOrderReportDataSet ds = BuildDataSet(payload);

            using (var report = new ReportDocument())
            {
                report.Load(reportPath);

                // Optional child tables that participate in the main report
                // are inner-joined by Crystal by default. Add key-only rows
                // for empty children so they do not suppress header/items.
                EnsureOptionalMainReportRows(report, ds);

                // Crystal does NOT connect to SQL Server.
                // The complete report data is supplied here.
                report.SetDataSource(ds);

                // SetDataSource(DataSet) does not reliably refresh the table
                // bindings after the .rpt schema has been changed. Crystal can
                // then export successfully while every bound field is empty.
                // Bind each main-report table explicitly, but keep the DataSet
                // assignment above so its parent/child relations remain
                // available to the report.
                BindMainReportTables(report, ds);

                using (Stream stream = report.ExportToStream(
                    ExportFormatType.PortableDocFormat))
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }


        private static void EnsureOptionalMainReportRows(
            ReportDocument report,
            DataSet dataSet)
        {
            foreach (Table reportTable in report.Database.Tables)
            {
                DataTable childTable = FindDataTable(
                    dataSet,
                    reportTable.Name,
                    reportTable.Location);

                if (childTable == null || childTable.Rows.Count != 0)
                    continue;

                DataRelation relation = dataSet.Relations
                    .Cast<DataRelation>()
                    .FirstOrDefault(x => x.ChildTable == childTable);

                if (relation == null || relation.ParentTable.Rows.Count == 0)
                    continue;

                foreach (DataRow parentRow in relation.ParentTable.Rows)
                {
                    DataRow childRow = childTable.NewRow();

                    for (int i = 0; i < relation.ParentColumns.Length; i++)
                    {
                        childRow[relation.ChildColumns[i]] =
                            parentRow[relation.ParentColumns[i]];
                    }

                    childTable.Rows.Add(childRow);
                }
            }
        }
        private static void BindMainReportTables(
            ReportDocument report,
            DataSet dataSet)
        {
            foreach (Table reportTable in report.Database.Tables)
            {
                DataTable sourceTable = FindDataTable(
                    dataSet,
                    reportTable.Name,
                    reportTable.Location);

                if (sourceTable == null)
                {
                    throw new InvalidOperationException(
                        "The Crystal Report table '" + reportTable.Name +
                        "' does not exist in PurchaseOrderReportDataSet.");
                }

                reportTable.SetDataSource(sourceTable);
            }
        }



        private static DataTable FindDataTable(
            DataSet dataSet,
            params string[] reportTableNames)
        {
            foreach (string rawName in reportTableNames)
            {
                if (string.IsNullOrWhiteSpace(rawName))
                    continue;

                string tableName = rawName;
                int separatorIndex = tableName.LastIndexOf('.');
                if (separatorIndex >= 0)
                    tableName = tableName.Substring(separatorIndex + 1);

                DataTable exactMatch = dataSet.Tables
                    .Cast<DataTable>()
                    .FirstOrDefault(x => string.Equals(
                        x.TableName,
                        tableName,
                        StringComparison.OrdinalIgnoreCase));

                if (exactMatch != null)
                    return exactMatch;
            }

            return null;
        }

        // ================================================================
        // PAYLOAD -> TYPED DATASET
        // ================================================================

        public PurchaseOrderReportDataSet BuildDataSet(
            IEnumerable<POGroupReportPayload> payload)
        {
            if (payload == null)
                throw new ArgumentNullException("payload");

            var ds = new PurchaseOrderReportDataSet();

            foreach (POGroupReportPayload rawGroup in payload)
            {
                if (rawGroup == null)
                    continue;

                POGroupReportPayload group = NormalizePayload(rawGroup);

                string groupId = string.IsNullOrWhiteSpace(group.GroupId)
                    ? Guid.NewGuid().ToString()
                    : group.GroupId;


                // ========================================================
                // PO HEADER
                // ========================================================
                DataRow header = ds.POHeader.NewRow();

                Set(header, "GroupId", groupId);
                Set(header, "GroupName", group.GroupName);
                Set(header, "PONumber", group.PONumber);
                Set(header, "PRNumber", group.PRNumber);
                Set(header, "DepartmentName", group.DepartmentName);

                Set(header, "SupplierId", ToStringValue(group.SupplierId));
                Set(header, "SupplierName", group.SupplierName);
                Set(header, "CtrlNo", group.CtrlNo);
                Set(header, "SupBusiness", group.SupBusiness);
                Set(header, "SupAddress", group.SupAddress);
                Set(header, "SupTIN", group.SupTIN);
                Set(header, "SupEmail", group.SupEmail);
                Set(header, "SupZipCode", group.SupZipCode);
                Set(header, "SupContactNo", group.SupContactNo);

                Set(header, "PODate", group.PODate);
                Set(header,
                    "DeliveryPeriodDays",
                    ParseNullableInt(group.DeliveryPeriodDays));
                Set(header, "DeliveryDate", group.DeliveryDate);
                Set(header, "PlaceOfDelivery", group.PlaceOfDelivery);
                Set(header, "TermDelivery", group.TermDelivery);
                Set(header, "PaymentTerms", group.PaymentTerms);
                Set(header, "ModeOfProcurement", group.ModeOfProcurement);

                Set(header, "SignedBySuppName", group.SignedBySuppName);
                Set(header, "SignedBySuppDate", group.SignedBySuppDate);
                Set(header, "SignedByAuthName", group.SignedByAuthName);
                Set(header,
                    "SignedByAuthDesignation",
                    group.SignedByAuthDesignation);
                Set(header, "ResoNo", group.ResoNo);
                Set(header,
                    "CertifiedCorrectBy",
                    group.CertifiedCorrectBy);
                Set(header,
                    "CertifiedCorrectDate",
                    group.CertifiedCorrectDate);

                decimal grandTotal = group.Items
                    .Where(x => x != null)
                    .Sum(x => x.Quantity * x.UnitCost);

                Set(header, "GrandTotal", grandTotal);

                ds.POHeader.Rows.Add(header);


                // ========================================================
                // SOURCE PRs
                // ========================================================
                foreach (SourcePRReportPayload sourcePr in group.SourcePRs)
                {
                    if (sourcePr == null)
                        continue;

                    DataRow row = ds.SourcePRs.NewRow();

                    Set(row, "GroupId", groupId);
                    Set(row, "PRId", ToStringValue(sourcePr.PRId));
                    Set(row, "PRNumber", sourcePr.PRNumber);
                    Set(row,
                        "DepartmentId",
                        ToStringValue(sourcePr.DepartmentId));
                    Set(row,
                        "DepartmentName",
                        sourcePr.DepartmentName);

                    ds.SourcePRs.Rows.Add(row);
                }


                // ========================================================
                // PO ITEMS
                // ========================================================
                foreach (POItemReportPayload item in group.Items)
                {
                    if (item == null)
                        continue;

                    string itemId = ToStringValue(item.Id);

                    // The XSD uses ItemId as a key.
                    // For actual saved data this should always have a value.
                    // For an unsaved Wizard preview, generate a report-only key
                    // if the item does not yet have one.
                    if (string.IsNullOrWhiteSpace(itemId))
                        itemId = Guid.NewGuid().ToString();

                    DataRow itemRow = ds.POItems.NewRow();

                    Set(itemRow, "GroupId", groupId);
                    Set(itemRow, "ItemId", itemId);
                    Set(itemRow, "ItemNo", item.ItemNo);
                    Set(itemRow, "ItemCode", item.ItemCode);
                    Set(itemRow,
                        "ItemCodeId",
                        ToStringValue(item.ItemCodeId));
                    Set(itemRow, "PpmpCode", item.PpmpCode);
                    Set(itemRow, "StockNo", item.StockNo);
                    Set(itemRow, "Description", item.Description);
                    Set(itemRow, "Quantity", item.Quantity);
                    Set(itemRow, "Unit", item.Unit);
                    Set(itemRow, "UnitCost", item.UnitCost);
                    Set(itemRow,
                        "TotalCost",
                        item.Quantity * item.UnitCost);
                    Set(itemRow, "Category", item.Category);
                    Set(itemRow, "Account", item.Account);
                    Set(itemRow, "SubAccount", item.SubAccount);
                    Set(itemRow, "GSOCategory", item.GSOCategory);
                    Set(itemRow,
                        "TechnicalDescription",
                        item.TechnicalDescription);

                    ds.POItems.Rows.Add(itemRow);


                    // ====================================================
                    // ADDITIONAL SPECS
                    // ====================================================
                    if (item.AdditionalSpecs != null)
                    {
                        DataRow specsRow = ds.AdditionalSpecs.NewRow();

                        Set(specsRow, "ItemId", itemId);
                        Set(specsRow,
                            "AdditionalSpecsId",
                            ToStringValue(item.AdditionalSpecs.Id));
                        Set(specsRow,
                            "Multipliers",
                            item.AdditionalSpecs.Multipliers);
                        Set(specsRow, "Brand", item.AdditionalSpecs.Brand);
                        Set(specsRow, "Model", item.AdditionalSpecs.Model_);
                        Set(specsRow,
                            "Dimension",
                            item.AdditionalSpecs.Dimension);
                        Set(specsRow, "Size", item.AdditionalSpecs.Size);
                        Set(specsRow, "Weight", item.AdditionalSpecs.Weight);
                        Set(specsRow,
                            "Materials",
                            item.AdditionalSpecs.Materials);
                        Set(specsRow,
                            "Capacity",
                            item.AdditionalSpecs.Capacity);
                        Set(specsRow, "Color", item.AdditionalSpecs.Color);

                        ds.AdditionalSpecs.Rows.Add(specsRow);
                    }


                    // ====================================================
                    // SET / LOT ITEMS
                    // ====================================================
                    foreach (SetLotItemReportPayload subItem
                        in item.SetLotItems ?? new List<SetLotItemReportPayload>())
                    {
                        if (subItem == null)
                            continue;

                        DataRow subRow = ds.SetLotItems.NewRow();

                        Set(subRow, "ItemId", itemId);
                        Set(subRow,
                            "RequestSubItemId",
                            ToStringValue(subItem.RequestSubItemId));
                        Set(subRow, "ItemNo", subItem.ItemNo);
                        Set(subRow, "ItemName", subItem.ItemName);
                        Set(subRow, "Unit", subItem.Unit);
                        Set(subRow, "Qty", subItem.Qty);
                        Set(subRow,
                            "EstimatedCost",
                            subItem.EstimatedCost);

                        ds.SetLotItems.Rows.Add(subRow);
                    }


                    // ====================================================
                    // ALLOCATIONS
                    // ====================================================
                    foreach (AllocationReportPayload allocation
                        in item.Allocations ?? new List<AllocationReportPayload>())
                    {
                        if (allocation == null)
                            continue;

                        DataRow allocationRow = ds.Allocations.NewRow();

                        Set(allocationRow, "ItemId", itemId);
                        Set(allocationRow,
                            "PRId",
                            ToStringValue(allocation.PRId));
                        Set(allocationRow,
                            "RequestItemId",
                            ToStringValue(allocation.RequestItemId));
                        Set(allocationRow,
                            "PRNumber",
                            allocation.PRNumber);
                        Set(allocationRow,
                            "Quantity",
                            allocation.Quantity);

                        ds.Allocations.Rows.Add(allocationRow);
                    }
                }


                // ========================================================
                // PO COPY
                // ========================================================
                if (group.POCopyDoc != null)
                {
                    DataRow row = ds.POCopyDocs.NewRow();

                    Set(row, "GroupId", groupId);
                    Set(row, "DocumentId", group.POCopyDoc.Id);
                    Set(row, "FileName", group.POCopyDoc.FileName);
                    Set(row, "FilePath", group.POCopyDoc.FilePath);
                    Set(row, "FileSize", group.POCopyDoc.FileSize);
                    Set(row, "Category", group.POCopyDoc.Category);
                    Set(row, "UploadedAt", group.POCopyDoc.UploadedAt);

                    ds.POCopyDocs.Rows.Add(row);
                }


                // ========================================================
                // ADDITIONAL DOCUMENTS
                // ========================================================
                foreach (PODocumentReportPayload doc
                    in group.AdditionalDocs ?? new List<PODocumentReportPayload>())
                {
                    if (doc == null)
                        continue;

                    DataRow row = ds.AdditionalDocs.NewRow();

                    Set(row, "GroupId", groupId);
                    Set(row, "DocumentId", doc.Id);
                    Set(row, "FileName", doc.FileName);
                    Set(row, "FilePath", doc.FilePath);
                    Set(row, "FileSize", doc.FileSize);
                    Set(row, "Category", doc.Category);
                    Set(row, "UploadedAt", doc.UploadedAt);

                    ds.AdditionalDocs.Rows.Add(row);
                }
            }

            ds.AcceptChanges();
            return ds;
        }

        // ================================================================
        // NORMALIZATION
        // ================================================================

        /// <summary>
        /// Makes Wizard and database-generated payloads behave identically.
        /// If summary PR/department fields are not populated, they are
        /// created from SourcePRs.
        /// </summary>
        private static POGroupReportPayload NormalizePayload(
            POGroupReportPayload group)
        {
            if (group.SourcePRs == null)
                group.SourcePRs = new List<SourcePRReportPayload>();

            if (group.Items == null)
                group.Items = new List<POItemReportPayload>();

            if (group.AdditionalDocs == null)
                group.AdditionalDocs = new List<PODocumentReportPayload>();

            if (string.IsNullOrWhiteSpace(group.PRNumber))
            {
                group.PRNumber = string.Join(
                    ", ",
                    group.SourcePRs
                        .Where(x => x != null &&
                                    !string.IsNullOrWhiteSpace(x.PRNumber))
                        .Select(x => x.PRNumber)
                        .Distinct());
            }

            if (string.IsNullOrWhiteSpace(group.DepartmentName))
            {
                group.DepartmentName = string.Join(
                    ", ",
                    group.SourcePRs
                        .Where(x => x != null &&
                                    !string.IsNullOrWhiteSpace(
                                        x.DepartmentName))
                        .Select(x => x.DepartmentName)
                        .Distinct());
            }

            return group;
        }

        // ================================================================
        // HELPERS
        // ================================================================



        private static void Set(
            DataRow row,
            string columnName,
            object value)
        {
            if (row == null ||
                !row.Table.Columns.Contains(columnName))
                return;

            if (value == null)
            {
                row[columnName] = DBNull.Value;
                return;
            }

            string text = value as string;

            if (text != null)
            {
                row[columnName] =
                    string.IsNullOrWhiteSpace(text)
                        ? (object)DBNull.Value
                        : text;

                return;
            }

            row[columnName] = value;
        }

        private static string ToStringValue(object value)
        {
            if (value == null)
                return null;

            string text = Convert.ToString(value);

            return string.IsNullOrWhiteSpace(text)
                ? null
                : text;
        }

        private static int? ParseNullableInt(string value)
        {
            int parsed;

            return int.TryParse(value, out parsed)
                ? (int?)parsed
                : null;
        }
    }


    // ====================================================================
    // SHARED REPORT PAYLOAD
    // ====================================================================

    public class POGroupReportPayload
    {
        public string GroupId { get; set; }
        public string GroupName { get; set; }
        public string PONumber { get; set; }

        // Summary/display values.
        public string PRNumber { get; set; }
        public string DepartmentName { get; set; }

        public object SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string CtrlNo { get; set; }
        public string SupBusiness { get; set; }
        public string SupAddress { get; set; }
        public string SupTIN { get; set; }
        public string SupEmail { get; set; }
        public string SupZipCode { get; set; }
        public string SupContactNo { get; set; }

        public DateTime? PODate { get; set; }
        public string DeliveryPeriodDays { get; set; }
        public string DeliveryDate { get; set; }
        public string PlaceOfDelivery { get; set; }
        public string TermDelivery { get; set; }
        public string PaymentTerms { get; set; }
        public string ModeOfProcurement { get; set; }

        public string SignedBySuppName { get; set; }
        public DateTime? SignedBySuppDate { get; set; }
        public string SignedByAuthName { get; set; }
        public string SignedByAuthDesignation { get; set; }
        public string ResoNo { get; set; }
        public string CertifiedCorrectBy { get; set; }
        public DateTime? CertifiedCorrectDate { get; set; }

        public List<SourcePRReportPayload> SourcePRs { get; set; }
        public List<POItemReportPayload> Items { get; set; }

        public PODocumentReportPayload POCopyDoc { get; set; }
        public List<PODocumentReportPayload> AdditionalDocs { get; set; }
    }

    public class SourcePRReportPayload
    {
        public object PRId { get; set; }
        public string PRNumber { get; set; }
        public object DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }

    public class POItemReportPayload
    {
        public object Id { get; set; }
        public string ItemNo { get; set; }
        public string ItemCode { get; set; }
        public object ItemCodeId { get; set; }
        public string PpmpCode { get; set; }
        public string StockNo { get; set; }

        public string Description { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public decimal UnitCost { get; set; }

        public string Category { get; set; }
        public string Account { get; set; }
        public string SubAccount { get; set; }
        public string GSOCategory { get; set; }
        public string TechnicalDescription { get; set; }

        public AdditionalSpecsReportPayload AdditionalSpecs { get; set; }
        public Dictionary<string, string> AllFields { get; set; }
        public List<SetLotItemReportPayload> SetLotItems { get; set; }
        public List<AllocationReportPayload> Allocations { get; set; }
    }

    public class AdditionalSpecsReportPayload
    {
        public object Id { get; set; }
        public string Multipliers { get; set; }
        public string Brand { get; set; }
        public string Model_ { get; set; }
        public string Dimension { get; set; }
        public string Size { get; set; }
        public string Weight { get; set; }
        public string Materials { get; set; }
        public string Capacity { get; set; }
        public string Color { get; set; }
    }

    public class SetLotItemReportPayload
    {
        public object RequestSubItemId { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string Unit { get; set; }
        public decimal Qty { get; set; }
        public decimal EstimatedCost { get; set; }
    }

    public class AllocationReportPayload
    {
        public object PRId { get; set; }
        public object RequestItemId { get; set; }
        public string PRNumber { get; set; }
        public decimal Quantity { get; set; }
    }

    public class PODocumentReportPayload
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileSize { get; set; }
        public string Category { get; set; }
        public DateTime? UploadedAt { get; set; }
    }
}

