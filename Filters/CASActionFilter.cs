using System;
using System.Web;
using System.Web.Mvc;
using JetBrains.Annotations;
using NGM.CasClient.Client;
using NGM.CasClient.Client.Extensions;
using NGM.CasClient.Client.State;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Mvc.Filters;

namespace NGM.CasClient.Filters {
    [UsedImplicitly]
    public class CASActionFilter : FilterProvider, IActionFilter {
        private readonly IServiceTicketManagerWrapper _serviceTicketManagerWrapper;
        private readonly IProxyTicketManagerWrapper _proxyTicketManagerWrapper;
        private readonly ICASClient _casClient;
        private readonly IRequestEvaluator _requestEvaluator;
        private readonly ICasServices _casServices;

        public CASActionFilter(IServiceTicketManagerWrapper serviceTicketManagerWrapper,
            IProxyTicketManagerWrapper proxyTicketManagerWrapper,
            ICASClient casClient,
            IRequestEvaluator requestEvaluator,
            ICasServices casServices) {
            _serviceTicketManagerWrapper = serviceTicketManagerWrapper;
            _proxyTicketManagerWrapper = proxyTicketManagerWrapper;
            _casClient = casClient;
            _requestEvaluator = requestEvaluator;
            _casServices = casServices;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public void OnActionExecuting(ActionExecutingContext filterContext) {
            if (!_casServices.Settings.IsConfigured()) {
                Logger.Debug("CAS is not configured correctly");
                return;
            }

            HttpContextBase context = filterContext.HttpContext;
            HttpRequestBase request = context.Request;

            Logger.Debug("Starting BeginRequest for " + request.RawUrl);

            _serviceTicketManagerWrapper.RemoveExpiredTickets();
            _proxyTicketManagerWrapper.RemoveExpiredMappings();

            ActionResult redirectRequest = null;

            if (_casServices.Settings.ProcessIncomingSingleSignOutRequests && _requestEvaluator.GetRequestIsCasSingleSignOut(context)) {
                Logger.Information("Processing inbound Single Sign Out request.");
                redirectRequest = _casClient.ProcessSingleSignOutRequest(context);
            }
            else if (_requestEvaluator.GetRequestIsProxyResponse(context)) {
                Logger.Information("Processing Proxy Callback request");
                redirectRequest = _casClient.ProcessProxyCallbackRequest(context);
            }

            if (redirectRequest != null)
                filterContext.Result = redirectRequest;

            Logger.Debug("Ending BeginRequest for " + request.RawUrl);
        }

        public void OnActionExecuted(ActionExecutedContext filterContext) {
            if (!_casServices.Settings.IsConfigured()) {
                Logger.Debug("CAS is not configured correctly");
                return;
            }

            HttpContextBase context = filterContext.HttpContext;
            HttpRequestBase request = context.Request;

            if (!_requestEvaluator.GetRequestIsAppropriateForCasAuthentication(context)) {
                Logger.Debug("No EndRequest processing for " + request.RawUrl);
                return;
            }

            Logger.Debug("Starting EndRequest for " + request.RawUrl);

            ActionResult redirectRequest = null;

            if (_requestEvaluator.GetRequestRequiresGateway(context, _casClient.GetGatewayStatus(context))) {
                Logger.Information("  Performing Gateway Authentication");
                redirectRequest = _casClient.GatewayAuthenticate(context, true);
            }
            else if (_requestEvaluator.GetUserDoesNotAllowSessionCookies(context, _casClient.GetGatewayStatus(context))) {
                Logger.Information("  Cookies not supported.  Redirecting to Cookies Required Page");
                redirectRequest = _casClient.RedirectToCookiesRequiredPage();
            }
            else if (_requestEvaluator.GetRequestHasCasTicket(context)) {
                Logger.Information("  Redirecting from login callback");
                //redirectRequest = _casClient.RedirectFromLoginCallback(context, filterContext.Result);
            }
            else if (_requestEvaluator.GetRequestHasGatewayParameter(context)) {
                Logger.Information("  Redirecting from failed gateway callback");
                redirectRequest = _casClient.RedirectFromFailedGatewayCallback(context);
            }
            else if (_requestEvaluator.GetRequestIsUnauthorized(context) &&
                     !String.IsNullOrEmpty(_casServices.Settings.NotAuthorizedUrl)) {

                Logger.Information("  Redirecting to Unauthorized Page");
                redirectRequest = _casClient.RedirectToNotAuthorizedPage();
            }
            else if (_requestEvaluator.GetRequestIsUnauthorized(context)) {
                Logger.Information("  Redirecting to CAS Login Page (Unauthorized without NotAuthorizedUrl defined)");
                redirectRequest = _casClient.RedirectToLoginPage(true);
            }
            else if (_requestEvaluator.GetRequestIsUnAuthenticated(context)) {
                Logger.Information("  Redirecting to CAS Login Page");
                redirectRequest = _casClient.RedirectToLoginPage();
            }

            if (redirectRequest != null)
                filterContext.Result = redirectRequest;

            Logger.Debug("Ending EndRequest for " + request.RawUrl);
        }
    }
}