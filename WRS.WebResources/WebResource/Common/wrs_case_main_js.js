function HideSocialPaneItems(socialPaneType) {
	var ctrlElement = parent.document.getElementById("header_notescontrol");
	if (ctrlElement.children != null && ctrlElement.children.length > 0) {
		for (var ele = 0; ele < ctrlElement.children.length; ele++) {
			var ctrl = ctrlElement.children[ele];
			if (ctrl.title == socialPaneType) {
				ctrl.style.display = "none";
				if (ele + 1 < ctrlElement.children.length) {
					ctrlElement.children[ele + 1].click();
					return;
				} else if (ele - 1 >= 0) {
					ctrlElement.children[ele - 1].click();
					return;
				}
			}
		}
	}
}

function SetCaseTypesToText(executionContext) {

    var formContext = executionContext.getFormContext();
	var enquiry = formContext.getAttribute('wrs_enquiry').getValue();
	var complaint = formContext.getAttribute('wrs_complaint').getValue();
	var compliment = formContext.getAttribute('wrs_compliment').getValue();
	var suggestion = formContext.getAttribute('wrs_suggestion').getValue();

	var caseTypes = '';
	if (enquiry == 1) {
		caseTypes = "Enquiry";
	}

	if (complaint == 1) {
		if (caseTypes != '') {
			caseTypes = caseTypes + ", Complaint";
		} else
			caseTypes = "Complaint";
	}

	if (suggestion == 1) {
		if (caseTypes != '') {
			caseTypes = caseTypes + ", Suggestion";
		} else
			caseTypes = "Suggestion";
	}

	if (compliment == 1) {
		if (caseTypes != '') {
			caseTypes = caseTypes + ", Compliment";
		} else
			caseTypes = "Compliment";
	}

	formContext.getAttribute("wrs_casetypes").setValue(caseTypes);
}
