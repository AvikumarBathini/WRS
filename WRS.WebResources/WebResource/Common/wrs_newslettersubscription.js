function SetSourceFrom(executionContext) {

    var formContext = executionContext.getFormContext();
    var sourcefield = formContext.getAttribute("wrs_sourcefrom");
    if (sourcefield) {
        sourcefield.setValue("crm");
        sourcefield.setSubmitMode("always");
        formContext.data.entity.save();
    }
}