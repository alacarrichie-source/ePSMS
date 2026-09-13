/* Existing Purchase Order mode reuses the four wizard steps and never saves creation progress. */
var poExistingSaveInFlight = false;
var existingAllocationsChanged = false;

function restoreExistingPO() {
    if (!existingOrderId || !existingPOJson) return;
    var source = JSON.parse(existingPOJson), group = {};
    Object.keys(source).forEach(function(key) { group[key.charAt(0).toLowerCase()+key.slice(1)] = source[key]; });
    group.groupId = existingOrderId;
    group.poNumber = source.PONumber;
    group.poDate = source.PODate;
    group.PRNumber = source.PRNumber;
    group.DepartmentName = source.DepartmentName;
    group.poCopyDoc = source.POCopyDoc;
    group.sourcePRs = source.SourcePRs || [];
    group.items = (source.Items || []).map(function(item) {
        var mapped = {};
        Object.keys(item).forEach(function(key) { mapped[key.charAt(0).toLowerCase()+key.slice(1)] = item[key]; });
        mapped.qty = item.Quantity;
        mapped.stockNo = item.StockNo;
        mapped.PPMPCode = item.PpmpCode || item.ItemCode;
        mapped.sourcePRs = (item.Allocations || []).map(function(a) {
            return { requestItemId:a.RequestItemId, prId:a.PRId, prNo:a.PRNumber, allocatedQty:a.Quantity };
        });
        return mapped;
    });
    poGroups = [group];
    var sources = {};
    allocatedItems = [];
    group.items.forEach(function(item) {
        item.sourcePRs.forEach(function(a) {
            if(a.prId) sources[String(a.prId)] = true;
            var previous = allocatedItems.filter(function(r){return r.Id === a.requestItemId;})[0];
            if(previous) { previous.AssignedQty += a.allocatedQty; previous.RequestedQty += a.allocatedQty; return; }
            allocatedItems.push({
                Id:a.requestItemId, PRId:a.prId, PRNumber:a.prNo, Department:source.DepartmentName || "",
                Description:item.description, PPMPCode:item.PPMPCode, Unit:item.unit, UnitCost:item.unitCost,
                RequestedQty:a.allocatedQty, AssignedQty:a.allocatedQty, RemainingQty:0, TargetGroupId:existingOrderId,
                ItemCodeId:item.itemCodeId, StockNo:item.stockNo, SetLotItems:item.setLotItems || []
            });
        });
    });
    selectedPRIds = Object.keys(sources);
    currentWizardStep = 3;
    wizardDraftId = null;
    wizardDraftNo = null;
    $("#wizardDraftStatusText").text("Editing existing PO " + source.PONumber);
    $("#btnSaveDraft,#btnStep4SaveDraft").text("Save Changes");
    $("#btnPostPurchaseOrders").text("Save & Repost This PO");
    $(".page-title,.breadcrumbs > span:last-child").text("Edit Purchase Order " + source.PONumber);
    $(".page-subtitle").text("Correct this existing Purchase Order, then save or repost the same record.");
    $("#poWizardForm").on("input change", "#wizardStepPane2 input,#wizardStepPane2 select", function(){existingAllocationsChanged=true;});
}
function saveExistingPurchaseOrder(post) {
    if (poExistingSaveInFlight) return $.Deferred().reject().promise();
    if (typeof captureVisibleStep3AdditionalSpecs === "function") captureVisibleStep3AdditionalSpecs();
    if (post && !$("#chkStep4Certification").is(":checked")) {
        showWizardMessage("Confirm the compliance certification before reposting.");
        return $.Deferred().reject().promise();
    }
    poExistingSaveInFlight = true;
    kendo.ui.progress($(".po-wizard"),true);
    return $.ajax({
        url:poExistingSaveUrl,type:"POST",
        data:{__RequestVerificationToken:getAntiForgeryToken(),id:existingOrderId,post:post,poGroupsJson:JSON.stringify(buildPOPayload())}
    }).done(function(response) {
        if(!response.success) { showWizardMessage(response.message || "The PO could not be saved.");return; }
        wizardDraftDirty=false;
        window.location.href=post ? response.redirectUrl : poExistingWizardUrl+"?orderId="+encodeURIComponent(existingOrderId);
    }).fail(function(){showWizardMessage("The PO could not be saved. Please retry.");})
      .always(function(){poExistingSaveInFlight=false;kendo.ui.progress($(".po-wizard"),false);});
}
function poLifecycleAction(id, action) {
    var verb=action==="unpost"?"Unpost":"Delete";
    kendo.confirm(verb+" this Purchase Order?").done(function(){
        $.ajax({url:action==="unpost"?poUnpostUrl:poDeleteUrl,type:"POST",
            data:{id:id,__RequestVerificationToken:$("input[name='__RequestVerificationToken']").first().val()}
        }).done(function(response){
            kendo.alert(response.message || (response.success ? "Completed." : "The operation was blocked."));
            if(response.success){var grid=$("#PurchaseOrdersGrid").data("kendoGrid");if(grid){grid.dataSource.read();}}
        }).fail(function(){kendo.alert("The operation failed. Refresh the Purchase Order before retrying.");});
    });
}
