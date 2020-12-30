function OnChangeProduct(executionContext) {
	var entityObject = executionContext.getFormContext().data.entity;
	var productAttr = entityObject.attributes.getByName("wrs_srproductid");
	var product = productAttr.getValue();
	//var product = Xrm.Page.getAttribute('wrs_freebieproductid').getValue();
	if (product != null) {
		var productId = product[0].id;

		if (productId != null) {
			productId = productId.replace('{', '').replace('}', '');
			var req = new XMLHttpRequest();
			var url = executionContext.getContext().getClientUrl() + "/api/data/v8.2/wrs_servicerecoveryproducts(" + productId + ")?$select=wrs_price";
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
						var price = result["wrs_price"];
						if (price != null) {
							var quantityAttr = entityObject.attributes.getByName("wrs_freebieqty");
							var quantity = quantityAttr.getValue();
							var totalAmt = price * quantity;
							var valueAttr = entityObject.attributes.getByName("wrs_valueforfreebies");
							valueAttr.setValue(totalAmt);
						} else {
							var valueAttr = entityObject.attributes.getByName("wrs_valueforfreebies");
							valueAttr.setValue(0);
						}

					} else {
						Xrm.Utility.alertDialog(this.statusText);
					}
				}
			};
			req.send();

		}
	} else {
		var valueAttr = entityObject.attributes.getByName("wrs_valueforfreebies");
		valueAttr.setValue(0);
	}
}

//not using
function OnChangeProduct_Old(executionContext) {
	var entityObject = executionContext.getFormContext().data.entity;
	var productAttr = entityObject.attributes.getByName("wrs_freebieproductid");
	var product = productAttr.getValue();
	//var product = Xrm.Page.getAttribute('wrs_freebieproductid').getValue();
	if (product != null) {
		var productId = product[0].id;
		var pricelistId = '';

		if (productId != null) {
			productId = productId.replace('{', '').replace('}', '');
			var req = new XMLHttpRequest();
			var url = executionContext.getContext().getClientUrl() + "/api/data/v8.2/products(" + productId + ")?$select=_pricelevelid_value";
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
						pricelistId = result["_pricelevelid_value"];
						var _pricelevelid_value_formatted = result["_pricelevelid_value@OData.Community.Display.V1.FormattedValue"];
						var _pricelevelid_value_lookuplogicalname = result["_pricelevelid_value@Microsoft.Dynamics.CRM.lookuplogicalname"];

					} else {
						Xrm.Utility.alertDialog(this.statusText);
					}
				}
			};
			req.send();

			if (pricelistId != null) {
				CalculateTotalAmount(executionContext, productId, pricelistId);
			} else {
				var valueAttr = entityObject.attributes.getByName("wrs_valueforfreebies");
				valueAttr.setValue(0);
			}
		}
	} else {
		var valueAttr = entityObject.attributes.getByName("wrs_valueforfreebies");
		valueAttr.setValue(0);
	}
}

function CalculateTotalAmount(executionContext, product, pricelist) {
	var entityObject = executionContext.getFormContext().data.entity;
	var req = new XMLHttpRequest();
	req.open("GET", executionContext.getContext().getClientUrl() + "/api/data/v8.2/productpricelevels?$select=amount&$filter=_productid_value eq " + product + " and  _pricelevelid_value eq " + pricelist, false);
	req.setRequestHeader("OData-MaxVersion", "4.0");
	req.setRequestHeader("OData-Version", "4.0");
	req.setRequestHeader("Accept", "application/json");
	req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
	req.setRequestHeader("Prefer", "odata.include-annotations=\"*\",odata.maxpagesize=1");
	req.onreadystatechange = function () {
		if (this.readyState === 4) {
			req.onreadystatechange = null;
			if (this.status === 200) {
				var results = JSON.parse(this.response);
				for (var i = 0; i < results.value.length; i++) {
					var amount = results.value[i]["amount"];
					var amount_formatted = results.value[i]["amount@OData.Community.Display.V1.FormattedValue"];

					var quantityAttr = entityObject.attributes.getByName("wrs_freebieqty");
					var quantity = quantityAttr.getValue();
					var totalAmt = amount * quantity;
					var valueAttr = entityObject.attributes.getByName("wrs_valueforfreebies");
					valueAttr.setValue(totalAmt);
				}
			} else {
				Xrm.Utility.alertDialog(this.statusText);
			}
		}
	};
	req.send();
}

function OnRowSelect(executionContext) {
	var entityObject = executionContext.getFormContext().data.entity;
	var valueAttr = entityObject.attributes.getByName("wrs_valueforfreebies");
	var valueControl = valueAttr.controls.get(0);
	valueControl.setDisabled(true);
}
