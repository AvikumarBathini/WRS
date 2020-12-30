function SetSourceFrom() {
    var sourcefield = Xrm.Page.getAttribute("wrs_sourcefrom");
    if (sourcefield) {
        sourcefield.setValue("crm");
        sourcefield.setSubmitMode("always");
        Xrm.Page.data.entity.save();
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
