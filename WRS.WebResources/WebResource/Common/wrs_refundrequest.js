function OnChangeCase(executionContext) {

    var formContext = executionContext.getFormContext();
    var globalContext = Xrm.Utility.getGlobalContext();

    var incident = formContext.getAttribute('wrs_caseid').getValue();
	if (incident != null) {
		var incidentId = incident[0].id;
		var customer = null;

		if (incidentId != null) {
			incidentId = incidentId.replace('{', '').replace('}', '');
			var req = new XMLHttpRequest();
			var url = globalContext.getClientUrl() + "/api/data/v8.2/incidents(" + incidentId + ")?$select=_customerid_value";
			req.open("GET", url, false);
			req.setRequestHeader("OData-MaxVersion", "4.0");
			req.setRequestHeader("OData-Version", "4.0");
			req.setRequestHeader("Accept", "application/json");
			req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
			req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
			req.onreadystatechange = function () {
				if (this.readyState === 4) {
					req.onreadystatechange = null;
					if (this.status === 200) {
						var result = JSON.parse(this.response);
						customer = result["_customerid_value"];

					} else {
						Xrm.Utility.alertDialog(this.statusText);
					}
				}
			};
			req.send();

			if (customer != null) {
			    SetCustomerLookup(executionContext,customer);
			} else {
			    formContext.getAttribute("wrs_customerid").setValue(null);
			}
		}
	} else {
	    formContext.getAttribute("wrs_customerid").setValue(null);
	}
}

function SetCustomerLookup(executionContext,custId) {

    var formContext = executionContext.getFormContext();
    var globalContext = Xrm.Utility.getGlobalContext();

	var customerId = custId.replace('{', '').replace('}', '');
	var req = new XMLHttpRequest();
	var url = globalContext.getClientUrl() + "/api/data/v8.2/contacts(" + customerId + ")?$select=fullname";
	req.open("GET", url, false);
	req.setRequestHeader("OData-MaxVersion", "4.0");
	req.setRequestHeader("OData-Version", "4.0");
	req.setRequestHeader("Accept", "application/json");
	req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
	req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
	req.onreadystatechange = function () {
		if (this.readyState === 4) {
			req.onreadystatechange = null;
			if (this.status === 200) {
				var result = JSON.parse(this.response);
				var customername = result["fullname"];

				var value = new Array();
				value[0] = new Object();
				value[0].id = custId;
				value[0].name = customername;
				value[0].entityType = "contact";
				formContext.getAttribute("wrs_customerid").setValue(value);

			} else {
			    Xrm.Navigation.openAlertDialog(this.statusText);
			}
		}
	};
	req.send();
}
