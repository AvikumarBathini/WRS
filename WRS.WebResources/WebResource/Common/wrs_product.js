if (typeof (WRS) === "undefined") {
    window.WRS = {};
}

if (typeof (WRS.Product) === "undefined") {
    window.WRS.Product = {};
}

if (typeof (WRS.Product.Form) === "undefined") {
    window.WRS.Product.Form = {};
}

if (typeof (WRS.Product.Form.GlobalVariables) === "undefined") {
    window.WRS.Product.Form.GlobalVariables = {};
}

var isParentProductPublished = false;

function SetSourceFrom(executionContext) {

    var formContext = executionContext.getFormContext();
    var sourcefield = formContext.getAttribute("wrs_sourcefrom");
    if (sourcefield) {
        sourcefield.setValue("crm");
        sourcefield.setSubmitMode("always");
        formContext.data.entity.save();
    }
}

function OnLoad(executionContext) {
    debugger;
    var formContext = executionContext.getFormContext();
    WRS.Product.Form.GlobalVariables.FormContext = formContext;
    var status = formContext.getAttribute('statuscode').getValue();
    var wrs_published = formContext.getAttribute('wrs_published');
    if (wrs_published != null && wrs_published != undefined && !wrs_published.getValue() && status == 167320000) {
        formContext.ui.setFormNotification("This product has been de-commissioned from EMP.", "WARNING")
    }
    else {
        formContext.ui.clearFormNotification();
    }

    if (status == 1) {
        formContext.getControl("name").setDisabled(true);
        formContext.getControl("description").setDisabled(true);
    }
    else {
        formContext.getControl("name").setDisabled(false);
        formContext.getControl("description").setDisabled(false);
    }
}

function OnSaveAlert(executionContext) {
    var formContext = executionContext.getFormContext();

    var isdirty = formContext.data.entity.getIsDirty();

    if (isdirty) {
        Xrm.Navigation.openConfirmDialog("Your changes will be saved", function () {
            // formContext.getAttribute("description").setValue("Yes call back selected");
        }, function () {
            executionContext.getEventArgs().preventDefault();
        });
    }
}

//Trigger this function when user cliks "Publish" button on Product
function PushProductToEMP(executionContext) {

    var formContext = WRS.Product.Form.GlobalVariables.FormContext;
    var confirmStrings = {
        text: "Do you want to push the selected Product to Author ?", title: "Publish for Author Confirmation!"
    };
    var confirmOptions = {
        height: 100, width: 450
    };

    Xrm.Navigation.openConfirmDialog(confirmStrings, confirmOptions).then(
     function (success) {
         if (success.confirmed) {
             var IsParentPublished = CheckParentRecordIsPublished();
             if (isParentProductPublished) {
                 var _productguid = formContext.data.entity.getId();
                 var parameters = {
                 };
                 var req = new XMLHttpRequest();
                 req.open("POST", formContext.context.getClientUrl() + "/api/data/v8.2/products(" + _productguid.replace("{", "").replace("}", "") + ")/Microsoft.Dynamics.CRM.wrs_pushproducttoemp", false);
                 req.setRequestHeader("OData-MaxVersion", "4.0");
                 req.setRequestHeader("OData-Version", "4.0");
                 req.setRequestHeader("Accept", "application/json");
                 req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
                 req.onreadystatechange = function () {
                     if (this.readyState === 4) {
                         req.onreadystatechange = null;
                         if (this.status === 200) {
                             var results = JSON.parse(this.response);
                             //if(results)
                             if (results != null) {
                                 if (results.IsPublished == false) {
                                     Xrm.Navigation.openAlertDialog("Error publishing Product to Author.")
                                 }
                                 else {
                                     var IsPublished = formContext.getAttribute('wrs_published');
                                     if (IsPublished != null && IsPublished.getValue())
                                         Xrm.Navigation.openAlertDialog("Product successfully pushed to Author.");
                                     if (IsPublished != null && !IsPublished.getValue())
                                         Xrm.Navigation.openAlertDialog("Product successfully de-commissioned from EMP.");
                                 }
                             }
                             formContext.data.refresh();
                         } else {
                             //Xrm.Navigation.openAlertDialog(this.statusText);
                         }
                     }
                 };
                 req.send(JSON.stringify(parameters));
             }
             else {
                 Xrm.Navigation.openAlertDialog("Please publish the parent Product.");
             }
         }
         else {
             return;
         }
     });
}

function CheckParentRecordIsPublished() {

    var formContext = WRS.Product.Form.GlobalVariables.FormContext;
    var _productguid = formContext.data.entity.getId();
    var req = new XMLHttpRequest();
    req.open("GET", formContext.context.getClientUrl() + "/api/data/v8.2/products(" + _productguid.replace("{", "").replace("}", "") + ")?$expand=parentproductid($select=statecode,statuscode)", false);
    req.setRequestHeader("OData-MaxVersion", "4.0");
    req.setRequestHeader("OData-Version", "4.0");
    req.setRequestHeader("Accept", "application/json");
    req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
    req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
    req.onreadystatechange = function () {
        if (this.readyState === 4) {
            req.onreadystatechange = null;
            if (this.status === 200) {
                debugger;
                var result = JSON.parse(this.response);
                var productid = result["productid"];
                if (result.hasOwnProperty("parentproductid")) {
                    if (result["parentproductid"] != null) {
                        var parentproductid_statecode = result["parentproductid"]["statecode"];
                        var parentproductid_statecode_formatted = result["parentproductid"]["statecode@OData.Community.Display.V1.FormattedValue"];
                        var parentproductid_statuscode = result["parentproductid"]["statuscode"];
                        if (parentproductid_statecode != 0)
                            isParentProductPublished = false;
                        else
                            isParentProductPublished = true;
                    }
                    else
                        isParentProductPublished = true;
                }
                else
                    isParentProductPublished = true;
            } else {
                Xrm.Navigation.openAlertDialog(this.statusText);
            }
        }
    };
    req.send();
}

function IsPublishVisible() {
    var status = Xrm.Page.getAttribute('statuscode');
    if (status != null && status != undefined && (status.getValue() == 2 || status.getValue() == 3)) {
        var req = new XMLHttpRequest();
        req.open("GET", Xrm.Page.context.getClientUrl() + "/api/data/v8.2/roles?fetchXml=%3Cfetch%20version%3D%221.0%22%20output-format%3D%22xml-platform%22%20mapping%3D%22logical%22%20distinct%3D%22true%22%3E%3Centity%20name%3D%22role%22%3E%3Cattribute%20name%3D%22name%22%20%2F%3E%3Cattribute%20name%3D%22businessunitid%22%20%2F%3E%3Cattribute%20name%3D%22roleid%22%20%2F%3E%3Corder%20attribute%3D%22name%22%20descending%3D%22false%22%20%2F%3E%3Cfilter%20type%3D%22and%22%3E%3Ccondition%20attribute%3D%22name%22%20operator%3D%22eq%22%20value%3D%22WRS%20Product%20Owner%22%20%2F%3E%3C%2Ffilter%3E%3Clink-entity%20name%3D%22systemuserroles%22%20from%3D%22roleid%22%20to%3D%22roleid%22%20visible%3D%22false%22%20intersect%3D%22true%22%3E%3Clink-entity%20name%3D%22systemuser%22%20from%3D%22systemuserid%22%20to%3D%22systemuserid%22%20alias%3D%22ac%22%3E%3Cfilter%20type%3D%22and%22%3E%3Ccondition%20attribute%3D%22systemuserid%22%20operator%3D%22eq-userid%22%20%2F%3E%3C%2Ffilter%3E%3C%2Flink-entity%3E%3C%2Flink-entity%3E%3C%2Fentity%3E%3C%2Ffetch%3E", false);
        req.setRequestHeader("OData-MaxVersion", "4.0");
        req.setRequestHeader("OData-Version", "4.0");
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
        req.onreadystatechange = function () {
            if (this.readyState === 4) {
                req.onreadystatechange = null;
                if (this.status === 200) {
                    var results = JSON.parse(this.response);
                    if (results != null && results != undefined && results.value != null && results.value != undefined && results.value.length > 0)
                        return true;
                    else
                        return false;
                } else {
                    Xrm.Navigation.openAlertDialog(this.statusText);
                }
            }
        };
        req.send();
    }
    else
        return false;
}

function RevokeProductToNC() {
    Xrm.Navigation.openConfirmDialog("Do you want to push the selected Product for Review ?",
        function () {
            var _productguid = Xrm.Page.data.entity.getId();
            var parameters = {};
            var req = new XMLHttpRequest();
            req.open("POST", Xrm.Page.context.getClientUrl() + "/api/data/v8.2/products(" + _productguid.replace("{", "").replace("}", "") + ")/Microsoft.Dynamics.CRM.wrs_EmailtoITTeamregardingProductReview", false);
            req.setRequestHeader("OData-MaxVersion", "4.0");
            req.setRequestHeader("OData-Version", "4.0");
            req.setRequestHeader("Accept", "application/json");
            req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
            req.onreadystatechange = function () {
                if (this.readyState === 4) {
                    req.onreadystatechange = null;
                    if (this.status === 204) {
                        //Success - No Return Data - Do Something
                    } else {
                        Xrm.Navigation.openAlertDialog(this.statusText);
                    }
                }
            };
            req.send(JSON.stringify(parameters));
        },
        function () {
            return;
        });
}