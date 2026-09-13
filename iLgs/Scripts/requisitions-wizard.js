(function ($) {
    "use strict";

    $(function () {
        var root = $("#risWizard");
        if (!root.length) return;

        var payload = null;
        var busy = false;
        var currentStep = 1;
        var selectedItemData = null;

        var fields = [
            ["RisDate", "RIS Date", "date"],
            ["Purpose", "Purpose", "text"],
            ["RequestedBy", "Requested By", "text"],
            ["RequestedByDesignation", "Requested By Designation", "text"],
            ["RequestedDate", "Requested Date", "date"],
            ["ApprovedBy", "Approved By", "text"],
            ["ApprovedByDesignation", "Approved By Designation", "text"],
            ["ApprovedDate", "Approved Date", "date"],
            ["IssuedBy", "Issued By", "text"],
            ["IssuedByDesignation", "Issued By Designation", "text"],
            ["IssuedDate", "Issued Date", "date"],
            ["ReceivedBy", "Received By", "text"],
            ["ReceivedByDesignation", "Received By Designation", "text"],
            ["ReceivedDate", "Received Date", "date"]
        ];

        function showError(message) {
            $("#wizardError").html("<span class='k-icon k-i-warning'></span> " + message.replace(/\n/g, "<br/>")).show();
            window.scrollTo({ top: $("#wizardError").offset().top - 20, behavior: "smooth" });
        }

        function hideError() {
            $("#wizardError").hide().empty();
        }

        function updateStepperUI(targetStep) {
            for (var i = 1; i <= 3; i++) {
                var indicator = $("#stepIndicator" + i);
                var circle = $("#circle" + i);
                var label = $("#label" + i);
                var line = $("#line" + i);

                indicator.removeClass("is-active is-complete is-pending");

                if (i < targetStep) {
                    indicator.addClass("is-complete");
                    circle.css({ "background-color": "#1d4ed8", "color": "#ffffff", "box-shadow": "none" })
                        .html("<span class='k-icon k-i-check'></span>");
                    label.css({ "color": "#2c3e50", "font-weight": "600" });
                    if (line.length) line.css("background-color", "#1d4ed8");
                } else if (i === targetStep) {
                    indicator.addClass("is-active");
                    circle.css({ "background-color": "#2c3e50", "color": "#ffffff", "box-shadow": "0 0 0 4px #f1f5f9" })
                        .text(i);
                    label.css({ "color": "#2c3e50", "font-weight": "700" });
                } else {
                    indicator.addClass("is-pending");
                    circle.css({ "background-color": "#e2e8f0", "color": "#64748b", "box-shadow": "none" })
                        .text(i);
                    label.css({ "color": "#64748b", "font-weight": "500" });
                    if (line.length) line.css("background-color", "#cbd5e1");
                }
            }
        }

        function goToStep(stepNumber) {
            if (stepNumber < 1 || stepNumber > 3) return;
            if (stepNumber > 1 && !payload) {
                showError("Please select a Purchase Order in Step 1 before proceeding.");
                return;
            }
            if (stepNumber === 3 && !validate()) return;

            currentStep = stepNumber;
            hideError();
            $("#risStep1, #risStep2, #risStep3").hide();
            $("#risStep" + stepNumber).fadeIn(150);
            updateStepperUI(stepNumber);
            kendo.resize(root);
            window.scrollTo({ top: 0, behavior: "smooth" });
        }
        window.risGoToStep = goToStep;

        function dateValue(value) {
            if (!value) return "";
            var date = kendo.parseDate(value);
            return date ? kendo.toString(date, "yyyy-MM-dd") : "";
        }

        function participating(group) {
            return $.grep(group.Items, function (x) {
                return (x.QtyRequest && x.QtyRequest > 0) || (x.QtyIssue && x.QtyIssue > 0);
            }).length > 0;
        }

        function groupCard(group, target, isReview) {
            var card = $("<details class='ris-group' open></details>").appendTo(target);
            var summary = $("<summary></summary>").appendTo(card);
            var headerLeft = $("<div></div>").appendTo(summary);
            headerLeft.html(
                "<span class='fw-bold text-dark me-2'><span class='k-icon k-i-file-txt text-primary'></span> PR " + (group.Header.PrNo || "--") + "</span>" +
                "<span class='text-muted small'>&bull; " + (group.Header.Office || "") + "</span>"
            );
            var headerRight = $("<div></div>").appendTo(summary);
            headerRight.html(
                "<span class='badge bg-light text-secondary border me-2'>Fund: " + (group.Header.Fund || "General") + "</span>" +
                "<span class='badge bg-primary'>" + (group.Items ? group.Items.length : 0) + " Item(s)</span>"
            );
            return $("<div class='ris-group-body'></div>").appendTo(card);
        }

        function headerPanel(group, body, review, index) {
            $("<div class='ris-field-meta mb-3'></div>")
                .html("<span class='k-icon k-i-info text-info me-1'></span> <strong>Fund:</strong> " + (group.Header.Fund || "--") + " &nbsp;|&nbsp; <em>RIS and Control Numbers are officially generated on posting.</em>")
                .appendTo(body);

            var panel = $("<div class='ris-fields'></div>").appendTo(body);

            $.each(fields, function (_, spec) {
                var fieldKey = spec[0];
                var fieldLabel = spec[1];
                var fieldType = spec[2];

                var fieldContainer = $("<div class='ris-field'></div>").appendTo(panel);
                if (fieldKey === "Purpose") {
                    fieldContainer.addClass("ris-field-full");
                }

                var fieldId = "ris_" + index + "_" + fieldKey;
                $("<label></label>").attr("for", review ? null : fieldId).text(fieldLabel).appendTo(fieldContainer);

                if (review) {
                    var val = group.Header[fieldKey];
                    if (fieldType === "date" && val) {
                        var parsed = kendo.parseDate(val);
                        val = parsed ? kendo.toString(parsed, "MMM dd, yyyy") : val;
                    }
                    $("<div class='ris-value'></div>").text(val || "--").appendTo(fieldContainer);
                } else {
                    var input = $("<input class='form-control-custom' />")
                        .attr({ id: fieldId, type: fieldType, required: "required" })
                        .val(group.Header[fieldKey] || "")
                        .appendTo(fieldContainer)
                        .on("input change", function () {
                            group.Header[fieldKey] = $(this).val();
                        });

                    if (fieldType === "date") {
                        input.kendoDatePicker({
                            format: "yyyy-MM-dd",
                            value: dateValue(group.Header[fieldKey]),
                            change: function () {
                                group.Header[fieldKey] = this.value() ? kendo.toString(this.value(), "yyyy-MM-dd") : "";
                            }
                        });
                    }
                }
            });
        }

        function itemsPanel(group, body, review) {
            var responsiveWrap = $("<div class='table-responsive mt-3'></div>").appendTo(body);
            var table = $("<table class='table table-bordered table-hover align-middle mb-0 ris-items'></table>").appendTo(responsiveWrap);

            var thead = $("<thead></thead>").appendTo(table);
            var headerRow = $("<tr></tr>").appendTo(thead);

            var headers = [
                { text: "#", cls: "text-center", width: "40px" },
                { text: "Item No.", width: "70px" },
                { text: "PPMP Code", width: "110px" },
                { text: "Description" },
                { text: "Unit", width: "60px" },
                { text: "Allocated", num: true, width: "95px" },
                { text: "Prev. Issued", num: true, width: "100px" },
                { text: "Remaining", num: true, width: "100px" },
                { text: "Qty Requested", num: true, width: "125px" },
                { text: "Qty Issued", num: true, width: "125px" }
            ];

            $.each(headers, function (_, h) {
                var th = $("<th></th>").text(h.text);
                if (h.cls) th.addClass(h.cls);
                if (h.num) th.addClass("num text-end");
                if (h.width) th.css("width", h.width);
                th.appendTo(headerRow);
            });

            var tbody = $("<tbody></tbody>").appendTo(table);

            var totAlloc = 0, totPrev = 0, totRem = 0, totReq = 0, totIss = 0;

            $.each(group.Items, function (idx, item) {
                if (review && (!item.QtyRequest || item.QtyRequest === 0) && (!item.QtyIssue || item.QtyIssue === 0)) return;

                totAlloc += (item.AllocatedQty || 0);
                totPrev += (item.PreviousQty || 0);
                totRem += (item.RemainingQty || 0);
                totReq += (item.QtyRequest || 0);
                totIss += (item.QtyIssue || 0);

                var row = $("<tr></tr>").appendTo(tbody);

                $("<td class='text-center text-muted'></td>").text(idx + 1).appendTo(row);
                $("<td></td>").text(item.ItemNo || "--").appendTo(row);
                $("<td class='font-monospace small'></td>").text(item.ItemCode || "--").appendTo(row);
                $("<td class='fw-semibold'></td>").text(item.Description || "--").appendTo(row);
                $("<td class='text-secondary'></td>").text(item.Unit || "--").appendTo(row);

                $("<td class='num text-end font-monospace'></td>").text(kendo.toString(item.AllocatedQty, "n2")).appendTo(row);
                $("<td class='num text-end font-monospace text-muted'></td>").text(kendo.toString(item.PreviousQty, "n2")).appendTo(row);
                $("<td class='num text-end font-monospace fw-bold text-primary'></td>").text(kendo.toString(item.RemainingQty, "n2")).appendTo(row);

                if (review) {
                    $("<td class='num text-end font-monospace fw-bold'></td>").text(kendo.toString(item.QtyRequest, "n2")).appendTo(row);
                    $("<td class='num text-end font-monospace fw-bold text-success'></td>").text(kendo.toString(item.QtyIssue, "n2")).appendTo(row);
                } else {
                    var cellReq = $("<td class='num text-end'></td>").appendTo(row);
                    var inputReq = $("<input />").attr("aria-label", "Requested item " + item.ItemNo).appendTo(cellReq);
                    inputReq.kendoNumericTextBox({
                        min: 0,
                        max: item.RemainingQty,
                        decimals: 2,
                        format: "n2",
                        value: item.QtyRequest,
                        change: function () {
                            item.QtyRequest = this.value() === null ? 0 : this.value();
                            updateFooterTotals();
                        }
                    });

                    var cellIss = $("<td class='num text-end'></td>").appendTo(row);
                    var inputIss = $("<input />").attr("aria-label", "Issued item " + item.ItemNo).appendTo(cellIss);
                    inputIss.kendoNumericTextBox({
                        min: 0,
                        max: item.RemainingQty,
                        decimals: 2,
                        format: "n2",
                        value: item.QtyIssue,
                        change: function () {
                            item.QtyIssue = this.value() === null ? 0 : this.value();
                            updateFooterTotals();
                        }
                    });
                }
            });

            // Table Footer with Totals
            var tfoot = $("<tfoot></tfoot>").appendTo(table);
            var footerRow = $("<tr></tr>").appendTo(tfoot);
            $("<td colspan='5' class='text-end fw-bold'>PR Totals:</td>").appendTo(footerRow);
            $("<td class='num text-end font-monospace fw-bold'></td>").text(kendo.toString(totAlloc, "n2")).appendTo(footerRow);
            $("<td class='num text-end font-monospace fw-bold text-muted'></td>").text(kendo.toString(totPrev, "n2")).appendTo(footerRow);
            $("<td class='num text-end font-monospace fw-bold text-primary'></td>").text(kendo.toString(totRem, "n2")).appendTo(footerRow);
            var footReq = $("<td class='num text-end font-monospace fw-bold'></td>").text(kendo.toString(totReq, "n2")).appendTo(footerRow);
            var footIss = $("<td class='num text-end font-monospace fw-bold text-success'></td>").text(kendo.toString(totIss, "n2")).appendTo(footerRow);

            function updateFooterTotals() {
                var rTot = 0, iTot = 0;
                $.each(group.Items, function (_, itm) {
                    rTot += (itm.QtyRequest || 0);
                    iTot += (itm.QtyIssue || 0);
                });
                footReq.text(kendo.toString(rTot, "n2"));
                footIss.text(kendo.toString(iTot, "n2"));
            }
        }

        function renderEntry() {
            var target = $("#risEntryGroups");
            kendo.destroy(target);
            target.empty();

            $.each(payload.Groups, function (index, group) {
                $.each(fields, function (_, spec) {
                    if (spec[2] === "date") {
                        group.Header[spec[0]] = dateValue(group.Header[spec[0]]);
                    }
                });

                var body = groupCard(group, target, false);
                headerPanel(group, body, false, index);

                // Group Action Toolbar (AIR Style)
                var actions = $("<div class='ris-group-actions'></div>").appendTo(body);

                $("<button type='button' class='btn-procurement-secondary btn-sm'><span class='k-icon k-i-check'></span> Use All Remaining</button>")
                    .appendTo(actions)
                    .click(function () {
                        $.each(group.Items, function (_, x) {
                            x.QtyRequest = x.RemainingQty;
                            x.QtyIssue = x.RemainingQty;
                        });
                        renderEntry();
                    });

                $("<button type='button' class='btn-procurement-secondary btn-sm'><span class='k-icon k-i-undo'></span> Reset Quantities</button>")
                    .appendTo(actions)
                    .click(function () {
                        $.each(group.Items, function (_, x) {
                            x.QtyRequest = 0;
                            x.QtyIssue = 0;
                        });
                        renderEntry();
                    });

                itemsPanel(group, body, false);
            });
        }

        function validate() {
            var messages = [];
            var participatingCount = 0;

            $.each(payload.Groups, function (_, group) {
                if (!participating(group)) return;
                participatingCount++;

                var prefix = "PR " + (group.Header.PrNo || "") + ": ";
                var issuedTotal = 0;

                $.each(fields, function (_, spec) {
                    if (!$.trim(group.Header[spec[0]] || "")) {
                        messages.push(prefix + spec[1] + " is required.");
                    }
                });

                var dates = ["RisDate", "RequestedDate", "ApprovedDate", "IssuedDate", "ReceivedDate"];
                var previous = dateValue(group.Header.PoDate);

                $.each(dates, function (_, key) {
                    var current = group.Header[key];
                    if (current && previous && current < previous) {
                        messages.push(prefix + key + " must not precede the prior milestone date.");
                    }
                    if (current) previous = current;
                });

                $.each(group.Items, function (_, item) {
                    var req = item.QtyRequest || 0;
                    var iss = item.QtyIssue || 0;
                    var rem = item.RemainingQty || 0;

                    if (!isFinite(req) || !isFinite(iss) || req < 0 || iss < 0 || iss > req || req > rem) {
                        messages.push(prefix + "Item " + item.ItemNo + ": quantity must satisfy 0 <= Issued (" + iss + ") <= Requested (" + req + ") <= Remaining (" + rem + ").");
                    }
                    issuedTotal += iss;
                });

                if (!(issuedTotal > 0)) {
                    messages.push(prefix + "Please enter a positive issued quantity for at least one item.");
                }
            });

            if (!participatingCount) {
                messages.push("Please enter quantities for at least one participating Purchase Request.");
            }

            if (messages.length) {
                showError(messages.join("\n"));
                return false;
            }

            return true;
        }

        // ============================================================
        // STEP 1 SETUP & CANDIDATE PO GRID
        // ============================================================
        var grid = $("#risCandidates").data("kendoGrid");
        grid.dataSource.bind("error", function () {
            showError("Unable to load candidate Purchase Orders. Please refresh.");
        });

        grid.bind("dataBinding", function () {
            grid.clearSelection();
            selectedItemData = null;
            $("#risNext1").prop("disabled", true);
            $("#step1PoSummaryBar").hide();
            $("#step1SelectionStatusBadge").removeClass("status-posted").addClass("status-draft").text("No PO Selected");
        });

        window.onSelectCandidateRow = function (orderId) {
            var items = grid.dataSource.data();
            var item = null;
            for (var i = 0; i < items.length; i++) {
                if (items[i].Id === orderId) {
                    item = items[i];
                    break;
                }
            }
            if (!item) return;

            selectedItemData = item;
            var poDateStr = item.PoDate ? kendo.toString(item.PoDate, "MMM dd, yyyy") : "--";
            $("#lblSelectedPoNo").text(item.PoNo + " (" + poDateStr + ")");
            $("#lblSelectedPoDetails").text("Supplier: " + (item.Supplier || "--") + " | Department: " + (item.Department || "--") + " | PR: " + (item.PrNo || "--"));
            $("#lblSelectedPoRemaining").text(kendo.toString(item.RemainingQty, "n2"));

            $("#step1PoSummaryBar").fadeIn(150);
            $("#step1SelectionStatusBadge").removeClass("status-draft").addClass("status-posted")
                .html("<span class='k-icon k-i-check'></span> PO Selected");
            $("#risNext1").prop("disabled", false);
            hideError();
        };

        grid.table.on("click", "tr", function () {
            var radio = $(this).find("input[name='selectedCandidatePo']");
            if (radio.length && !radio.prop("checked")) {
                radio.prop("checked", true).trigger("change");
            }
        });

        $("#wizardDepartment").kendoDropDownList({
            optionLabel: "Select department",
            dataTextField: "Text",
            dataValueField: "Value",
            dataSource: {
                transport: {
                    read: {
                        url: root.attr("data-departments"),
                        dataType: "json"
                    }
                },
                error: function () {
                    showError("Unable to load eligible departments.");
                }
            },
            change: function () {
                payload = null;
                selectedItemData = null;
                grid.clearSelection();
                $("#step1PoSummaryBar").hide();
                $("#step1SelectionStatusBadge").removeClass("status-posted").addClass("status-draft").text("No PO Selected");
                $("#risNext1").prop("disabled", true);
                grid.dataSource.query({ page: 1, pageSize: 10 });
            }
        });

        // Next 1: Load Wizard Data & Go to Step 2
        $("#risNext1").click(function () {
            if (busy) return;
            var department = $("#wizardDepartment").val();
            if (!selectedItemData || !department) {
                showError("Please select a department and one Purchase Order.");
                return;
            }

            if (payload && payload.OrderId === selectedItemData.Id && payload.Department === department) {
                goToStep(2);
                return;
            }

            busy = true;
            $(this).prop("disabled", true);
            hideError();

            $.getJSON(root.attr("data-load"), { orderId: selectedItemData.Id, department: department })
                .done(function (result) {
                    if (result.Error) {
                        showError(result.Error);
                        return;
                    }
                    payload = result;
                    $(".ris-po").text(payload.PoNo);
                    renderEntry();
                    goToStep(2);
                })
                .fail(function () {
                    showError("Unable to load requisition details for the selected Purchase Order.");
                })
                .always(function () {
                    busy = false;
                    $("#risNext1").prop("disabled", false);
                });
        });

        // Step 2 & 3 Navigation
        $("#risBack2").click(function () {
            if (!busy) goToStep(1);
        });

        $("#risBack3").click(function () {
            if (!busy) goToStep(2);
        });

        $("#risNext2").click(function () {
            if (!validate()) return;

            var target = $("#risReviewGroups");
            kendo.destroy(target);
            target.empty();

            var totalAlloc = 0, totalReq = 0, totalIss = 0, prCount = 0;

            $.each(payload.Groups, function (i, g) {
                if (participating(g)) {
                    prCount++;
                    $.each(g.Items, function (_, item) {
                        totalAlloc += (item.AllocatedQty || 0);
                        totalReq += (item.QtyRequest || 0);
                        totalIss += (item.QtyIssue || 0);
                    });

                    var body = groupCard(g, target, true);
                    headerPanel(g, body, true, i);
                    itemsPanel(g, body, true);
                }
            });

            // Update Step 3 Summary Metrics
            $("#revPrCount").text(prCount);
            $("#revAllocatedQty").text(kendo.toString(totalAlloc, "n2"));
            $("#revRequestedQty").text(kendo.toString(totalReq, "n2"));
            $("#revIssuedQty").text(kendo.toString(totalIss, "n2"));

            $("#risPostModalSummary").text(
                "You are about to post " + prCount + " RIS transaction(s) totaling " +
                kendo.toString(totalIss, "n2") + " issued supply items for Purchase Order " + payload.PoNo + "."
            );

            goToStep(3);
        });

        // ============================================================
        // STEP 3: POST CONFIRMATION MODAL & SUBMIT
        // ============================================================
        var confirmation = $("#risPostConfirm").data("kendoWindow");

        $("#risPostAll").click(function () {
            if (!busy && validate()) {
                confirmation.center().open();
            }
        });

        $("#risCancelPost").click(function () {
            confirmation.close();
        });

        $("#risConfirmPost").click(function () {
            if (busy || !validate()) return;

            busy = true;
            confirmation.close();
            $("#risPostAll, #risBack3").prop("disabled", true);
            hideError();

            $.post(root.attr("data-submit"), {
                payload: JSON.stringify(payload),
                submissionKey: $("#risSubmissionKey").val(),
                __RequestVerificationToken: root.find("input[name=__RequestVerificationToken]").val()
            }).done(function (r) {
                if (r.Errors && r.Errors.length) {
                    showError($.isArray(r.Errors) ? r.Errors.join("\n") : r.Errors);
                } else {
                    window.location.href = root.attr("data-index");
                }
            }).fail(function () {
                showError("Posting could not be confirmed. Please check the RIS Index before retrying.");
            }).always(function () {
                busy = false;
                $("#risPostAll, #risBack3").prop("disabled", false);
            });
        });
    });
})(jQuery);
