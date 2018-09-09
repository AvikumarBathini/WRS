﻿var globalSubscriptionFrom = null;
var globalSubscriptionStatus = null;
var globalDuplicateEmailValidation = false;

function SetSourceFrom() {
    var sourcefield = Xrm.Page.getAttribute("wrs_sourcefrom");
    if (sourcefield) {
        sourcefield.setValue("crm");
        sourcefield.setSubmitMode("always");
        Xrm.Page.data.entity.save();
    }
};

function ValidateEmailandPhone(execContext) {
    debugger;
    var eventArgs = execContext.getEventArgs();
    if (Xrm.Page.ui.getFormType() == 1 || eventArgs.getSaveMode() == 70 || eventArgs.getSaveMode() == 1) {
        var email = Xrm.Page.getAttribute("emailaddress1");
        var phone = Xrm.Page.getAttribute("telephone1");
        if ((email == null || email == undefined || email.getValue() == undefined || email.getValue() == undefined) && (phone == null || phone == undefined || phone.getValue() == undefined || phone.getValue() == undefined)) {
            Xrm.Page.ui.clearFormNotification();
            Xrm.Utility.alertDialog("Please provide Email or Phone/Phone 1", function () { execContext.getEventArgs().preventDefault(); })
        }
        else {
            if (globalDuplicateEmailValidation) {
                execContext.getEventArgs().preventDefault();
            }
            Xrm.Page.ui.clearFormNotification();
        }
    }
};

function DuplicateEmail() {
    var email = Xrm.Page.getAttribute("emailaddress1");
    if (email != null && email != undefined) {
        var emailId = email.getValue();
        if (emailId != null) {
            var req = new XMLHttpRequest();
            req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v8.2/contacts?$select=emailaddress1,telephone1&$filter=emailaddress1 eq '" + emailId.replace('@', '%40') + "' and  wrs_id ne null", false);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
            req.onreadystatechange = function () {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 200) {
                        var results = JSON.parse(this.response);
                        if (results != null && results.value.length > 0) {
                            Xrm.Page.ui.clearFormNotification();
                            globalDuplicateEmailValidation = true;
                            Xrm.Utility.alertDialog("Contact already exists with same Email - " + emailId + ".", function () {
                                globalDuplicateEmailValidation = true;
                            })
                        }
                        else {
                            globalDuplicateEmailValidation = false;
                            Xrm.Page.ui.clearFormNotification();
                        }
                    } else {
                        Xrm.Utility.alertDialog(this.statusText);
                        globalDuplicateEmailValidation = false;
                    }
                }
            };
            req.send();
        }
    }
};

function UpdateEmailSubscription() {
    var subscriptionStatus = Xrm.Page.getAttribute("wrs_subscriptionstatus");
    var subscriptionFrom = Xrm.Page.getAttribute("wrs_subscriptionsourcefrom");
    if (subscriptionStatus != null && subscriptionStatus != undefined && subscriptionStatus.getValue() != null && subscriptionStatus != undefined) {
        if (globalSubscriptionFrom == null && globalSubscriptionStatus == null) {
            if (subscriptionFrom != null && subscriptionFrom != undefined && subscriptionFrom.getValue() != null && subscriptionFrom != undefined) {
                globalSubscriptionFrom = subscriptionFrom.getValue();
                globalSubscriptionStatus = subscriptionStatus.getValue();
            }
            else {
                globalSubscriptionFrom = 167320001;
            }
        }
        if (globalSubscriptionStatus == subscriptionStatus.getValue()) {
            Xrm.Page.getAttribute("wrs_subscriptionsourcefrom").setValue(globalSubscriptionFrom);
        }
        else {
            Xrm.Page.getAttribute("wrs_subscriptionsourcefrom").setValue(167320001);
        }
    }
}