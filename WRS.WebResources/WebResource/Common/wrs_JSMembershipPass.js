function getElement(elementName) {
    if (document.getElementById(elementName) == null || document.getElementById(elementName) == 'undefined') {
        return window.parent.document.getElementById(elementName);
    }
    else {
        return document.getElementById(elementName);
    }
}

function dynamicallyFilterSubGrid(executionContext) {
    debugger;
    debugger;
    var formContext = executionContext.getFormContext();
    //CRM loads subgrid after form is loaded.. so when we are adding script on form load.. need to wait unitil subgrid is loaded.
    // thats why adding delay..
    var subGrid = formContext.getControl("MembershipOrders");
    if (subGrid == null) {
        //The subgrid hasn't loaded, wait 100 mili second and        then try again
        setTimeout(dynamicallyFilterSubGrid(executionContext), 100);
        return;
    }

    // Layout of subgrid.
    var LayoutXml = "<grid name='resultset' object='8' jump='title' select='1' preview='1' icon='1'>" +
                        " <row name='result' id='name'>" +
                                "<cell name='name' width='300' />" +
                                "<cell name='customerid' width='200' />" +
                                "<cell name='wrs_galaxyordernumber' width='100' />" +
                            "</row>" +
                     "</grid>";

    var emailid = formContext.getAttribute("wrs_mainmemberemail");
    if (emailid != null && emailid != undefined && emailid.getValue() != undefined) {
        emailid = emailid.getValue();
        var fetchXML = "<fetch version='1.0' mapping='logical' distinct='false'>";
        fetchXML += "<entity name='salesorder'>";
        fetchXML += "<attribute name='name' />";
        fetchXML += "<attribute name='customerid' />";
        fetchXML += "<attribute name='statuscode' />";
        fetchXML += "<attribute name='totalamount' />";
        fetchXML += "<attribute name='salesorderid' />";
        fetchXML += "<attribute name='wrs_id' />";
        fetchXML += "<attribute name='wrs_galaxyordernumber' />";
        fetchXML += "<order attribute='name' descending='false' />";
        fetchXML += "<filter type='and'>";
        fetchXML += "<condition attribute='wrs_customeremail' operator='eq' uitype='contact' value='" + emailid + "' />";
        fetchXML += "</filter>";
        fetchXML += "</entity>";
        fetchXML += "</fetch>";

        formContext.getControl("MembershipOrders").setFilterXml(fetchXML);

        formContext.ui.controls.get("MembershipOrders").refresh();
        //var setFetchXmlStr = Microsoft.Crm.Client.Core.Storage.DataApi.ListQuery.prototype.set_FetchXml.toString();
        // var newFunc = setFetchXmlStr.replace("function(e){", "function(e){if (e.indexOf('ZZZAAA') >= 0) {e = fetchXML;}");
        // eval("Microsoft.Crm.Client.Core.Storage.DataApi.ListQuery.prototype.set_FetchXml=" + newFunc);
        // subGrid.refresh();

        //subGrid.control.SetParameter("fetchXml", fetchXML);
        //subGrid.control.SetParameter("layoutXml", LayoutXml);
        //subGrid.control.SetParameter("effectiveFetchXml", fetchXML);
        //subGrid.control.SetParameter("fetchXmlForFilters", fetchXML);
        //subGrid.control.refresh();
    }
}


function disableFormFields(executionContext) {
    var formContext = executionContext.getFormContext();
    formContext.ui.controls.forEach(function (control, index) {
        var controlType = control.getControlType();
        if (controlType != "iframe" && controlType != "webresource" && controlType != "subgrid") {
            try {
                control.setDisabled(true);
            } catch (e) {
                debugger;
                debugger;
            }
        }
    });
}