function OnLoad() {
	var formType = Xrm.Page.ui.getFormType();

	if (formType == 2) {
		Xrm.Page.getControl("wrs_name").setDisabled(true);
		Xrm.Page.getControl("wrs_blackoutcalendarid").setDisabled(true);
	}
}

function OnSaveAlert(context) {
	var isdirty = Xrm.Page.data.entity.getIsDirty();

	if (isdirty) {
		Xrm.Utility.confirmDialog("Your changes will be saved", function () {
			// Xrm.Page.getAttribute("description").setValue("Yes call back selected");
		}, function () {
			context.getEventArgs().preventDefault();
		});
	}
}

function OnChangeDate() {
	var dateFrom = Xrm.Page.getAttribute("wrs_datefrom").getValue();
	var dateTo = Xrm.Page.getAttribute("wrs_dateto").getValue();
	var current = new Date();
	var validate = true;

	if (dateFrom != "" && dateFrom != null) {
		var dateTimeDiff = dateFrom - current;
		var diffDays = (dateTimeDiff / 1000) / 3600 / 24;
		if (diffDays < 0) {
			alert("Date cannot be past date.");
			Xrm.Page.getAttribute("wrs_datefrom").setValue("");
			validate = false;
		}
	}

	if (validate && dateTo != "" && dateTo != null) {
		var dateTimeDiff = dateTo - current;
		var diffDays = (dateTimeDiff / 1000) / 3600 / 24;
		if (diffDays < 0) {
			alert("Date cannot be past date.");
			Xrm.Page.getAttribute("wrs_dateto").setValue("");
			validate = false;
		}
	}

	if (validate && dateFrom != "" && dateFrom != null && dateTo != "" && dateTo != null) {
		var dateTimeDiff = dateTo - dateFrom;
		var diffDays = (dateTimeDiff / 1000) / 3600 / 24;
		if (diffDays < 0) {
			alert("ToDate should be greater or equal to FromDate.");
			Xrm.Page.getAttribute("wrs_dateto").setValue("");
		}
	}
}
