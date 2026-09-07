(function ($) {
    "use strict";
    $(function () {
        var root = $("#risWizard"), payload = null, busy = false, step = 1;
        if (!root.length) return;
        var fields = [
            ["RisDate", "RIS Date", "date"], ["Purpose", "Purpose", "text"],
            ["RequestedBy", "Requested By", "text"], ["RequestedByDesignation", "Requested By Designation", "text"],
            ["RequestedDate", "Requested Date", "date"], ["ApprovedBy", "Approved By", "text"],
            ["ApprovedByDesignation", "Approved By Designation", "text"], ["ApprovedDate", "Approved Date", "date"],
            ["IssuedBy", "Issued By", "text"], ["IssuedByDesignation", "Issued By Designation", "text"],
            ["IssuedDate", "Issued Date", "date"], ["ReceivedBy", "Received By", "text"],
            ["ReceivedByDesignation", "Received By Designation", "text"], ["ReceivedDate", "Received Date", "date"]
        ];
        function error(message) { $("#wizardError").text(message).show(); }
        function go(number) {
            step = number; $("#wizardError").hide();
            $("#risStep1,#risStep2,#risStep3").hide(); $("#risStep" + number).show();
            root.find(".ris-steps span").removeClass("active").filter("[data-step=" + number + "]").addClass("active");
            kendo.resize(root);
        }
        function dateValue(value) {
            var date = kendo.parseDate(value);
            return date ? kendo.toString(date, "yyyy-MM-dd") : "";
        }
        function participating(group) {
            return $.grep(group.Items, function (x) { return x.QtyRequest !== 0 || x.QtyIssue !== 0; }).length > 0;
        }
        function groupCard(group, target) {
            var card = $("<details class='ris-group' open></details>").appendTo(target);
            $("<summary></summary>").text("PR " + (group.Header.PrNo || "") + " — " + (group.Header.Office || "")).appendTo(card);
            return $("<div class='ris-group-body'></div>").appendTo(card);
        }
        function headerPanel(group, body, review, index) {
            var panel = $("<div class='ris-fields'></div>").appendTo(body);
            $("<div class='ris-field ris-field-full'></div>").text("Fund: " + (group.Header.Fund || "") + " • RIS and control numbers are assigned on posting.").appendTo(panel);
            $.each(fields, function (_, spec) {
                var field = $("<div class='ris-field'></div>").appendTo(panel);
                if (spec[0] === "Purpose") field.addClass("ris-field-full");
                var id = "ris_" + index + "_" + spec[0];
                $("<label></label>").attr("for", review ? null : id).text(spec[1]).appendTo(field);
                if (review) {
                    $("<div class='ris-value'></div>").text(group.Header[spec[0]] || "—").appendTo(field);
                } else {
                    $("<input class='form-control-custom' />").attr({id:id,type:spec[2],required:"required"})
                        .val(group.Header[spec[0]] || "").appendTo(field)
                        .on("input change", function () { group.Header[spec[0]] = $(this).val(); });
                }
            });
        }
        function itemsPanel(group, body, review) {
            var table = $("<table class='table table-bordered ris-items'></table>")
                .appendTo($("<div class='table-responsive'></div>").appendTo(body));
            var tr = $("<tr></tr>").appendTo($("<thead></thead>").appendTo(table));
            $.each(["Item No.", "PPMP / Item Code", "Description", "Unit", "Allocated Qty", "Previous Posted RIS", "Remaining", "Qty Requested", "Qty Issued"], function (i, title) {
                $("<th></th>").text(title).toggleClass("num", i >= 4).appendTo(tr);
            });
            var tbody = $("<tbody></tbody>").appendTo(table);
            $.each(group.Items, function (_, item) {
                if (review && item.QtyRequest === 0 && item.QtyIssue === 0) return;
                var row = $("<tr></tr>").appendTo(tbody);
                $.each(["ItemNo", "ItemCode", "Description", "Unit"], function (_, key) { $("<td></td>").text(item[key] || "").appendTo(row); });
                $.each(["AllocatedQty", "PreviousQty", "RemainingQty"], function (_, key) {
                    $("<td class='num'></td>").text(kendo.toString(item[key], "n2")).appendTo(row);
                });
                $.each(["QtyRequest", "QtyIssue"], function (_, key) {
                    var cell = $("<td class='num'></td>").appendTo(row);
                    if (review) cell.text(kendo.toString(item[key], "n2"));
                    else {
                        var input = $("<input />").attr("aria-label", key + " item " + item.ItemNo).appendTo(cell);
                        input.kendoNumericTextBox({
                            min:0, max:item.RemainingQty, decimals:2, format:"n2", value:item[key],
                            change:function () { item[key] = this.value() === null ? NaN : this.value(); }
                        });
                    }
                });
            });
        }
        function renderEntry() {
            var target = $("#risEntryGroups"); kendo.destroy(target); target.empty();
            $.each(payload.Groups, function (index, group) {
                $.each(fields, function (_, spec) { if (spec[2] === "date") group.Header[spec[0]] = dateValue(group.Header[spec[0]]); });
                var body = groupCard(group, target); headerPanel(group, body, false, index);
                var actions = $("<div class='ris-actions'></div>").appendTo(body);
                $("<button type='button' class='k-button'>Use All Remaining</button>").appendTo(actions).click(function () {
                    $.each(group.Items, function (_, x) { x.QtyRequest = x.RemainingQty; x.QtyIssue = x.RemainingQty; }); renderEntry();
                });
                $("<button type='button' class='k-button'>Reset Quantities</button>").appendTo(actions).click(function () {
                    $.each(group.Items, function (_, x) { x.QtyRequest = 0; x.QtyIssue = 0; }); renderEntry();
                });
                itemsPanel(group, body, false);
            });
        }
        function validate() {
            var messages = [], count = 0;
            $.each(payload.Groups, function (_, group) {
                if (!participating(group)) return;
                count++;
                var prefix = "PR " + group.Header.PrNo + ": ", issued = 0;
                $.each(fields, function (_, spec) {
                    if (!$.trim(group.Header[spec[0]] || "")) messages.push(prefix + spec[1] + " is required.");
                });
                var dates = ["RisDate","RequestedDate","ApprovedDate","IssuedDate","ReceivedDate"], previous = dateValue(group.Header.PoDate);
                $.each(dates, function (_, key) {
                    var current = group.Header[key];
                    if (current && previous && current < previous) messages.push(prefix + key + " must not precede the prior date.");
                    previous = current;
                });
                $.each(group.Items, function (_, item) {
                    if (!isFinite(item.QtyRequest) || !isFinite(item.QtyIssue) || item.QtyRequest < 0 || item.QtyIssue < 0 ||
                        item.QtyIssue > item.QtyRequest || item.QtyRequest > item.RemainingQty)
                        messages.push(prefix + "item " + item.ItemNo + ": use 0 <= issued <= requested <= remaining.");
                    issued += item.QtyIssue;
                });
                if (!(issued > 0)) messages.push(prefix + "enter a positive issued quantity.");
            });
            if (!count) messages.push("Enter quantities for at least one PR.");
            if (messages.length) { error(messages.join("\n")); return false; }
            return true;
        }
        var grid = $("#risCandidates").data("kendoGrid");
        grid.dataSource.bind("error", function () { error("Unable to load eligible POs. Please refresh."); });
        grid.bind("dataBinding", function () { grid.clearSelection(); });
        $("#wizardDepartment").kendoDropDownList({
            optionLabel:"Select department", dataTextField:"Text", dataValueField:"Value",
            dataSource:{transport:{read:{url:root.attr("data-departments"),dataType:"json"}},error:function(){error("Unable to load departments.");}},
            change:function(){payload=null;grid.clearSelection();grid.dataSource.query({page:1,pageSize:10});}
        });
        $("#risNext1").click(function () {
            if (busy) return;
            var item = grid.dataItem(grid.select()), department = $("#wizardDepartment").val();
            if (!item || !department) { error("Select a department and one PO."); return; }
            if (payload && payload.OrderId === item.Id && payload.Department === department) { go(2); return; }
            busy=true; $(this).prop("disabled",true);
            $.getJSON(root.attr("data-load"), {orderId:item.Id,department:department}).done(function (result) {
                if (result.Error) {error(result.Error);return;}
                payload=result; $(".ris-po").text(payload.PoNo); renderEntry(); go(2);
            }).fail(function(){error("Unable to load requisition entry.");})
            .always(function(){busy=false;$("#risNext1").prop("disabled",false);});
        });
        $("#risBack2").click(function(){if(!busy)go(1);});
        $("#risBack3").click(function(){if(!busy)go(2);});
        $("#risNext2").click(function(){
            if (!validate()) return;
            var target=$("#risReviewGroups");target.empty();
            $.each(payload.Groups,function(i,g){if(participating(g)){var body=groupCard(g,target);headerPanel(g,body,true,i);itemsPanel(g,body,true);}});
            go(3);
        });
        var confirmation=$("#risPostConfirm").kendoWindow({title:"Post RIS",modal:true,visible:false,width:400}).data("kendoWindow");
        $("#risPostAll").click(function(){if(!busy&&validate())confirmation.center().open();});
        $("#risCancelPost").click(function(){confirmation.close();});
        $("#risConfirmPost").click(function(){
            if(busy||!validate())return;
            busy=true;confirmation.close();$("#risPostAll,#risBack3").prop("disabled",true);
            $.post(root.attr("data-submit"),{
                payload:JSON.stringify(payload),submissionKey:$("#risSubmissionKey").val(),
                __RequestVerificationToken:root.find("input[name=__RequestVerificationToken]").val()
            }).done(function(r){
                if(r.Errors&&r.Errors.length){error($.isArray(r.Errors)?r.Errors.join("\n"):r.Errors);}
                else{window.location.href=root.attr("data-index");}
            }).fail(function(){error("Posting could not be confirmed. Check the RIS Index before retrying.");})
            .always(function(){busy=false;$("#risPostAll,#risBack3").prop("disabled",false);});
        });
    });
})(jQuery);
