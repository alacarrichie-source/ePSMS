/**
 * epsms-dialog.js - Canonical Dialog & Confirmation System for ePSMS
 * Reusable implementation based on the canonical ParSet confirmation pattern.
 * Provides unified, styled confirmation modals, alerts, notifications, and button busy states.
 */
(function (window, $) {
    "use strict";

    // 1. Core notification helpers
    function getNotificationWidget() {
        return $("#propertyCardNotification").data("kendoNotification") ||
               $("#parNotification").data("kendoNotification") ||
               $("#epsmsNotification").data("kendoNotification");
    }

    function notifySuccess(message) {
        var notification = getNotificationWidget();
        if (notification) {
            notification.show(message, "success");
        } else if (window.toastr) {
            toastr.success(message);
        } else {
            console.log("[SUCCESS]", message);
        }
    }

    function notifyWarning(message) {
        var notification = getNotificationWidget();
        if (notification) {
            notification.show(message, "warning");
        } else {
            epsmsAlert({
                title: "Warning",
                type: "warning",
                message: message
            });
        }
    }

    function notifyError(message) {
        var notification = getNotificationWidget();
        if (notification) {
            notification.show(message, "error");
        } else {
            epsmsAlert({
                title: "Error",
                type: "danger",
                message: message
            });
        }
    }

    // 2. Button busy/restore helpers (ParSet pattern)
    function setClickedButtonBusy($button, text) {
        if (!$button || !$button.length) return;
        $button.data("epsms-original-html", $button.html());
        $button.prop("disabled", true).addClass("k-state-disabled").html("<span class='k-icon k-i-loading'></span> " + (text || "Processing..."));
    }

    function restoreClickedButton($button) {
        if (!$button || !$button.length) return;
        var originalHtml = $button.data("epsms-original-html");
        $button.prop("disabled", false).removeClass("k-state-disabled");
        if (originalHtml) {
            $button.html(originalHtml).removeData("epsms-original-html");
        }
    }

    // 3. AJAX Failure message extractor (ParSet pattern)
    function ajaxFailureMessage(xhr, fallback) {
        if (xhr && xhr.responseJSON) {
            if (xhr.responseJSON.message) return xhr.responseJSON.message;
            if (xhr.responseJSON.Message) return xhr.responseJSON.Message;
            if (xhr.responseJSON.Errors) {
                var errs = xhr.responseJSON.Errors;
                if (Array.isArray(errs)) {
                    return errs.map(function (e) { return e.Message || e.message || e; }).join(" ");
                }
                return String(errs);
            }
        }
        if (xhr && xhr.responseText) {
            var responseText = $.trim($("<div>").html(xhr.responseText).text());
            if (responseText) {
                return responseText.length > 500 ? responseText.substring(0, 500) : responseText;
            }
        }
        if (xhr && xhr.statusText && xhr.statusText !== "error") {
            return xhr.statusText;
        }
        return fallback || "An unexpected server error occurred.";
    }

    // 4. Dynamic Window Remover
    function removeDynamicWindow(selector) {
        var $element = $(selector);
        var widget = $element.data("kendoWindow");
        if (widget) {
            widget.destroy();
        }
        $element.remove();
    }

    // 5. Canonical Confirmation Dialog: epsmsConfirm (matching ParSet modal design)
    function epsmsConfirm(options) {
        options = options || {};
        var type = options.type || "danger"; // danger | warning | info | primary
        var title = options.title || "Confirm Action";
        var headline = options.headline || title;
        var message = options.message || "";
        var entityLabel = options.entityLabel || "";
        var entityValue = options.entityValue || "";
        var cancelText = options.cancelText || "Cancel";
        var confirmText = options.confirmText || (type === "danger" ? "Delete" : "Confirm");
        var width = options.width || 480;

        var typeConfig = {
            danger: {
                boxClass: "alert-danger",
                boxStyle: "background: rgb(254, 242, 242); border: 1px solid rgb(254, 202, 202); color: rgb(153, 27, 27); border-radius: 6px; padding: 12px; margin-bottom: 15px;",
                entityColor: "color: rgb(127, 29, 29);",
                btnStyle: "background: rgb(220, 38, 38); border-color: rgb(220, 38, 38); color: rgb(255, 255, 255);",
                icon: options.icon || "k-i-delete",
                confirmIcon: options.confirmIcon || "k-i-delete"
            },
            warning: {
                boxClass: "alert-warning",
                boxStyle: "background: rgb(255, 251, 235); border: 1px solid rgb(253, 230, 138); color: rgb(146, 64, 14); border-radius: 6px; padding: 12px; margin-bottom: 15px;",
                entityColor: "color: rgb(180, 83, 9);",
                btnStyle: "background: rgb(217, 119, 6); border-color: rgb(217, 119, 6); color: rgb(255, 255, 255);",
                icon: options.icon || "k-i-undo",
                confirmIcon: options.confirmIcon || "k-i-undo"
            },
            info: {
                boxClass: "alert-info",
                boxStyle: "background: rgb(240, 249, 255); border: 1px solid rgb(186, 230, 253); color: rgb(3, 105, 161); border-radius: 6px; padding: 12px; margin-bottom: 15px;",
                entityColor: "color: rgb(12, 74, 110);",
                btnStyle: "background: rgb(0, 37, 118); border-color: rgb(0, 37, 118); color: rgb(255, 255, 255);",
                icon: options.icon || "k-i-check-circle",
                confirmIcon: options.confirmIcon || "k-i-check"
            },
            primary: {
                boxClass: "alert-info",
                boxStyle: "background: rgb(240, 249, 255); border: 1px solid rgb(186, 230, 253); color: rgb(3, 105, 161); border-radius: 6px; padding: 12px; margin-bottom: 15px;",
                entityColor: "color: rgb(12, 74, 110);",
                btnStyle: "background: #2c3e50; border-color: #2c3e50; color: #ffffff;",
                icon: options.icon || "k-i-check",
                confirmIcon: options.confirmIcon || "k-i-check"
            }
        };

        var cfg = typeConfig[type] || typeConfig.danger;

        var entityHtml = "";
        if (entityLabel && entityValue) {
            entityHtml = '<div>' + kendo.htmlEncode(entityLabel) + ': <strong style="font-family: Consolas, monospace; ' + cfg.entityColor + ' font-size: 14px;">' + kendo.htmlEncode(entityValue) + '</strong></div>';
        }

        var winId = "epsmsConfirmDialog_" + Math.floor(Math.random() * 1000000);
        removeDynamicWindow("#" + winId);

        var container = $("#windowcontainer").length ? $("#windowcontainer") : $("body");
        var html = '<div id="' + winId + '" class="epsms-confirm-window" style="display:none;">' +
            '<div class="p-3" style="font-size: 13px; padding: 18px;">' +
                '<div class="alert ' + cfg.boxClass + ' mb-3" style="' + cfg.boxStyle + '">' +
                    '<strong style="display:block; margin-bottom: 6px; font-size: 14px;">' +
                        '<span class="k-icon ' + cfg.icon + '"></span> ' + kendo.htmlEncode(headline) +
                    '</strong>' +
                    entityHtml +
                    '<div style="font-size: 12px; margin-top: 6px; line-height: 1.4;">' +
                        message +
                    '</div>' +
                '</div>' +
                '<div style="display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; padding-top: 12px; border-top: 1px solid rgb(226, 232, 240);">' +
                    '<button type="button" class="btn-procurement-secondary btn-cancel-dialog">' + kendo.htmlEncode(cancelText) + '</button>' +
                    '<button type="button" class="btn-procurement-primary btn-confirm-dialog" style="' + cfg.btnStyle + '">' +
                        '<span class="k-icon ' + cfg.confirmIcon + '"></span> ' + kendo.htmlEncode(confirmText) +
                    '</button>' +
                '</div>' +
            '</div>' +
        '</div>';

        container.append(html);

        var $win = $("#" + winId);
        var winWidget = $win.kendoWindow({
            title: title,
            width: width,
            modal: true,
            visible: false,
            resizable: false,
            draggable: true,
            actions: ["Close"],
            deactivate: function () {
                var $element = this.element;
                this.destroy();
                $element.remove();
                if (typeof options.onCancel === "function" && !$win.data("confirmed")) {
                    options.onCancel();
                }
            }
        }).data("kendoWindow");

        var $confirmBtn = $win.find(".btn-confirm-dialog");
        var $cancelBtn = $win.find(".btn-cancel-dialog");

        $cancelBtn.on("click", function (e) {
            e.preventDefault();
            winWidget.close();
        });

        $confirmBtn.on("click", function (e) {
            e.preventDefault();
            $win.data("confirmed", true);
            if (typeof options.onConfirm === "function") {
                options.onConfirm(winWidget, $confirmBtn);
            }
        });

        winWidget.center().open();
        winWidget.toFront();
        $confirmBtn.focus();
        return winWidget;
    }

    // 6. Canonical Alert Dialog: epsmsAlert (matching ParSet notice/warning design)
    function epsmsAlert(options) {
        if (typeof options === "string") {
            options = { message: options };
        }
        options = options || {};
        var type = options.type || "warning";
        var title = options.title || (type === "danger" ? "Error" : "Notice");
        var headline = options.headline || title;
        var message = options.message || "";
        var buttonText = options.buttonText || "OK";
        var width = options.width || 480;

        var typeConfig = {
            danger: {
                boxClass: "alert-danger",
                boxStyle: "background: rgb(254, 242, 242); border: 1px solid rgb(254, 202, 202); color: rgb(153, 27, 27); border-radius: 6px; padding: 12px; margin-bottom: 15px;",
                icon: "k-i-warning",
                btnStyle: "background: rgb(220, 38, 38); border-color: rgb(220, 38, 38); color: rgb(255, 255, 255);"
            },
            warning: {
                boxClass: "alert-warning",
                boxStyle: "background: rgb(255, 251, 235); border: 1px solid rgb(253, 230, 138); color: rgb(146, 64, 14); border-radius: 6px; padding: 12px; margin-bottom: 15px;",
                icon: "k-i-warning",
                btnStyle: "background: rgb(217, 119, 6); border-color: rgb(217, 119, 6); color: rgb(255, 255, 255);"
            },
            info: {
                boxClass: "alert-info",
                boxStyle: "background: rgb(240, 249, 255); border: 1px solid rgb(186, 230, 253); color: rgb(3, 105, 161); border-radius: 6px; padding: 12px; margin-bottom: 15px;",
                icon: "k-i-info",
                btnStyle: "background: rgb(0, 37, 118); border-color: rgb(0, 37, 118); color: rgb(255, 255, 255);"
            }
        };

        var cfg = typeConfig[type] || typeConfig.warning;

        var winId = "epsmsAlertDialog_" + Math.floor(Math.random() * 1000000);
        removeDynamicWindow("#" + winId);

        var container = $("#windowcontainer").length ? $("#windowcontainer") : $("body");
        var html = '<div id="' + winId + '" class="epsms-confirm-window" style="display:none;">' +
            '<div class="p-3" style="font-size: 13px; padding: 18px;">' +
                '<div class="alert ' + cfg.boxClass + ' mb-3" style="' + cfg.boxStyle + '">' +
                    '<strong style="display:block; margin-bottom: 6px; font-size: 14px;">' +
                        '<span class="k-icon ' + cfg.icon + '"></span> ' + kendo.htmlEncode(headline) +
                    '</strong>' +
                    '<div style="font-size: 12px; margin-top: 6px; line-height: 1.4;">' +
                        message +
                    '</div>' +
                '</div>' +
                '<div style="display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; padding-top: 12px; border-top: 1px solid rgb(226, 232, 240);">' +
                    '<button type="button" class="btn-procurement-primary btn-alert-ok" style="' + cfg.btnStyle + '">' +
                        kendo.htmlEncode(buttonText) +
                    '</button>' +
                '</div>' +
            '</div>' +
        '</div>';

        container.append(html);

        var $win = $("#" + winId);
        var winWidget = $win.kendoWindow({
            title: title,
            width: width,
            modal: true,
            visible: false,
            resizable: false,
            draggable: true,
            actions: ["Close"],
            deactivate: function () {
                var $element = this.element;
                this.destroy();
                $element.remove();
                if (typeof options.onClose === "function") {
                    options.onClose();
                }
            }
        }).data("kendoWindow");

        $win.find(".btn-alert-ok").on("click", function (e) {
            e.preventDefault();
            winWidget.close();
        });

        winWidget.center().open();
        winWidget.toFront();
        $win.find(".btn-alert-ok").focus();
        return winWidget;
    }

    // Expose globally on window
    window.epsmsConfirm = epsmsConfirm;
    window.epsmsAlert = epsmsAlert;
    window.notifySuccess = window.notifySuccess || notifySuccess;
    window.notifyWarning = window.notifyWarning || notifyWarning;
    window.notifyError = window.notifyError || notifyError;
    window.setClickedButtonBusy = setClickedButtonBusy;
    window.restoreClickedButton = restoreClickedButton;
    window.ajaxFailureMessage = ajaxFailureMessage;
    window.removeDynamicWindow = removeDynamicWindow;

})(window, jQuery);
