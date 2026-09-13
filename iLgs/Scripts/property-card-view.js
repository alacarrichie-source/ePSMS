/* Property Card workspace. Existing mutation endpoints and popup forms remain authoritative. */
var pcWorkspace = propertyCardConfig.workspace;
var pcTabNames = ['Overview', 'Acquisitions', 'Individual Units', 'Accountability', 'History', 'Documents'];
var pcActiveAcquisition = null;
var pcPendingUnitTransfer = null;
var pcPendingUnit = null;
var pcPendingDocument = null;
var pcTabLoads = {};
var pcUploadOwner = null;
selectedId = pcWorkspace.id;
selectedStockNo = pcWorkspace.cardNumber;

function pcText(value) { return kendo.htmlEncode(value == null || value === '' ? '\u2014' : String(value)); }
function pcNumber(value) { return kendo.toString(value || 0, 'n0'); }
function pcDate(value) { var date = kendo.parseDate(value); return date ? kendo.toString(date, 'MM/dd/yyyy') : '\u2014'; }
function pcMoney(value) { return value == null ? '\u2014' : kendo.toString(value, 'n2'); }
function pcWorkspaceParent(grid) {
    return grid.element.hasClass('pc-acquisitions') ? { Id: pcWorkspace.id } : pcActiveAcquisition;
}
function pcError(message) { $('#pcWorkspaceError').prop('hidden', false).text(message || 'Unable to load this record. Please retry.'); }
function pcWorkspaceDataError(e) { pcError(e.errors ? JSON.stringify(e.errors) : 'Unable to load records. Refresh to retry.'); }
function pcWindow(title, url, data, closed, iframe) {
    var element = $('<div></div>').appendTo('#windowcontainer');
    var widget = element.kendoWindow({
        title: title, width: Math.min(1000, $(window).width() - 40), height: iframe ? Math.min(720, $(window).height() - 80) : undefined,
        modal: true, visible: false, resizable: true, iframe: !!iframe, actions: ['Maximize', 'Close'],
        close: function () { if (closed) { closed(); } },
        deactivate: function () { this.destroy(); },
        refresh: function () { this.center(); }
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
    $.each(['Individual Units','History','Accountability','Documents'], function (_, name) { pcTabLoads[name] = null; });
}
function pcAcquisitionGrid() { return $('#gridItems_' + pcWorkspace.id).data('kendoGrid'); }
function pcSetAcquisition(grid, item) {
    selectedId = pcWorkspace.id;
    selectedGrid = grid;
    selectedItemId = item ? item.Id : null;
    selectedTransferId = item ? item.TransferId : null;
    if (item) {
        var row = grid.tbody.children("tr[data-uid='" + item.uid + "']");
        if (!grid.select().is(row)) { grid.select(row); }
    }
}
function pcAcquisitionReference(d) {
    return '<strong>' + pcText(d.PoNo) + '</strong><span class="pc-cell-secondary">' + pcDate(d.PoDate) + '</span>';
}
function pcAcquisitionCost(d) {
    return '<strong>' + pcNumber(d.Qty) + ' ' + pcText(d.Unit) + '</strong><span class="pc-cell-secondary">' +
        pcMoney(d.UnitCost) + ' / unit</span><span class="pc-cell-secondary">Balance value: ' + pcMoney(d.Amount) + '</span>';
}
function pcAcquisitionLocation(d) {
    return '<strong>' + pcText(d.Department) + '</strong><span class="pc-cell-secondary">' + pcText(d.Location) +
        '</span><span class="pc-cell-secondary">Code: ' + pcText(d.LocCode) + '</span>';
}
function pcAcquisitionMovement(d) {
    var html = '<span>Receipt: ' + pcNumber(d.ParentId ? 0 : d.Qty) + '</span>';
    $.each([['Transfer In','TransferIn'],['Issued','QtyIss'],['Transfer Out','TransferOut']], function (_, pair) {
        html += '<span class="pc-cell-secondary ' + (d[pair[1]] ? '' : 'pc-zero') + '">' + pair[0] + ': ' + pcNumber(d[pair[1]]) + '</span>';
    });
    return html;
}
function pcAcquisitionStatus(d) {
    return '<span class="pc-status ' + (IsPosted(d) ? 'pc-posted' : 'pc-draft') + '">' +
        (IsPosted(d) ? 'Posted' : 'Draft') + '</span>' + (IsPosted(d) ? '<span class="pc-cell-secondary">' + pcText(d.PostedBy) + '<br>' + pcDate(d.PostedDt) + '</span>' : '');
}
function pcAcquisitionDetailHtml(d) {
    return '<div class="pc-acquisition-detail"><section><h4>PO / AIR Reference</h4><p>PO: ' + pcText(d.PoNo) + ' · ' + pcDate(d.PoDate) +
        '</p><p>AIR: ' + pcText(d.AirNo) + ' · ' + pcDate(d.AirDate) + '</p></section><section><h4>Receipt & Location</h4>' +
        pcAcquisitionCost(d) + pcAcquisitionLocation(d) + '</section><section><h4>Movement</h4>' + pcAcquisitionMovement(d) +
        '<p>Balance: <strong>' + pcNumber(d.QtyBal) + ' ' + pcText(d.Unit) + '</strong></p></section><section><h4>Audit</h4><p>Created: ' +
        pcText(d.InsertedBy) + ' · ' + pcDate(d.InsertedDt) + '</p>' + pcAcquisitionStatus(d) + '</section></div>' +
        '<button type="button" class="k-button pc-acquisition-units" data-transfer-id="' + pcText(d.TransferId) + '">Individual Units</button>';
}
function pcShowAcquisitionDetails(e) {
    e.preventDefault();
    var row = $(e.currentTarget).closest('tr'), item = this.dataItem(row);
    pcSetAcquisition(this, item);
    if (row.next().is('.k-detail-row:visible')) { this.collapseRow(row); } else { this.expandRow(row); }
}
function pcAcquisitionMore(e) {
    e.preventDefault();
    var grid = this, row = $(e.currentTarget).closest('tr'), item = grid.dataItem(row);
    pcSetAcquisition(grid, item);
    var actions = ['Edit', 'Print PO - Original', 'Print PO - Update', IsPosted(item) ? 'Unpost' : 'Post'];
    if (!IsPosted(item)) { actions.push('Transfer', 'Delete'); }
    var old = $('#pcAcquisitionMenu').data('kendoContextMenu');
    if (old) { old.destroy(); $('#pcAcquisitionMenu').remove(); }
    var menu = $('<ul id="pcAcquisitionMenu"></ul>').appendTo(document.body);
    $.each(actions, function (_, name) { $('<li></li>').text(name).appendTo(menu); });
    var context = menu.kendoContextMenu({target: e.currentTarget, showOn: 'click', select: function (event) {
        var action = $(event.item).text();
        pcSetAcquisition(grid, item);
        if (action === 'Edit') { grid.editRow(row); }
        else if (action === 'Delete') {
            kendo.confirm('Delete this acquisition record?').done(function () { grid.removeRow(row); });
        } else if (action === 'Post' || action === 'Unpost') {
            pcPostAcquisition(item, action === 'Post', e.currentTarget);
        } else if (action === 'Transfer') {
            kendo.confirm('Transfer this PO item to another Property/Stock Card?').done(function () {
                onClickTransfer.call(grid, {preventDefault:$.noop, currentTarget:row[0]});
            });
        } else { selectedItemId = item.Id; PrintPo(action === 'Print PO - Original' ? 1 : 0); }
    }}).data('kendoContextMenu');
    context.open(e.currentTarget);
}
function pcPostAcquisition(item, post, triggerElement) {
    var actionName = post ? 'Post' : 'Unpost';
    var actionMsg = post ? 'Posting...' : 'Unposting...';
    kendo.confirm(actionName + ' this acquisition record?').done(function () {
        if (!propertyCardButtonStart(triggerElement, actionMsg)) {
            return;
        }
        $.ajax({
            type: 'POST',
            url: post ? propertyCardConfig.urls.StockCard_PostItemRecord : propertyCardConfig.urls.StockCard_UnpostItemRecord,
            data: { psCardItemId: item.Id }
        }).done(function (result) {
            if (result.Errors || result.errors) {
                pcError(JSON.stringify(result.Errors || result.errors));
                return;
            }
            var grid = pcAcquisitionGrid();
            if (grid) { grid.dataSource.read(); }
            pcAfterMutation();
        }).fail(function () {
            pcError('The ' + (post ? 'posting' : 'unposting') + ' operation failed. Refresh the record before retrying.');
        }).always(function () {
            propertyCardButtonStop(triggerElement);
        });
    });
}
function onDataBoundGridItems() {
    var item = selectedTransferId ? this.dataSource.get(selectedTransferId) : null;
    if (item) { this.select(this.tbody.children("tr[data-uid='" + item.uid + "']")); }
}
function onChangeGridItems() { pcSetAcquisition(this, this.dataItem(this.select())); }
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
        optionLabel:'Search and select a record…', dataTextField:'Label', dataValueField:'Id',
        filter:'contains', minLength:1,
        dataSource:{serverFiltering:true, transport:{read:{url:pcWorkspace.choicesUrl, dataType:'json', data:function () {
            var widget=$(selector).data('kendoDropDownList');
            return {id:pcWorkspace.id, kind:kind, text:widget && widget.filterInput ? widget.filterInput.val() : ''};
        }}}},
        change:function () { var item=this.dataItem(); if (item && item.Id) { callback(item); } }
    });
}
function pcInitTab(name) {
    if (name === 'Individual Units') {
        pcChoice('#pcAcquisitionChoice','acquisitions',function (item) { pcLoadUnits(item.Id); });
        if (pcPendingUnitTransfer) { pcLoadUnits(pcPendingUnitTransfer); pcPendingUnitTransfer=null; }
    } else if (name === 'Accountability') {
        pcChoice('#pcAccountabilityChoice','units',function (item) { pcLoadAccountability(item.Id); });
        if (pcPendingUnit) { pcLoadAccountability(pcPendingUnit); pcPendingUnit=null; }
    } else if (name === 'Documents') {
        pcChoice('#pcDocumentChoice','documents',function (item) { pcLoadDocuments(item.Id); });
        if (pcPendingDocument) { pcLoadDocuments(pcPendingDocument); pcPendingDocument=null; }
    } else if (name === 'Acquisitions' && /[?&]addAcquisition=true/.test(window.location.search)) {
        var grid = pcAcquisitionGrid();
        if (grid) {
            grid.one('dataBound',function () { grid.addRow(); });
            window.history.replaceState(null,'',window.location.pathname+'?id='+encodeURIComponent(pcWorkspace.id));
        }
    }
}
var pcUnitLoad = 0;
function pcLoadUnits(transferId) {
    var sequence = ++pcUnitLoad, host=$('#pcUnitsHost');
    kendo.ui.progress(host,true);
    $.getJSON(pcWorkspace.unitsUrl,{id:pcWorkspace.id,transferId:transferId}).done(function(result) {
        if (sequence !== pcUnitLoad) { return; }
        var template=$('#'+result.Template);
        if (!template.length || $.inArray(result.Template,['ItemExtnVehicle','ItemExtnOther','ItemExtnLand','ItemExtnBldg'])<0) {
            pcError('Individual-unit details are not configured for this category.'); return;
        }
        pcActiveAcquisition=result.Row;
        selectedItemId=result.Row.Id; selectedTransferId=result.Row.TransferId; selectedItemExtnId=null;
        $('#pcUnitContext').text('PO '+(result.Row.PoNo||'—')+' · '+(result.Row.Location||'Origin')+' · Balance '+pcNumber(result.Row.QtyBal));
        kendo.destroy(host); host.empty().attr('data-parent-id',result.Row.ParentId||'');
        host.html(kendo.template(template.html())(result.Row));
    }).fail(function(){pcError('Unable to load the selected acquisition units.');})
      .always(function(){if(sequence===pcUnitLoad){kendo.ui.progress(host,false);}});
}
function pcViewUnits(transferId) {
    pcOpenTab('Individual Units').done(function () { pcLoadUnits(transferId); });
}
function onChangeGridItemExtn() {
    var item=this.dataItem(this.select()); selectedItemExtnId=item?item.Id:null; selectedGrid=this;
}
function onDataBoundGridItemExtn() {
    if (pcActiveAcquisition && pcActiveAcquisition.ParentId) { this.wrapper.find('.k-grid-add').hide(); }
    var item=selectedItemExtnId?this.dataSource.get(selectedItemExtnId):null;
    if(item){this.select(this.tbody.children("tr[data-uid='"+item.uid+"']"));}
}
function onRequestEndGridItemExtn(e) {
    if(e.response && !e.response.Errors && !e.response.errors && e.type!=='read'){
        var source=this;
        if(e.type==='create' && e.response.Data && e.response.Data.length){selectedItemExtnId=e.response.Data[0].Id;}
        setTimeout(function(){source.read();var grid=pcAcquisitionGrid();if(grid){grid.dataSource.read();}pcAfterMutation();},0);
    }
}
function pcDeleteUnit(e) {
    e.preventDefault();
    var grid = this, row = $(e.currentTarget).closest('tr'), item = grid.dataItem(row), btn = e.currentTarget;
    selectedGrid = grid;
    kendo.confirm('Delete this individual unit?').done(function () {
        if (!propertyCardButtonStart(btn, "Deleting...")) {
            return;
        }
        grid.dataSource.remove(item);
        grid.dataSource.sync().fail(function () {
            propertyCardButtonStop(btn);
        });
    });
}
function pcUnitDocuments(e){e.preventDefault();var item=this.dataItem($(e.currentTarget).closest('tr'));pcOpenTab('Documents').done(function(){pcLoadDocuments(item.Id);});}
function pcUnitAccountability(e){e.preventDefault();var item=this.dataItem($(e.currentTarget).closest('tr'));pcOpenTab('Accountability').done(function(){pcLoadAccountability(item.Id);});}
function pcLoadPartial(host, url, data, context) {
    var generation=(host.data('generation')||0)+1;host.data('generation',generation);
    kendo.ui.progress(host,true);
    $.get(url,data).done(function(html){
        if(host.data('generation')!==generation){return;}
        kendo.destroy(host);host.empty().html(html);
        if(context){context.acquisitionId=host.find('.pc-document-scope').attr('data-acquisition-id'); var grid=host.find('.k-grid').data('kendoGrid'); if(grid){grid.element.data('pc-upload-context',context); grid.dataSource.bind('error',GridError); var assign=function(){ $.each(grid.dataSource.data(),function(_,item){item.PsCardItemId=context.acquisitionId;}); }; grid.bind('dataBound',assign);assign();}}
    }).fail(function(){pcError('Unable to load the selected records.');})
      .always(function(){if(host.data('generation')===generation){kendo.ui.progress(host,false);}});
}
function pcLoadAccountability(id){pcLoadPartial($('#pcAccountabilityHost'),pcWorkspace.accountabilityUrl,{id:pcWorkspace.id,unitId:id});}
function pcLoadDocuments(id){pcLoadPartial($('#pcDocumentsHost'),pcWorkspace.documentsUrl,{id:pcWorkspace.id,imageId:id},{imageId:id,controller:'CardUpload'});}
function onEditGridImages(e){selectedGrid=e.sender;selectedGridImagesItem=e.model.Id;var context=e.sender.element.data('pc-upload-context');if(context){e.model.set('PsCardItemId',context.acquisitionId);}}
function onClickUpload(e) {
    var button=e.sender.element,grid=$('#'+button.data('gridname')).data('kendoGrid');
    var context=grid.element.data('pc-upload-context')||{imageId:button.data('imageid'),controller:'ItemCard'};
    if(!context.imageId){pcError('No document parent was selected.');return;}
    pcUploadOwner=grid;selectedGrid=grid;propertyCardConfig.uploadContext=context;
    var url=context.controller==='CardUpload'?propertyCardConfig.urls.CardUpload__ImagesAdd:propertyCardConfig.urls.ItemCard__ImagesAdd;
    if(!url){url=propertyCardConfig.workspace.itemUploadUrl;}
    pcWindow('Upload Images / Documents',url,{imageId:context.imageId,description:button.data('description')});
}
function UploadPara(e){var context=propertyCardConfig.uploadContext||{};e.data={imageId:context.imageId||$('#ImageId').val(),PsCardItemId:context.acquisitionId||$('#PsCardItemId').val(),Description:$('#formUpload #Description').val()};}
function onUploadSuccess(){if(pcUploadOwner){pcUploadOwner.dataSource.read();}}
var pcOriginalAddCostCalculation=typeof GetAddCost !== 'undefined' ? GetAddCost : function () {};
function onClickAddCost(){
    var id=selectedItemExtnId || $('#Id').val();
    if(!id || id==='00000000-0000-0000-0000-000000000000'){kendo.alert('Save the individual unit before adding costs.');return;}
    pcWindow('Additional Cost',propertyCardConfig.urls.ItemCard__AddCost,{psCardItemExtnId:id},function(){
        if($('#AddCost').data('kendoNumericTextBox') && $('#AcqCost').data('kendoNumericTextBox')){pcOriginalAddCostCalculation(id);}
        pcAfterMutation();
    });
}
function UploadAddCost(imageId,postedBy){
    pcWindow('Additional Cost Documents',propertyCardConfig.urls.ItemCard__Images,{imageId:imageId,postedBy:postedBy,description:'ADDITIONAL COST'});
}

function onClickTransfer(e) {
    e.preventDefault();
    var item=this.dataItem($(e.currentTarget).closest('tr'));
    pcSetAcquisition(this,item);
    pcWindow('Transfer PO Item to another Stock/Property Card',propertyCardConfig.urls.StockCard__PoTransfer,
        {psCardItemId:item.Id},function(){var grid=pcAcquisitionGrid();if(grid){grid.dataSource.read();}pcAfterMutation();});
}
function PrintPo(originalSw) {
    pcWindow('PO Preview',propertyCardConfig.urls.StockCard_StockCardPoRpt,
        {selectedItemId:selectedItemId,originalSw:originalSw},null,true);
}
function pcGridError(args, fallback) {
    var grid=fallback, messages=[];
    $('.k-grid').each(function(){var candidate=$(this).data('kendoGrid');if(candidate && candidate.dataSource===args.sender){grid=candidate;}});
    $.each(args.errors || {},function(_,entry){messages=messages.concat(entry.errors || [String(entry)]);});
    if(!messages.length){messages.push('The operation failed. Refresh the records and retry.');}
    pcError(messages.join(' '));
    if(!grid){return;}
    if(grid.editable){
        grid.one('dataBinding',function(e){e.preventDefault();});
        grid.editable.element.find('.errors').empty().append($('<p role="alert"></p>').text(messages.join(' ')));
        grid.editable.element.find('#ErrorContent,#AddCostErrorContent').removeClass('hidden');
    }else if(grid.dataSource.hasChanges()){
        grid.dataSource.cancelChanges();
        grid.dataSource.read();
    }
}
function GridError(args) {
    pcGridError(args, selectedGrid);
}
function GridItemError(args) {
    var grid = pcAcquisitionGrid();
    pcGridError(args, grid);
}
function GridErrorAddCost(args){pcGridError(args,$('#gridAddCost').data('kendoGrid'));}

$(function(){
    $('#pcWorkspaceTabs').kendoTabStrip({
        animation:false,
        select:function(e){pcLoadTab(pcTabNames[$(e.item).index()]);},
        activate:function(e){kendo.resize($(e.contentElement));}
    });
    $('#pcWorkspace').on('click','[data-pc-tab]',function(){pcOpenTab($(this).attr('data-pc-tab'));})
      .on('click','.pc-acquisition-units',function(){pcViewUnits($(this).attr('data-transfer-id'));});
    $('#pcEditCard').on('click',function(){StockCard('E',pcWorkspace.id);});
    $('#pcPrintCard').on('click',function(){pcWindow('Property Card',propertyCardConfig.urls.PropertyCard_StockCardRpt,{selectedId:pcWorkspace.id},null,true);});
    $('#pcRefreshCard').on('click', function () {
        var btn = this;
        if (!propertyCardButtonStart(btn, "Refreshing...")) {
            return;
        }
        var posPromise = pcRefreshPosition();
        var grid = pcAcquisitionGrid();
        var gridPromise = grid ? grid.dataSource.read() : null;
        $.when(posPromise, gridPromise).always(function () {
            propertyCardButtonStop(btn);
        });
    });
    if(/[?&]addAcquisition=true/.test(window.location.search)){pcOpenTab('Acquisitions');}
});
