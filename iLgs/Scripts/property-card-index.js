/* Shared server-paged data for the Property Card grid and catalogue. */

var _unfilteredTotal = null;

function propertyCardClassification(item) {
    if (!item) return '\u2014';
    var parts = [];
    var raw = [item.SubAccount1, item.SubAccount2, item.SubAccount3, item.SubAccount4];
    for (var i = 0; i < raw.length; i++) {
        var v = raw[i];
        if (v != null && $.trim(v) !== '') {
            parts.push($.trim(v));
        }
    }
    if (!parts.length) return '\u2014';
    return parts.join(' \u203A ');
}

function propertyCardClassificationHtml(item) {
    var text = propertyCardClassification(item);
    var encoded = kendo.htmlEncode(text);
    return '<span class="pc-col-classification" title="' + encoded + '">' + encoded + '</span>';
}

function propertyCardStatusCue(item) {
    if (!item) return '';
    if (item.NotPosted != null && item.NotPosted > 0) {
        var count = kendo.htmlEncode(item.NotPosted);
        return '<span class="pc-card-status pc-status-pending" title="' + count + ' acquisition item(s) pending posting"><span class="pc-status-dot"></span>' + count + ' Unposted</span>';
    }
    return '<span class="pc-card-status pc-status-posted" title="All acquisition items posted"><span class="k-icon k-i-check mr-1"></span>Complete</span>';
}

function propertyCardSetSelection(grid, item) {
    selectedGrid = grid;
    selectedId = item ? item.Id : null;
    selectedStockNo = item ? item.PsNo : null;
    selectedItemId = null;
    selectedTransferId = null;
    postedBy = item ? (item.PostedBy || null) : null;
    if (grid && grid.wrapper) {
        grid.wrapper.find('.k-grid-btnPrintCard')
            .toggleClass('k-disabled', !item).attr('aria-disabled', item ? 'false' : 'true');
    }
}

function propertyCardUpdateCount(source) {
    var total = (source && typeof source.total === 'function') ? source.total() : 0;
    var getFiltersFn = window.getActivePropertyCardFilters || getActivePropertyCardFilters;
    var info = (typeof getFiltersFn === 'function') ? getFiltersFn() : { count: 0, filters: [] };

    // Record unfiltered total when no active filters
    if (info.count === 0 && total > 0) {
        _unfilteredTotal = total;
    }

    var countText = '';
    var cardWord = (total === 1 ? 'Property Card' : 'Property Cards');

    if (info.count > 0) {
        var filterWord = (info.count === 1 ? 'filter applied' : 'filters applied');
        if (_unfilteredTotal != null && _unfilteredTotal > total) {
            countText = kendo.toString(total, 'n0') + ' of ' + kendo.toString(_unfilteredTotal, 'n0') + ' ' + cardWord + ' \u00B7 ' + info.count + ' ' + filterWord;
        } else {
            countText = kendo.toString(total, 'n0') + ' ' + cardWord + ' \u00B7 ' + info.count + ' ' + filterWord;
        }
    } else {
        countText = kendo.toString(total, 'n0') + ' ' + cardWord;
    }

    $('#pcResultCount').text(countText);
    if (typeof updateActiveFilterBadge === 'function') {
        updateActiveFilterBadge(info.count);
    }
}

function onPropertyCardDetailExpand(e) {
    this.select(e.masterRow);
    propertyCardSetSelection(this, this.dataItem(e.masterRow));
}

function propertyCardSwitchView(mode) {
    var catalogue = mode === 'catalogue';
    var workspace = $('#propertyCardWorkspace');
    workspace.toggleClass('pc-catalogue-mode', catalogue);
    $('#pcCataloguePanel').prop('hidden', !catalogue);
    workspace.find('[data-pc-view]').each(function () {
        var active = $(this).attr('data-pc-view') === mode;
        $(this).attr('aria-pressed', active ? 'true' : 'false')
               .toggleClass('k-button-solid-primary', active)
               .toggleClass('k-button-solid-base', !active);
    });
    var grid = $('#grid').data('kendoGrid');
    if (grid && !catalogue) {
        grid.resize();
    }
    if (catalogue) {
        var list = $('#pcCatalogue').data('kendoListView');
        if (list) { list.refresh(); }
        kendo.resize($('#pcCataloguePanel'));
    }
}

function propertyCardOpen(id, button) {
    if (id) {
        if (button) {
            propertyCardButtonStart(button, "Loading...");
        }
        window.location.href = propertyCardConfig.viewUrl + '?id=' + encodeURIComponent(id);
    }
}

function pcIsCardPosted(item) {
    if (!item) return false;
    return Boolean(item.PostedDt != null || (item.PostedBy != null && item.PostedBy !== '') || item.IsPosted);
}

// --------------------------------------------------------------------------
// More Menu & Actions (List & Catalog)
// --------------------------------------------------------------------------
function pcIndexMoreMenu() {
    var $menu = $('#pcIndexMoreMenu');
    if (!$menu.length) {
        $menu = $('<ul id="pcIndexMoreMenu" class="pc-index-more-menu" role="menu" aria-label="Property Card Actions"></ul>');
        $('body').append($menu);

        $menu.on('click', 'li[data-action]', function (e) {
            e.preventDefault();
            e.stopPropagation();
            var action = $(this).attr('data-action');
            var item = $menu.data('activeItem');
            var grid = $menu.data('activeGrid');
            var row = $menu.data('activeRow');
            var btn = $menu.data('activeBtn');
            pcCloseIndexMoreMenu();

            if (!item) return;

            if (action === 'view') {
                propertyCardOpen(item.Id, btn ? btn[0] : null);
            } else if (action === 'edit') {
                propertyCardSetSelection(grid, item);
                StockCard('E', item.Id);
            } else if (action === 'print') {
                pcPrintPropertyCard(item, grid);
            } else if (action === 'post') {
                pcPostPropertyCard(item, grid);
            } else if (action === 'unpost') {
                pcUnpostPropertyCard(item, grid);
            } else if (action === 'delete') {
                onDeletePropertyCardRecord(item, grid, row);
            }
        });
    }
    return $menu;
}

function pcCloseIndexMoreMenu() {
    var $menu = $('#pcIndexMoreMenu');
    if ($menu.length && $menu.is(':visible')) {
        $menu.hide();
        var $btn = $menu.data('activeBtn');
        if ($btn && $btn.length) {
            $btn.attr('aria-expanded', 'false').removeClass('pc-more-active');
        }
        $menu.removeData('activeBtn').removeData('activeItem').removeData('activeRow').removeData('activeGrid');
    }
}

function pcOpenIndexMoreMenu(btn, grid) {
    var $btn = $(btn);
    if (!$btn || !$btn.length) return;

    var $menu = $('#pcIndexMoreMenu');
    if ($menu.length && $menu.is(':visible') && $menu.data('activeBtn') && $menu.data('activeBtn')[0] === $btn[0]) {
        pcCloseIndexMoreMenu();
        return;
    }

    pcCloseIndexMoreMenu();

    var cardId = $btn.attr('data-card-id');
    var $row = $btn.closest('tr');
    if (!grid) {
        grid = $('#grid').data('kendoGrid');
    }

    var item = null;
    if (cardId && grid && grid.dataSource) {
        item = grid.dataSource.get(cardId);
    }
    if (!item && grid && $row.length) {
        item = getSafeGridDataItem(grid, $row);
    }
    if (!item) return;

    propertyCardSetSelection(grid, item);
    $menu = pcIndexMoreMenu();

    // Dynamically build complete action set for this Property Card record
    var isPosted = pcIsCardPosted(item);
    var menuHtml = '';

    menuHtml += '<li data-action="view" role="menuitem" tabindex="-1"><span class="k-icon k-i-folder-open"></span> View Property Card</li>';
    menuHtml += '<li data-action="edit" role="menuitem" tabindex="-1"><span class="k-icon k-i-edit"></span> Edit</li>';
    menuHtml += '<li data-action="print" role="menuitem" tabindex="-1"><span class="k-icon k-i-print"></span> Print</li>';

    if (isPosted) {
        menuHtml += '<li data-action="unpost" role="menuitem" tabindex="-1"><span class="k-icon k-i-undo"></span> Unpost</li>';
    } else {
        menuHtml += '<li data-action="post" role="menuitem" tabindex="-1"><span class="k-icon k-i-check"></span> Post</li>';
    }

    menuHtml += '<li class="pc-menu-separator" role="separator"></li>';
    menuHtml += '<li data-action="delete" role="menuitem" tabindex="-1" class="pc-menu-item-danger"><span class="k-icon k-i-delete"></span> Delete</li>';

    $menu.html(menuHtml);

    // Store references
    $menu.data('activeBtn', $btn);
    $menu.data('activeRow', $row);
    $menu.data('activeItem', item);
    $menu.data('activeGrid', grid);

    // Position menu relative to button
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

    // Default: below button. If overflows bottom, position above
    var top = offset.top + btnHeight + 2;
    if (top + menuHeight > scrollTop + winHeight && offset.top - menuHeight > scrollTop) {
        top = Math.max(scrollTop + 4, offset.top - menuHeight - 2);
    }

    // Default: align right or left edge according to available space
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

function pcPostPropertyCard(item, grid) {
    if (!item || !item.Id) {
        if (typeof notifyWarning === 'function') {
            notifyWarning("Please select a Property Card to post.");
        }
        return;
    }
    if (item.PostedBy || item.PostedDt) {
        if (typeof notifyWarning === 'function') {
            notifyWarning("This Property Card has already been posted" + (item.PostedBy ? (" by " + item.PostedBy) : "") + ".");
        }
        return;
    }

    epsmsConfirm({
        title: "Post Property Card",
        type: "info",
        headline: "Post Official Property Card?",
        entityLabel: "Property Card No.",
        entityValue: item.PsNo || "Card",
        message: "Once posted, this Property Card will be formally recognized in the property registry and its records will become official.",
        cancelText: "Cancel",
        confirmText: "Post Card",
        confirmIcon: "k-i-check",
        onConfirm: function (dialog, $confirmBtn) {
            setClickedButtonBusy($confirmBtn, "Posting...");
            $.ajax({
                type: "POST",
                url: '' + propertyCardConfig.urls.PropertyCard_PostRecord + '',
                data: { psCardId: item.Id },
                async: true
            }).done(function (result) {
                restoreClickedButton($confirmBtn);
                dialog.close();
                if (!result || result.Errors == null || result.Errors === "" || (Array.isArray(result.Errors) && result.Errors.length === 0)) {
                    notifySuccess("Property Card posted successfully.");
                    if (!grid) { grid = $('#grid').data('kendoGrid'); }
                    if (grid && grid.dataSource) { grid.dataSource.read(); }
                } else {
                    var errMsg = Array.isArray(result.Errors) ? result.Errors.join(", ") : result.Errors;
                    notifyWarning(errMsg);
                }
            }).fail(function (req) {
                restoreClickedButton($confirmBtn);
                notifyWarning(ajaxFailureMessage(req, "Unable to post the Property Card."));
            });
        }
    });
}

function pcUnpostPropertyCard(item, grid) {
    if (!item || !item.Id) {
        if (typeof notifyWarning === 'function') {
            notifyWarning("Please select a Property Card to unpost.");
        }
        return;
    }

    epsmsConfirm({
        title: "Unpost Property Card",
        type: "warning",
        headline: "Unpost Property Card?",
        entityLabel: "Property Card No.",
        entityValue: item.PsNo || "Card",
        message: "This will return the Property Card to an unposted status.",
        cancelText: "Cancel",
        confirmText: "Unpost Card",
        confirmIcon: "k-i-undo",
        onConfirm: function (dialog, $confirmBtn) {
            setClickedButtonBusy($confirmBtn, "Unposting...");
            $.ajax({
                type: "POST",
                url: '' + propertyCardConfig.urls.PropertyCard_UnpostRecord + '',
                data: { psCardId: item.Id },
                async: true
            }).done(function (result) {
                restoreClickedButton($confirmBtn);
                dialog.close();
                if (!result || result.Errors == null || result.Errors === "" || (Array.isArray(result.Errors) && result.Errors.length === 0)) {
                    notifySuccess("Property Card unposted.");
                    if (!grid) { grid = $('#grid').data('kendoGrid'); }
                    if (grid && grid.dataSource) { grid.dataSource.read(); }
                } else {
                    var errMsg = Array.isArray(result.Errors) ? result.Errors.join(", ") : result.Errors;
                    notifyWarning(errMsg);
                }
            }).fail(function (req) {
                restoreClickedButton($confirmBtn);
                notifyWarning(ajaxFailureMessage(req, "Unable to unpost the Property Card."));
            });
        }
    });
}

function pcPrintPropertyCard(item, grid) {
    if (!item || !item.Id) {
        if (typeof notifyWarning === 'function') {
            notifyWarning("Please select a Property Card to print.");
        }
        return;
    }
    propertyCardSetSelection(grid, item);
    var url = '' + propertyCardConfig.urls.PropertyCard_StockCardRpt + '?selectedId=' + encodeURIComponent(item.Id);

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

    if (mywindow) {
        mywindow.setOptions({
            title: "Preview - " + (item.PsNo || "Property Card"),
            width: 1200,
            height: 700
        });
        mywindow.refresh({ url: url });
        mywindow.center().open();
    }
}

function onDeletePropertyCardRecord(item, grid, row) {
    if (!item) return;

    epsmsConfirm({
        title: "Delete Property Card",
        type: "danger",
        headline: "Delete Property Card?",
        entityLabel: "Property Card No.",
        entityValue: item.PsNo || "Card",
        message: "This will permanently delete this Property Card and its associated records. Property cards with existing inventory history, posted acquisitions, or accountability records cannot be deleted.",
        cancelText: "Cancel",
        confirmText: "Delete Card",
        confirmIcon: "k-i-delete",
        onConfirm: function (dialog, $confirmBtn) {
            setClickedButtonBusy($confirmBtn, "Deleting...");
            if (!grid) { grid = $('#grid').data('kendoGrid'); }
            if (grid && grid.dataSource) {
                grid.dataSource.remove(item);
                grid.dataSource.sync()
                    .done(function () {
                        restoreClickedButton($confirmBtn);
                        dialog.close();
                        notifySuccess("Property Card deleted successfully.");
                        grid.dataSource.read();
                    })
                    .fail(function (xhr) {
                        restoreClickedButton($confirmBtn);
                        grid.dataSource.read();
                        notifyWarning(ajaxFailureMessage(xhr, "Unable to delete the Property Card."));
                    });
            }
        }
    });
}

function onDeletePropertyCard(e) {
    e.preventDefault();
    var btn = e.currentTarget;
    var grid = this;
    var item = getSafeGridDataItem(grid, btn);
    if (!item) return;
    onDeletePropertyCardRecord(item, grid, $(btn).closest("tr"));
}

function onViewPropertyCard(e) {
    e.preventDefault();
    var btn = e.currentTarget;
    var grid = this;
    var item = getSafeGridDataItem(grid, btn);
    if (item) { propertyCardOpen(item.Id, btn); }
}

function getSafeGridDataItem(grid, target) {
    if (!grid || !grid.tbody) return null;
    var $tr = $(target).closest('tr');
    if (!$tr.length || $tr.hasClass('k-grouping-row') || $tr.hasClass('k-detail-row') || $tr.hasClass('k-group-footer') || !$tr.attr('data-uid')) {
        return null;
    }
    try {
        return grid.dataItem($tr);
    } catch (err) {
        return null;
    }
}

// --------------------------------------------------------------------------
// Centralized Filtering Logic (Section: Filter Logic & Synchronization)
// --------------------------------------------------------------------------
function getActivePropertyCardFilters() {
    var filters = [];
    var count = 0;

    // 1. Quick search across all 8 searchable fields
    var quickSearch = $.trim($('#pcFilterQuickSearch').val());
    if (quickSearch) {
        count++;
        filters.push({
            logic: "or",
            isQuickSearch: true,
            filters: [
                { field: "Fund", operator: "contains", value: quickSearch },
                { field: "PsNo", operator: "contains", value: quickSearch },
                { field: "ItemType", operator: "contains", value: quickSearch },
                { field: "SubAccount1", operator: "contains", value: quickSearch },
                { field: "SubAccount2", operator: "contains", value: quickSearch },
                { field: "SubAccount3", operator: "contains", value: quickSearch },
                { field: "SubAccount4", operator: "contains", value: quickSearch },
                { field: "Item", operator: "contains", value: quickSearch }
            ]
        });
    }

    // 2. Fund filter (Kendo ComboBox or text fallback)
    var fundCombo = $('#pcFilterFund').data('kendoComboBox');
    var fundVal = fundCombo ? $.trim(fundCombo.value()) : $.trim($('#pcFilterFund').val());
    if (fundVal && fundVal !== "ALL") {
        count++;
        filters.push({
            field: "Fund",
            operator: "contains",
            value: fundVal,
            isToolbarFilter: true
        });
    }

    // 3. Item Type filter (Kendo ComboBox or text fallback)
    var itemTypeCombo = $('#pcFilterItemType').data('kendoComboBox');
    var itemTypeVal = itemTypeCombo ? $.trim(itemTypeCombo.value()) : $.trim($('#pcFilterItemType').val());
    if (itemTypeVal && itemTypeVal !== "ALL") {
        count++;
        filters.push({
            field: "ItemType",
            operator: "contains",
            value: itemTypeVal,
            isToolbarFilter: true
        });
    }

    // 4. Specific drawer fields
    var fieldMap = [
        { id: "#pcFilterPsNo", field: "PsNo" },
        { id: "#pcFilterItem", field: "Item" },
        { id: "#pcFilterSubAccount1", field: "SubAccount1" },
        { id: "#pcFilterSubAccount2", field: "SubAccount2" },
        { id: "#pcFilterSubAccount3", field: "SubAccount3" },
        { id: "#pcFilterSubAccount4", field: "SubAccount4" }
    ];

    $.each(fieldMap, function (_, item) {
        var val = $.trim($(item.id).val());
        if (val) {
            count++;
            filters.push({
                field: item.field,
                operator: "contains",
                value: val,
                isToolbarFilter: true
            });
        }
    });

    // 5. Check encoder filter
    var userCombo = $('#UserName').data('kendoMultiColumnComboBox');
    if (userCombo && userCombo.value()) {
        count++;
    }

    return {
        filters: filters,
        count: count
    };
}

function extractPreservedGridFilters(currentFilter) {
    if (!currentFilter) return [];
    var preserved = [];
    var rawList = currentFilter.filters || [];

    var activeFields = {};
    if ($.trim($('#pcFilterPsNo').val())) activeFields['PsNo'] = true;
    if ($.trim($('#pcFilterItem').val())) activeFields['Item'] = true;
    if ($.trim($('#pcFilterSubAccount1').val())) activeFields['SubAccount1'] = true;
    if ($.trim($('#pcFilterSubAccount2').val())) activeFields['SubAccount2'] = true;
    if ($.trim($('#pcFilterSubAccount3').val())) activeFields['SubAccount3'] = true;
    if ($.trim($('#pcFilterSubAccount4').val())) activeFields['SubAccount4'] = true;

    var fundCombo = $('#pcFilterFund').data('kendoComboBox');
    var fundVal = fundCombo ? $.trim(fundCombo.value()) : $.trim($('#pcFilterFund').val());
    if (fundVal && fundVal !== "ALL") activeFields['Fund'] = true;

    var typeCombo = $('#pcFilterItemType').data('kendoComboBox');
    var typeVal = typeCombo ? $.trim(typeCombo.value()) : $.trim($('#pcFilterItemType').val());
    if (typeVal && typeVal !== "ALL") activeFields['ItemType'] = true;

    $.each(rawList, function (_, f) {
        if (!f) return;
        if (f.isToolbarFilter || f.isQuickSearch) return;

        // Skip column filter if active toolbar filter on same field
        if (f.field && activeFields[f.field]) {
            return;
        }

        preserved.push(f);
    });

    return preserved;
}

function updateActiveFilterBadge(count) {
    var badge = $('#pcActiveFilterBadge');
    if (!badge.length) return;
    if (count > 0) {
        badge.text('\u2022 ' + count + (count === 1 ? ' filter active' : ' filters active')).show();
    } else {
        badge.hide();
    }
}

function applyPropertyCardFilters() {
    var grid = $('#grid').data('kendoGrid');
    if (!grid) return;

    var currentFilter = grid.dataSource.filter();
    var preservedGridFilters = extractPreservedGridFilters(currentFilter);
    var filterInfo = getActivePropertyCardFilters();

    var totalCount = filterInfo.count + preservedGridFilters.length;
    updateActiveFilterBadge(totalCount);

    var combinedFilters = preservedGridFilters.concat(filterInfo.filters);

    if (combinedFilters.length > 0) {
        grid.dataSource.filter({
            logic: "and",
            filters: combinedFilters
        });
    } else {
        grid.dataSource.filter([]);
    }
}

function onChangeFilterCombo(e) {
    applyPropertyCardFilters();
}

function onSelectFilterCombo(e) {
    setTimeout(function () {
        applyPropertyCardFilters();
    }, 60);
}

$(function () {
    var grid = $('#grid').data('kendoGrid');
    if (!grid) { return; }

    // Observe the existing grid source; do not start a second request.
    var list = $('#pcCatalogue').kendoListView({
        dataSource: grid.dataSource,
        autoBind: false,
        template: kendo.template($('#pcCatalogueTemplate').html()),
        dataBound: function () {
            this.element.find('.pc-catalogue-empty').remove();
            if (!this.dataSource.view().length) {
                var emptyHtml = '<div class="pc-catalogue-empty" role="status">' +
                    '<div class="pc-empty-icon"><span class="k-icon k-i-search"></span></div>' +
                    '<h3 class="pc-empty-title">No Property Cards Found</h3>' +
                    '<p class="pc-empty-desc">No records match your search criteria or field filters.</p>' +
                    '<button type="button" class="k-button k-button-md k-rounded-md k-button-solid k-button-solid-primary pc-empty-clear"><span class="k-icon k-i-filter-clear mr-1"></span>Clear Filters</button>' +
                    '</div>';
                this.content.append(emptyHtml);
            }
        }
    }).data('kendoListView');

    $('#pcCataloguePager').kendoPager({
        dataSource: grid.dataSource,
        autoBind: false,
        pageSizes: [12, 20, 40, 80],
        buttonCount: 5,
        numeric: true,
        info: true,
        previousNext: true,
        refresh: true,
        responsive: true,
        messages: {
            display: "Showing {0}\u2013{1} of {2} Property Cards",
            empty: "No property cards to display",
            page: "Page",
            of: "of {0}",
            itemsPerPage: "cards per page",
            first: "Go to first page",
            previous: "Previous page",
            next: "Next page",
            last: "Go to last page",
            refresh: "Refresh"
        }
    });

    // Loading indicator for catalog mode
    grid.dataSource.bind("requestStart", function () {
        if ($('#propertyCardWorkspace').hasClass('pc-catalogue-mode')) {
            kendo.ui.progress($('#pcCatalogue'), true);
        }
    });
    grid.dataSource.bind("requestEnd", function () {
        kendo.ui.progress($('#pcCatalogue'), false);
    });

    // Catalog sort selector
    var sortSelect = $('#pcCatalogueSort');
    if (sortSelect.length) {
        sortSelect.kendoDropDownList({
            dataTextField: "text",
            dataValueField: "value",
            dataSource: [
                { text: "Latest Created", value: "inserted_desc" },
                { text: "Oldest Created", value: "inserted_asc" },
                { text: "Property Card No.", value: "psno_asc" },
                { text: "Article (A-Z)", value: "item_asc" },
                { text: "Fund", value: "fund_asc" },
                { text: "Item Type (A-Z)", value: "itemtype_asc" }
            ],
            value: "inserted_desc",
            change: function () {
                var val = this.value();
                var sortExpr;
                if (val === "inserted_asc") {
                    sortExpr = { field: "InsertedDt", dir: "asc" };
                } else if (val === "psno_asc") {
                    sortExpr = { field: "PsNo", dir: "asc" };
                } else if (val === "item_asc") {
                    sortExpr = { field: "Item", dir: "asc" };
                } else if (val === "fund_asc") {
                    sortExpr = { field: "Fund", dir: "asc" };
                } else if (val === "itemtype_asc") {
                    sortExpr = { field: "ItemType", dir: "asc" };
                } else {
                    sortExpr = { field: "InsertedDt", dir: "desc" };
                }
                grid.dataSource.sort(sortExpr);
            }
        });
    }

    // Initial sync without extra network request
    list.refresh();
    propertyCardUpdateCount(grid.dataSource);

    // Event delegation for view switch
    $('#propertyCardWorkspace').on('click', '[data-pc-view]', function () {
        propertyCardSwitchView($(this).attr('data-pc-view'));
    });

    // Toggle More Filters drawer
    $('#pcBtnToggleMoreFilters').on('click', function () {
        var drawer = $('#pcMoreFiltersDrawer');
        var isVisible = drawer.is(':visible');
        drawer.slideToggle(180);
        var nowExpanded = !isVisible;
        $(this).attr('aria-expanded', nowExpanded ? 'true' : 'false');
        $(this).find('.pc-chevron-icon')
               .toggleClass('k-i-arrow-chevron-down', !nowExpanded)
               .toggleClass('k-i-arrow-chevron-up', nowExpanded);
    });

    // Debounced live filtering on drawer filter inputs & quick search
    var filterTimer = null;
    $('#propertyCardWorkspace').on('input', '.pc-filter-input, #pcFilterQuickSearch', function () {
        var input = $(this);
        if (input.attr('id') === 'pcFilterQuickSearch') {
            $('#pcBtnClearQuickSearch').toggle(Boolean($.trim(input.val())));
        }
        clearTimeout(filterTimer);
        filterTimer = setTimeout(function () {
            applyPropertyCardFilters();
        }, 320);
    }).on('keydown', '.pc-filter-input, #pcFilterQuickSearch', function (e) {
        if (e.which === 13) {
            e.preventDefault();
            clearTimeout(filterTimer);
            applyPropertyCardFilters();
        }
    });

    // Clear quick search button inside input
    $('#pcBtnClearQuickSearch').on('click', function () {
        $('#pcFilterQuickSearch').val('');
        $(this).hide();
        applyPropertyCardFilters();
    });

    // List Grid Row Actions
    $('#grid').on('click', '.pc-grid-view-btn', function (e) {
        e.preventDefault();
        e.stopPropagation();
        var cardId = $(this).attr('data-card-id');
        if (cardId) {
            propertyCardOpen(cardId, this);
        } else {
            var item = getSafeGridDataItem(grid, this);
            if (item) { propertyCardOpen(item.Id, this); }
        }
    }).on('click', '.pc-grid-edit-btn', function (e) {
        e.preventDefault();
        e.stopPropagation();
        var cardId = $(this).attr('data-card-id');
        var item = cardId ? grid.dataSource.get(cardId) : getSafeGridDataItem(grid, this);
        if (item) {
            propertyCardSetSelection(grid, item);
            StockCard('E', item.Id);
        }
    }).on('click', '.pc-grid-more-btn', function (e) {
        e.preventDefault();
        e.stopPropagation();
        pcOpenIndexMoreMenu(this, grid);
    });

    // Catalog Card Actions
    $('#pcCatalogue').on('click', '.pc-open-card', function (e) {
        e.preventDefault();
        e.stopPropagation();
        var cardId = $(this).attr('data-card-id');
        propertyCardOpen(cardId, this);
    }).on('click', '.pc-edit-card', function (e) {
        e.preventDefault();
        e.stopPropagation();
        var cardId = $(this).attr('data-card-id');
        var item = grid.dataSource.get(cardId);
        if (item) {
            propertyCardSetSelection(grid, item);
            StockCard('E', item.Id);
        }
    }).on('click', '.pc-card-more-btn', function (e) {
        e.preventDefault();
        e.stopPropagation();
        pcOpenIndexMoreMenu(this, grid);
    }).on('click', '.pc-card', function (e) {
        // Normal non-action area click opens card
        if ($(e.target).closest('button, a, input, [role="button"], .pc-card-actions').length) {
            return;
        }
        var cardId = $(this).attr('data-card-id') || $(this).find('.pc-open-card').attr('data-card-id');
        if (cardId) {
            propertyCardOpen(cardId, $(this).find('.pc-open-card')[0]);
        }
    }).on('click', '.pc-empty-clear', function (e) {
        e.preventDefault();
        $('#pcClearFilters').trigger('click');
    });

    // Guard toolbar Print Selected Card button if no row selected
    $(document).off('click.pcPrintGuard', '.k-grid-btnPrintCard').on('click.pcPrintGuard', '.k-grid-btnPrintCard', function (e) {
        if (!selectedId) {
            e.preventDefault();
            e.stopImmediatePropagation();
            if (typeof notifyWarning === 'function') {
                notifyWarning("Please select a Property Card to print.");
            } else if (typeof epsmsAlert === 'function') {
                epsmsAlert({ title: "Print Property Card", message: "Please select a Property Card to print." });
            }
            return false;
        }
    });

    // Centralized Clear Filters button
    $('#pcClearFilters').on('click', function () {
        // 1. Reset Encoder
        var userCombo = $('#UserName').data('kendoMultiColumnComboBox');
        if (userCombo) { userCombo.value(''); }

        // 2. Reset Fund
        var fundCombo = $('#pcFilterFund').data('kendoComboBox');
        if (fundCombo) { fundCombo.value(''); }

        // 3. Reset Item Type
        var typeCombo = $('#pcFilterItemType').data('kendoComboBox');
        if (typeCombo) { typeCombo.value(''); }

        // 4. Reset Quick Search
        $('#pcFilterQuickSearch').val('');
        $('#pcBtnClearQuickSearch').hide();

        // 5. Reset Drawer text inputs
        $('.pc-filter-input').val('');

        // 6. Reset Sort
        var sortDrop = $('#pcCatalogueSort').data('kendoDropDownList');
        if (sortDrop) { sortDrop.value('inserted_desc'); }

        // 7. Reset active badge
        updateActiveFilterBadge(0);

        // 8. Single query to reset page and clear filters
        grid.dataSource.query({
            page: 1,
            pageSize: grid.dataSource.pageSize(),
            sort: { field: "InsertedDt", dir: "desc" },
            filter: []
        });
    });

    // Dismiss More Menu on click outside or escape
    $(document).on('click.pcIndexMoreMenu', function (e) {
        if (!$(e.target).closest('#pcIndexMoreMenu, .pc-grid-more-btn, .pc-card-more-btn').length) {
            pcCloseIndexMoreMenu();
        }
    }).on('keydown.pcIndexMoreMenu', function (e) {
        if (e.which === 27) { // Escape key
            pcCloseIndexMoreMenu();
        }
    });

    $(window).on('resize.pcIndexMoreMenu scroll.pcIndexMoreMenu', function () {
        pcCloseIndexMoreMenu();
    });
});

// Explicitly expose Index handlers to window for Kendo callbacks
window.propertyCardClassification = propertyCardClassification;
window.propertyCardClassificationHtml = propertyCardClassificationHtml;
window.propertyCardStatusCue = propertyCardStatusCue;
window.propertyCardSetSelection = propertyCardSetSelection;
window.propertyCardUpdateCount = propertyCardUpdateCount;
window.onPropertyCardDetailExpand = onPropertyCardDetailExpand;
window.propertyCardSwitchView = propertyCardSwitchView;
window.propertyCardOpen = propertyCardOpen;
window.onViewPropertyCard = onViewPropertyCard;
window.getActivePropertyCardFilters = getActivePropertyCardFilters;
window.extractPreservedGridFilters = extractPreservedGridFilters;
window.applyPropertyCardFilters = applyPropertyCardFilters;
window.updateActiveFilterBadge = updateActiveFilterBadge;
window.onChangeFilterCombo = onChangeFilterCombo;
window.onSelectFilterCombo = onSelectFilterCombo;
window.onDeletePropertyCard = onDeletePropertyCard;
window.onDeletePropertyCardRecord = onDeletePropertyCardRecord;
window.pcOpenIndexMoreMenu = pcOpenIndexMoreMenu;
window.pcCloseIndexMoreMenu = pcCloseIndexMoreMenu;
window.pcPrintPropertyCard = pcPrintPropertyCard;
window.pcIsCardPosted = pcIsCardPosted;
window.pcPostPropertyCard = pcPostPropertyCard;
window.pcUnpostPropertyCard = pcUnpostPropertyCard;
window.getSafeGridDataItem = getSafeGridDataItem;

window.pcIsCardPosted = pcIsCardPosted;
window.pcPostPropertyCard = pcPostPropertyCard;
window.pcUnpostPropertyCard = pcUnpostPropertyCard;