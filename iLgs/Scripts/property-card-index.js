/* Shared server-paged data for the Property Card grid and catalogue. */
function propertyCardClassification(item) {
    return $.grep([item.SubAccount1, item.SubAccount2, item.SubAccount3, item.SubAccount4], function (value) {
        return value != null && value !== '';
    }).join(' / ') || '\u2014';
}
function propertyCardSetSelection(grid, item) {
    selectedGrid = grid;
    selectedId = item ? item.Id : null;
    selectedStockNo = item ? item.PsNo : null;
    selectedItemId = null;
    selectedTransferId = null;
    grid.wrapper.find('.k-grid-btnPrintCard')
        .toggleClass('k-disabled', !item).attr('aria-disabled', item ? 'false' : 'true');
}
function propertyCardUpdateCount(source) {
    var total = source.total() || 0;
    $('#pcResultCount').text(kendo.toString(total, 'n0') + (total === 1 ? ' matching card' : ' matching cards'));
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
        $(this).attr('aria-pressed', active ? 'true' : 'false').toggleClass('k-button-solid-primary', active).toggleClass('k-button-solid-base', !active);
    });
    var grid = $('#grid').data('kendoGrid');
    if (grid && !catalogue) { grid.resize(); }
    if (catalogue) { kendo.resize($('#pcCataloguePanel')); }
}
function propertyCardOpen(id, button) {
    if (id) {
        if (button) {
            propertyCardButtonStart(button, "Loading...");
        }
        window.location.href = propertyCardConfig.viewUrl + '?id=' + encodeURIComponent(id);
    }
}
function onViewPropertyCard(e) {
    e.preventDefault();
    var btn = e.currentTarget;
    var item = this.dataItem($(btn).closest('tr'));
    if (item) { propertyCardOpen(item.Id, btn); }
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
            this.element.find('.pc-empty').remove();
            if (!this.dataSource.view().length) {
                this.content.append('<p class="pc-empty" role="status">No property cards match your filters. Try another search or clear the filters.</p>');
            }
        }
    }).data('kendoListView');
    $('#pcCataloguePager').kendoPager({
        dataSource: grid.dataSource, autoBind: false,
        pageSizes: [12, 20, 40, 80], buttonCount: 5,
        numeric: true, info: true, previousNext: true, refresh: true, responsive: true
    });
    // Handle an already completed initial read without fetching again.
    list.refresh();
    propertyCardUpdateCount(grid.dataSource);
    $('#propertyCardWorkspace').on('click', '[data-pc-view]', function () {
        propertyCardSwitchView($(this).attr('data-pc-view'));
    });
    $('#pcCatalogue').on('click', '.pc-open-card', function () {
        var btn = this;
        var cardId = $(btn).attr('data-card-id');
        propertyCardOpen(cardId, btn);
    }).on('click', '.pc-edit-card', function () {
        var item = grid.dataSource.get($(this).attr('data-card-id'));
        if (item) { propertyCardSetSelection(grid, item); StockCard('E', item.Id); }
    });
    $('#pcCatalogue').on('click', '.pc-print-card', function () {
        var item = grid.dataSource.get($(this).attr('data-card-id'));
        if (item) {
            propertyCardSetSelection(grid, item);
            grid.wrapper.find('.k-grid-btnPrintCard').trigger('click');
        }
    });
    $('#pcClearFilters').on('click', function () {
        $('#UserName').data('kendoMultiColumnComboBox').value('');
        grid.wrapper.find('.k-grid-search input').val('');
        grid.dataSource.query({ page: 1, pageSize: grid.dataSource.pageSize(), sort: grid.dataSource.sort(), filter: [] });
    });
});
