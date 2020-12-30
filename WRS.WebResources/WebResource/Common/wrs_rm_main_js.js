var initialVal_status;
var initialVal_confirmStatus;
var initialVal_qty;
var changeAdjustQty = false;
var changeStatus = false;

function OnLoad(executionContext) {

    var formContext = executionContext.getFormContext();
    initialVal_status = formContext.getAttribute('wrs_status').getValue();
    initialVal_confirmStatus = formContext.getAttribute('wrs_confirmstatus').getValue();
    initialVal_qty = formContext.getAttribute('wrs_quantity').getValue();
}

function AdjustQtyOnChange(executionContext) {

    var formContext = executionContext.getFormContext();
    var additional = formContext.getAttribute('wrs_additional').getValue();
	if (additional != null) {
		initialVal_qty = additional;
	}

	var adjustqty = formContext.getAttribute('wrs_adjustquantity').getValue();
	var qty = formContext.getAttribute('wrs_quantity').getValue();

	if (adjustqty === parseInt(adjustqty, 10)) {
		if (!changeStatus) {
		    formContext.getAttribute("wrs_confirmstatus").setValue(false);
		}

	    //formContext.getAttribute("wrs_quantity").setValue(qty + adjustqty);
		formContext.getAttribute("wrs_quantity").setValue(initialVal_qty +adjustqty);
		changeAdjustQty = true;

	} else {
		if (!changeStatus) {
		    formContext.getAttribute("wrs_confirmstatus").setValue(initialVal_confirmStatus);
		}
		formContext.getAttribute("wrs_quantity").setValue(initialVal_qty);
		changeAdjustQty = false;

	}
}

function OnsaveAlert(executionContext) {

    var formContext = executionContext.getFormContext();
    var status = formContext.getAttribute('wrs_confirmstatus').getValue();

	if (status == 0) {
	    Xrm.Navigation.openAlertDialog("Please change the Confirm Status to Published for confirming your changes", function () {
		    formContext.getControl("wrs_confirmstatus").setDisabled(false);
		    executionContext.getEventArgs().preventDefault();
		});
	} else {
	    //formContext.data.refresh(true);
	    //formContext.data.save().then(successCallback, errorCallback);
	    formContext.getAttribute("wrs_adjustquantity").setValue(null);
		changeAdjustQty = false;
		changeStatus = false;
		//OnLoad();
		initialVal_status = formContext.getAttribute('wrs_status').getValue();
		initialVal_confirmStatus = formContext.getAttribute('wrs_confirmstatus').getValue();
	}
}

function successCallback(executionContext) {

    var formContext = executionContext.getFormContext();
    formContext.getAttribute("wrs_adjustquantity").setValue(null);
	changeAdjustQty = false;
	changeStatus = false;
	OnLoad();
}

function errorCallback(executionContext) {
    /* var adjustqty = formContext.getAttribute('wrs_adjustquantity').getValue();
	var qty = formContext.getAttribute('wrs_quantity').getValue(); */
    var formContext = executionContext.getFormContext();
    formContext.getAttribute("wrs_quantity").setValue(initialVal_qty);
    formContext.getAttribute("wrs_adjustquantity").setValue(null);
}

function StatusOnChange(executionContext) {
    var formContext = executionContext.getFormContext();

	if (!changeAdjustQty) {
	    var statusVal = formContext.getAttribute('wrs_status').getValue();

		if (statusVal != initialVal_status) {
		    formContext.getAttribute("wrs_confirmstatus").setValue(false);
			changeStatus = true;
		} else if (statusVal == initialVal_status) {
		    formContext.getAttribute("wrs_confirmstatus").setValue(initialVal_confirmStatus);
			changeStatus = false;
		}
	} else {
	    var statusVal = formContext.getAttribute('wrs_status').getValue();

		if (statusVal != initialVal_status) {
			changeStatus = true;
		} else if (statusVal == initialVal_status) {
			changeStatus = false;
		}
	}
}

function SetSourceFrom(executionContext) {

    var formContext = executionContext.getFormContext();
    var sourcefield = formContext.getAttribute("wrs_sourcefrom");
	if (sourcefield) {
		sourcefield.setValue("crm");
		sourcefield.setSubmitMode("always");
		formContext.data.entity.save();
	}
}
