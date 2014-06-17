using System;
using System.Web.Mvc;
using NGM.CasClient.Client;
using NGM.CasClient.Client.Extensions;
using NGM.CasClient.Client.State;
using Orchard;
using Orchard.Localization;
using Orchard.Logging;

namespace NGM.CasClient.Filters {
    public interface ICASActionFilter : IDependency {
        ActionResult OnActionExecuting(WorkContext workContext);
        ActionResult OnActionExecuted(WorkContext workContext);
    }

    public class CasActionFilter : ICASActionFilter {
                private readonly IServiceTicketManagerWrapper _serviceTicketManagerWrapper;
        private readonly IProxyTicketManagerWrapper _proxyTicketManagerWrapper;
        private readonly ICASClient _casClient;
        private readonly IRequestEvaluator _requestEvaluator;
        private readonly ICasServices _casServices;

        public CasActionFilter(IServiceTicketManagerWrapper serviceTicketManagerWrapper,
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

        public ActionResult OnActionExecuting(WorkContext workContext) {
            _serviceTicketManagerWrapper.RemoveExpiredTickets();
            _proxyTicketManagerWrapper.RemoveExpiredMappings();

            if (_casServices.Settings.ProcessIncomingSingleSignOutRequests && _requestEvaluator.GetRequestIsCasSingleSignOut(workContext)) {
                Logger.Information("Processing inbound Single Sign Out request.");
                return _casClient.ProcessSingleSignOutRequest(workContext);
            }
            if (_requestEvaluator.GetRequestIsProxyResponse(workContext)) {
                Logger.Information("Processing Proxy Callback request");
                return _casClient.ProcessProxyCallbackRequest(workContext);
            }
            return null;
        }

        public ActionResult OnActionExecuted(WorkContext workContext) {
            if (_requestEvaluator.GetRequestRequiresGateway(workContext, _casClient.GetGatewayStatus(workContext))) {
                Logger.Information("  Performing Gateway Authentication");
                return _casClient.GatewayAuthenticate(workContext, true);
            }
            if (_requestEvaluator.GetUserDoesNotAllowSessionCookies(workContext, _casClient.GetGatewayStatus(workContext))) {
                Logger.Information("  Cookies not supported.  Redirecting to Cookies Required Page");
                return _casClient.RedirectToCookiesRequiredPage();
            }
            if (_requestEvaluator.GetRequestHasCasTicket(workContext)) {
                Logger.Information("  Redirecting from login callback");
                //redirectRequest = _casClient.RedirectFromLoginCallback(context, filterContext.Result);
            }
            else if (_requestEvaluator.GetRequestHasGatewayParameter(workContext)) {
                Logger.Information("  Redirecting from failed gateway callback");
                return _casClient.RedirectFromFailedGatewayCallback(workContext);
            }
            else if (_requestEvaluator.GetRequestIsUnauthorized(workContext) &&
                     !String.IsNullOrEmpty(_casServices.Settings.NotAuthorizedUrl)) {

                Logger.Information("  Redirecting to Unauthorized Page");
                return _casClient.RedirectToNotAuthorizedPage();
            }
            else if (_requestEvaluator.GetRequestIsUnauthorized(workContext)) {
                Logger.Information("  Redirecting to CAS Login Page (Unauthorized without NotAuthorizedUrl defined)");
                return _casClient.RedirectToLoginPage(true);
            }
            else if (_requestEvaluator.GetRequestIsUnAuthenticated(workContext)) {
                Logger.Information("  Redirecting to CAS Login Page");
                return _casClient.RedirectToLoginPage();
            }

            return null;
        }
    }
}