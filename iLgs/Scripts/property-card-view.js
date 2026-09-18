/* Property Card workspace. Existing mutation endpoints and popup forms remain authoritative. */
var pcWorkspace = propertyCardConfig.workspace;
var pcCurrentAcquisition = {
    id: null,
    isPosted: false,
    postedDt: null,
    item: null
};

function pcUpdateCurrentAcquisitionState(id, isPosted, postedDt, item) {
    if (!id && item) {
        id = item.AcquisitionId || item.Id || item.TransferId;
    }
    if (id) {
        pcCurrentAcquisition.id = id;
    }
    pcCurrentAcquisition.isPosted = !!isPosted;
    pcCurrentAcquisition.postedDt = postedDt !== undefined ? postedDt : (isPosted ? (pcCurrentAcquisition.postedDt || (Date.prototype.toISOString ? new Date().toISOString() : String(new Date()))) : null);
    if (item) {
        pcCurrentAcquisition.item = item;
    } else if (pcCurrentAcquisition.item && pcCurrentAcquisition.id) {
        pcCurrentAcquisition.item.PostedDt = pcCurrentAcquisition.postedDt;
    }
    if (pcActiveAcquisition && pcCurrentAcquisition.id) {
        var activeId = pcActiveAcquisition.AcquisitionId || pcActiveAcquisition.Id || pcActiveAcquisition.TransferId;
        if (String(activeId).toLowerCase() === String(pcCurrentAcquisition.id).toLowerCase()) {
            pcActiveAcquisition.PostedDt = pcCurrentAcquisition.postedDt;
        }
    }
    if (pcAcquisitionsData && pcCurrentAcquisition.id) {
        for (var i = 0; i < pcAcquisitionsData.length; i++) {
            var row = pcAcquisitionsData[i];
            var rId = row.AcquisitionId || row.Id || row.TransferId;
            if (String(rId).toLowerCase() === String(pcCurrentAcquisition.id).toLowerCase()) {
                row.PostedDt = pcCurrentAcquisition.postedDt;
            }
        }
    }
    return pcCurrentAcquisition;
}
var pcTabNames = ['Overview', 'Acquisitions', 'Individual Units', 'Components', 'Accountability', 'History', 'Documents'];
var pcActiveAcquisition = null;
var pcPendingUnitTransfer = null;
var pcPendingUnit = null;
var pcPendingDocument = null;
var pcTabLoads = {};
var pcUploadOwner = null;
var propertyCardSelectedAcquisitionId = null;
var pcAcquisitionsData = null;
var pcAcquisitionsPromise = null;
var selectedId = pcWorkspace ? pcWorkspace.id : null;
var selectedStockNo = pcWorkspace ? pcWorkspace.cardNumber : null;
var selectedItemId = null;
var selectedTransferId = null;
var selectedItemExtnId = null;
var selectedGrid = null;

function pcText(value) { return kendo.htmlEncode(value == null || value === '' ? '\u2014' : String(value)); }
function pcNumber(value) { return kendo.toString(value || 0, 'n0'); }
function pcDate(value) { var date = kendo.parseDate(value); return date ? kendo.toString(date, 'MM/dd/yyyy') : '\u2014'; }
function pcMoney(value) { return value == null ? '\u2014' : kendo.toString(value, 'n2'); }
function pcWorkspaceParent(grid) {
    return grid.element.hasClass('pc-acquisitions') ? { Id: pcWorkspace.id } : pcActiveAcquisition;
}
function pcError(message) { $('#pcWorkspaceError').prop('hidden', false).text(message || 'Unable to load this record. Please retry.'); }
function pcWorkspaceDataError(e) { pcError(e.errors ? JSON.stringify(e.errors) : 'Unable to load records. Refresh to retry.'); }
function initializePropertyCardPopupValidation(container) {
    if (!container) return;
    var $container = $(container);
    $container.find('form').each(function () {
        var $form = $(this);
        $form.removeData('validator').removeData('unobtrusiveValidation');
        if (typeof $.validator !== 'undefined' && $.validator.unobtrusive) {
            $.validator.unobtrusive.parse($form);
        }
    });
}

function showPropertyCardPopupError(container, message) {
    if (!container) return;
    var $container = $(container);
    var $error = $container.find('.pc-popup-validation').first();
    if (!$error.length) {
        $error = $container.closest('.k-window').find('.pc-popup-validation').first();
    }
    if (!$error.length) {
        var $target = $container.find('form').first();
        if (!$target.length) { $target = $container; }
        $error = $('<div class="pc-popup-validation alert alert-danger" style="display:none;" role="alert"></div>').prependTo($target);
    }
    var msgText = message || 'Unable to complete request.';
    $error.html('<i class="fa fa-exclamation-circle"></i> <span>' + kendo.htmlEncode(msgText) + '</span>').show();

    // Also populate legacy containers if present for compatibility
    var $legacy = $container.find('#ErrorContent, #AddCostErrorContent');
    if ($legacy.length) {
        $legacy.removeClass('hidden').find('.errors').empty().append($('<p role="alert"></p>').text(msgText));
    }
}

function clearPropertyCardPopupError(container) {
    if (!container) return;
    var $container = $(container);
    $container.find('.pc-popup-validation').hide().empty();
    $container.find('#ErrorContent, #AddCostErrorContent').addClass('hidden').find('.errors').empty();
}

window.initializePropertyCardPopupValidation = initializePropertyCardPopupValidation;
window.showPropertyCardPopupError = showPropertyCardPopupError;
window.clearPropertyCardPopupError = clearPropertyCardPopupError;

function pcWindow(title, url, data, closed, iframe) {
    var element = $('<div></div>').appendTo('#windowcontainer');
    var widget = element.kendoWindow({
        title: title, width: Math.min(1000, $(window).width() - 40), height: iframe ? Math.min(720, $(window).height() - 80) : undefined,
        modal: true, visible: false, resizable: true, iframe: !!iframe, actions: ['Maximize', 'Close'],
        close: function () { if (closed) { closed(); } },
        deactivate: function () { this.destroy(); },
        refresh: function () { this.center(); initializePropertyCardPopupValidation(this.element); }
    }).data('kendoWindow');
    if (iframe && data) {
        url += (url.indexOf('?') < 0 ? '?' : '&') + $.param(data);
        data = {};
    }
    widget.refresh({url:url, data:data || {}});
    widget.center().open();
    return widget;
}
function pcRefreshPosition() {
    return $.getJSON(pcWorkspace.positionUrl, {id:pcWorkspace.id}).done(function (position) {
        $('[data-pc-stat]').each(function () {
            var key = $(this).attr('data-pc-stat');
            $(this).text(key === 'BalanceValue' ? pcMoney(position[key]) : pcNumber(position[key]));
        });
        $('[data-pc-text]').each(function () { $(this).text(position[$(this).attr('data-pc-text')] || '\u2014'); });
        $('[data-pc-date]').each(function () { $(this).text(pcDate(position[$(this).attr('data-pc-date')])); });
    }).fail(function () { pcError('Inventory summary could not be refreshed.'); });
}
function pcAfterMutation() {
    pcRefreshPosition();
    pcEnsureAcquisitionsLoaded(true).done(function() {
        var currentTab = pcCurrentTabName();
        if (currentTab) {
            pcRenderAcquisitionList(currentTab);
        }
    });
}
function pcAcquisitionGrid() {
    var grid = $('.pc-acquisitions').data('kendoGrid');
    if (!grid && typeof selectedId !== 'undefined' && selectedId) {
        grid = $('#gridItems_' + selectedId).data('kendoGrid');
    }
    if (!grid && typeof pcWorkspace !== 'undefined' && pcWorkspace && pcWorkspace.id) {
        grid = $('#gridItems_' + pcWorkspace.id).data('kendoGrid');
    }
    return grid;
}
function pcAcquisitionReference(data) {
    return '<div><strong>' + (data.PoNo ? ('PO ' + pcText(data.PoNo)) : 'Acquisition / No PO') + '</strong>' +
        '<span class="pc-cell-secondary">PO Date: ' + pcDate(data.PoDate) + '</span>' +
        (data.AirNo ? ('<span class="pc-cell-secondary">AIR ' + pcText(data.AirNo) + ' &bull; ' + pcDate(data.AirDate) + '</span>') : '') +
        (data.ParentId ? '<span class="pc-cell-secondary text-info">Transferred In</span>' : '') + '</div>';
}
function pcDescription(data) {
    return '<div>' + pcText(data.Description) +
        (data.Remarks ? ('<span class="pc-cell-secondary">' + pcText(data.Remarks) + '</span>') : '') + '</div>';
}
function pcAcquisitionCost(data) {
    return '<div><span>Qty: <strong>' + pcNumber(data.Qty) + ' ' + pcText(data.Unit) + '</strong></span>' +
        '<span class="pc-cell-secondary">Cost: ' + pcMoney(data.UnitCost) + '</span>' +
        '<span class="pc-cell-secondary">Total: ' + pcMoney(data.Amount) + '</span>' +
        (data.AddCost ? ('<span class="pc-cell-secondary">Add Cost: ' + pcMoney(data.AddCost) + '</span>') : '') + '</div>';
}
function pcAcquisitionLocation(data) {
    return '<div><strong>' + pcText(data.Department || data.DeptDisplay) + '</strong>' +
        (data.Location ? ('<span class="pc-cell-secondary">Loc: ' + pcText(data.Location) + '</span>') : '') +
        (data.DeptCode ? ('<span class="pc-cell-secondary">Code: ' + pcText(data.DeptCode) + '</span>') : '') + '</div>';
}
function pcAcquisitionMovement(data) {
    return '<div><span>Transit In: ' + pcNumber(data.TransferIn) + '</span>' +
        '<span class="pc-cell-secondary">Issued: ' + pcNumber(data.QtyIss) + '</span>' +
        '<span class="pc-cell-secondary">Transit Out: ' + pcNumber(data.TransferOut) + '</span></div>';
}
function pcAcquisitionStatus(data) {
    var posted = data.PostedDt != null;
    return '<div><span class="pc-status ' + (posted ? 'pc-posted' : 'pc-draft') + '">' + (posted ? 'POSTED' : 'DRAFT') + '</span>' +
        '<span class="pc-cell-secondary">' + (posted ? ('Posted: ' + pcDate(data.PostedDt) + ' by ' + pcText(data.PostedBy)) : ('Created: ' + pcDate(data.InsertedDt) + ' by ' + pcText(data.InsertedBy))) + '</span></div>';
}
function pcAcquisitionDetailHtml(data) {
    return '<div class="pc-acquisition-detail">' +
        '<div><h4>Audit</h4><p>Created by ' + pcText(data.InsertedBy) + ' on ' + pcDate(data.InsertedDt) + '</p>' +
        (data.PostedDt ? ('<p>Posted by ' + pcText(data.PostedBy) + ' on ' + pcDate(data.PostedDt) + '</p>') : '<p class="pc-zero">Not posted</p>') + '</div>' +
        '<div><h4>Costs</h4><p>Unit Cost: ' + pcMoney(data.UnitCost) + '</p><p>Total: ' + pcMoney(data.Amount) + '</p><p>Add. Cost: ' + pcMoney(data.AddCost) + '</p></div>' +
        '<div><h4>Quantities</h4><p>Received: ' + pcNumber(data.Qty) + ' ' + pcText(data.Unit) + '</p><p>Issued: ' + pcNumber(data.QtyIss) + '</p><p>Balance: ' + pcNumber(data.QtyBal) + '</p></div>' +
        '<div><h4>Organization</h4><p>Department: ' + pcText(data.Department || data.DeptDisplay) + '</p><p>Code: ' + pcText(data.DeptCode) + '</p></div>' +
        '</div>';
}
function pcAcquisitionUnitsButton(data) {
    var id = data.TransferId || data.Id;
    return '<button type="button" class="k-button k-button-md k-rounded-md k-button-solid k-button-solid-base pc-acquisition-units" data-transfer-id="' + id + '">Units</button>';
}
function forEachKendoDataRow(grid, callback) {
    if (!grid || !grid.tbody || typeof callback !== 'function') {
        return;
    }
    grid.tbody.find('> tr[data-uid]:not(.k-detail-row)').each(function () {
        var $row = $(this);
        var dataItem = grid.dataItem($row);
        if (!dataItem) {
            return;
        }
        callback($row, dataItem);
    });
}

function updateIndividualUnitsPostedState(isPosted) {
    var acqId = propertyCardSelectedAcquisitionId || pcCurrentAcquisition.id;
    if (typeof isPosted === 'undefined') {
        isPosted = pcIsAcquisitionPosted(acqId);
    }
    isPosted = !!isPosted;

    var $banner = $('#pcUnitsPostedLockBanner');
    var $addBtn = $('#pcBtnAddUnit');
    var $addFirstBtn = $('#pcBtnAddFirstUnit');

    if (isPosted) {
        if (!$banner.length) {
            $banner = $('<div id="pcUnitsPostedLockBanner" class="pc-posted-lock-banner"><i class="fa fa-lock"></i> This acquisition is posted. Individual units are read-only.</div>').insertBefore($('#pcUnitSummaryBanner'));
        }
        $banner.show();
        $addBtn.hide();
        $addFirstBtn.hide();
    } else {
        $banner.hide();
        $addBtn.show();
        $addFirstBtn.show();
    }

    var $gridEl = $('.pc-units-grid');
    if (!$gridEl.length) {
        return;
    }
    var grid = $gridEl.data('kendoGrid');
    if (!grid || !grid.tbody) {
        return;
    }

    var totalUnits = (grid.dataSource && grid.dataSource.data) ? grid.dataSource.data().length : 0;
    var maxQty = pcActiveAcquisition ? (pcActiveAcquisition.Qty || 0) : 0;
    var remaining = Math.max(0, maxQty - totalUnits);

    if (!isPosted) {
        if (remaining <= 0) {
            $addBtn.prop('disabled', true).addClass('k-disabled').attr('title', 'All ' + maxQty + ' units have been created for this acquisition.');
        } else {
            $addBtn.prop('disabled', false).removeClass('k-disabled').attr('title', '');
        }
    }

    forEachKendoDataRow(grid, function (row, item) {
        var canDelete = item.CanDelete !== false;
        var canEdit = item.CanEdit !== false;
        if (isPosted) {
            row.find('.k-grid-delete, .k-grid-Delete').hide();
            var editBtn = row.find('.k-grid-edit');
            editBtn.contents().filter(function () { return this.nodeType === 3; }).replaceWith(' View');
            editBtn.attr('title', 'View Unit Details (Acquisition Posted - Read Only)');
        } else {
            if (canDelete) {
                row.find('.k-grid-delete, .k-grid-Delete').show();
            } else {
                row.find('.k-grid-delete, .k-grid-Delete').hide();
            }
            var editBtn = row.find('.k-grid-edit');
            if (canEdit) {
                editBtn.contents().filter(function () { return this.nodeType === 3; }).replaceWith(' Edit');
                editBtn.attr('title', 'Edit Physical Unit');
            } else {
                editBtn.contents().filter(function () { return this.nodeType === 3; }).replaceWith(' View');
                editBtn.attr('title', 'View Unit Details (Accountable - Read Only)');
            }
        }
    });
}

function updateComponentsPostedState(isPosted) {
    var acqId = propertyCardSelectedAcquisitionId || pcCurrentAcquisition.id;
    if (typeof isPosted === 'undefined') {
        isPosted = pcIsAcquisitionPosted(acqId);
    }
    isPosted = !!isPosted;

    var $banner = $('#pcCompPostedLockBanner');
    var $gridWrapper = $('.pc-components-grid');
    if (isPosted) {
        if (!$banner.length && $gridWrapper.length) {
            $banner = $('<div id="pcCompPostedLockBanner" class="pc-posted-lock-banner"><i class="fa fa-lock"></i> This acquisition is posted. Components are read-only.</div>').insertBefore($gridWrapper);
        }
        $banner.show();
        $gridWrapper.find('.k-grid-add').hide();
    } else {
        $banner.hide();
        $gridWrapper.find('.k-grid-add').show();
    }

    if (!$gridWrapper.length) {
        return;
    }
    var grid = $gridWrapper.data('kendoGrid');
    if (grid && grid.tbody) {
        forEachKendoDataRow(grid, function (row, item) {
            var canDelete = item.CanDelete !== false;
            var canEdit = item.CanEdit !== false;
            if (isPosted) {
                row.find('.k-grid-delete, .k-grid-Delete').hide();
                var editBtn = row.find('.k-grid-edit');
                editBtn.contents().filter(function () { return this.nodeType === 3; }).replaceWith(' View');
                editBtn.attr('title', 'View Component Details (Acquisition Posted - Read Only)');
            } else {
                if (canDelete) {
                    row.find('.k-grid-delete, .k-grid-Delete').show();
                } else {
                    row.find('.k-grid-delete, .k-grid-Delete').hide();
                }
                var editBtn = row.find('.k-grid-edit');
                if (canEdit) {
                    editBtn.contents().filter(function () { return this.nodeType === 3; }).replaceWith(' Edit');
                    editBtn.attr('title', 'Edit Component Details');
                } else {
                    editBtn.contents().filter(function () { return this.nodeType === 3; }).replaceWith(' View');
                    editBtn.attr('title', 'View Component Details (Read Only)');
                }
            }
        });
    }

    var $detailGrids = $('.pc-component-units-grid, .k-detail-row .k-grid');
    $detailGrids.each(function () {
        var dGrid = $(this).data('kendoGrid');
        if (!dGrid || !dGrid.tbody) return;
        if (isPosted) {
            dGrid.wrapper.find('.k-grid-add').hide();
        } else {
            dGrid.wrapper.find('.k-grid-add').show();
        }
        forEachKendoDataRow(dGrid, function (dRow, dItem) {
            var canDel = dItem.CanDelete !== false;
            var canEd = dItem.CanEdit !== false;
            if (isPosted) {
                dRow.find('.k-grid-delete, .k-grid-Delete').hide();
                var dEditBtn = dRow.find('.k-grid-edit');
                dEditBtn.contents().filter(function () { return this.nodeType === 3; }).replaceWith(' View');
                dEditBtn.attr('title', 'View Component Unit Details (Acquisition Posted - Read Only)');
            } else {
                if (canDel) {
                    dRow.find('.k-grid-delete, .k-grid-Delete').show();
                } else {
                    dRow.find('.k-grid-delete, .k-grid-Delete').hide();
                }
                var dEditBtn = dRow.find('.k-grid-edit');
                if (canEd) {
                    dEditBtn.contents().filter(function () { return this.nodeType === 3; }).replaceWith(' Edit');
                    dEditBtn.attr('title', 'Edit Component Unit');
                } else {
                    dEditBtn.contents().filter(function () { return this.nodeType === 3; }).replaceWith(' View');
                    dEditBtn.attr('title', 'View Component Unit Details (Accountable - Read Only)');
                }
            }
        });
    });
}

function updateDocumentsPostedState(isPosted) {
    var acqId = propertyCardSelectedAcquisitionId || pcCurrentAcquisition.id;
    if (typeof isPosted === 'undefined') {
        isPosted = pcIsAcquisitionPosted(acqId);
    }
    isPosted = !!isPosted;

    var $banner = $('#pcDocPostedLockBanner');
    var $host = $('#pcDocumentsHost');
    if (isPosted) {
        if (!$banner.length && $host.length) {
            $banner = $('<div id="pcDocPostedLockBanner" class="pc-posted-lock-banner"><i class="fa fa-lock"></i> This acquisition is posted. Documents are read-only.</div>').insertBefore($host);
        }
        $banner.show();
    } else {
        $banner.hide();
    }

    if (!$host.length) {
        return;
    }

    var $uploadButtons = $host.find('[id^="btnUpload_"], .pc-btn-upload, button[data-gridname]');
    var $toolbar = $host.find('.k-grid-toolbar');
    if (isPosted) {
        $uploadButtons.hide().prop('disabled', true);
        $toolbar.hide();
    } else {
        $uploadButtons.show().prop('disabled', false);
        $toolbar.show();
    }

    var grid = $host.find('.k-grid').data('kendoGrid');
    if (grid && grid.tbody) {
        grid.tbody.find('> tr[data-uid]:not(.k-detail-row)').each(function () {
            var row = $(this);
            if (isPosted) {
                row.find('.k-grid-delete, .k-grid-Delete').hide();
                var editBtn = row.find('.k-grid-edit');
                editBtn.contents().filter(function () { return this.nodeType === 3; }).replaceWith(' View');
                editBtn.attr('title', 'View Document (Acquisition Posted - Read Only)');
            } else {
                row.find('.k-grid-delete, .k-grid-Delete').show();
                var editBtn = row.find('.k-grid-edit');
                editBtn.contents().filter(function () { return this.nodeType === 3; }).replaceWith(' Edit');
                editBtn.attr('title', 'Edit Document');
            }
        });
    }
}

function applyAcquisitionPostedState(isPosted, targetAcquisitionId) {
    var acqId = targetAcquisitionId || propertyCardSelectedAcquisitionId || pcCurrentAcquisition.id;
    if (!acqId && pcActiveAcquisition) {
        acqId = pcActiveAcquisition.AcquisitionId || pcActiveAcquisition.Id || pcActiveAcquisition.TransferId;
    }
    isPosted = !!isPosted;

    // 1. Update shared state
    pcUpdateCurrentAcquisitionState(acqId, isPosted, isPosted ? (pcCurrentAcquisition.postedDt || (Date.prototype.toISOString ? new Date().toISOString() : String(new Date()))) : null);

    // 2. Update Acquisitions Grid row if present
    var grid = pcAcquisitionGrid();
    if (grid && grid.dataSource && acqId) {
        var targetStr = String(acqId).toLowerCase();
        var data = grid.dataSource.data();
        for (var i = 0; i < data.length; i++) {
            var gItem = data[i];
            if (String(gItem.Id).toLowerCase() === targetStr || String(gItem.TransferId || '').toLowerCase() === targetStr) {
                if (typeof gItem.set === 'function') {
                    gItem.set('PostedDt', isPosted ? (gItem.PostedDt || new Date()) : null);
                } else {
                    gItem.PostedDt = isPosted ? (gItem.PostedDt || new Date()) : null;
                }
                var row = grid.tbody.find("tr[data-uid='" + gItem.uid + "']");
                if (row.length) {
                    if (isPosted) {
                        row.addClass('pc-acquisition-posted');
                    } else {
                        row.removeClass('pc-acquisition-posted');
                    }
                    var $statusContainer = row.find('.pc-status').parent();
                    if ($statusContainer.length) {
                        $statusContainer.html(pcAcquisitionStatus(gItem));
                    }
                }
                break;
            }
        }
    }

    // 3. Update master pane cards
    if (acqId) {
        var $cards = $('.pc-acquisition-card[data-acquisition-id="' + acqId + '"]');
        $cards.each(function () {
            var cardItem = $(this).data('pcAcquisitionItem');
            if (cardItem) {
                cardItem.PostedDt = isPosted ? (cardItem.PostedDt || new Date()) : null;
            }
        });
    }

    // 4. Update tab UIs
    updateIndividualUnitsPostedState(isPosted);
    updateComponentsPostedState(isPosted);
    updateDocumentsPostedState(isPosted);
}

function pcPostAcquisitionItem(item, button, grid) {
    if (!item || !item.Id) return;
    var targetId = item.Id;
    var poRef = item.PoNo ? ('PO #' + item.PoNo) : 'Acquisition Record';

    epsmsConfirm({
        title: "Post Acquisition Record",
        type: "info",
        headline: "Post Acquisition Record?",
        entityLabel: "Reference",
        entityValue: poRef,
        message: "Once posted, this acquisition record will be officially entered into the property ledger.",
        cancelText: "Cancel",
        confirmText: "Post",
        confirmIcon: "k-i-check",
        onConfirm: function (dialog, $confirmBtn) {
            setClickedButtonBusy($confirmBtn, "Posting...");
            if (button && button.length) {
                propertyCardButtonStart(button, "Posting...");
            }

            $.ajax({
                type: "POST",
                url: propertyCardConfig.urls.StockCard_PostItemRecord,
                data: { psCardItemId: targetId },
                dataType: "json"
            }).done(function (result) {
                restoreClickedButton($confirmBtn);
                dialog.close();
                var errors = result ? (result.Errors || result.errors) : null;
                var hasError = false;
                var errorMsg = "";

                if (errors) {
                    if ($.isArray(errors)) {
                        if (errors.length > 0) {
                            hasError = true;
                            errorMsg = errors.join('<br>');
                        }
                    } else if (typeof errors === 'string' && errors.trim() !== '') {
                        hasError = true;
                        errorMsg = errors;
                    }
                }

                if (!hasError) {
                    notifySuccess("Acquisition record posted.");
                    applyAcquisitionPostedState(true, targetId);
                    var targetGrid = grid || pcAcquisitionGrid();
                    if (targetGrid) {
                        targetGrid.dataSource.read();
                    }
                    pcAfterMutation();
                } else {
                    notifyWarning(errorMsg || "Unable to post acquisition record.");
                }
            }).fail(function (xhr) {
                restoreClickedButton($confirmBtn);
                notifyWarning(ajaxFailureMessage(xhr, "Unable to post acquisition record."));
            }).always(function () {
                if (button && button.length) {
                    propertyCardButtonStop(button);
                }
            });
        }
    });
}

function pcUnpostAcquisitionItem(item, button, grid) {
    if (!item || !item.Id) return;
    var targetId = item.Id;
    var poRef = item.PoNo ? ('PO #' + item.PoNo) : 'Acquisition Record';

    epsmsConfirm({
        title: "Unpost Acquisition Record",
        type: "warning",
        headline: "Unpost Acquisition Record?",
        entityLabel: "Reference",
        entityValue: poRef,
        message: "This will return the acquisition record to unposted status.",
        cancelText: "Cancel",
        confirmText: "Unpost",
        confirmIcon: "k-i-undo",
        onConfirm: function (dialog, $confirmBtn) {
            setClickedButtonBusy($confirmBtn, "Unposting...");
            if (button && button.length) {
                propertyCardButtonStart(button, "Unposting...");
            }

            $.ajax({
                type: "POST",
                url: propertyCardConfig.urls.StockCard_UnpostItemRecord,
                data: { psCardItemId: targetId },
                dataType: "json"
            }).done(function (result) {
                restoreClickedButton($confirmBtn);
                dialog.close();
                var errors = result ? (result.Errors || result.errors) : null;
                var hasError = false;
                var errorMsg = "";

                if (errors) {
                    if ($.isArray(errors)) {
                        if (errors.length > 0) {
                            hasError = true;
                            errorMsg = errors.join('<br>');
                        }
                    } else if (typeof errors === 'string' && errors.trim() !== '') {
                        hasError = true;
                        errorMsg = errors;
                    }
                }

                if (!hasError) {
                    notifySuccess("Acquisition record unposted.");
                    applyAcquisitionPostedState(false, targetId);
                    var targetGrid = grid || pcAcquisitionGrid();
                    if (targetGrid) {
                        targetGrid.dataSource.read();
                    }
                    pcAfterMutation();
                } else {
                    notifyWarning(errorMsg || "Unable to unpost acquisition record.");
                }
            }).fail(function (xhr) {
                restoreClickedButton($confirmBtn);
                notifyWarning(ajaxFailureMessage(xhr, "Unable to unpost acquisition record."));
            }).always(function () {
                if (button && button.length) {
                    propertyCardButtonStop(button);
                }
            });
        }
    });
}

function pcCloseAcquisitionMenu() {
    var $menu = $('#pcAcquisitionMenu');
    if ($menu.length && $menu.is(':visible')) {
        $menu.hide();
        var $btn = $menu.data('activeBtn');
        if ($btn && $btn.length) {
            $btn.attr('aria-expanded', 'false').removeClass('pc-more-active');
        }
        $menu.removeData('activeBtn').removeData('activeItem').removeData('activeRow').removeData('activeGrid');
    }
}

function pcOpenAcquisitionMenu(btn, grid) {
    var $btn = $(btn);
    if (!$btn || !$btn.length) { return; }
    var $row = $btn.closest('tr');
    if (!grid) {
        grid = pcAcquisitionGrid();
    }
    if (!grid && $row && $row.length) {
        var gridEl = $row.closest('.k-grid, [data-role="grid"]');
        if (gridEl.length) { grid = gridEl.data('kendoGrid'); }
    }
    var item = null;
    if (grid) {
        if ($row && $row.length) {
            item = grid.dataItem($row);
        }
        if (!item) {
            item = grid.dataItem($btn);
        }
    }
    if (!item) { return; }

    pcSetAcquisition(grid, item);
    var $menu = pcAcquisitionMenu();

    // Store row context
    $menu.data('activeBtn', $btn);
    $menu.data('activeRow', $row);
    $menu.data('activeItem', item);
    $menu.data('activeGrid', grid);

    // State-based item visibility
    var isPosted = (item.PostedDt != null || pcIsAcquisitionPosted(item.Id));
    $menu.find('[data-action="post"]').toggle(!isPosted);
    $menu.find('[data-action="unpost"]').toggle(isPosted);
    $menu.find('[data-action="edit"]').toggle(!isPosted);
    $menu.find('[data-action="delete"]').toggle(!isPosted);

    // Position menu relative to button in body
    $menu.css({ display: 'block', visibility: 'hidden', top: 0, left: 0 });
    var offset = $btn.offset();
    var btnHeight = $btn.outerHeight();
    var btnWidth = $btn.outerWidth();
    var menuWidth = $menu.outerWidth();
    var menuHeight = $menu.outerHeight();
    var winWidth = $(window).width();
    var winHeight = $(window).height();
    var scrollTop = $(window).scrollTop();
    var scrollLeft = $(window).scrollLeft();

    // Default: below button. If overflows window bottom, show above button
    var top = offset.top + btnHeight + 2;
    if (top + menuHeight > scrollTop + winHeight && offset.top - menuHeight > scrollTop) {
        top = Math.max(scrollTop + 4, offset.top - menuHeight - 2);
    }

    // Default: align left edge. If overflows window right, align right edge
    var left = offset.left;
    if (left + menuWidth > scrollLeft + winWidth) {
        left = Math.max(scrollLeft + 4, offset.left + btnWidth - menuWidth);
    }

    $menu.css({
        top: top + 'px',
        left: left + 'px',
        visibility: 'visible',
        display: 'block',
        zIndex: 10005
    });

    $btn.attr('aria-expanded', 'true').addClass('pc-more-active');
}

function pcAcquisitionMenu() {
    var $menu = $('#pcAcquisitionMenu');
    if (!$menu.length) {
        $menu = $('<ul id="pcAcquisitionMenu" class="pc-acquisition-menu" role="menu" aria-label="Acquisition Actions">' +
            '<li data-action="post" role="menuitem" tabindex="-1"><span class="k-icon k-i-check"></span> Post</li>' +
            '<li data-action="unpost" role="menuitem" tabindex="-1"><span class="k-icon k-i-undo"></span> Unpost</li>' +
            '<li data-action="edit" role="menuitem" tabindex="-1"><span class="k-icon k-i-edit"></span> Edit</li>' +
            '<li data-action="print" role="menuitem" tabindex="-1"><span class="k-icon k-i-print"></span> Print PO</li>' +
            '<li data-action="transfer" role="menuitem" tabindex="-1"><span class="k-icon k-i-hyperlink-open"></span> Transfer PO</li>' +
            '<li data-action="units" role="menuitem" tabindex="-1"><span class="k-icon k-i-grid-layout"></span> View Units</li>' +
            '<li data-action="documents" role="menuitem" tabindex="-1"><span class="k-icon k-i-file"></span> Documents</li>' +
            '<li data-action="details" role="menuitem" tabindex="-1"><span class="k-icon k-i-information"></span> Toggle Details</li>' +
            '<li data-action="delete" role="menuitem" tabindex="-1"><span class="k-icon k-i-delete"></span> Delete</li>' +
            '</ul>').appendTo('body');

        // Item click delegation
        $menu.on('click', 'li[data-action]', function (e) {
            e.preventDefault();
            e.stopPropagation();
            var action = $(this).attr('data-action');
            var item = $menu.data('activeItem');
            var $btn = $menu.data('activeBtn');
            var $row = $menu.data('activeRow');
            var grid = $menu.data('activeGrid') || pcAcquisitionGrid();

            pcCloseAcquisitionMenu();
            if (!item) { return; }

            if (action === 'post') {
                var isAlreadyPosted = (item.PostedDt != null || pcIsAcquisitionPosted(item.Id));
                if (isAlreadyPosted) {
                    epsmsAlert({ title: "Post Not Permitted", type: "warning", headline: "Acquisition Already Posted", message: "This acquisition is already posted." });
                    return;
                }
                pcPostAcquisitionItem(item, $btn, grid);
            } else if (action === 'unpost') {
                var isCurrentlyPosted = (item.PostedDt != null || pcIsAcquisitionPosted(item.Id));
                if (!isCurrentlyPosted) {
                    epsmsAlert({ title: "Unpost Not Permitted", type: "warning", headline: "Acquisition Not Posted", message: "This acquisition is not posted." });
                    return;
                }
                pcUnpostAcquisitionItem(item, $btn, grid);
            } else if (action === 'edit') {
                if (item.PostedDt != null) {
                    epsmsAlert({ title: "Edit Not Permitted", type: "warning", headline: "Acquisition Posted", message: "This acquisition is posted and cannot be edited." });
                    return;
                }
                if (grid && $row && $row.length) { grid.editRow($row); }
            } else if (action === 'delete') {
                if (item.PostedDt != null) {
                    epsmsAlert({ title: "Delete Prohibited", type: "warning", headline: "Acquisition Posted", message: "This acquisition is posted and cannot be deleted." });
                    return;
                }
                epsmsConfirm({
                    title: "Delete Acquisition", type: "danger", headline: "Delete Acquisition Record?",
                    entityLabel: "Reference", entityValue: item.PoNo || "Acquisition Record",
                    message: "This will permanently remove this acquisition and its linked transit records. This action cannot be undone.",
                    cancelText: "Cancel", confirmText: "Delete", confirmIcon: "k-i-delete",
                    onConfirm: function (dialog, $confirmBtn) {
                        setClickedButtonBusy($confirmBtn, "Deleting...");
                        if (grid) {
                            grid.dataSource.remove(item);
                            grid.dataSource.sync().done(function () {
                                restoreClickedButton($confirmBtn); dialog.close(); notifySuccess("Acquisition deleted."); pcAfterMutation();
                            }).fail(function (xhr) {
                                restoreClickedButton($confirmBtn); grid.dataSource.read(); notifyWarning(ajaxFailureMessage(xhr, "Unable to delete acquisition."));
                            });
                        }
                    }
                });
            } else if (action === 'print') {
                selectedItemId = item.Id;
                PrintPo(0);
            } else if (action === 'transfer') {
                if (typeof onClickTransfer === 'function') {
                    onClickTransfer.call(grid, { currentTarget: $btn && $btn.length ? $btn : $row, preventDefault: function () { } });
                }
            } else if (action === 'units') {
                pcViewUnits(item.TransferId || item.Id);
            } else if (action === 'documents') {
                var docId = item.Id;
                propertyCardSelectedAcquisitionId = docId;
                pcOpenTab('Documents').done(function () { selectPropertyCardAcquisition(docId, true); });
            } else if (action === 'details') {
                if (grid && $row && $row.length) { pcToggleAcquisitionRow(grid, $row); }
            }
        });

        // Global dismiss listeners
        $(document).off('click.pcAcqMenuOutside').on('click.pcAcqMenuOutside', function (e) {
            if (!$(e.target).closest('#pcAcquisitionMenu, .k-grid-More, .pc-acquisition-more').length) {
                pcCloseAcquisitionMenu();
            }
        });

        $(document).off('keydown.pcAcqMenuKey').on('keydown.pcAcqMenuKey', function (e) {
            if (e.which === 27) { // Escape
                pcCloseAcquisitionMenu();
            }
        });

        $(window).off('scroll.pcAcquisitionMenu resize.pcAcquisitionMenu').on('scroll.pcAcquisitionMenu resize.pcAcquisitionMenu', function () {
            pcCloseAcquisitionMenu();
        });
    }

    $menu.open = function ($target) { pcOpenAcquisitionMenu($target); };
    $menu.close = function () { pcCloseAcquisitionMenu(); };
    return $menu;
}
function pcShowAcquisitionDetails(e) {
    e.preventDefault();
    var grid = this;
    var row = $(e.currentTarget).closest('tr');
    var item = grid.dataItem(row);
    if (!item) return;

    var acqId = item.Id || item.TransferId;
    var host = $('#pcAcquisitionDetailHost');

    if (host.is(':visible') && host.data('current-acq-id') === acqId) {
        host.slideUp(150, function () {
            host.empty();
            host.removeData('current-acq-id');
        });
        return;
    }

    kendo.ui.progress(host, true);
    host.data('current-acq-id', acqId);
    host.hide().empty();

    $.get(pcWorkspace.acquisitionUrl, { id: pcWorkspace.id, acquisitionId: acqId })
        .done(function (html) {
            host.html(html);
            host.slideDown(200);
            $('html, body').animate({
                scrollTop: host.offset().top - 80
            }, 300);
        })
        .fail(function () {
            host.removeData('current-acq-id');
            pcError('Unable to load acquisition details.');
        })
        .always(function () {
            kendo.ui.progress(host, false);
        });
}
function pcToggleAcquisitionRow(grid, row) {
    if (row.hasClass('k-master-row')) {
        if (row.next().hasClass('k-detail-row')) { grid.collapseRow(row); } else { grid.expandRow(row); }
    }
}
function pcAcquisitionMore(e) {
    if (e) {
        if (e._pcHandled) { return; }
        e._pcHandled = true;
        if (e.preventDefault) { e.preventDefault(); }
        if (e.stopPropagation) { e.stopPropagation(); }
    }
    var grid = (this && this.dataSource) ? this : null;
    var $btn = $(e ? (e.currentTarget || e.target) : this).closest('.k-grid-More, .pc-acquisition-more');
    if (!$btn.length) { return; }

    var $menu = $('#pcAcquisitionMenu');
    if ($menu.length && $menu.is(':visible') && $menu.data('activeBtn') && $menu.data('activeBtn')[0] === $btn[0]) {
        pcCloseAcquisitionMenu();
        return;
    }

    pcOpenAcquisitionMenu($btn, grid);
}
function pcSetAcquisition(grid, item) {
    pcActiveAcquisition = item;
    selectedItemId = item.Id;
    selectedTransferId = item.TransferId;
    propertyCardSelectedAcquisitionId = item.Id;
    pcUpdateCurrentAcquisitionState(item.Id, item.PostedDt != null, item.PostedDt, item);
}
function onChangeGridItems() {
    var item = this.dataItem(this.select());
    if (item) {
        pcSetAcquisition(this, item);
        $('#pcAcquisitionDetailHost').slideUp(150, function () { $(this).empty(); });
    }
}
function onDataBoundGridItems() {
    var grid = this;
    pcCloseAcquisitionMenu();
    pcAcquisitionMenu();

    grid.tbody.find('.k-grid-More').each(function () {
        var $btn = $(this);
        $btn.addClass('pc-acquisition-more');
        $btn.attr('type', 'button');
        $btn.attr('aria-haspopup', 'true');
        $btn.attr('aria-expanded', 'false');
        if (!$btn.find('.pc-more-caret').length) {
            $btn.append('<span class="pc-more-caret">&#9662;</span>');
        }
    });

    forEachKendoDataRow(grid, function (row, item) {
        row.removeClass('k-state-disabled disabled');
        var posted = item && (item.PostedDt != null || (pcCurrentAcquisition.id && String(item.Id).toLowerCase() === String(pcCurrentAcquisition.id).toLowerCase() && pcCurrentAcquisition.isPosted));
        if (posted) {
            row.addClass('pc-acquisition-posted');
        } else {
            row.removeClass('pc-acquisition-posted');
        }
    });

    if (selectedTransferId) {
        var item = grid.dataSource.get(selectedTransferId);
        if (item) { grid.select(grid.tbody.children("tr[data-uid='" + item.uid + "']")); }
    }
}
function onEditGridItems(e) {
    selectedGrid = e.sender;
    clearPropertyCardPopupError(e.container);
    initializePropertyCardPopupValidation(e.container);
    e.container.find('.k-grid-update').off('click.pcPopupValidation').on('click.pcPopupValidation', function () {
        clearPropertyCardPopupError(e.container);
    });
    if (e.model.isNew()) {
        selectedItemId = null; selectedTransferId = null;
        e.model.set('PsCardId', pcWorkspace.id);
        e.model.set('QtyBal', 0); e.model.set('QtyIss', 0); e.model.set('TransferIn', 0); e.model.set('TransferOut', 0);
    } else {
        selectedItemId = e.model.Id; selectedTransferId = e.model.TransferId;
        propertyCardSelectedAcquisitionId = e.model.Id;
        if (e.model.PostedDt != null) {
            e.container.data('kendoWindow').title('View Acquisition (Posted)');
            e.container.find('.k-grid-update').hide();
            e.container.find('input, textarea, select').prop('readonly', true);
        }
    }
}
function onRemoveGridItems(e) { selectedGrid = e.sender; }
function onRequestEndGridItems(e) {
    if (e.response && !e.response.Errors && !e.response.errors && e.type !== 'read') {
        var source = this;
        setTimeout(function () { source.read(); pcAfterMutation(); }, 0);
    }
}
function pcLoadTab(name) {
    if (name === 'Overview') { return $.Deferred().resolve().promise(); }
    if (pcTabLoads[name]) { return pcTabLoads[name]; }
    var host = $('[data-pc-tab-host="' + name + '"]');
    kendo.destroy(host); host.empty();
    kendo.ui.progress(host, true);
    pcTabLoads[name] = $.get(pcWorkspace.tabUrl, {id:pcWorkspace.id, tab:name}).then(function (html) {
        host.html(html);
        pcInitTab(name);
        kendo.resize(host);
    }, function () {
        pcTabLoads[name] = null;
        host.html('<p role="alert">This section could not be loaded.</p><button type="button" class="k-button pc-tab-retry">Retry</button>');
        host.find('.pc-tab-retry').on('click',function () { pcLoadTab(name); });
        return $.Deferred().reject().promise();
    }).always(function () { kendo.ui.progress(host,false); });
    return pcTabLoads[name];
}
function pcOpenTab(name) {
    var tab = $('#pcWorkspaceTabs').data('kendoTabStrip');
    tab.select(pcTabNames.indexOf(name));
    return pcLoadTab(name);
}
function pcChoice(selector, kind, callback) {
    $(selector).kendoDropDownList({
        optionLabel:'Search and select a record.', dataTextField:'Label', dataValueField:'Id',
        filter:'contains', minLength:1,
        dataSource:{serverFiltering:true, transport:{read:{url:pcWorkspace.choicesUrl, dataType:'json', data:function () {
            var widget=$(selector).data('kendoDropDownList');
            return {id:pcWorkspace.id, kind:kind, text:widget && widget.filterInput ? widget.filterInput.val() : ''};
        }}}},
        change:function () { var item=this.dataItem(); if (item && item.Id) { callback(item); } }
    });
}

/* ==========================================================================
   Master-Detail Orchestration & State Management
   ========================================================================== */
function pcCurrentTabName() {
    var tabStrip = $('#pcWorkspaceTabs').data('kendoTabStrip');
    if (tabStrip && tabStrip.select().length) {
        return pcTabNames[tabStrip.select().index()];
    }
    return null;
}

function pcParseAcquisitionItem(item) {
    var label = item.Label || '';
    var parts = label.split(' / ');
    var poNo = item.PoNo || parts[0] || 'No PO';
    var desc = parts.length > 1 ? parts[1] : (item.Description || '-');
    var code = parts.length > 2 ? parts[2] : (item.Code || 'Origin');
    var loc = parts.length > 3 ? parts.slice(3).join(' / ') : (item.Location || '');
    return {
        id: item.AcquisitionId || item.Id,
        poNo: poNo,
        description: desc,
        code: code,
        location: loc,
        rawLabel: label
    };
}

function pcEnsureAcquisitionsLoaded(forceRefresh) {
    if (!forceRefresh && pcAcquisitionsData !== null) {
        return $.Deferred().resolve(pcAcquisitionsData).promise();
    }
    if (!forceRefresh && pcAcquisitionsPromise) {
        return pcAcquisitionsPromise;
    }
    pcAcquisitionsPromise = $.getJSON(pcWorkspace.choicesUrl, { id: pcWorkspace.id, kind: 'acquisitions' })
        .then(function (items) {
            pcAcquisitionsData = items || [];
            pcAcquisitionsPromise = null;
            return pcAcquisitionsData;
        }, function () {
            pcAcquisitionsPromise = null;
            return [];
        });
    return pcAcquisitionsPromise;
}

function pcFindAcquisitionById(acquisitionId) {
    if (!acquisitionId) return null;
    var target = String(acquisitionId).toLowerCase();
    if (pcActiveAcquisition) {
        var activeId = String(pcActiveAcquisition.AcquisitionId || pcActiveAcquisition.Id).toLowerCase();
        if (activeId === target) {
            return pcActiveAcquisition;
        }
    }
    var grid = pcAcquisitionGrid();
    if (grid && grid.dataSource) {
        var data = grid.dataSource.data();
        for (var j = 0; j < data.length; j++) {
            var gItem = data[j];
            if (String(gItem.Id).toLowerCase() === target || String(gItem.AcquisitionId || '').toLowerCase() === target) {
                return gItem;
            }
        }
    }
    if (pcAcquisitionsData) {
        for (var i = 0; i < pcAcquisitionsData.length; i++) {
            var it = pcAcquisitionsData[i];
            if (String(it.Id).toLowerCase() === target || String(it.AcquisitionId).toLowerCase() === target) {
                return it;
            }
        }
    }
    return null;
}

function pcIsAcquisitionPosted(acquisitionId) {
    var id = acquisitionId || propertyCardSelectedAcquisitionId || pcCurrentAcquisition.id || (pcActiveAcquisition ? (pcActiveAcquisition.AcquisitionId || pcActiveAcquisition.Id) : null);
    if (!id) return false;
    var target = String(id).toLowerCase();
    if (pcCurrentAcquisition.id && String(pcCurrentAcquisition.id).toLowerCase() === target) {
        return !!pcCurrentAcquisition.isPosted;
    }
    if (pcActiveAcquisition) {
        var activeId = String(pcActiveAcquisition.AcquisitionId || pcActiveAcquisition.Id).toLowerCase();
        if (activeId === target) {
            return pcActiveAcquisition.PostedDt != null;
        }
    }
    var matching = pcFindAcquisitionById(id);
    if (matching && matching.PostedDt != null) {
        return true;
    }
    return false;
}

function pcBindAcquisitionSearch($pane) {
    var $searchInput = $pane.find('.pc-acquisition-search');
    var $clearBtn = $pane.find('.pc-acquisition-search-clear');
    var $body = $pane.find('.pc-acquisition-list-body');
    var $searchEmpty = $pane.find('.pc-acquisition-search-empty');

    $searchInput.off('input.pcSearch').on('input.pcSearch', function () {
        var q = $.trim($(this).val()).toLowerCase();
        if (q.length > 0) {
            $clearBtn.show();
        } else {
            $clearBtn.hide();
        }
        var matches = 0;
        var $cards = $body.find('.pc-acquisition-card');
        $cards.each(function () {
            var text = $(this).attr('data-search-text') || '';
            if (!q || text.indexOf(q) >= 0) {
                $(this).show();
                matches++;
            } else {
                $(this).hide();
            }
        });
        if (matches === 0 && $cards.length > 0) {
            $searchEmpty.show();
        } else {
            $searchEmpty.hide();
        }
    });

    $clearBtn.off('click.pcClear').on('click.pcClear', function () {
        $searchInput.val('').trigger('input');
        $(this).hide();
        $searchInput.focus();
    });
}

function pcRenderAcquisitionList(tabName) {
    var $tabContainer = $('.pc-tab-section[data-tab-name="' + tabName + '"]');
    if (!$tabContainer.length) {
        $tabContainer = $('#pcWorkspace');
    }
    var $pane = $tabContainer.find('.pc-master-pane');
    if (!$pane.length) return;

    var $body = $pane.find('.pc-acquisition-list-body');
    var $badge = $pane.find('.pc-acq-badge');
    var $empty = $pane.find('.pc-acquisition-empty');
    var $searchEmpty = $pane.find('.pc-acquisition-search-empty');

    pcEnsureAcquisitionsLoaded().done(function (items) {
        $body.empty();
        $badge.text(items.length);
        if (!items.length) {
            $empty.show();
            $searchEmpty.hide();
            return;
        }
        $empty.hide();
        $searchEmpty.hide();

        if (!propertyCardSelectedAcquisitionId && items.length > 0) {
            var firstItem = pcParseAcquisitionItem(items[0]);
            propertyCardSelectedAcquisitionId = firstItem.id;
        }

        $.each(items, function (index, item) {
            var parsed = pcParseAcquisitionItem(item);
            var isSelected = (propertyCardSelectedAcquisitionId &&
                (String(propertyCardSelectedAcquisitionId).toLowerCase() === String(parsed.id).toLowerCase()));

            var cardHtml = '<div class="pc-acquisition-card' + (isSelected ? ' is-selected' : '') + '" ' +
                'data-acquisition-id="' + parsed.id + '" ' +
                'data-search-text="' + kendo.htmlEncode((parsed.poNo + ' ' + parsed.description + ' ' + parsed.code + ' ' + parsed.location).toLowerCase()) + '" ' +
                'role="option" tabindex="0" aria-selected="' + (isSelected ? 'true' : 'false') + '">' +
                    '<div class="pc-acquisition-card-header">' +
                        '<span class="pc-acquisition-card-po">' + kendo.htmlEncode(parsed.poNo) + '</span>' +
                        '<span class="pc-acquisition-card-code">' + kendo.htmlEncode(parsed.code) + '</span>' +
                    '</div>' +
                    '<div class="pc-acquisition-card-desc">' + kendo.htmlEncode(parsed.description) + '</div>' +
                    '<div class="pc-acquisition-card-footer">' +
                        '<span class="pc-acquisition-card-loc"><i class="fa fa-map-marker-alt"></i> ' + kendo.htmlEncode(parsed.location || 'Origin') + '</span>' +
                        '<span class="pc-acquisition-card-indicator"><i class="fa fa-check"></i></span>' +
                    '</div>' +
                '</div>';

            var $card = $(cardHtml);
            $card.data('pcAcquisitionItem', item);
            $card.data('pcParsed', parsed);
            $body.append($card);
        });

        pcBindAcquisitionSearch($pane);
    });
}

function selectPropertyCardAcquisition(acquisitionId, triggerLoad) {
    if (!acquisitionId) return;
    propertyCardSelectedAcquisitionId = acquisitionId;
    var matchingAcq = pcFindAcquisitionById(acquisitionId);
    if (matchingAcq) {
        pcUpdateCurrentAcquisitionState(acquisitionId, matchingAcq.PostedDt != null, matchingAcq.PostedDt, matchingAcq);
    } else {
        pcUpdateCurrentAcquisitionState(acquisitionId, pcIsAcquisitionPosted(acquisitionId), null, null);
    }

    // Highlight card across all master panes
    $('.pc-acquisition-card').removeClass('is-selected').attr('aria-selected', 'false');
    $('.pc-acquisition-card').filter(function () {
        return String($(this).attr('data-acquisition-id')).toLowerCase() === String(acquisitionId).toLowerCase();
    }).addClass('is-selected').attr('aria-selected', 'true');

    if (triggerLoad !== false) {
        var tab = pcCurrentTabName();
        if (tab === 'Individual Units') {
            pcLoadUnits(acquisitionId);
        } else if (tab === 'Components') {
            pcLoadComponents(acquisitionId);
        } else if (tab === 'Accountability') {
            pcLoadAccountabilityTabForAcquisition(acquisitionId);
        } else if (tab === 'Documents') {
            pcLoadDocumentsTabForAcquisition(acquisitionId);
        }
    }
}

function pcLoadComponents(acquisitionId) {
    var $tabContainer = $('.pc-tab-section[data-tab-name="Components"]');
    var $detailPane = $tabContainer.find('.pc-detail-pane');
    kendo.ui.progress($detailPane.length ? $detailPane : $('#pcWorkspace'), true);

    function updateCompHeader(item) {
        var parsed = pcParseAcquisitionItem(item);
        $('#pcCompAcqPo').text(parsed.poNo ? ('PO #' + parsed.poNo) : 'Acquisition / No PO');
        $('#pcCompAcqDesc').html(kendo.htmlEncode(parsed.description) + (parsed.location ? (' &bull; Location: <strong>' + kendo.htmlEncode(parsed.location) + '</strong>') : ''));
        $('#pcComponentContext').text('Filtered by: ' + (parsed.poNo || 'Acquisition') + ' (' + (parsed.location || 'Origin') + ')');
    }

    pcEnsureAcquisitionsLoaded().done(function (items) {
        var matching = pcFindAcquisitionById(acquisitionId);
        if (matching) {
            updateCompHeader(matching);
        }
        updateComponentsPostedState(pcIsAcquisitionPosted(acquisitionId));
    });

    updateComponentsPostedState(pcIsAcquisitionPosted(acquisitionId));

    var grid = $('.pc-components-grid').data('kendoGrid');
    if (grid) {
        grid.dataSource.read().always(function () {
            kendo.ui.progress($detailPane.length ? $detailPane : $('#pcWorkspace'), false);
        });
    } else {
        kendo.ui.progress($detailPane.length ? $detailPane : $('#pcWorkspace'), false);
    }
}

function pcLoadAccountabilityTabForAcquisition(acquisitionId, scrollTargetUnitId) {
    var $tabContainer = $('.pc-tab-section[data-tab-name="Accountability"]');
    var $detailPane = $tabContainer.find('.pc-detail-pane');
    var $empty = $('#pcAcctEmptyState');
    var $host = $('#pcAccountabilityHost');

    kendo.ui.progress($detailPane.length ? $detailPane : $('#pcWorkspace'), true);

    pcEnsureAcquisitionsLoaded().done(function (items) {
        var matching = pcFindAcquisitionById(acquisitionId);
        if (matching) {
            var parsed = pcParseAcquisitionItem(matching);
            $('#pcAcctAcqPo').text(parsed.poNo ? ('PO #' + parsed.poNo) : 'Acquisition / No PO');
            $('#pcAcctAcqDesc').html(kendo.htmlEncode(parsed.description) + (parsed.location ? (' &bull; Location: <strong>' + kendo.htmlEncode(parsed.location) + '</strong>') : ''));
        }
    });

    $.getJSON(pcWorkspace.choicesUrl, { id: pcWorkspace.id, kind: 'units' }).done(function (units) {
        var matchingUnits = $.grep(units || [], function (u) {
            return u.AcquisitionId === acquisitionId || String(u.AcquisitionId).toLowerCase() === String(acquisitionId).toLowerCase();
        });

        if (!matchingUnits || matchingUnits.length === 0) {
            $host.empty().hide();
            $empty.show();
            kendo.ui.progress($detailPane.length ? $detailPane : $('#pcWorkspace'), false);
            return;
        }

        $empty.hide();
        $host.show();

        var promises = [];
        var unitHtmls = new Array(matchingUnits.length);

        $.each(matchingUnits, function (index, unit) {
            var p = $.ajax({
                url: pcWorkspace.accountabilityUrl,
                data: { id: pcWorkspace.id, unitId: unit.Id },
                type: 'GET'
            }).done(function (html) {
                unitHtmls[index] = html;
            }).fail(function () {
                unitHtmls[index] = '<div class="alert alert-danger" style="margin-bottom: 16px;">Unable to load accountability details for Unit #' + (unit.ContentNo || (index + 1)) + '.</div>';
            });
            promises.push(p);
        });

        $.when.apply($, promises).always(function () {
            kendo.destroy($host);
            $host.empty();
            for (var i = 0; i < unitHtmls.length; i++) {
                if (unitHtmls[i]) {
                    $host.append(unitHtmls[i]);
                }
            }
            kendo.ui.progress($detailPane.length ? $detailPane : $('#pcWorkspace'), false);

            var targetUnit = scrollTargetUnitId || pcPendingUnit;
            if (targetUnit) {
                var $targetEl = $host.find('.pc-accountability-scope[data-unit-id="' + targetUnit + '"]');
                if ($targetEl.length) {
                    $targetEl[0].scrollIntoView({ behavior: 'smooth', block: 'start' });
                }
                pcPendingUnit = null;
            }
        });
    }).fail(function () {
        $host.empty().hide();
        $empty.show();
        pcError('Unable to load physical units for accountability.');
        kendo.ui.progress($detailPane.length ? $detailPane : $('#pcWorkspace'), false);
    });
}

function pcLoadDocumentsTabForAcquisition(acquisitionId) {
    pcEnsureAcquisitionsLoaded().done(function (items) {
        var matching = pcFindAcquisitionById(acquisitionId);
        if (matching) {
            var parsed = pcParseAcquisitionItem(matching);
            $('#pcDocAcqPo').text(parsed.poNo ? ('PO #' + parsed.poNo) : 'Acquisition / No PO');
            $('#pcDocAcqDesc').html(kendo.htmlEncode(parsed.description) + (parsed.location ? (' &bull; Location: <strong>' + kendo.htmlEncode(parsed.location) + '</strong>') : ''));
        }
        updateDocumentsPostedState(pcIsAcquisitionPosted(acquisitionId));
    });

    updateDocumentsPostedState(pcIsAcquisitionPosted(acquisitionId));

    pcLoadDocuments(acquisitionId);

    $.getJSON(pcWorkspace.choicesUrl, { id: pcWorkspace.id, kind: 'units' }).done(function (units) {
        var matchingUnits = $.grep(units || [], function (u) {
            return u.AcquisitionId === acquisitionId || String(u.AcquisitionId).toLowerCase() === String(acquisitionId).toLowerCase();
        });
        var $switcher = $('#pcDocScopeSwitcher');
        var $select = $('#pcDocUnitSelect');
        $select.empty();
        var matching = pcFindAcquisitionById(acquisitionId);
        var acqLabel = matching ? ('Acquisition: ' + (matching.PoNo || 'PO')) : 'Acquisition Documents';
        $select.append($('<option></option>').val(acquisitionId).text(acqLabel));
        if (matchingUnits.length > 0) {
            $.each(matchingUnits, function (i, u) {
                var label = 'Unit: ' + (u.PropNo || u.CustItemNo || ('Unit #' + (i + 1))) + (u.Location ? (' - ' + u.Location) : '');
                $select.append($('<option></option>').val(u.Id).text(label));
            });
            $switcher.show();
        } else {
            $switcher.hide();
        }
    });
}

function pcInitTab(name) {
    if (name === 'Components') {
        pcRenderAcquisitionList('Components');
        if (propertyCardSelectedAcquisitionId) {
            selectPropertyCardAcquisition(propertyCardSelectedAcquisitionId, true);
        } else {
            pcEnsureAcquisitionsLoaded().done(function (items) {
                if (items && items.length) {
                    selectPropertyCardAcquisition(pcParseAcquisitionItem(items[0]).id, true);
                }
            });
        }
    } else if (name === 'Individual Units') {
        pcRenderAcquisitionList('Individual Units');
        if (pcPendingUnitTransfer) {
            propertyCardSelectedAcquisitionId = pcPendingUnitTransfer;
            selectPropertyCardAcquisition(pcPendingUnitTransfer, true);
            pcPendingUnitTransfer = null;
        } else if (propertyCardSelectedAcquisitionId) {
            selectPropertyCardAcquisition(propertyCardSelectedAcquisitionId, true);
        } else {
            pcEnsureAcquisitionsLoaded().done(function (items) {
                if (items && items.length) {
                    selectPropertyCardAcquisition(pcParseAcquisitionItem(items[0]).id, true);
                }
            });
        }
    } else if (name === 'Accountability') {
        pcRenderAcquisitionList('Accountability');
        if (pcPendingUnit) {
            $.getJSON(pcWorkspace.choicesUrl, { id: pcWorkspace.id, kind: 'units' }).done(function (units) {
                var target = String(pcPendingUnit).toLowerCase();
                for (var i = 0; i < (units || []).length; i++) {
                    if (String(units[i].Id).toLowerCase() === target) {
                        propertyCardSelectedAcquisitionId = units[i].AcquisitionId;
                        break;
                    }
                }
                selectPropertyCardAcquisition(propertyCardSelectedAcquisitionId, false);
                pcLoadAccountability(pcPendingUnit);
                pcPendingUnit = null;
            });
        } else if (propertyCardSelectedAcquisitionId) {
            selectPropertyCardAcquisition(propertyCardSelectedAcquisitionId, true);
        } else {
            pcEnsureAcquisitionsLoaded().done(function (items) {
                if (items && items.length) {
                    selectPropertyCardAcquisition(pcParseAcquisitionItem(items[0]).id, true);
                }
            });
        }
    } else if (name === 'Documents') {
        pcRenderAcquisitionList('Documents');
        if (pcPendingDocument) {
            pcLoadDocuments(pcPendingDocument);
            pcPendingDocument = null;
        } else if (propertyCardSelectedAcquisitionId) {
            selectPropertyCardAcquisition(propertyCardSelectedAcquisitionId, true);
        } else {
            pcEnsureAcquisitionsLoaded().done(function (items) {
                if (items && items.length) {
                    selectPropertyCardAcquisition(pcParseAcquisitionItem(items[0]).id, true);
                }
            });
        }
    } else if (name === 'Acquisitions' && /[?&]addAcquisition=true/.test(window.location.search)) {
        var grid = pcAcquisitionGrid();
        if (grid) {
            grid.one('dataBound', function () { grid.addRow(); });
            window.history.replaceState(null, '', window.location.pathname + '?id=' + encodeURIComponent(pcWorkspace.id));
        }
    }
}

var pcUnitLoad = 0;
function pcLoadUnits(transferId) {
    var sequence = ++pcUnitLoad;
    var $tabContainer = $('.pc-tab-section[data-tab-name="Individual Units"]');
    var $detailPane = $tabContainer.find('.pc-detail-pane');
    kendo.ui.progress($detailPane.length ? $detailPane : $('#pcWorkspace'), true);

    $.getJSON(pcWorkspace.unitsUrl, { id: pcWorkspace.id, transferId: transferId }).done(function (result) {
        if (sequence !== pcUnitLoad) { return; }
        pcActiveAcquisition = result.Row;
        propertyCardSelectedAcquisitionId = result.Row.Id;
        selectedItemId = result.Row.Id;
        selectedTransferId = result.Row.TransferId;
        selectedItemExtnId = null;

        var poText = result.Row.PoNo ? ('PO #' + result.Row.PoNo) : 'Acquisition / No PO';
        var descText = (result.Row.Description || '') + (result.Row.Location ? (' &bull; ' + result.Row.Location) : '');
        var receivedQty = result.Row.Qty || 0;

        $('#pcUnitAcqPo').text(poText);
        $('#pcUnitAcqDesc').html((result.Row.Description ? kendo.htmlEncode(result.Row.Description) : '') + (result.Row.Location ? (' &bull; Location: <strong>' + kendo.htmlEncode(result.Row.Location) + '</strong>') : ''));
        $('#pcUnitAcqPoBanner').text(result.Row.PoNo || 'No PO');
        $('#pcUnitAcqDescBanner').html(descText);
        $('#pcUnitQtyReceived').text(pcNumber(receivedQty));
        $('#pcUnitSummaryBanner').show();
        $('#pcUnitActionArea').show();

        if (pcCurrentAcquisition.id && String(pcCurrentAcquisition.id).toLowerCase() === String(result.Row.Id).toLowerCase()) {
            result.Row.PostedDt = pcCurrentAcquisition.isPosted ? (result.Row.PostedDt || pcCurrentAcquisition.postedDt) : null;
        } else {
            pcUpdateCurrentAcquisitionState(result.Row.Id, result.Row.PostedDt != null, result.Row.PostedDt, result.Row);
        }
        updateIndividualUnitsPostedState(pcIsAcquisitionPosted(result.Row.Id));

        $('#pcUnitContext').text('PO ' + (result.Row.PoNo || '-') + ' &bull; ' + (result.Row.Location || 'Origin') + ' &bull; Received Qty: ' + pcNumber(receivedQty));

        var grid = $('.pc-units-grid').data('kendoGrid');
        if (grid) {
            grid.dataSource.read();
        }
    }).fail(function () {
        pcError('Unable to load the selected acquisition units.');
    }).always(function () {
        if (sequence === pcUnitLoad) {
            kendo.ui.progress($detailPane.length ? $detailPane : $('#pcWorkspace'), false);
        }
    });
}
function pcViewUnits(transferId) {
    propertyCardSelectedAcquisitionId = transferId;
    pcOpenTab('Individual Units').done(function () { selectPropertyCardAcquisition(transferId, true); });
}
function onChangeGridItemExtn() {
    var item = this.dataItem(this.select()); selectedItemExtnId = item ? item.Id : null; selectedGrid = this;
}
function onDataBoundGridItemExtn() {
    var grid = this;
    if (pcActiveAcquisition && pcActiveAcquisition.ParentId) { grid.wrapper.find('.k-grid-add').hide(); }
    if (pcActiveAcquisition && pcActiveAcquisition.PostedDt != null) { grid.wrapper.find('.k-grid-add').hide(); }

    var data = grid.dataSource.data();
    var totalUnits = data.length;
    var availableCount = 0;
    var accountableCount = 0;

    forEachKendoDataRow(grid, function (row, item) {
        var s = (item.AccountabilityStatus || '').toUpperCase();
        if (s === 'AVAILABLE' || s.indexOf('AVAILABLE') >= 0) {
            availableCount++;
        } else {
            accountableCount++;
        }

        var isPostedUnit = (pcActiveAcquisition && pcActiveAcquisition.PostedDt != null) || pcIsAcquisitionPosted();
        if (item.CanDelete === false || isPostedUnit) {
            row.find('.k-grid-Delete, .k-grid-delete').hide();
        }
        if (item.CanEdit === false || isPostedUnit) {
            var editBtn = row.find('.k-grid-edit');
            editBtn.contents().filter(function () { return this.nodeType === 3; }).replaceWith(' View');
            editBtn.attr('title', isPostedUnit ? 'View Unit Details (Acquisition Posted - Read Only)' : 'View Unit Details (Accountable - Read Only)');
        }
    });

    $('#pcUnitQtyUnits').text(totalUnits);
    $('#pcUnitQtyAvailable').text(availableCount);
    $('#pcUnitQtyAccountable').text(accountableCount);

    var item = selectedItemExtnId ? grid.dataSource.get(selectedItemExtnId) : null;
    if (item) { grid.select(grid.tbody.children("tr[data-uid='" + item.uid + "']")); }
    updateIndividualUnitsPostedState(pcIsAcquisitionPosted());
}
function onRequestEndGridItemExtn(e) {
    if (e.response && !e.response.Errors && !e.response.errors && e.type !== 'read') {
        var source = this;
        if (e.type === 'create' && e.response.Data && e.response.Data.length) { selectedItemExtnId = e.response.Data[0].Id; }
        setTimeout(function () { source.read(); var grid = pcAcquisitionGrid(); if (grid) { grid.dataSource.read(); } pcAfterMutation(); }, 0);
    }
}
function onEditGridItemExtn(e) {
    selectedGrid = e.sender;
    clearPropertyCardPopupError(e.container);
    initializePropertyCardPopupValidation(e.container);
    e.container.find('.k-grid-update').off('click.pcPopupValidation').on('click.pcPopupValidation', function () {
        clearPropertyCardPopupError(e.container);
    });
    var parentModel = pcActiveAcquisition || pcWorkspaceParent(e.sender);

    if (e.model.isNew()) {
        selectedItemExtnId = null;
        if (parentModel) {
            e.model.set("PsCardItemId", parentModel.Id);
            e.model.set("TransferId", parentModel.TransferId);
            e.model.set("LocationId", parentModel.LocationId);
            e.model.set("Location", parentModel.Location);
            e.model.set("TContentNo", parentModel.TContentNo);
        }
        e.model.set("PsCardSubItemId", null);
    } else {
        selectedItemExtnId = e.model.get("Id");
        if (e.model.CanEdit === false) {
            var win = e.container.data("kendoWindow");
            if (win) { win.title("View Individual Unit (Accountable - Read Only)"); }
            e.container.find(".k-grid-update").hide();
            e.container.find("input, textarea, select").prop("readonly", true);
        }
    }
}
function onRemoveGridItemExtn(e) {
    selectedGrid = e.sender;
}
function pcDeleteUnit(e) {
    e.preventDefault();
    var grid = this, row = $(e.currentTarget).closest('tr'), item = grid.dataItem(row), btn = e.currentTarget;
    if (!item) { return; }
    if (item.CanDelete === false) {
        epsmsAlert({
            title: "Cannot Delete Physical Unit",
            type: "warning",
            headline: "Deletion Prohibited",
            message: "This physical unit cannot be deleted because it is assigned to an accountability record (PAR/ICS), transferred, issued, or generated from an accepted AIR."
        });
        return;
    }
    selectedGrid = grid;
    var propNo = item.PropNo || item.CustItemNo || item.PropertyNo || ("Unit #" + (item.ContentNo || ""));

    epsmsConfirm({
        title: "Delete Individual Unit",
        type: "danger",
        headline: "Delete Individual Physical Unit?",
        entityLabel: "Property No.",
        entityValue: propNo,
        message: "This will permanently remove this physical property unit from the Property Card. This action cannot be undone.",
        cancelText: "Cancel",
        confirmText: "Delete Unit",
        confirmIcon: "k-i-delete",
        onConfirm: function (dialog, $confirmBtn) {
            setClickedButtonBusy($confirmBtn, "Deleting...");
            if (!propertyCardButtonStart(btn, "Deleting...")) { return; }
            grid.dataSource.remove(item);
            grid.dataSource.sync()
                .done(function () {
                    restoreClickedButton($confirmBtn);
                    dialog.close();
                    notifySuccess("Physical unit deleted successfully.");
                    if (typeof pcRefreshWorkspaceStats === "function") {
                        pcRefreshWorkspaceStats();
                    }
                    pcRefreshPosition();
                })
                .fail(function (xhr) {
                    restoreClickedButton($confirmBtn);
                    grid.dataSource.read();
                    notifyWarning(ajaxFailureMessage(xhr, "Unable to delete the physical unit."));
                })
                .always(function () {
                    propertyCardButtonStop(btn);
                });
        }
    });
}
function pcUnitDocuments(e) {
    e.preventDefault();
    var item = this.dataItem($(e.currentTarget).closest('tr'));
    if (!item) return;
    pcPendingDocument = item.Id;
    if (item.PsCardItemId) {
        propertyCardSelectedAcquisitionId = item.PsCardItemId;
    }
    pcOpenTab('Documents').done(function () {
        if (propertyCardSelectedAcquisitionId) {
            selectPropertyCardAcquisition(propertyCardSelectedAcquisitionId, false);
        }
        pcLoadDocuments(item.Id);
    });
}
function pcUnitAccountability(e) {
    e.preventDefault();
    var item = this.dataItem($(e.currentTarget).closest('tr'));
    if (!item) return;
    pcPendingUnit = item.Id;
    if (item.PsCardItemId) {
        propertyCardSelectedAcquisitionId = item.PsCardItemId;
    }
    pcOpenTab('Accountability').done(function () {
        if (propertyCardSelectedAcquisitionId) {
            selectPropertyCardAcquisition(propertyCardSelectedAcquisitionId, true);
        }
    });
}
function pcLoadPartial(host, url, data, context) {
    var generation = (host.data('generation') || 0) + 1; host.data('generation', generation);
    kendo.ui.progress(host, true);
    $.get(url, data).done(function (html) {
        if (host.data('generation') !== generation) { return; }
        kendo.destroy(host); host.empty().html(html);
        if (context) {
            context.acquisitionId = host.find('.pc-document-scope').attr('data-acquisition-id');
            var grid = host.find('.k-grid').data('kendoGrid');
            if (grid) {
                grid.element.data('pc-upload-context', context);
                grid.dataSource.bind('error', GridError);
                var assign = function () {
                    $.each(grid.dataSource.data(), function (_, item) {
                        item.PsCardItemId = context.acquisitionId;
                    });
                    var isPosted = pcIsAcquisitionPosted(context.acquisitionId);
                    updateDocumentsPostedState(isPosted);
                };
                grid.bind('dataBound', assign); assign();
            }
        }
    }).fail(function () { pcError('Unable to load the selected records.'); })
      .always(function () { if (host.data('generation') === generation) { kendo.ui.progress(host, false); } });
}
function pcLoadAccountability(id) { pcLoadPartial($('#pcAccountabilityHost'), pcWorkspace.accountabilityUrl, { id: pcWorkspace.id, unitId: id }); }
function pcLoadDocuments(id) { pcLoadPartial($('#pcDocumentsHost'), pcWorkspace.documentsUrl, { id: pcWorkspace.id, imageId: id }, { imageId: id, controller: 'CardUpload' }); }
function onEditGridImages(e) {
    selectedGrid = e.sender;
    selectedGridImagesItem = e.model.Id;
    clearPropertyCardPopupError(e.container);
    initializePropertyCardPopupValidation(e.container);
    e.container.find('.k-grid-update').off('click.pcPopupValidation').on('click.pcPopupValidation', function () {
        clearPropertyCardPopupError(e.container);
    });
    var context = e.sender.element.data('pc-upload-context');
    if (context) { e.model.set('PsCardItemId', context.acquisitionId); }
    var targetAcqId = context ? context.acquisitionId : (pcActiveAcquisition ? (pcActiveAcquisition.AcquisitionId || pcActiveAcquisition.Id) : null);
    if (pcIsAcquisitionPosted(targetAcqId)) {
        var win = e.container.data("kendoWindow");
        if (win) { win.title("View Document"); }
        e.container.find('.k-grid-update').hide();
        e.container.find(':input').prop('readonly', true);
    }
}
function onClickUpload(e) {
    var button = e.sender.element, grid = $('#' + button.data('gridname')).data('kendoGrid');
    var context = grid.element.data('pc-upload-context') || { imageId: button.data('imageid'), controller: 'ItemCard' };
    if (!context.imageId) { pcError('No document parent was selected.'); return; }
    var targetAcqId = context.acquisitionId || (pcActiveAcquisition ? (pcActiveAcquisition.AcquisitionId || pcActiveAcquisition.Id) : null);
    if (pcIsAcquisitionPosted(targetAcqId)) {
        if (typeof notifyWarning === 'function') {
            notifyWarning('This acquisition is posted. Documents are read-only.');
        } else {
            pcError('This acquisition is posted. Documents are read-only.');
        }
        return;
    }
    pcUploadOwner = grid; selectedGrid = grid; propertyCardConfig.uploadContext = context;
    var url = context.controller === 'CardUpload' ? propertyCardConfig.urls.CardUpload__ImagesAdd : propertyCardConfig.urls.ItemCard__ImagesAdd;
    if (!url) { url = propertyCardConfig.workspace.itemUploadUrl; }
    pcWindow('Upload Images / Documents', url, { imageId: context.imageId, description: button.data('description') });
}
function UploadPara(e) {
    clearPropertyCardPopupError($('#formUpload').closest('.k-window-content'));
    var context = propertyCardConfig.uploadContext || {};
    e.data = { imageId: context.imageId || $('#ImageId').val(), PsCardItemId: context.acquisitionId || $('#PsCardItemId').val(), Description: $('#formUpload #Description').val() };
}
function onUploadSuccess() {
    var $popup = $('#formUpload').closest('.k-window-content');
    if ($popup.length && $popup.data('kendoWindow')) {
        $popup.data('kendoWindow').close();
    }
    if (pcUploadOwner) {
        pcUploadOwner.dataSource.read();
    }
}
function onUploadError(e) {
    var err = (e && e.XMLHttpRequest && e.XMLHttpRequest.responseText) ? e.XMLHttpRequest.responseText : (e ? e.responseText || e.message : 'Upload failed.');
    try {
        var parsed = typeof err === 'string' ? JSON.parse(err) : err;
        if (parsed.message) { err = parsed.message; }
        else if (parsed.Errors) { err = parsed.Errors; }
        else if (parsed.AddError) { err = parsed.AddError; }
    } catch (_) { }
    var $popup = $('#formUpload').closest('.k-window-content');
    if ($popup.length) {
        showPropertyCardPopupError($popup, err);
    } else {
        notifyWarning(err);
    }
}
window.onError = onUploadError;
window.onUploadError = onUploadError;
var pcOriginalAddCostCalculation = typeof GetAddCost !== 'undefined' ? GetAddCost : function () { };
function onClickAddCost() {
    var id = selectedItemExtnId || $('#Id').val();
    if (!id || id === '00000000-0000-0000-0000-000000000000') { notifyWarning('Please save the individual unit before adding additional costs.'); return; }
    pcWindow('Additional Cost', propertyCardConfig.urls.ItemCard__AddCost, { psCardItemExtnId: id }, function () {
        if ($('#AddCost').data('kendoNumericTextBox') && $('#AcqCost').data('kendoNumericTextBox')) { pcOriginalAddCostCalculation(id); }
        pcAfterMutation();
    });
}
function UploadAddCost(imageId, postedBy) {
    pcWindow('Additional Cost Documents', propertyCardConfig.urls.ItemCard__Images, { imageId: imageId, postedBy: postedBy, description: 'ADDITIONAL COST' });
}
function onClickTransfer(e) {
    e.preventDefault();
    var item = this.dataItem($(e.currentTarget).closest('tr'));
    pcSetAcquisition(this, item);
    pcWindow('Transfer PO Item to another Stock/Property Card', propertyCardConfig.urls.StockCard__PoTransfer,
        { psCardItemId: item.Id }, function () { var grid = pcAcquisitionGrid(); if (grid) { grid.dataSource.read(); } pcAfterMutation(); });
}
function PrintPo(originalSw) {
    pcWindow('PO Preview', propertyCardConfig.urls.StockCard_StockCardPoRpt,
        { selectedItemId: selectedItemId, originalSw: originalSw }, null, true);
}
function pcGridError(args, fallback) {
    var grid = fallback, messages = [];
    $('.k-grid').each(function () { var candidate = $(this).data('kendoGrid'); if (candidate && candidate.dataSource === args.sender) { grid = candidate; } });
    $.each(args.errors || {}, function (_, entry) { messages = messages.concat(entry.errors || [String(entry)]); });
    if (!messages.length) { messages.push('The operation failed. Refresh the records and retry.'); }
    var combinedMessage = messages.join(' ');

    if (grid && grid.editable) {
        // Prevent Kendo Grid from closing the popup or rebinding
        grid.one('dataBinding', function (e) { e.preventDefault(); });
        // Display the error strictly inside the popup
        showPropertyCardPopupError(grid.editable.element, combinedMessage);
        // Do NOT call pcError to avoid caller-window error duplication
        return;
    }

    // Non-popup error -> route to caller-window notification
    pcError(combinedMessage);
    if (grid && grid.dataSource.hasChanges()) {
        grid.dataSource.cancelChanges();
        grid.dataSource.read();
    }
}
function GridError(args) { pcGridError(args, selectedGrid); }
function GridItemError(args) { var grid = pcAcquisitionGrid(); pcGridError(args, grid); }
function GridErrorAddCost(args) { pcGridError(args, $('#gridAddCost').data('kendoGrid')); }

function pcUnitStatusBadge(data) {
    var status = data.AccountabilityStatus || 'AVAILABLE';
    var isAccountable = data.CanEdit === false;
    var badgeClass = 'k-badge-solid-base';

    if (status === 'AVAILABLE') {
        badgeClass = 'k-badge-solid-success';
    } else if (status.indexOf('PAR') >= 0) {
        badgeClass = 'k-badge-solid-primary';
    } else if (status.indexOf('ICS') >= 0) {
        badgeClass = 'k-badge-solid-info';
    } else if (status.indexOf('DRAFT') >= 0) {
        badgeClass = 'k-badge-solid-warning';
    } else if (status.indexOf('TRANSFERRED') >= 0) {
        badgeClass = 'k-badge-solid-secondary';
    } else if (status.indexOf('ISSUED') >= 0) {
        badgeClass = 'k-badge-solid-dark';
    }

    var icon = isAccountable ? '<i class="fa fa-lock" style="margin-right:3px;"></i>' : '';
    return '<span class="k-badge ' + badgeClass + '" style="font-size:11px;">' + icon + kendo.htmlEncode(status) + '</span>';
}

function pcComponentSourceBadge(data) {
    var type = data.SourceType || 'ORDERED';
    var cls = type === 'ORDERED' ? 'k-badge-solid-primary' :
              type === 'EXISTING_INVENTORY' ? 'k-badge-solid-info' : 'k-badge-solid-secondary';
    return '<span class="k-badge ' + cls + '" style="font-size:11px;">' + kendo.htmlEncode(type) + '</span>';
}

function pcComponentRequiredBadge(data) {
    if (data.IsRequiredForBundle) {
        return '<span class="k-badge k-badge-solid-success" style="font-size:11px;"><i class="fa fa-check"></i> Required</span>';
    }
    return '<span class="k-badge k-badge-solid-base" style="font-size:11px;">Optional</span>';
}

function pcComponentUnitStatusBadge(data) {
    var status = data.AccountabilityStatus || 'AVAILABLE';
    var cls = status === 'AVAILABLE' ? 'k-badge-solid-success' :
              status.indexOf('ASSIGNED') >= 0 ? 'k-badge-solid-primary' :
              status.indexOf('DRAFT') >= 0 ? 'k-badge-solid-warning' :
              status.indexOf('TRANSFERRED') >= 0 || status.indexOf('Transferred') >= 0 ? 'k-badge-solid-info' : 'k-badge-solid-success';
    return '<span class="k-badge ' + cls + '" style="font-size:11px;">' + kendo.htmlEncode(status) + '</span>';
}

function pcComponentReadData() {
    var acquisitionId = propertyCardSelectedAcquisitionId;
    if (!acquisitionId) {
        var choice = $('#pcComponentAcquisitionChoice').data('kendoDropDownList');
        if (choice && choice.value()) {
            var choiceItem = choice.dataItem();
            acquisitionId = (choiceItem && choiceItem.AcquisitionId) ? choiceItem.AcquisitionId : choice.value();
        } else if (pcActiveAcquisition) {
            acquisitionId = pcActiveAcquisition.AcquisitionId || pcActiveAcquisition.Id || null;
        }
    }
    return { acquisitionId: acquisitionId };
}

function onErrorGridComponents(args) {
    pcGridError(args, $('.pc-components-grid').data('kendoGrid'));
}

function onRequestEndGridComponents(e) {
    if (e.response && !e.response.Errors && !e.response.errors) {
        if (e.type === "create" || e.type === "update" || e.type === "destroy") {
            var grid = $('.pc-components-grid').data('kendoGrid');
            if (grid) { grid.dataSource.read(); }
            pcRefreshPosition();
        }
    }
}

function onEditGridComponents(e) {
    var win = e.container.data("kendoWindow");
    clearPropertyCardPopupError(e.container);
    initializePropertyCardPopupValidation(e.container);
    e.container.find('.k-grid-update').off('click.pcPopupValidation').on('click.pcPopupValidation', function () {
        clearPropertyCardPopupError(e.container);
    });
    var acqId = propertyCardSelectedAcquisitionId ||
                ((choiceItem && choiceItem.AcquisitionId) ? choiceItem.AcquisitionId :
                (pcActiveAcquisition ? (pcActiveAcquisition.AcquisitionId || pcActiveAcquisition.Id) : null));
    var isPosted = pcIsAcquisitionPosted(acqId);

    if (e.model.isNew()) {
        if (win) { win.title("Add Property Card Component"); }
        var choice = $('#pcComponentAcquisitionChoice').data('kendoDropDownList');
        var choiceItem = choice ? choice.dataItem() : null;
        var targetAcqId = acqId || ((choiceItem && choiceItem.AcquisitionId) ? choiceItem.AcquisitionId : null);
        if (targetAcqId) { e.model.set("PsCardItemId", targetAcqId); }
    } else {
        if (isPosted) {
            if (win) { win.title("View Component: " + (e.model.Description || '')); }
            e.container.find('.k-grid-update').hide();
            e.container.find(':input').prop('readonly', true);
            e.container.find('[data-role="dropdownlist"], [data-role="combobox"], [data-role="numerictextbox"]').each(function () {
                var widget = kendo.widgetInstance($(this));
                if (widget && widget.enable) { widget.enable(false); }
            });
        } else {
            if (win) { win.title("Edit Component: " + (e.model.Description || '')); }
            if (e.model.IsAirSource === true) {
                e.container.find("#pcComponentAirAlert").show();
                e.container.find("#Description, #Unit, #Qty, #QtyPerParent, #SourceType").prop("readonly", true);
                var dd = e.container.find("#SourceType").data("kendoDropDownList"); if (dd) { dd.enable(false); }
                var n1 = e.container.find("#Qty").data("kendoNumericTextBox"); if (n1) { n1.enable(false); }
                var n2 = e.container.find("#QtyPerParent").data("kendoNumericTextBox"); if (n2) { n2.enable(false); }
            }
        }
    }
}

function onDataBoundGridComponents(e) {
    var grid = this;
    var acqId = propertyCardSelectedAcquisitionId || (pcActiveAcquisition ? (pcActiveAcquisition.AcquisitionId || pcActiveAcquisition.Id) : null);
    var matching = pcFindAcquisitionById(acqId);
    var isPosted = (matching && matching.PostedDt != null) || (pcActiveAcquisition && pcActiveAcquisition.PostedDt != null);
    if (isPosted) {
        grid.wrapper.find('.k-grid-add').hide();
    }
    forEachKendoDataRow(grid, function (row, item) {
        if (item.CanDelete === false || isPosted) {
            row.find('.k-grid-delete, .k-grid-Delete').hide();
        }
        if (item.CanEdit === false || isPosted) {
            var editBtn = row.find('.k-grid-edit');
            editBtn.contents().filter(function () { return this.nodeType === 3; }).replaceWith(' View');
            editBtn.attr('title', isPosted ? 'View Component Details (Acquisition Posted - Read Only)' : 'View Component Details (Read Only)');
        }
    });
}

function pcComponentUnitCreateData(e) {
    var compGrid = $(e).closest('.k-grid');
    var compId = compGrid.data('component-id') || (e ? e.PsCardSubItemId : null);
    var acqId = propertyCardSelectedAcquisitionId || (pcActiveAcquisition ? (pcActiveAcquisition.AcquisitionId || pcActiveAcquisition.Id) : null);
    return {
        psCardItemId: acqId,
        psCardSubItemId: compId
    };
}

function onErrorGridComponentUnits(args) {
    pcGridError(args, $('.pc-child-unit-grid').data('kendoGrid'));
}

function onRequestEndGridComponentUnits(e) {
    if (e.response && !e.response.Errors && !e.response.errors) {
        if (e.type === "create" || e.type === "update" || e.type === "destroy") {
            var grid = $('.pc-components-grid').data('kendoGrid');
            if (grid) { grid.dataSource.read(); }
            pcRefreshPosition();
        }
    }
}

function onEditGridComponentUnits(e) {
    var win = e.container.data("kendoWindow");
    clearPropertyCardPopupError(e.container);
    initializePropertyCardPopupValidation(e.container);
    e.container.find('.k-grid-update').off('click.pcPopupValidation').on('click.pcPopupValidation', function () {
        clearPropertyCardPopupError(e.container);
    });
    var parentGrid = e.container.closest('.pc-child-units-detail').find('.pc-child-unit-grid');
    var compId = parentGrid.attr('data-component-id');
    var acqId = propertyCardSelectedAcquisitionId || (pcActiveAcquisition ? (pcActiveAcquisition.AcquisitionId || pcActiveAcquisition.Id) : null);

    var isPosted = pcIsAcquisitionPosted(acqId);

    if (e.model.isNew()) {
        if (win) { win.title("Add Component Physical Unit"); }
        if (compId) { e.model.set("PsCardSubItemId", compId); }
        if (acqId) { e.model.set("PsCardItemId", acqId); }
        e.model.set("Condition", "Good");
    } else {
        if (e.model.CanEdit === false || isPosted) {
            if (isPosted) {
                if (win) { win.title("View Component Unit: " + (e.model.SerialNo || ('Unit #' + e.model.ContentNo))); }
            } else {
                if (win) { win.title("Edit Component Unit: " + (e.model.SerialNo || ('Unit #' + e.model.ContentNo))); }
            }
            e.container.find("#pcCompUnitAccountableAlert").show();
            e.container.find('input[name="SerialNo"], input[name="PropertyNo"], input[name="CustItemNo"]').prop('readonly', true).addClass('k-state-disabled');
            e.container.find(".k-grid-update").hide();
        } else {
            if (win) { win.title("Edit Component Unit: " + (e.model.SerialNo || ('Unit #' + e.model.ContentNo))); }
        }
    }
}

function onDataBoundGridComponentUnits(e) {
    var grid = this;
    var acqId = propertyCardSelectedAcquisitionId || (pcActiveAcquisition ? (pcActiveAcquisition.AcquisitionId || pcActiveAcquisition.Id) : null);
    var matching = pcFindAcquisitionById(acqId);
    var isPosted = (matching && matching.PostedDt != null) || (pcActiveAcquisition && pcActiveAcquisition.PostedDt != null);
    if (isPosted) {
        grid.wrapper.find('.k-grid-add').hide();
    }
    forEachKendoDataRow(grid, function (row, item) {
        if (item.CanDelete === false || isPosted) {
            row.find('.k-grid-delete, .k-grid-Delete').hide();
        }
        if (item.CanEdit === false || isPosted) {
            var editBtn = row.find('.k-grid-edit');
            editBtn.contents().filter(function () { return this.nodeType === 3; }).replaceWith(' View');
            editBtn.attr('title', isPosted ? 'View Component Unit Details (Acquisition Posted - Read Only)' : 'View Component Unit Details (Accountable - Read Only)');
        }
    });
}

window.pcCurrentAcquisition = pcCurrentAcquisition;
window.pcUpdateCurrentAcquisitionState = pcUpdateCurrentAcquisitionState;
window.applyAcquisitionPostedState = applyAcquisitionPostedState;
window.updateIndividualUnitsPostedState = updateIndividualUnitsPostedState;
window.updateComponentsPostedState = updateComponentsPostedState;
window.updateDocumentsPostedState = updateDocumentsPostedState;
window.forEachKendoDataRow = forEachKendoDataRow;
window.pcIsAcquisitionPosted = pcIsAcquisitionPosted;
window.pcUnitStatusBadge = pcUnitStatusBadge;
window.pcComponentSourceBadge = pcComponentSourceBadge;
window.pcComponentRequiredBadge = pcComponentRequiredBadge;
window.pcComponentUnitStatusBadge = pcComponentUnitStatusBadge;
window.pcComponentReadData = pcComponentReadData;
window.onErrorGridComponents = onErrorGridComponents;
window.onRequestEndGridComponents = onRequestEndGridComponents;
window.onEditGridComponents = onEditGridComponents;
window.onDataBoundGridComponents = onDataBoundGridComponents;
window.pcComponentUnitCreateData = pcComponentUnitCreateData;
window.onErrorGridComponentUnits = onErrorGridComponentUnits;
window.onRequestEndGridComponentUnits = onRequestEndGridComponentUnits;
window.onEditGridComponentUnits = onEditGridComponentUnits;
window.onDataBoundGridComponentUnits = onDataBoundGridComponentUnits;
window.pcAcquisitionReference = pcAcquisitionReference;
window.pcDescription = pcDescription;
window.pcAcquisitionCost = pcAcquisitionCost;
window.pcAcquisitionLocation = pcAcquisitionLocation;
window.pcAcquisitionMovement = pcAcquisitionMovement;
window.pcAcquisitionMore = pcAcquisitionMore;
window.selectPropertyCardAcquisition = selectPropertyCardAcquisition;
window.pcRenderAcquisitionList = pcRenderAcquisitionList;

function pcIndividualUnitCreateData() {
    var acqId = propertyCardSelectedAcquisitionId;
    if (!acqId) {
        var choice = $('#pcAcquisitionChoice').data('kendoDropDownList');
        if (choice && choice.value()) {
            var choiceItem = choice.dataItem();
            acqId = (choiceItem && choiceItem.AcquisitionId) ? choiceItem.AcquisitionId : choice.value();
        } else if (pcActiveAcquisition) {
            acqId = pcActiveAcquisition.AcquisitionId || pcActiveAcquisition.Id || null;
        }
    }
    return {
        psCardItemId: acqId,
        acquisitionId: acqId
    };
}

function pcUnitReadData() {
    var acquisitionId = propertyCardSelectedAcquisitionId;
    if (!acquisitionId) {
        var choice = $('#pcAcquisitionChoice').data('kendoDropDownList');
        if (choice && choice.value()) {
            var choiceItem = choice.dataItem();
            acquisitionId = (choiceItem && choiceItem.AcquisitionId) ? choiceItem.AcquisitionId : choice.value();
        } else if (pcActiveAcquisition) {
            acquisitionId = pcActiveAcquisition.AcquisitionId || pcActiveAcquisition.Id || null;
        }
    }
    return { acquisitionId: acquisitionId };
}

function onDataBoundGridUnits() {
    var grid = this;
    var data = grid.dataSource.data();
    var totalUnits = data.length;
    var availableCount = 0;
    var accountableCount = 0;

    forEachKendoDataRow(grid, function (row, item) {
        var s = (item.AccountabilityStatus || '').toUpperCase();
        if (s === 'AVAILABLE' || s.indexOf('AVAILABLE') >= 0) {
            availableCount++;
        } else {
            accountableCount++;
        }

        var isPostedUnit = (pcActiveAcquisition && pcActiveAcquisition.PostedDt != null) || pcIsAcquisitionPosted();
        if (item.CanDelete === false || isPostedUnit) {
            row.find('.k-grid-delete, .k-grid-Delete').hide();
        }
        if (item.CanEdit === false || isPostedUnit) {
            var editBtn = row.find('.k-grid-edit');
            editBtn.contents().filter(function () { return this.nodeType === 3; }).replaceWith(' View');
            editBtn.attr('title', isPostedUnit ? 'View Unit Details (Acquisition Posted - Read Only)' : 'View Unit Details (Accountable - Read Only)');
        }
    });

    $('#pcUnitQtyUnits').text(totalUnits);
    $('#pcUnitQtyAvailable').text(availableCount);
    $('#pcUnitQtyAccountable').text(accountableCount);

    var maxQty = pcActiveAcquisition ? (pcActiveAcquisition.Qty || 0) : 0;
    var remaining = Math.max(0, maxQty - totalUnits);
    $('#pcUnitQtyRemaining').text(remaining);

    var addBtn = $('#pcBtnAddUnit');
    if (remaining <= 0) {
        addBtn.prop('disabled', true).addClass('k-disabled').attr('title', 'All ' + maxQty + ' units have been created for this acquisition.');
        $('#pcUnitRemainingStatus').text('All ' + maxQty + ' physical units created.');
    } else {
        addBtn.prop('disabled', false).removeClass('k-disabled').attr('title', '');
        $('#pcUnitRemainingStatus').text(remaining + ' unit' + (remaining === 1 ? '' : 's') + ' remaining to create.');
    }

    if (totalUnits === 0) {
        $('#pcEmptyMaxQty').text(maxQty);
        $('#pcUnitsEmptyState').show();
        $('#pcUnitsGridContainer').hide();
    } else {
        $('#pcUnitsEmptyState').hide();
        $('#pcUnitsGridContainer').show();
    }

    var item = selectedItemExtnId ? grid.dataSource.get(selectedItemExtnId) : null;
    if (item) { grid.select(grid.tbody.children("tr[data-uid='" + item.uid + "']")); }
    updateIndividualUnitsPostedState(pcIsAcquisitionPosted());
}

function onEditGridUnits(e) {
    selectedGrid = e.sender;
    clearPropertyCardPopupError(e.container);
    initializePropertyCardPopupValidation(e.container);
    e.container.find('.k-grid-update').off('click.pcPopupValidation').on('click.pcPopupValidation', function () {
        clearPropertyCardPopupError(e.container);
    });
    if (e.model.isNew()) {
        selectedItemExtnId = null;
        e.model.set('PsCardSubItemId', null);
        var acqId = propertyCardSelectedAcquisitionId || (pcActiveAcquisition ? (pcActiveAcquisition.AcquisitionId || pcActiveAcquisition.Id) : null);
        if (acqId) {
            e.model.set('PsCardItemId', acqId);
        }
        if (pcActiveAcquisition) {
            if (pcActiveAcquisition.UnitCost) {
                e.model.set('AcquisitionCost', pcActiveAcquisition.UnitCost);
            }
            if (pcActiveAcquisition.LocationId) {
                e.model.set('LocationId', pcActiveAcquisition.LocationId);
            }
        }
        e.model.set('Condition', 'Good');
        e.container.kendoWindow('title', 'Add Individual Physical Unit');
    } else {
        selectedItemExtnId = e.model.Id;
        e.container.kendoWindow('title', e.model.CanEdit ? 'Edit Individual Physical Unit' : 'View Individual Physical Unit');
        if (e.model.CanEdit === false) {
            e.container.find('#pcUnitAccountableAlert').show();
            e.container.find('input[name="PropertyNo"]').prop('readonly', true).addClass('k-state-disabled');
            e.container.find('input[name="SerialNo"]').prop('readonly', true).addClass('k-state-disabled');
            e.container.find('input[name="CustItemNo"]').prop('readonly', true).addClass('k-state-disabled');
            e.container.find('.k-grid-update').hide();
        }
    }
}

function onErrorGridUnits(args) {
    pcGridError(args, $('.pc-units-grid').data('kendoGrid'));
}

function onRequestEndGridUnits(e) {
    if (e.response && !e.response.Errors && !e.response.errors) {
        if (e.type === 'create' || e.type === 'update' || e.type === 'destroy') {
            var grid = $('.pc-units-grid').data('kendoGrid');
            if (grid) { grid.dataSource.read(); }
            pcAfterMutation();
        }
    }
}

function pcTriggerAddUnit() {
    var grid = $('.pc-units-grid').data('kendoGrid');
    if (!grid) return;
    if (pcActiveAcquisition) {
        var totalUnits = grid.dataSource.data().length;
        var maxQty = pcActiveAcquisition.Qty || 0;
        if (totalUnits >= maxQty) {
            pcError('Cannot add another individual unit. This acquisition has a received quantity of ' + maxQty + ' and already has ' + totalUnits + ' physical units.');
            return;
        }
    }
    $('#pcUnitsEmptyState').hide();
    $('#pcUnitsGridContainer').show();
    grid.addRow();
}

window.pcIndividualUnitCreateData = pcIndividualUnitCreateData;
window.pcUnitReadData = pcUnitReadData;
window.onDataBoundGridUnits = onDataBoundGridUnits;
window.onEditGridUnits = onEditGridUnits;
window.onErrorGridUnits = onErrorGridUnits;
window.onRequestEndGridUnits = onRequestEndGridUnits;
window.pcTriggerAddUnit = pcTriggerAddUnit;
window.pcAcquisitionUnitsButton = pcAcquisitionUnitsButton;
window.pcPostAcquisitionItem = pcPostAcquisitionItem;
window.pcUnpostAcquisitionItem = pcUnpostAcquisitionItem;
window.postAcquisition = pcPostAcquisitionItem;
window.onPostAcquisition = pcPostAcquisitionItem;
window.propertyCardPost = pcPostAcquisitionItem;
window.postItem = pcPostAcquisitionItem;
window.unpostItem = pcUnpostAcquisitionItem;
window.pcAcquisitionMenu = pcAcquisitionMenu;
window.pcOpenAcquisitionMenu = pcOpenAcquisitionMenu;
window.pcCloseAcquisitionMenu = pcCloseAcquisitionMenu;
if (typeof IsPosted !== 'undefined') { window.IsPosted = IsPosted; }
if (typeof onEditGridItems !== 'undefined') { window.onEditGridItems = onEditGridItems; }
if (typeof onChangeGridItems !== 'undefined') { window.onChangeGridItems = onChangeGridItems; }
if (typeof onDataBoundGridItems !== 'undefined') { window.onDataBoundGridItems = onDataBoundGridItems; }
if (typeof onRequestEndGridItems !== 'undefined') { window.onRequestEndGridItems = onRequestEndGridItems; }
if (typeof onRemoveGridItems !== 'undefined') { window.onRemoveGridItems = onRemoveGridItems; }
if (typeof onClickTransfer !== 'undefined') { window.onClickTransfer = onClickTransfer; }
if (typeof PrintPo !== 'undefined') { window.PrintPo = PrintPo; }

$(function () {
    $('#pcWorkspaceTabs').kendoTabStrip({
        animation: false,
        select: function (e) {
            var tabName = pcTabNames[$(e.item).index()];
            pcLoadTab(tabName).done(function () {
                if (tabName === 'Individual Units' || tabName === 'Components' || tabName === 'Accountability' || tabName === 'Documents') {
                    if (propertyCardSelectedAcquisitionId) {
                        selectPropertyCardAcquisition(propertyCardSelectedAcquisitionId, true);
                    }
                }
            });
        },
        activate: function (e) { kendo.resize($(e.contentElement)); }
    });

    // Delegate acquisition card clicks across all master panes
    $('#pcWorkspace').on('click', '.pc-acquisition-card', function () {
        var acqId = $(this).attr('data-acquisition-id');
        if (acqId) {
            selectPropertyCardAcquisition(acqId, true);
        }
    });



    // Delegate Documents unit/acquisition scope change
    $('#pcWorkspace').on('change', '#pcDocUnitSelect', function () {
        var selectedId = $(this).val();
        if (selectedId) {
            pcLoadDocuments(selectedId);
        }
    });

    $('#pcWorkspace').on('click', '[data-pc-tab]', function () { pcOpenTab($(this).attr('data-pc-tab')); })
      .on('click', '.pc-acquisition-units', function () { pcViewUnits($(this).attr('data-transfer-id')); })
      .on('click', '.pc-acquisition-documents', function () {
          var acqId = $(this).attr('data-acquisition-id');
          if (acqId) {
              propertyCardSelectedAcquisitionId = acqId;
              pcOpenTab('Documents').done(function () {
                  selectPropertyCardAcquisition(acqId, true);
              });
          }
      });
    $('#pcEditCard').on('click', function () { StockCard('E', pcWorkspace.id); });
    $('#pcPrintCard').on('click', function () { pcWindow('Property Card', propertyCardConfig.urls.PropertyCard_StockCardRpt, { selectedId: pcWorkspace.id }, null, true); });
    $('#pcRefreshCard').on('click', function () {
        var btn = this;
        if (!propertyCardButtonStart(btn, "Refreshing...")) { return; }
        var posPromise = pcRefreshPosition();
        var grid = pcAcquisitionGrid();
        var gridPromise = grid ? grid.dataSource.read() : null;
        var acqPromise = pcEnsureAcquisitionsLoaded(true).done(function () {
            var currentTab = pcCurrentTabName();
            if (currentTab) { pcRenderAcquisitionList(currentTab); }
        });
        $.when(posPromise, gridPromise, acqPromise).always(function () { propertyCardButtonStop(btn); });
    });
    if (/[?&]addAcquisition=true/.test(window.location.search)) { pcOpenTab('Acquisitions'); }

    // Pre-fetch acquisitions data
    pcEnsureAcquisitionsLoaded();
    pcAcquisitionMenu();
    $(document).off('click.pcAcquisitionMore', '.k-grid-More, .pc-acquisition-more').on('click.pcAcquisitionMore', '.k-grid-More, .pc-acquisition-more', pcAcquisitionMore);
});

function onRemoveGridUnits(e) {
    e.preventDefault();
    var grid = this;
    var item = e.model;
    if (!item) return;

    if (item.CanDelete === false) {
        epsmsAlert({
            title: "Cannot Delete Physical Unit",
            type: "warning",
            headline: "Deletion Prohibited",
            message: "This physical unit cannot be deleted because it is assigned to an accountability record (PAR/ICS), transferred, issued, or generated from an accepted AIR."
        });
        return;
    }

    var propNo = item.PropertyNo || item.CustItemNo || item.SerialNo || ("Unit #" + (item.ContentNo || ""));

    epsmsConfirm({
        title: "Delete Individual Unit",
        type: "danger",
        headline: "Delete Individual Physical Unit?",
        entityLabel: "Property No.",
        entityValue: propNo,
        message: "This will permanently remove this physical property unit from the Property Card. This action cannot be undone.",
        cancelText: "Cancel",
        confirmText: "Delete Unit",
        confirmIcon: "k-i-delete",
        onConfirm: function (dialog, $confirmBtn) {
            setClickedButtonBusy($confirmBtn, "Deleting...");
            grid.dataSource.remove(item);
            grid.dataSource.sync()
                .done(function () {
                    restoreClickedButton($confirmBtn);
                    dialog.close();
                    notifySuccess("Physical unit deleted successfully.");
                    if (typeof pcRefreshWorkspaceStats === "function") {
                        pcRefreshWorkspaceStats();
                    }
                    pcRefreshPosition();
                })
                .fail(function (xhr) {
                    restoreClickedButton($confirmBtn);
                    grid.dataSource.read();
                    notifyWarning(ajaxFailureMessage(xhr, "Unable to delete the physical unit."));
                });
        }
    });
}

function onRemoveGridComponents(e) {
    e.preventDefault();
    var grid = this;
    var item = e.model;
    if (!item) return;

    epsmsConfirm({
        title: "Delete Component",
        type: "danger",
        headline: "Delete Component Definition?",
        entityLabel: "Component",
        entityValue: item.Description || ("Sub Item #" + item.SubItemNo),
        message: "This will permanently delete this component definition from the Property Card. All physical units registered under this component will also be deleted.",
        cancelText: "Cancel",
        confirmText: "Delete Component",
        confirmIcon: "k-i-delete",
        onConfirm: function (dialog, $confirmBtn) {
            setClickedButtonBusy($confirmBtn, "Deleting...");
            grid.dataSource.remove(item);
            grid.dataSource.sync()
                .done(function () {
                    restoreClickedButton($confirmBtn);
                    dialog.close();
                    notifySuccess("Component deleted successfully.");
                    pcRefreshPosition();
                })
                .fail(function (xhr) {
                    restoreClickedButton($confirmBtn);
                    grid.dataSource.read();
                    notifyWarning(ajaxFailureMessage(xhr, "Unable to delete the component."));
                });
        }
    });
}

function onRemoveGridComponentUnits(e) {
    e.preventDefault();
    var grid = this;
    var item = e.model;
    if (!item) return;

    if (item.CanDelete === false) {
        epsmsAlert({
            title: "Cannot Delete Component Unit",
            type: "warning",
            headline: "Deletion Prohibited",
            message: "This component physical unit cannot be deleted because it is assigned to an active PAR/ICS accountability bundle."
        });
        return;
    }

    epsmsConfirm({
        title: "Delete Component Unit",
        type: "danger",
        headline: "Delete Component Physical Unit?",
        entityLabel: "Unit",
        entityValue: item.SerialNo || ("Unit #" + (item.ContentNo || "")),
        message: "This will permanently remove this physical unit for this component. This action cannot be undone.",
        cancelText: "Cancel",
        confirmText: "Delete Unit",
        confirmIcon: "k-i-delete",
        onConfirm: function (dialog, $confirmBtn) {
            setClickedButtonBusy($confirmBtn, "Deleting...");
            grid.dataSource.remove(item);
            grid.dataSource.sync()
                .done(function () {
                    restoreClickedButton($confirmBtn);
                    dialog.close();
                    notifySuccess("Component physical unit deleted.");
                    pcRefreshPosition();
                })
                .fail(function (xhr) {
                    restoreClickedButton($confirmBtn);
                    grid.dataSource.read();
                    notifyWarning(ajaxFailureMessage(xhr, "Unable to delete component unit."));
                });
        }
    });
}
