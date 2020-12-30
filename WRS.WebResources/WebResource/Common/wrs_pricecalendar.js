function OnSaveAlert(executionContext) {
    var formContext = executionContext.getFormContext();

    var isdirty = formContext.data.entity.getIsDirty();

	if (isdirty) {
	    Xrm.Navigation.openConfirmDialog("Your changes will be saved", function () {
			// Xrm.Page.getAttribute("description").setValue("Yes call back selected");
		}, function () {
		    executionContext.getEventArgs().preventDefault()
		});
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