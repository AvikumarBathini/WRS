var globalSubscriptionFrom = null;
var globalSubscriptionStatus = null;
var globalDuplicateEmailValidation = false;

function SetSourceFrom(executionContext) {
debugger;
debugger;
var formContext = executionContext.getFormContext();
var sourcefield = formContext.getAttribute("wrs_sourcefrom");
    if (sourcefield) {
        sourcefield.setValue("crm");
        sourcefield.setSubmitMode("always");
        formContext.data.entity.save();
    }
};

function ValidateEmailandPhone(executionContext) {
    debugger;
    var formContext = executionContext.getFormContext();
    var eventArgs = executionContext.getEventArgs();
    if (formContext.ui.getFormType() == 1 || eventArgs.getSaveMode() == 70 || eventArgs.getSaveMode() == 1) {
        var email = formContext.getAttribute("emailaddress1");
        var phone = formContext.getAttribute("telephone1");
        if ((email == null || email == undefined || email.getValue() == undefined || email.getValue() == undefined) && (phone == null || phone == undefined || phone.getValue() == undefined || phone.getValue() == undefined)) {
            formContext.ui.clearFormNotification();
            Xrm.Utility.alertDialog("Please provide Email or Phone/Phone 1", function () { executionContext.getEventArgs().preventDefault(); })
        }
        else {
            if (globalDuplicateEmailValidation) {
                executionContext.getEventArgs().preventDefault();
            }
            formContext.ui.clearFormNotification();
        }
    }
};

function DuplicateEmail(executionContext) {

    var formContext = executionContext.getFormContext();
    var email = formContext.getAttribute("emailaddress1");
    if (email != null && email != undefined) {
        var emailId = email.getValue();
        if (emailId != null) {
            var req = new XMLHttpRequest();
            req.open("GET", Xrm.Utility.getGlobalContext().getClientUrl() + "/api/data/v8.2/contacts?$select=emailaddress1,telephone1&$filter=emailaddress1 eq '" + emailId.replace('@', '%40') + "' and  wrs_id ne null", false);
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
                            formContext.ui.clearFormNotification();
                            globalDuplicateEmailValidation = true;
                            Xrm.Navigation.openAlertDialog("Contact already exists with same Email - " + emailId + ".", function () {
                                globalDuplicateEmailValidation = true;
                            })
                        }
                        else {
                            globalDuplicateEmailValidation = false;
                            formContext.ui.clearFormNotification();
                        }
                    } else {
                        Xrm.Navigation.openAlertDialog(this.statusText);
                        globalDuplicateEmailValidation = false;
                    }
                }
            };
            req.send();
        }
    }
};

function UpdateEmailSubscription(executionContext) {

    var formContext = executionContext.getFormContext();
    var subscriptionStatus = formContext.getAttribute("wrs_subscriptionstatus");
    var subscriptionFrom = formContext.getAttribute("wrs_subscriptionsourcefrom");
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
            formContext.getAttribute("wrs_subscriptionsourcefrom").setValue(globalSubscriptionFrom);
        }
        else {
            formContext.getAttribute("wrs_subscriptionsourcefrom").setValue(167320001);
        }
    }
}