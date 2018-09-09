var FORM_TYPE_CREATE = 1;
var FORM_TYPE_UPDATE = 2;
var isUserPresentinGETeam = false;
var isUserPresentinMembershipTeam = false;
var GETeamName = "GE";
var MembershipTeamName = "Membership";
var enquiryQueueName = "FeedbackSupport";
var membershipQueueName = "<Membership>";
var queueEntityType = "queue";
var queueid = null;
var teamName = null;
var fromSet = false;

function emailOnLoad() {
    var formType = Xrm.Page.ui.getFormType();
    if ((formType == FORM_TYPE_CREATE || formType == FORM_TYPE_UPDATE) && fromSet != true) {

        retrieveUserTeams(getLoggedinUser());
        if (isUserPresentinGETeam) {
            var enquiryQueue = new Array();
            enquiryQueue[0] = new Object();
            getQueueId(enquiryQueueName);
            enquiryQueue[0].id = queueid //guid of lookup record
            enquiryQueue[0].name = enquiryQueueName; //formatted value of lookup record
            enquiryQueue[0].entityType = queueEntityType; //entity type

            Xrm.Page.getAttribute("from").setValue(enquiryQueue);
            fromSet = true;
        }
        if (isUserPresentinMembershipTeam) {
            var membershipQueue = new Array();
            membershipQueue[0] = new Object();
            getQueueId(membershipQueueName);
            membershipQueue[0].id = queueid //guid of lookup record
            membershipQueue[0].name = membershipQueueName; //formatted value of lookup record
            membershipQueue[0].entityType = queueEntityType; //entity type
            Xrm.Page.getAttribute("from").setValue(membershipQueue);
            fromSet = true;
        }
    }
}

function getLoggedinUser() {
    var userId = Xrm.Page.context.getUserId();
    // var guid = "[A-z0-9]{8}-[A-z0-9]{4}-[A-z0-9]{4}-[A-z0-9]{4}-[A-z0-9]{12}";
    // userId = userId.match(guid);
    userId = userId.replace("{", "").replace("}", "");
    return userId;
}

function retrieveUserTeams(userId) {
    var query = "$select=systemuserid,teamid&$filter=systemuserid eq " + userId;
    SDK.WEBAPI.retrieveMultipleRecordsSync("teammemberships", query, function (result) {

        if (result.value.length > 0) {
            for (var i = 0; i < result.value.length; i++) {
                var systemuserid = result.value[i]["systemuserid"];
                var teamid = result.value[i]["teamid"];
                teamid = teamid.replace("{", "").replace("}", "");
                retrieveTeamName(teamid);
                if (teamName == GETeamName) {
                    isUserPresentinGETeam = true;
                    break;
                }

                if (teamName == MembershipTeamName) {
                    isUserPresentinMembershipTeam = true;
                    break;
                }
            }
        } else {
            alert("Escalate to TeamID not found");
        }
        //alert("Success");
    },
		function () {
		    alert("Error extracing Escalate to TeamID");
		    return;
		},
		function () {
		    //alert("Complete");
		});

}

function retrieveTeamName(teamid) {

    var query = "$select=name&$filter=teamid eq  " + teamid;
    SDK.WEBAPI.retrieveMultipleRecordsSync("teams", query, function (result) {

        if (result.value.length > 0) {
            teamName = result.value[0]["name"];
            //return name;
        } else {
            alert("Escalate to TeamID not found");
        }
        //alert("Success");
    },
		function () {
		    alert("Error extracing Escalate to TeamID");
		    return;
		},
		function () {
		    //alert("Complete");
		});

}

function getQueueId(enquiryQueueName) {
    var query = "$select=queueid&$filter=name eq  '" + enquiryQueueName + "'";
    SDK.WEBAPI.retrieveMultipleRecordsSync("queue", query, function (result) {

        if (result.value.length > 0) {
            queueid = result.value[0]["queueid"];
            //return queueid;
        } else {
            alert("queueid not found");
        }
        //alert("Success");
    },
		function () {
		    alert("Error extracing queueid");
		    return;
		},
		function () {
		    //alert("Complete");
		});

}

if (typeof (SDK) == "undefined") {
    SDK = {
        __namespace: true
    };
}
SDK.WEBAPI = {
    _context: function () {
        ///
        /// Private function to the context object.
        ///

        ///Context
        if (typeof GetGlobalContext != "undefined") {
            return GetGlobalContext();
        } else {
            if (typeof Xrm != "undefined") {
                return Xrm.Page.context;
            } else {
                throw new Error("Context is not available.");
            }
        }
    },
    _getClientUrl: function () {
        ///
        /// Private function to return the server URL from the context
        ///

        ///String
        var clientUrl = this._context().getClientUrl()

        return clientUrl;
    },
    _WebAPIPath: function () {
        ///
        /// Private function to return the path to the REST endpoint.
        ///

        ///String
        return this._getClientUrl() + "/api/data/v8.2/";
    },
    _errorHandler: function (req) {
        ///
        /// Private function return an Error object to the errorCallback
        ///

        ////// The XMLHttpRequest response that returned an error.
        /// ///Error
        //Error descriptions come from http://support.microsoft.com/kb/193625
        if (req.status == 12029) {
            return new Error("The attempt to connect to the server failed.");
        }
        if (req.status == 12007) {
            return new Error("The server name could not be resolved.");
        }
        var errorText;
        try {
            errorText = JSON.parse(req.responseText).error.message.value;
        } catch (e) {
            errorText = req.responseText
        }

        return new Error("Error : " +
			req.status + ": " +
			req.statusText + ": " + errorText);
    },
    _dateReviver: function (key, value) {
        ///
        /// Private function to convert matching string values to Date objects.
        ///

        ////// The key used to identify the object property
        /// ////// The string value representing a date
        /// var a;
        if (typeof value === 'string') {
            a = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2}(?:\.\d*)?)Z$/.exec(value);
            if (a) {
                return new Date(Date.UTC(+a[1], +a[2] - 1, +a[3], +a[4], +a[5], +a[6]));
            }
        }
        return value;
    },
    _parameterCheck: function (parameter, message) {
        ///
        /// Private function used to check whether required parameters are null or undefined
        ///

        /// The parameter to check;
        /// The error message text to include when the error is thrown.
        if ((typeof parameter === "undefined") || parameter === null) {
            throw new Error(message);
        }
    },
    _stringParameterCheck: function (parameter, message) {
        ///
        /// Private function used to check whether required parameters are null or undefined
        ///

        ///The string parameter to check;
        /// The error message text to include when the error is thrown.
        if (typeof parameter != "string") {
            throw new Error(message);
        }
    },
    _callbackParameterCheck: function (callbackParameter, message) {
        ///
        /// Private function used to check whether required callback parameters are functions
        ///

        /// The callback parameter to check;
        ///  The error message text to include when the error is thrown.
        if (typeof callbackParameter != "function") {
            throw new Error(message);
        }
    },

    retrieveMultipleRecordsSync: function (type, options, successCallback, errorCallback, OnComplete) {

        if (type.slice(-1) != "s") {
            type = type + "s";
        }

        var optionsString;
        if (options != null) {
            if (options.charAt(0) != "?") {
                optionsString = "?" + options;
            } else {
                optionsString = options;
            }
        }
        var req = new XMLHttpRequest();
        req.open("GET", this._WebAPIPath() + type + optionsString, false);
        req.setRequestHeader("Accept", "application/json");
        req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        req.setRequestHeader("OData-MaxVersion", "4.0");
        req.setRequestHeader("OData-Version", "4.0");
        req.setRequestHeader("Prefer", "odata.include-annotations=\"*\"");
        req.onreadystatechange = function () {
            if (this.readyState == 4) {
                req.onreadystatechange = null;
                if (this.status == 200) {
                    var data = JSON.parse(this.response, SDK.WEBAPI._dateReviver)
                    successCallback(data);
                    //OnComplete();
                } else {
                    errorCallback(SDK.WEBAPI._errorHandler(this));
                }
            }
        };
        req.send();
    }
};
