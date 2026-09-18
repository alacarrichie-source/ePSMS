function propertyCardButtonStart(button, message) {
    var $button = $(button);

    if (!$button.length) {
        return false;
    }

    if ($button.data("processing") === true) {
        return false;
    }

    $button.data("processing", true);

    if (!$button.data("original-html")) {
        $button.data("original-html", $button.html());
    }

    $button
        .addClass("epsms-processing-button k-state-disabled")
        .attr("aria-disabled", "true");

    if ($button.is("button") || $button.is("input")) {
        $button.prop("disabled", true);
    }

    $button.html(
        "<span class='k-icon k-i-loading epsms-button-spinner'></span>" +
        "<span>" + kendo.htmlEncode(message || "Processing...") + "</span>"
    );

    return true;
}

function propertyCardButtonStop(button) {
    var $button = $(button);

    if (!$button.length) {
        return;
    }

    var originalHtml = $button.data("original-html");

    $button
        .data("processing", false)
        .removeClass("epsms-processing-button k-state-disabled")
        .removeAttr("aria-disabled");

    if ($button.is("button") || $button.is("input")) {
        $button.prop("disabled", false);
    }

    if (originalHtml) {
        $button.html(originalHtml);
        $button.removeData("original-html");
    }
}

// Backward-compatible aliases
function propertyCardStartProcessing(button, container, message) {
    var msg = typeof container === "string" && !message ? container : message;
    return propertyCardButtonStart(button, msg);
}

function propertyCardStopProcessing(button, container) {
    propertyCardButtonStop(button);
}

window.propertyCardButtonStart = propertyCardButtonStart;
window.propertyCardButtonStop = propertyCardButtonStop;
window.propertyCardStartProcessing = propertyCardStartProcessing;
window.propertyCardStopProcessing = propertyCardStopProcessing;


    function columnTemplate(data) {
        return '<div style="white-space: normal; word-wrap: break-word;">' + kendo.htmlEncode(data == null ? "" : String(data)) + '</div>';
    }

    function GridError(args) {
        if (args.errors) {
            var validationError = true;
            $.each(args.errors, function (propertyName) {
                if (propertyName == "DeleteError" || propertyName == "AddError" || propertyName == "UpdateError") {
                    notifyWarning(this.errors);
                    validationError = false;
                    selectedGrid.dataSource.read();
                }
            });
            if (validationError) {
                var validationTemplate = kendo.template($("#validationMessageTemplate").html());
                $('#ErrorContent').removeClass('hidden');
                selectedGrid.one("dataBinding", function (e) {
                    e.preventDefault();
                    var errors = selectedGrid.editable.element.find(".errors");
                    errors.empty();
                    $.each(args.errors, function (propertyName) {
                        var renderedTemplate = validationTemplate({ field: propertyName, messages: this.errors });
                        errors.append(renderedTemplate);
                    });
                });
            }
        }
    }

    function GridItemError(args) {
        if (args.errors) {
            var selectedGrid = $("#gridItems_" + selectedId).data("kendoGrid");
            var validationError = true;
            $.each(args.errors, function (propertyName) {
                if (propertyName == "DeleteError" || propertyName == "AddError" || propertyName == "UpdateError") {
                    notifyWarning(this.errors);
                    validationError = false;
                    selectedGrid.dataSource.read();
                }
            });
            if (validationError) {
                var validationTemplate = kendo.template($("#validationMessageTemplate").html());
                $('#ErrorContent').removeClass('hidden');
                selectedGrid.one("dataBinding", function (e) {
                    e.preventDefault();
                    var errors = selectedGrid.editable.element.find(".errors");
                    errors.empty();
                    $.each(args.errors, function (propertyName) {
                        var renderedTemplate = validationTemplate({ field: propertyName, messages: this.errors });
                        errors.append(renderedTemplate);
                    });
                });
            }
        }
    }

    function onListDataBound(e) {
        this.select($(".item:first"));
    }

    function onCriteriaChange() {
        var items = $("#itemsList").data("kendoListView"),
            item = items.dataSource.getByUid(items.select().attr("data-uid")),
            //itemQuarterSales = $("#itemQuarterSales").data("kendoChart"),
            //itemAverageSales = $("#itemAverageSales").data("kendoChart"),
            //teamSales = $("#TeamSales").data("kendoChart"),
            //itemSales = $("#itemSales").data("kendoScheduler"),
            //startDate = $("#StartDate").data("kendoDatePicker"),
            //endDate = $("#EndDate").data("kendoDatePicker"),
            //filter = { itemID: item.itemID, startDate: startDate.value(), endDate: endDate.value() },
            template = kendo.template($("#itemBioTemplate").html());

        //$("#itemBio").html(template(item));

        //itemSales.dataSource.filter({ field: "itemID", operator: "eq", value: item.itemID });

        //teamSales.dataSource.read(filter);

        //itemQuarterSales.dataSource.read(filter);
        //itemAverageSales.dataSource.read(filter);
    }


    var selectedGrid = null;
    var selectedId = null;
    var selectedItemId = null;
    var selectedTransferId = null;
    var selectedStockNo = null;

    function onGridDeleteClick(e) {
        e.preventDefault();
        selectedGrid = this;

        //const tr = $(e.currentTarget).closest("tr");
        //const dataItem = this.dataItem(tr);
        //const rowitem = this.dataSource.get(dataItem.Id);
        //this.select($('[data-uid=' + rowitem.uid + ']'));
        //this.removeRow(tr);

        var tr = $(e.currentTarget).closest("tr"); var dataItem = this.dataItem(tr);
        selectedId = dataItem.Id;
        var rowitem = selectedGrid.dataSource.get(selectedId);
        selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));
        selectedGrid.removeRow(tr);
    }


    // GRID STOCKS
    function onClickBtnPrint(e) {
        e.preventDefault();
        var url = '' + propertyCardConfig.urls.PropertyCard_StockCardRpt + '?stockNo=' + selectedStockNo;

        $("#windowcontainer").append("<div id='windowPrint'></div>");
        var mywindow = $("#windowPrint")
                .kendoWindow({
                    actions: ["Maximize", "Minimize", "Close"],
                    resizable: true,
                    draggable: true,
                    modal: true,
                    visible: false,
                    iframe: true,
                    deactivate: function () {
                        this.destroy();
                    }
                }).data("kendoWindow");
        mywindow.setOptions({
            title: "Preview",
            width: 1200,
            height: 700
        });
        mywindow.refresh({
            url: url
        });
        mywindow.center();
        mywindow.open();
    }

    $(".k-grid-btnAddNew").click(function (e) {
        e.preventDefault();
        StockCard("A", null);
    });

    $(".k-grid-btnPrintCard").click(function (e) {
        e.preventDefault();

        if (!selectedId) { return; }
        var url = '' + propertyCardConfig.urls.PropertyCard_StockCardRpt + '?selectedId=' + selectedId;

        $("#windowcontainer").append("<div id='windowPrint'></div>");
        var mywindow = $("#windowPrint")
                .kendoWindow({
                    actions: ["Maximize", "Minimize", "Close"],
                    resizable: true,
                    draggable: true,
                    modal: true,
                    visible: false,
                    iframe: true,
                    deactivate: function () {
                        this.destroy();
                    }
                }).data("kendoWindow");
        mywindow.setOptions({
            title: "Preview",
            width: 1200,
            height: 700
        });
        mywindow.refresh({
            url: url
        });
        mywindow.center();
        mywindow.open();
    });

    $(".k-grid-btnPost").click(function (e) {
        e.preventDefault();
        var btn = this;
        if (postedBy) {
            notifyWarning("This Property Card has already been posted by " + postedBy);
            return;
        }

        epsmsConfirm({
            title: "Post Property Card",
            type: "info",
            headline: "Post Official Property Card?",
            entityLabel: "Property Card No.",
            entityValue: selectedStockNo || "Card",
            message: "Once posted, this Property Card will be formally recognized in the property registry and its records will become official.",
            cancelText: "Cancel",
            confirmText: "Post Card",
            confirmIcon: "k-i-check",
            onConfirm: function (dialog, $confirmBtn) {
                setClickedButtonBusy($confirmBtn, "Posting...");
                if (!propertyCardButtonStart(btn, "Posting...")) { return; }

                $.ajax({
                    type: "POST",
                    url: '' + propertyCardConfig.urls.PropertyCard_PostRecord + '',
                    data: { psCardId: selectedId },
                    async: true
                }).done(function (result) {
                    restoreClickedButton($confirmBtn);
                    dialog.close();
                    if (!result || result.Errors == null || result.Errors === "") {
                        notifySuccess("Property Card posted successfully.");
                        $("#grid").data("kendoGrid").dataSource.read();
                    } else {
                        notifyWarning(result.Errors);
                    }
                }).fail(function (req) {
                    restoreClickedButton($confirmBtn);
                    notifyWarning(ajaxFailureMessage(req, "Unable to post the Property Card."));
                }).always(function () {
                    propertyCardButtonStop(btn);
                });
            }
        });
    });

    $(".k-grid-btnUnpost").click(function (e) {
        e.preventDefault();
        var btn = this;

        epsmsConfirm({
            title: "Unpost Property Card",
            type: "warning",
            headline: "Unpost Property Card?",
            entityLabel: "Property Card No.",
            entityValue: selectedStockNo || "Card",
            message: "This will return the Property Card to an unposted status.",
            cancelText: "Cancel",
            confirmText: "Unpost Card",
            confirmIcon: "k-i-undo",
            onConfirm: function (dialog, $confirmBtn) {
                setClickedButtonBusy($confirmBtn, "Unposting...");
                if (!propertyCardButtonStart(btn, "Unposting...")) { return; }

                $.ajax({
                    type: "POST",
                    url: '' + propertyCardConfig.urls.PropertyCard_UnpostRecord + '',
                    data: { psCardId: selectedId },
                    async: true
                }).done(function (result) {
                    restoreClickedButton($confirmBtn);
                    dialog.close();
                    if (!result || result.Errors == null || result.Errors === "") {
                        notifySuccess("Property Card unposted.");
                        $("#grid").data("kendoGrid").dataSource.read();
                    } else {
                        notifyWarning(result.Errors);
                    }
                }).fail(function (req) {
                    restoreClickedButton($confirmBtn);
                    notifyWarning(ajaxFailureMessage(req, "Unable to unpost the Property Card."));
                }).always(function () {
                    propertyCardButtonStop(btn);
                });
            }
        });
    });

    function onRequestEndGrid(e) {
        if (e.response && !e.response.Errors && !e.response.errors) {
            if (e.type == "destroy" || e.type == "create" || e.type == "update") {
                selectedGrid.dataSource.read();
            }
            if (e.type == "create")
            {
                var responseData = e.response;
                var newRecord = responseData.Data[0];
                selectedId = newRecord.Id;
            }
        }
    }

    var postedBy = null;

    function onChangeGrid(e) {
        var item = this.dataItem(this.select());
        propertyCardSetSelection(this, item);
    }

    function onDataBoundGrid(e) {
        var item = selectedId ? this.dataSource.get(selectedId) : null;
        if (!item && this.dataSource.view().length) {
            item = this.dataSource.view()[0];
        }
        propertyCardSetSelection(this, item);
        if (item) {
            this.select(this.tbody.children("tr[data-uid='" + item.uid + "']"));
        }
        propertyCardUpdateCount(this.dataSource);
    }
    function DataStockCard() {
        var userName = $("#UserName").data("kendoMultiColumnComboBox").value();
        return {
            userName: userName
        };
    }

    function DataStockCardItem() {
        var userName = $("#UserName").data("kendoMultiColumnComboBox").value();
        return {
            cardId: selectedId,
            userName: userName
        };
    }

    function onSelectUserName(e) {
        if (!e.item) {
            return;
        }

        var dataItem = this.dataItem(e.item.index());

        if (dataItem) {
            var userName = dataItem.UserName;
            this.value(userName);
            var source = $("#grid").data("kendoGrid").dataSource;
            source.query({ page: 1, pageSize: source.pageSize(), sort: source.sort(), filter: source.filter() });
        }
    }

    function onChangeUserName(e) {
        var comboBox = this;
        var value = comboBox.value(); // Get the current value of the combo box

        if (!value) {
            var userName = "";
            this.value(userName);
            var source = $("#grid").data("kendoGrid").dataSource;
            source.query({ page: 1, pageSize: source.pageSize(), sort: source.sort(), filter: source.filter() });
        }
    }

    function onEditGrid(e) {
        e.preventDefault();
        selectedGrid = e.sender;
        if (e.model.isNew()) {
            var row = e.sender.tbody.find("tr:first");
            this.select(row);
            selectedId = null;
            selectedItemId = null;
            selectedTransferId = null;
            //e.model.set("PsId", selectedId);
        } else {
            selectedId = e.model.get("Id");
            var rowitem = selectedGrid.dataSource.get(selectedId);
            selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));
        }
    }

    //function onGridDeleteClick(e) {
    //    //e.preventDefault();
    //    //selectedGrid = this;
    //    //var tr = $(e.currentTarget).closest("tr");
    //    //var dataItem = selectedGrid.dataItem(tr);
    //    //var rowitem = selectedGrid.dataSource.get(dataItem.Id);
    //    //selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));
    //    //setTimeout(function () { selectedGrid.removeRow(tr); }, 10);
    //    e.preventDefault();
    //    var selectedGrid = $("#grid").data("kendoGrid"); // Replace "grid" with your grid's ID
    //    var tr = $(e.currentTarget).closest("tr");
    //    var dataItem = selectedGrid.dataItem(tr);
    //    var rowitem = selectedGrid.dataSource.get(dataItem.Id);
    //    selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));
    //    selectedGrid.removeRow(tr);
    //}


    function error_handler(e, status) {
        if ((e.errors)) {
            var message = "Errors:\n";
            $.each(e.errors, function (key, value) {
                if ('errors' in value) {
                    $.each(value.errors, function () {
                        message += this + "\n";
                    });
                }
            });
            notifyWarning(message);
        }
    }

    // CARD ADD EDIT
    function onClickBtnAdd(e) {
        e.preventDefault();
        StockCard("A", null);
    }

    function onClickBtnEdit(e) {
        e.preventDefault();
        selectedGrid = this;
        var dataItem = this.dataItem($(e.currentTarget).closest("tr"));
        selectedId = dataItem.Id;
        var rowitem = selectedGrid.dataSource.get(selectedId);
        selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));
        StockCard("E", dataItem.Id);
    }

    function StockCard(mode, recId) {
        var myTitle = (mode == "A") ? "Property Card - ADD" : "Property Card - EDIT";
        var width = 1000;
        var height = 650;
        $("#windowcontainer").append("<div id='window'></div>");
        var mywindow = $("#window")
                .kendoWindow({
                    actions: ["Minimize", "Close"],
                    draggable: true,
                    modal: true,
                    visible: false,
                    autoheight: true,
                    close: function () {
                        if (propertyCardConfig.workspace) { window.location.reload(); return; }
                        var grid = $("#grid").data("kendoGrid");
                        if ($("#IsDuplicateStockNo").val() === "true") {
                            grid.dataSource.query({page: 1, pageSize: grid.dataSource.pageSize(),
                                filter: {logic: "and", filters: [
                                    {field: "PsNo", operator: "eq", value: $("#PsNo").val()},
                                    {field: "Fund", operator: "eq", value: $("#Fund").val()}
                                ]}}).then(function () {
                                    var item = grid.dataSource.view()[0];
                                    if (item) { window.location.href = propertyCardConfig.viewUrl + "?id=" + encodeURIComponent(item.Id) + "&addAcquisition=true"; }
                                });
                        } else { grid.dataSource.read(); }
                    },
                    deactivate: function () {
                        this.destroy();
                    }
                }).data("kendoWindow");
        mywindow.setOptions({
            title: myTitle,
            width: width
            //height: height
        });
        mywindow.refresh({
            url: '' + propertyCardConfig.urls.PropertyCard__PropertyCardAddEdit + '',
            data: {
                cardId: recId,
                mode: mode,
                isAdmin: propertyCardConfig.isAdmin
            },
            // Specify a callback function to execute after content is loaded
            complete: function () {
                // Resize the window to fit its content
                mywindow.resize();
                mywindow.center();
            }
        });
        //mywindow.center();
        mywindow.open();
        //resizeWindow();
    }

    function resizeWindow() {
        var mywindow = $("#window").data("kendoWindow");
        if (mywindow) {
            mywindow.setOptions({
                width: "1000",
                height: "auto"
            });
            mywindow.center(); // Optional: Center the window after resizing
        }
    }
    //


    // Card TEMPLATE
    function DataItemCode() {
        var text = $("#ItemCodeId").data("kendoMultiColumnComboBox").text();
        return {
            category: "P",
            text: text
        };
    }

    function onSelectItemCode(e) {
        var dataItem = this.dataItem(e.item.index());

        $("#ItemCodeId").val(dataItem.Id).trigger("change");
        $("#ItemCode").val(dataItem.Code).trigger("change");
        $("#ItemNo").val(dataItem.ItemNo).trigger("change");
        $("#Item").val(dataItem.Description).trigger("change");
        $("#ItemTypeCode").val(dataItem.Type).trigger("change");
        $("#ItemType").val(dataItem.TypeDesc).trigger("change");
        $("#SubAccount").val(dataItem.MainDesc).trigger("change");
        $("#SubAccountCode").val(dataItem.MainDescCode).trigger("change");

        //UpdateDescription(e);

        RefreshDivs(dataItem);

        UpdateDescription();
    }

    function RefreshDivs(dataItem) {
        //var selectedValue = this.value();
        var fieldsContainer = $('#DIV_ALLFIELDS');

        fieldsContainer.empty(); // Clear existing content

        var url = '' + propertyCardConfig.urls.PropertyCard_LoadFields + '';
        var data = $("#form").serialize();

        $.ajax({
            url: url,
            type: 'POST',
            data: data,
            //contentType: "application/json; charset=utf-8",
            success: function(data) {
                fieldsContainer.html(data);
                resizeWindow();
            }
        });

        //for (var i = 1; i <= 26; i++) {
        //    var divId = "DIV_ITEM_" + String.fromCharCode(64 + i);
        //    $("#" + divId).hide();
        //}

        //$("#DIV_ITEM_" + itemType).show();
    }

    function onDataBoundItemCode(e) {
        var comboBox = this;
        var selectedValue = comboBox.value();

        if (selectedValue !== null && selectedValue !== undefined && selectedValue != "") {
            var dataItem = comboBox.dataItem();
            RefreshDivs(dataItem);
        }
    }

    function RefreshGridItemExtn() {
        $("#gridItemExtn").data("kendoGrid").dataSource.read();
    }

    function DataItemExtnGrid() {
        return {
            cardId: $("#Id").val(),
            accountCode: $("#AccountCode").val()
        };
    }

    function onSaveGridItemExtn(e) {
        var grid = e.sender;
        var model = $("#gridItemExtn").data().kendoGrid.editable.options.model;

        for (var key in e.values) {
            if (e.values.hasOwnProperty(key)) {
                if (key == "ItemValue") {
                    model.set('ItemValue', e.values[key]);
                } else {
                    model.set('Sequence', e.values[key]);
                }
            }
        }

        // Create an array to store the row values
        var rowValues = [];
        var gridData = e.sender.dataSource.data();
        var sortDescriptor = e.sender.dataSource.sort(); // Retrieve the current sort descriptor

        // Sort the data manually using the sort descriptor
        gridData.sort(function (a, b) {
            // Sort the data items first by 'ItemKey', then by 'Sequence'
            if (a.Sequence === b.Sequence) {
                return a.ItemNo.localeCompare(b.ItemNo);
            }
            return a.Sequence - b.Sequence;
        });


        for (var i = 0; i < gridData.length; i++) {
            var rowData = gridData[i];

            if (rowData.ItemNo == 1) {
                $("#ItemName").val(rowData.ItemValue).trigger('change');
                //} else {
                //    rowValues.push(rowData.ItemValue);
            }

            rowValues.push(rowData.ItemValue);
        }

        var description = rowValues.join(" ");
        $("#Description").val(description.trim()).trigger('change');
    }

    // END CARD TEMPLATE


    // GRID ITEMS

    function onDetailExpand(e) {
        // Find the corresponding row in the main grid and select it
        var mainGridRow = $(e.sender.wrapper).closest("tr").prev();
        var mainGrid = mainGridRow.closest("[data-role=grid]").data("kendoGrid");
        if (mainGrid) { mainGrid.select(mainGridRow); }

        var dataItem = this.dataItem(e.masterRow); // Get the data item for the current row
        var templateId = null;

        $.ajax({
            type: "POST",
            url: '' + propertyCardConfig.urls.PropertyCard_GetItemExtnTemplate + '',
            data: { id: dataItem.TransferId},
            success: function (result) {
                if (result.Errors == "") {
                    templateId = result.ItemExtnName;
                    // Get the template content and render it
                    var templateHtml = $("#" + templateId).html();
                    var template = kendo.template(templateHtml);

                    // Insert the rendered template into the detail cell
                    e.detailRow.find(".k-detail-cell").html(template(dataItem));
                } else {
                    notifyWarning(result.Errors);
                    return false;
                }
            },
            error: function (req, status, errorObj) {
                notifyWarning(ajaxFailureMessage(req, "Request failed."));
                return false;
            }
        });

    }

    function onRequestEndGridItems(e) {
        if (e.response && !e.response.Errors && !e.response.errors) {
            if (e.type == "destroy" || e.type == "create" || e.type == "update") {

                $("#gridItems_" + selectedId).data("kendoGrid").dataSource.read();

            }
            if (e.type == "create") {
                var responseData = e.response;
                var newRecord = responseData.Data[0];
                selectedItemId = newRecord.Id;
                selectedTransferId = newRecord.TransferId;
            }
        }
    }

    function onExpandGridItems(e) {
        // Find the corresponding row in the main grid and select it
        var mainGridRow = $(e.sender.wrapper).closest("tr").prev();
        var mainGrid = mainGridRow.closest("[data-role=grid]").data("kendoGrid");
        if (mainGrid) { mainGrid.select(mainGridRow); }
    }

    function onEditGridItems(e) {
        e.preventDefault();
        selectedGrid = e.sender;

        //the master row for this detail
        var parentRow = this.wrapper.closest("tr").prev();
        var parentGrid = parentRow.closest("[data-role=grid]").data("kendoGrid");
        var parentModel = parentGrid ? parentGrid.dataItem(parentRow) : pcWorkspaceParent(this);
        if (parentGrid) { parentGrid.select(parentRow); }

        if (e.model.isNew()) {
            var row = e.sender.tbody.find("tr:first");
            this.select(row);
            selectedItemId = null;
            selectedTransferId = null;
            e.model.set("PsCardId", parentModel.Id);
        } else {
            selectedItemId = e.model.get("Id");
            selectedTransferId = e.model.get("TransferId");
            //console.log(e.sender);
            //console.log(e.container);

            // Selecting the row in the subgrid using e.model
            var row = e.sender.tbody.find("tr[data-uid='" + e.model.uid + "']");
            e.sender.select(row);
        }

        var popupWindow = e.container.data("kendoWindow");

        if (popupWindow) {
            // Handle the Cancel button click separately
            setTimeout(function () {
                e.container.find(".k-grid-cancel").off("click").on("click", function (event) {
                    event.preventDefault();
                    event.stopImmediatePropagation();
                    epsmsConfirm({
                        title: "Discard Unsaved Changes",
                        type: "warning",
                        headline: "Discard Unsaved Changes?",
                        message: "Are you sure you want to close this form? Any unsaved changes will be lost.",
                        cancelText: "Keep Editing",
                        confirmText: "Discard Changes",
                        confirmIcon: "k-i-close",
                        onConfirm: function (dialog) {
                            dialog.close();
                            popupWindow.close();
                        }
                    });
                });
            });
        }
    }

    function onSaveGridItems(e) {
        var container = e.container;
        console.log(e.container);
        var fieldsContainer = $('#DIV_ITEMFIELDS');

        kendo.unbind(fieldsContainer); // Unbind the fields
        kendo.bind(fieldsContainer, e.model); // Re-bind the fields to update the model
    }

    function onChangeGridItems(e) {
        var selectedData = this.dataItem(this.select());
        selectedItemId = selectedData.Id;
        selectedTransferId = selectedData.TransferId;
        selectedIssuedId = null;
    }

    function onDataBoundGridItems(e) {
        if (selectedItemId) {
            var dataItem = this.dataSource.get(selectedItemId);
            if (dataItem) {
                this.select($('[data-uid=' + dataItem.uid + ']'));
            }
        } else {
            //var row = e.sender.tbody.find("tr:first");
            //this.select(row);

            //var firstRow = this.tbody.find("tr.k-master-row").first();
            //this.expandRow(firstRow);

            // Attach a handler to the "detailExpand" event
            this.bind("detailExpand", function (e) {
                // Select the expanded master row
                this.select(e.masterRow);
            });
        }

        var grid = this;
        grid.tbody.find("tr").each(function () {
            var dataItem = grid.dataItem(this);

            if (IsPosted(dataItem)) {
                $(this).find('.k-grid-Delete, .k-grid-delete').hide();
            }
        });
    }

    function IsPosted(dataItem) {
        // Return true if ForPosting is "Y", otherwise false
        return !!dataItem && dataItem.PostedDt != null;
    }

    function IsNotPosted(dataItem) {
        return !IsPosted(dataItem);
    }

    function onRemoveGridItems(e) {
        //e.preventDefault(); not deleteng when code is actie
        selectedGrid = e.sender;

        //the master row for this detail
        var parentRow = this.wrapper.closest("tr").prev();
        var parentGrid = parentRow.closest("[data-role=grid]").data("kendoGrid");
        var parentModel = parentGrid ? parentGrid.dataItem(parentRow) : pcWorkspaceParent(this);
        if (parentGrid) { parentGrid.select(parentRow); }
    }

    function onClickBtnPrintPo(e) {
        e.preventDefault();
        //the master row for this detail
        var parentRow = this.wrapper.closest("tr").prev();
        var parentGrid = parentRow.closest("[data-role=grid]").data("kendoGrid");

        if (parentGrid) { parentGrid.select(parentRow); }

        // the current row for this detail
        selectedGrid = this;
        var tr = $(e.currentTarget).closest("tr");
        var dataItem = selectedGrid.dataItem(tr);
        var rowitem = selectedGrid.dataSource.get(dataItem.TransferId);
        selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));

        var dataItem = this.dataItem($(e.currentTarget).closest("tr"));
        selectedItemId = dataItem.Id;

        PrintPo(1);
    }

    function onClickBtnPrintPoUpdate(e) {
        e.preventDefault();
        //the master row for this detail
        var parentRow = this.wrapper.closest("tr").prev();
        var parentGrid = parentRow.closest("[data-role=grid]").data("kendoGrid");

        if (parentGrid) { parentGrid.select(parentRow); }

        // the current row for this detail
        selectedGrid = this;
        var tr = $(e.currentTarget).closest("tr");
        var dataItem = selectedGrid.dataItem(tr);
        var rowitem = selectedGrid.dataSource.get(dataItem.TransferId);
        selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));

        var dataItem = this.dataItem($(e.currentTarget).closest("tr"));
        selectedItemId = dataItem.Id;
        PrintPo(0);
    }

    function PrintPo(originalSw) {
        var url = '' + propertyCardConfig.urls.StockCard_StockCardPoRpt + '?selectedItemId=' + selectedItemId + '&originalSw=' + originalSw;

        $("#windowcontainer").append("<div id='windowPrint'></div>");
        var mywindow = $("#windowPrint")
                .kendoWindow({
                    actions: ["Maximize", "Minimize", "Close"],
                    resizable: true,
                    draggable: true,
                    modal: true,
                    visible: false,
                    iframe: true,
                    deactivate: function () {
                        this.destroy();
                    }
                }).data("kendoWindow");
        mywindow.setOptions({
            title: "Preview",
            width: 1200,
            height: 700
        });
        mywindow.refresh({
            url: url
        });
        mywindow.center();
        mywindow.open();
    }

    function onClickBtnPostItem(e) {
        e.preventDefault();

        //the master row for this detail
        var parentRow = this.wrapper.closest("tr").prev();
        var parentGrid = parentRow.closest("[data-role=grid]").data("kendoGrid");

        if (parentGrid) { parentGrid.select(parentRow); }

        // the current row for this detail
        selectedGrid = this;
        var tr = $(e.currentTarget).closest("tr");
        var dataItem = selectedGrid.dataItem(tr);
        var rowitem = selectedGrid.dataSource.get(dataItem.TransferId);
        selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));

        var dataItem = this.dataItem($(e.currentTarget).closest("tr"));
        selectedItemId = dataItem.Id;

        postItem(e.currentTarget);
    }

    function onClickBtnUnPostItem(e) {
        e.preventDefault();

        //the master row for this detail
        var parentRow = this.wrapper.closest("tr").prev();
        var parentGrid = parentRow.closest("[data-role=grid]").data("kendoGrid");

        if (parentGrid) { parentGrid.select(parentRow); }

        // the current row for this detail
        selectedGrid = this;
        var tr = $(e.currentTarget).closest("tr");
        var dataItem = selectedGrid.dataItem(tr);
        var rowitem = selectedGrid.dataSource.get(dataItem.TransferId);
        selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));

        var dataItem = this.dataItem($(e.currentTarget).closest("tr"));
        selectedItemId = dataItem.Id;

        unpostItem(e.currentTarget);
    }

    function postItem(btn) {
        epsmsConfirm({
            title: "Post Acquisition Record",
            type: "info",
            headline: "Post Acquisition Record?",
            entityLabel: "Record",
            entityValue: "Selected Item",
            message: "Once posted, this acquisition record will be officially entered into the property ledger.",
            cancelText: "Cancel",
            confirmText: "Post",
            confirmIcon: "k-i-check",
            onConfirm: function (dialog, $confirmBtn) {
                setClickedButtonBusy($confirmBtn, "Posting...");
                if (!propertyCardButtonStart(btn, "Posting...")) { return; }

                $.ajax({
                    type: "POST",
                    url: '' + propertyCardConfig.urls.StockCard_PostItemRecord + '',
                    data: { psCardItemId: selectedItemId },
                    async: true
                }).done(function (result) {
                    restoreClickedButton($confirmBtn);
                    dialog.close();
                    if (!result || result.Errors == null || result.Errors === "") {
                        notifySuccess("Acquisition record posted.");
                        var g = $("#gridItems_" + selectedId).data("kendoGrid");
                        if (g) { g.dataSource.read(); }
                    } else {
                        notifyWarning(result.Errors);
                    }
                }).fail(function (req) {
                    restoreClickedButton($confirmBtn);
                    notifyWarning(ajaxFailureMessage(req, "Unable to post acquisition record."));
                }).always(function () {
                    propertyCardButtonStop(btn);
                });
            }
        });
    }

    function unpostItem(btn) {
        epsmsConfirm({
            title: "Unpost Acquisition Record",
            type: "warning",
            headline: "Unpost Acquisition Record?",
            entityLabel: "Record",
            entityValue: "Selected Item",
            message: "This will return the acquisition record to unposted status.",
            cancelText: "Cancel",
            confirmText: "Unpost",
            confirmIcon: "k-i-undo",
            onConfirm: function (dialog, $confirmBtn) {
                setClickedButtonBusy($confirmBtn, "Unposting...");
                if (!propertyCardButtonStart(btn, "Unposting...")) { return; }

                $.ajax({
                    type: "POST",
                    url: '' + propertyCardConfig.urls.StockCard_UnpostItemRecord + '',
                    data: { psCardItemId: selectedItemId },
                    async: true
                }).done(function (result) {
                    restoreClickedButton($confirmBtn);
                    dialog.close();
                    if (!result || result.Errors == null || result.Errors === "") {
                        notifySuccess("Acquisition record unposted.");
                        var g = $("#gridItems_" + selectedId).data("kendoGrid");
                        if (g) { g.dataSource.read(); }
                    } else {
                        notifyWarning(result.Errors);
                    }
                }).fail(function (req) {
                    restoreClickedButton($confirmBtn);
                    notifyWarning(ajaxFailureMessage(req, "Unable to unpost acquisition record."));
                }).always(function () {
                    propertyCardButtonStop(btn);
                });
            }
        });
    }


    function onClickBtnAddItem(e) {
        e.preventDefault();

        selectedGrid = this;
        // the master row for this detail
        var parentRow = this.wrapper.closest("tr").prev();
        var parentGrid = parentRow.closest("[data-role=grid]").data("kendoGrid");
        if (parentGrid) { parentGrid.select(parentRow); }

        StockCardItem("A", null);
    }

    function onClickBtnEditItem(e) {
        e.preventDefault();

        //the master row for this detail
        var parentRow = this.wrapper.closest("tr").prev();
        var parentGrid = parentRow.closest("[data-role=grid]").data("kendoGrid");

        if (parentGrid) { parentGrid.select(parentRow); }

        // the current row for this detail
        selectedGrid = this;
        var tr = $(e.currentTarget).closest("tr");
        var dataItem = selectedGrid.dataItem(tr);
        var rowitem = selectedGrid.dataSource.get(dataItem.TransferId);
        selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));

        var dataItem = this.dataItem($(e.currentTarget).closest("tr"));
        StockCardItem("E", dataItem.Id);
    }

    function StockCardItem(mode, recId) {
        var myTitle = (mode == "A") ? "Card Item - ADD" : "Card Item - EDIT";
        var width = 800;
        $("#windowcontainer").append("<div id='windowCard'></div>");
        var mywindow = $("#windowCard")
                .kendoWindow({
                    actions: ["Minimize", "Close"],
                    draggable: true,
                    modal: true,
                    visible: false,
                    close: function () {
                        $("#gridItems_" + selectedId).data("kendoGrid").dataSource.read();
                    },
                    deactivate: function () {
                        this.destroy();
                    }
                }).data("kendoWindow");
        mywindow.setOptions({
            title: myTitle,
            width: width
        });
        mywindow.refresh({
            url: '' + propertyCardConfig.urls.PropertyCard__PropertyCardItemAddEdit + '',
            data: {
                cardId: selectedId,
                cardrItemId: recId
            },
            // Specify a callback function to execute after content is loaded
            complete: function () {
                // Resize the window to fit its content
                mywindow.resize();
                mywindow.center();
            }
        });
        mywindow.open();
    }

    function onClickTransfer(e) {
        e.preventDefault();

        //the master row for this detail
        var parentRow = this.wrapper.closest("tr").prev();
        var parentGrid = parentRow.closest("[data-role=grid]").data("kendoGrid");

        if (parentGrid) { parentGrid.select(parentRow); }

        // the current row for this detail
        selectedGrid = this;
        var tr = $(e.currentTarget).closest("tr");
        var dataItem = selectedGrid.dataItem(tr);
        var rowitem = selectedGrid.dataSource.get(dataItem.TransferId);
        selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));

        var dataItem = this.dataItem($(e.currentTarget).closest("tr"));
        selectedItemId = dataItem.Id;

        var myTitle = "Transfer PO Item to another Stock/Property Card";
        var width = 1000;
        $("#windowcontainer").append("<div id='windowCard'></div>");
        var mywindow = $("#windowCard")
                .kendoWindow({
                    actions: ["Minimize", "Close"],
                    draggable: true,
                    modal: true,
                    visible: false,
                    close: function () {
                        $("#gridItems_" + selectedId).data("kendoGrid").dataSource.read();
                    },
                    deactivate: function () {
                        this.destroy();
                    }
                }).data("kendoWindow");
        mywindow.setOptions({
            title: myTitle,
            width: width
        });
        mywindow.refresh({
            url: '' + propertyCardConfig.urls.StockCard__PoTransfer + '',
            data: {
                psCardItemId: selectedItemId
            },
            // Specify a callback function to execute after content is loaded
            complete: function () {
                // Resize the window to fit its content
                mywindow.resize();
                mywindow.center();
            }
        });
        mywindow.open();
    }

    function onSelectPsNo(e) {
        if (!e.item) {
            return;
        }

        var dataItem = this.dataItem(e.item.index());

        if (dataItem) {
            $("#Id").val(dataItem.Id).trigger("change");
            $("#Fund").val(dataItem.Fund).trigger("change");
            $("#Account").val(dataItem.Account).trigger("change");
            $("#SubAccount1").val(dataItem.SubAccount1).trigger("change");
            $("#SubAccount2").val(dataItem.SubAccount2).trigger("change");
            $("#SubAccount3").val(dataItem.SubAccount3).trigger("change");
            $("#SubAccount4").val(dataItem.SubAccount4).trigger("change");
            $("#Article").val(dataItem.Article).trigger("change");
        }
    }
    // END  GRID ITEMS

    // GRID ISSUANCE
    var selectedIssuedId = null;
    function onChangeGridIssuance(e) {
        var selectedData = this.dataItem(this.select());
        selectedIssuedId = selectedData.Id;
    }

    function onDataBoundGridIssuance(e) {
        e.preventDefault();

        if (selectedIssuedId) {
            var dataItem = this.dataSource.get(selectedIssuedId);
            if (dataItem) {
                this.select($('[data-uid=' + dataItem.uid + ']'));
            }
        } else {
            var row = e.sender.tbody.find("tr:first");
            this.select(row);

            this.bind("detailExpand", function (e) {
                // Select the expanded master row
                this.select(e.masterRow);
            });
        }
    }

    function onEditGridIssuance(e) {
        e.preventDefault();
        selectedGrid = e.sender;

        //the master row for this detail
        var parentRow = this.wrapper.closest("tr").prev();
        var parentGrid = parentRow.closest("[data-role=grid]").data("kendoGrid");
        var parentModel = parentGrid ? parentGrid.dataItem(parentRow) : pcWorkspaceParent(this);
        if (parentGrid) { parentGrid.select(parentRow); }

        if (e.model.isNew()) {
            var row = e.sender.tbody.find("tr:first");
            this.select(row);
            selectedIssuedId = null;
            //e.model.set("PsCardItemId", selectedItemId);
            e.model.set("PsCardItemId", parentModel.Id);
            e.model.set("UnitCost", parentModel.UnitCost);
        } else {
            selectedIssuedId = e.model.get("Id");
            var rowitem = selectedGrid.dataSource.get(selectedIssuedId);
            selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));
        }
    }

    function onRemoveGridIssuance(e) { // triggers after confirm
        //e.preventDefault();
        selectedGrid = e.sender;

        //the master row for this detail
        var parentRow = this.wrapper.closest("tr").prev();
        var parentGrid = parentRow.closest("[data-role=grid]").data("kendoGrid");
        var parentModel = parentGrid ? parentGrid.dataItem(parentRow) : pcWorkspaceParent(this);
        if (parentGrid) { parentGrid.select(parentRow); }
    }

    function onGridIssuanceDeleteClick(e) {
        e.preventDefault();
        selectedGrid = this;

        var tr = $(e.currentTarget).closest("tr"); var dataItem = this.dataItem(tr);
        selectedIssuedId = dataItem.Id;
        var rowitem = selectedGrid.dataSource.get(selectedIssuedId);
        selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));
        selectedGrid.removeRow(tr);
    }

    function onRequestEndGridIssuance(e) {
        if (e.response && !e.response.Errors && !e.response.errors) {
            if (e.type == "create") {
                var responseData = e.response;
                var newRecord = responseData.Data[0];
                selectedIssuedId = newRecord.Id;
            }
            if (e.type == "destroy" || e.type == "create" || e.type == "update") {
               var grid = $("#gridItems_" + selectedId).data("kendoGrid");

                // Trigger a read operation on the data source.
                grid.dataSource.read().then(function () {
                    // Additional actions to be performed after the read operation.

                    // Find the row with the specified Id.
                    var dataItem = grid.dataSource.get(selectedItemId);
                    //console.log(selectedItemId);
                    //console.log(dataItem);
                    // If the row is found, expand it.
                    if (dataItem) {
                        var row = grid.tbody.find("tr[data-uid='" + dataItem.uid + "']");
                        grid.expandRow(row);
                    }
                });
            }
        }
    }

    function changeAmount(e) {
        var qty = $("#Qty").data("kendoNumericTextBox").value();
        var unitCost = $("#UnitCost").val();
        var amount = qty * unitCost;

        $("#Amount").data("kendoNumericTextBox").value(amount);
        $("#Amount").data("kendoNumericTextBox").trigger("change");
    }

    // END GRID ISSUANCE

    function UpdateDescription(e)
    {
        setTimeout(function () {
            var disabled = $("#form").find(':input:disabled').removeAttr('disabled');
            var data = $("#form").serialize();

            $.ajax({
                type: "POST",
                url: '' + propertyCardConfig.urls.PropertyCard_GetDescription + '',
                data: data,
                success: function (result) {
                    $("#Description").val(result.Description).trigger("change");
                    $("#PsNo").val(result.StockNo).trigger("change");
                    $("#SelectedId").val(result.Id).trigger("change");
                    $("#IsDuplicateStockNo").val(result.IsDuplicateStockNo).trigger("change");
                    disabled.attr('disabled', 'disabled');
                },
                error: function (req, status, errorObj) {
                    notifyWarning(ajaxFailureMessage(req, "Request failed."));
                }
            });
        }, 500);
    }

    function selectRowById() {
        var grid = $("#grid").data("kendoGrid");
        var recordId = $("#SelectedId").val();

        function searchPage(pageNumber, totalPages) {
            grid.dataSource.page(pageNumber); // Set the current page
            grid.dataSource.fetch(function () {
                // Get the current page data
                var data = grid.dataSource.view();

                // Search for the record in the current page
                var dataItem = data.find(item => item.Id === recordId); // Replace "Id" with your actual ID field
                if (dataItem) {
                    // If found, select the row
                    var row = grid.table.find("tr[data-uid='" + dataItem.uid + "']");
                    grid.select(row);

                    // Restore the original page if necessary (optional)
                    //grid.dataSource.page(currentPage);
                    return;
                }

                // If not found, move to the next page
                if (pageNumber < totalPages) {
                    searchPage(pageNumber + 1, totalPages); // Recursive call
                    //} else {
                    //    alert("Record not found in the grid.");
                }
            });
        }

        var currentPage = grid.dataSource.page(); // Save the current page
        var totalPages = grid.dataSource.totalPages(); // Get total number of pages

        // Start searching from the first page
        searchPage(1, totalPages);
    }

    // Helper function to select a row
    function selectRow(grid, dataItem) {
        var row = grid.table.find("tr[data-uid='" + dataItem.uid + "']");
        notifyWarning("Unable to locate record row.");
        grid.select(row); // Select the row
    }

    function DataOfficerId() {
        var text = $("#OfficerId").data("kendoMultiColumnComboBox").text();
        if (text == null || text == "") {
            text = $("#OfficerId").data("kendoMultiColumnComboBox").value();
        }
        return {
            deptId: $("#LocationId").val(),
            text: text
        };
    }

    function UpdateBalance(e)
    {
        var qty = $("#Qty").data("kendoNumericTextBox").value();
        var qtyIss = $("#QtyIss").data("kendoNumericTextBox").value();
        var transferIn = $("#TransferIn").data("kendoNumericTextBox").value();
        var transferOut = $("#TransferOut").data("kendoNumericTextBox").value();
        var unitCost = $("#UnitCost").data("kendoNumericTextBox").value();
        var addCost = $("#AddCost").data("kendoNumericTextBox").value();

        qty = qty == null ? 0 : qty;
        qtyIss = qtyIss == null ? 0 : qtyIss;
        transferIn = transferIn == null ? 0 : transferIn;
        transferOut = transferOut == null ? 0 : transferOut;
        unitCost = unitCost == null ? 0 : unitCost;
        addCost = addCost == null ? 0 : addCost;

        var balance = (qty + transferIn) - (qtyIss + transferOut);
        var amount = (qty + transferIn) * unitCost;
        var tUnitCost = unitCost + addCost;
        var gTotalCost = (qty + transferIn) * tUnitCost;

        $("#QtyBal").data("kendoNumericTextBox").value(balance);
        $("#Amount").data("kendoNumericTextBox").value(amount);
        $("#TUnitCost").data("kendoNumericTextBox").value(tUnitCost);
        $("#GTotalCost").data("kendoNumericTextBox").value(gTotalCost);

        $("#QtyBal").data("kendoNumericTextBox").trigger("change");
        $("#Amount").data("kendoNumericTextBox").trigger("change");
        $("#TUnitCost").data("kendoNumericTextBox").trigger("change");
        $("#GTotalCost").data("kendoNumericTextBox").trigger("change");
    }

    function onSelectDeptId(e) {
        var dataItem = this.dataItem(e.item.index());

        $("#DeptDisplay").val(dataItem.Description).trigger("change");
    }

    //function onSelectLocationId(e) {
    //    var dataItem = this.dataItem(e.item.index());

    //    $("#Location").val(dataItem.Description).trigger("change");
    //}

    function onSelectLocationId(e) {
        var dataItem = this.dataItem(e.item.index());

        $("#LocationCode").val(dataItem.Code).trigger("change");
        if (dataItem.Desc3 == dataItem.Desc2) {
            $("#Location").val(dataItem.Desc2).trigger("change");
        }
        else {
            $("#Location").val(dataItem.Desc3 + "/" + dataItem.Desc2).trigger("change");
        }
    }

    function onDataBoundLocationId(e) {
        var comboBox = this;
        var dataItem = comboBox.dataItem();

        if (!dataItem) return; // No item selected, skip

        // Mimic the same logic as in the select event
        $("#LocationCode").val(dataItem.Code).trigger("change");

        if (dataItem.Desc3 == dataItem.Desc2) {
            $("#Location").val(dataItem.Desc2).trigger("change");
        } else {
            $("#Location").val(dataItem.Desc3 + "/" + dataItem.Desc2).trigger("change");
        }
    }

    function onChangeOpt(e) {
        var opt1 = $("[id='IssuedToSw_1']").is(":checked");
        var opt2 = $("[id='IssuedToSw_2']").is(":checked");
        //if (opt1 == true) {
        //    $("#OptDept").removeClass("hidden");
        //}
        //if (opt2 == true) {
        //    $("#OptDept").addClass("hidden");
        //}
    }

    function RefreshItemDivs() {
        var fieldsContainer = $('#DIV_ITEMFIELDS');

        fieldsContainer.empty(); // Clear existing content

        var url = '' + propertyCardConfig.urls.PropertyCard_LoadItemFields + '';
        var data = $("#form").serialize();

        $.ajax({
            url: url,
            type: 'POST',
            data: data,
            success: function(data) {
                fieldsContainer.html(data);
                resizeWindow();
            }
        });
    }

    function DataFpp() {
        var text = $("#FPP").data("kendoMultiColumnComboBox").text();
        return {
            deptId: $("#DeptId").val(),
            text: text
        };
    }

    function onCloseCardTemplate(e) {
        e.preventDefault();
        var win = this;
        epsmsConfirm({
            title: "Discard Unsaved Changes",
            type: "warning",
            headline: "Discard Unsaved Changes?",
            message: "Are you sure you want to close this form? Any unsaved changes will be lost.",
            cancelText: "Keep Editing",
            confirmText: "Discard Changes",
            confirmIcon: "k-i-close",
            onConfirm: function (dialog) {
                dialog.close();
                win.unbind("close");
                win.close();
            }
        });
    }

    function onOpenPopupTemplate(e) {
        var popupWindow = $(".k-window-content.k-popup-edit-form").data("kendoWindow");

        if (popupWindow) {
            popupWindow.unbind("close");
            popupWindow.bind("close", function (e) {
                e.preventDefault();
                var win = this;
                epsmsConfirm({
                    title: "Discard Unsaved Changes",
                    type: "warning",
                    headline: "Discard Unsaved Changes?",
                    message: "Are you sure you want to close this form? Any unsaved changes will be lost.",
                    cancelText: "Keep Editing",
                    confirmText: "Discard Changes",
                    confirmIcon: "k-i-close",
                    onConfirm: function (dialog) {
                        dialog.close();
                        win.unbind("close");
                        win.close();
                    }
                });
            });
        }
    }
    // END CARD ITEM TEMPLATE


    // GRID ITEMEXTN
    var selectedItemExtnId = null;
    function onChangeGridItemExtn(e) {
        var selectedData = this.dataItem(this.select());
        selectedItemExtnId = selectedData.Id;
    }

    function onDataBoundGridItemExtn(e) {
        e.preventDefault();

        var grid = e.sender;
        var parentId = grid.element.closest("[data-parent-id]").data("parent-id");

        if (parentId !== undefined && parentId !== null) {
            grid.wrapper.find(".k-grid-add").hide();
        }

        if (selectedItemExtnId) {
            var dataItem = this.dataSource.get(selectedItemExtnId);
            if (dataItem) {
                this.select($('[data-uid=' + dataItem.uid + ']'));
            }
        } else {
            var row = e.sender.tbody.find("tr:first");
            this.select(row);
        }

        this.bind("detailExpand", function (e) {
            // Select the expanded master row
            this.select(e.masterRow);
        });
    }

    var selectedItemExtnQty = null;
    function onEditGridItemExtn(e) {
        e.preventDefault();
        selectedGrid = e.sender;

        //the master row for this detail
        var parentRow = this.wrapper.closest("tr").prev();
        var parentGrid = parentRow.closest("[data-role=grid]").data("kendoGrid");
        var parentModel = parentGrid ? parentGrid.dataItem(parentRow) : pcWorkspaceParent(this);
        if (parentGrid) { parentGrid.select(parentRow); }

        if (e.model.isNew()) {
            var row = e.sender.tbody.find("tr:first");
            this.select(row);
            selectedItemExtnId = null;
            selectedItemExtnQty = parentModel ? parentModel.QtyBal : null;
            if (parentModel) {
                e.model.set("PsCardItemId", parentModel.Id);
                e.model.set("TransferId", parentModel.TransferId);
                e.model.set("LocationId", parentModel.LocationId);
                e.model.set("Location", parentModel.Location);
                e.model.set("TContentNo", parentModel.TContentNo);
            }
        } else {
            selectedItemExtnId = e.model.get("Id");
            var rowitem = selectedGrid.dataSource.get(selectedItemExtnId);
            if (rowitem) { selectedGrid.select($('[data-uid=' + rowitem.uid + ']')); }
            if (e.model.CanEdit === false) {
                var win = e.container.data("kendoWindow");
                if (win) { win.title("View Individual Unit (Accountable - Read Only)"); }
                e.container.find(".k-grid-update").hide();
                e.container.find("input, textarea, select").prop("readonly", true);
            }
        }
    }

    function onRemoveGridItemExtn(e) { // triggers after confirm
        //e.preventDefault();
        selectedGrid = e.sender;

        //the master row for this detail
        var parentRow = this.wrapper.closest("tr").prev();
        var parentGrid = parentRow.closest("[data-role=grid]").data("kendoGrid");
        var parentModel = parentGrid ? parentGrid.dataItem(parentRow) : pcWorkspaceParent(this);
        if (parentGrid) { parentGrid.select(parentRow); }
    }

    function onGridItemExtnDeleteClick(e) {
        e.preventDefault();
        selectedGrid = this;

        var tr = $(e.currentTarget).closest("tr"); var dataItem = this.dataItem(tr);
        selectedItemExtnId = dataItem.Id;
        var rowitem = selectedGrid.dataSource.get(selectedItemExtnId);
        selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));
        selectedGrid.removeRow(tr);
    }

    function onRequestEndGridItemExtn(e) {
        if (e.response && !e.response.Errors && !e.response.errors) {
            if (e.type == "create") {
                var responseData = e.response;
                var newRecord = responseData.Data[0];
                selectedItemExtnId = newRecord.Id;
            }
            if (e.type == "destroy" || e.type == "create" || e.type == "update") {

                var grid = $("#gridItems_" + selectedId).data("kendoGrid");

                // Trigger a read operation on the data source.
                grid.dataSource.read().then(function () {
                    // Additional actions to be performed after the read operation.

                    // Find the row with the specified Id.
                    var dataItem = grid.dataSource.get(selectedItemId);
                    // If the row is found, expand it.
                    if (dataItem) {
                        var row = grid.tbody.find("tr[data-uid='" + dataItem.uid + "']");
                        grid.expandRow(row);
                    }
                });
            }
        }
    }

    function UpdateEndSeries()
    {
        var begSeries = $("#BegSerial").val();
        var itemId = $("#PsCardItemId").val();
        var url = '' + propertyCardConfig.urls.StockCard_GetEndSeries + '';
        $.ajax({
            url: url,
            type: 'GET',
            data: { startSeries: begSeries, itemId: itemId},
            success: function(result) {
                if (result.Errors == "")
                {
                    $("#EndSerial").val(result.EndSeries).trigger("change");
                }
                else
                {
                    notifyWarning(result.Errors);
                }
            }
        });
    }
    // END GRID ITEMEXTN

    // GRID IMAGES
    function onError(e) {
        // invalid because the responseText is a raw string, not JSON
        //var err = $.parseJSON(e.XMLHttpRequest.responseText);
        var err = e.XMLHttpRequest.responseText;
        notifyWarning(err);
    };

    function GridUploadError(e) {
        if (e.errors) {
            //// Get the parent grid
            var parentGrid = $("#gridItems").data("kendoGrid");

            // Try to find the closest grid row
            var row = $(e.target).closest("tr[data-uid]"); // Look for the row with a data-uid attribute

            // Log the row for debugging
            console.log("Row element:", row);

            // Check if a valid row was found
            if (row.length === 0) {
                console.warn("No valid row found. The event target may not be within a grid row.");

                // Provide a fallback: Try getting the parent row from a specific known container
                var knownContainer = $("#gridItems"); // Replace with your grid's container
                row = knownContainer.find("tr[data-uid]"); // Grab any row as a fallback

                if (row.length === 0) {
                    console.warn("No rows found in the parent grid.");
                    return; // Exit if no rows are found
                } else {
                    console.warn("Fallback: Found rows in the parent grid but not the specific one.");
                }
            }

            // Get the dataItem for the parent grid
            var dataItem = parentGrid.dataItem(row);

            // Log the retrieved dataItem
            if (!dataItem) {
                // Get the index of the row in the parent grid
                var rowIndex = row.index();
                console.log("Row index:", rowIndex);

                // Attempt to access dataItem using the dataSource view
                dataItem = parentGrid.dataSource.view()[rowIndex];

                // Log the data item retrieved via index
                console.log("Data Item found using index:", dataItem);
            }


            // Find the subgrid and refresh it
            var subgridName = "gridImages_" + dataItem.GroupId; // Adjust based on how you identify the subgrid
            var subgrid = $("#" + subgridName).data("kendoGrid");

            if (subgrid) {
                subgrid.dataSource.read(); // Refresh the subgrid
            }

            $.each(e.errors, function (propertyName) {
                if (propertyName == "DeleteError" || propertyName == "AddError" || propertyName == "UpdateError") {
                    notifyWarning(this.errors);
                }
            });
        }
    }

    var selectedGridImagesItem = null;
    function onEditGridImages(e) {
        e.preventDefault();

        // the master row for this detail
        var parentRow = this.wrapper.closest("tr").prev();
        var parentGrid = parentRow.closest("[data-role=grid]").data("kendoGrid");
        //var parentModel = parentGrid ? parentGrid.dataItem(parentRow) : pcWorkspaceParent(this);
        if (parentGrid) { parentGrid.select(parentRow); }

        selectedGrid = e.sender;
        if (e.model.isNew()) {
            var row = e.sender.tbody.find("tr:first");
            this.select(row);
            e.model.set("ImageId", propertyCardConfig.imageId);
        } else {
            selectedGridImagesItem = e.model.get("Id");
            var rowitem = selectedGrid.dataSource.get(selectedGridImagesItem);
            selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));
        }
    }

    function onDataBoundGridImages(e) {
        if (selectedGridImagesItem) {
            var dataItem = this.dataSource.get(selectedGridImagesItem);
            if (dataItem) {
                this.select($('[data-uid=' + dataItem.uid + ']'));
            }
        } else {
            var row = e.sender.tbody.find("tr:first");
            this.select(row);
        }
    }

    function onDeleteGridImages(e) {
        e.preventDefault();
        selectedGrid = this;
        var tr = $(e.currentTarget).closest("tr");
        var dataItem = selectedGrid.dataItem(tr);
        var rowitem = selectedGrid.dataSource.get(dataItem.Id);
        selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));
        setTimeout(function () { selectedGrid.removeRow(tr); }, 10);
    }

    function onClickUpload(e) {
        var button = e.sender.element; // Use the sender's element;
        // Retrieve the grid name from the data attribute
        var gridName = $(button).data("gridname");
        selectedGrid = $("#" + gridName).data("kendoGrid");
        // the master row for this detail
        var parentRow = this.wrapper.closest("tr").prev();
        var parentGrid = parentRow.closest("[data-role=grid]").data("kendoGrid");
        var parentModel = parentGrid ? parentGrid.dataItem(parentRow) : pcWorkspaceParent(this);
        var imageId = parentModel.GroupId;

        var myUrl = '' + propertyCardConfig.urls.CardUpload__ImagesAdd + '?imageId=' + imageId;
        var myTitle = "Upload Images/Documents";
        var width = 800;
        var height = 650;

        $("#windowcontainer").append("<div id='windowUpload'></div>");
        var mywindow = $("#windowUpload")
                .kendoWindow({
                    actions: ["Minimize", "Maximize", "Close"],
                    resizable: true,
                    draggable: true,
                    modal: true,
                    visible: false,
                    deactivate: function () {
                        this.destroy();
                    }
                }).data("kendoWindow");

        mywindow.setOptions({ title: myTitle, width: width, height: height });
        mywindow.refresh({ url: myUrl });
        mywindow.center();
        mywindow.open();
    }

    function UploadPara(e) {
        var data = JSON.stringify({ model: $("#formUpload").serialize() });
        e.data = { imageId: $("#ImageId").val(), Description: $("#Description").val() }
    }

    function onClickPreview(e) {
        selectedGrid = this;

        var tr = $(e.currentTarget).closest("tr");
        var dataItem = selectedGrid.dataItem(tr);
        var rowitem = selectedGrid.dataSource.get(dataItem.Id);

        selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));
        selectedGridImagesItem = dataItem.Id;

        var myUrl = '' + propertyCardConfig.urls.CardUpload_PreviewUpload + '?id=' + selectedGridImagesItem;
        var myTitle = "Preview";
        var width = 800;
        var height = 650;


        $("#windowcontainer").append("<div id='windowPreview'></div>");
        var mywindow = $("#windowPreview")
                .kendoWindow({
                    actions: ["Minimize", "Maximize", "Close"],
                    resizable: true,
                    draggable: true,
                    modal: true,
                    visible: false,
                    iframe: true,
                    deactivate: function () {
                        this.destroy();
                    }
                }).data("kendoWindow");

        mywindow.setOptions({ title: myTitle, width: width, height: height });
        mywindow.refresh({ url: myUrl });
        mywindow.center();
        mywindow.open();
    }

    function onUploadSuccess(e)
    {
        selectedGrid.dataSource.read();
    }

    // END GRID IMAGES

    // additional cost
    function InlineGridError(e) {
        if (e.errors) {
            let message = "Errors:\n";

            $.each(e.errors, function (key, value) {
                if ('errors' in value) {
                    $.each(value.errors, function () {
                        message += (key ? key + ": " : "") + value.errors.join("\n") + "\n";
                    });
                }
            });

            notifyWarning(message); // You can replace this with any other UI method to display errors
        }
    }

    function onClickAddCost() {
        var myTitle = "Additional Cost";
        var width = 1000;
        var psCardItemExtnId = $("#Id").val();

        $("#windowcontainer").append("<div id='window'></div>");
        var mywindow = $("#window")
                .kendoWindow({
                    actions: ["Minimize", "Close"],
                    draggable: true,
                    modal: true,
                    visible: false,
                    autoheight: true,
                    close: function () {
                        GetAddCost(psCardItemExtnId);
                    },
                    deactivate: function () {
                        this.destroy();
                    }
                }).data("kendoWindow");
        mywindow.setOptions({
            title: myTitle,
            width: width
        });
        mywindow.refresh({
            url: '' + propertyCardConfig.urls.ItemCard__AddCost + '',
            data: {
                psCardItemExtnId: psCardItemExtnId
            },
            // Specify a callback function to execute after content is loaded
            complete: function () {
                // Resize the window to fit its content
                mywindow.resize();
                mywindow.center();
            }
        });
        mywindow.open();
    }

    function GetAddCost(psCardItemExtnId) {
        var url = '' + propertyCardConfig.urls.ItemCard_GetAddCost + '';
        $.ajax({
            url: url,
            type: 'POST',
            data: {psCardItemExtnId: psCardItemExtnId},
            success: function (data) {
                var acqCost = data.TotalAddCost + data.UnitCost;
                $("#AddCost").data("kendoNumericTextBox").value(data.TotalAddCost);
                $("#AddCost").data("kendoNumericTextBox").trigger("change");

                $("#AcqCost").data("kendoNumericTextBox").value(acqCost);
                $("#AcqCost").data("kendoNumericTextBox").trigger("change");
            }
        });
    }

    var selectedGridAddCostItem = null;
    function onEditGridAddCost(e) {
        e.preventDefault();

        selectedGrid = e.sender;
        if (e.model.isNew()) {
            var row = e.sender.tbody.find("tr:first");
            this.select(row);
            //e.model.set("PsCardItemExtnId", null); not working for inline.
        } else {
            selectedGridAddCostItem = e.model.get("Id");
            var rowitem = selectedGrid.dataSource.get(selectedGridAddCostItem);
            selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));
        }
    }

    function onChangeGridAddCost(e) {
        var selectedData = this.dataItem(this.select());
        selectedGridAddCostItem = selectedData.Id;
    }

    function onDataBoundGridAddCost(e) {
        if (selectedGridAddCostItem) {
            var dataItem = this.dataSource.get(selectedGridAddCostItem);
            if (dataItem) {
                this.select($('[data-uid=' + dataItem.uid + ']'));
            }
        } else {
            var row = e.sender.tbody.find("tr:first");
            this.select(row);
        }
    }

    function onRequestEndGridAddCost(e) {
        if (e.response && !e.response.Errors && !e.response.errors) {
            if (e.type == "destroy" || e.type == "create" || e.type == "update") {
                var grid = $("#gridAddCost").data("kendoGrid");

                grid.dataSource.read();
            }
        }
    }

    function onChangeAddCostPoNo(e) {
        var poNo = this.value();
        //var disabled = $("#formAddCost").find(':input:disabled').removeAttr('disabled');
        $.ajax({
            url: '' + propertyCardConfig.urls.ItemCard_GetPoNo + '',
            type: 'POST',
            data: { poNo: poNo },
            success: function (data) {
                if (data.Errors == "") {
                    var poDate = $("#Effectivity").data("kendoDatePicker");
                    poDate.value(data.PoDate);
                    poDate.trigger("change");
                }
            }
        });
        //disabled.attr('disabled', 'disabled');
    }

    function GridErrorAddCost(args) {
        if (args.errors) {
            var selectedGrid = $("#gridAddCost").data("kendoGrid");
            var validationError = true;
            $.each(args.errors, function (propertyName) {
                if (propertyName == "DeleteError" || propertyName == "AddError" || propertyName == "UpdateError") {
                    notifyWarning(this.errors);
                    validationError = false;
                    selectedGrid.dataSource.read();
                }
            });
            if (validationError) {
                var validationTemplate = kendo.template($("#validationMessageTemplate").html());
                $('#AddCostErrorContent').removeClass('hidden');
                selectedGrid.one("dataBinding", function (e) {
                    e.preventDefault();
                    var errors = selectedGrid.editable.element.find(".errors");
                    errors.empty();
                    $.each(args.errors, function (propertyName) {
                        var renderedTemplate = validationTemplate({ field: propertyName, messages: this.errors });
                        errors.append(renderedTemplate);
                    });
                });
            }
        }
    }

    function onClickUploadAddCost(e) {
        e.preventDefault();
        selectedGrid = this;
        var dataItem = this.dataItem($(e.currentTarget).closest("tr"));
        var rowitem = selectedGrid.dataSource.get(dataItem.Id);
        selectedGrid.select($('[data-uid=' + rowitem.uid + ']'));
        UploadAddCost(dataItem.Id, dataItem.PostedBy);
    }

    function UploadAddCost(imageId, postedBy) {
        var myTitle = "Uploads";
        var width = 1000;
        var height = 650;
        $("#windowcontainer").append("<div id='window'></div>");
        var mywindow = $("#window")
                .kendoWindow({
                    actions: ["Minimize", "Close"],
                    draggable: true,
                    modal: true,
                    visible: false,
                    autoheight: true,
                    deactivate: function () {
                        this.destroy();
                    }
                }).data("kendoWindow");
        mywindow.setOptions({
            title: myTitle,
            width: width
            //height: height
        });
        mywindow.refresh({
            url: '' + propertyCardConfig.urls.ItemCard__Images + '',
            data: {
                imageId: imageId,
                postedBy: postedBy,
                description: "ADDITIONAL COST"
            },
            // Specify a callback function to execute after content is loaded
            complete: function () {
                // Resize the window to fit its content
                mywindow.resize();
                mywindow.center();
            }
        });
        //mywindow.center();
        mywindow.open();
        //resizeWindow();
    }
    // end additional cost
// Explicitly expose Property Card handlers to window for Kendo MVC and global inline calls
window.onSelectUserName = onSelectUserName;
window.onChangeUserName = onChangeUserName;
window.DataStockCard = DataStockCard;
window.DataStockCardItem = DataStockCardItem;
window.GridError = GridError;
window.GridItemError = GridItemError;
window.onListDataBound = onListDataBound;
window.onCriteriaChange = onCriteriaChange;
window.onClickBtnAdd = onClickBtnAdd;
window.onClickBtnEdit = onClickBtnEdit;
window.onClickBtnPrint = onClickBtnPrint;
window.onChangeGrid = onChangeGrid;
window.onDataBoundGrid = onDataBoundGrid;
window.onRequestEndGrid = onRequestEndGrid;
window.onEditGridItemExtn = onEditGridItemExtn;
window.onRemoveGridItemExtn = onRemoveGridItemExtn;
window.onGridItemExtnDeleteClick = onGridItemExtnDeleteClick;
window.onRequestEndGridItemExtn = onRequestEndGridItemExtn;
