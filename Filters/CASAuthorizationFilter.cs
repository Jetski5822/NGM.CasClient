using System.Web;
using System.Web.Mvc;
using JetBrains.Annotations;
using NGM.CasClient.Client;
using NGM.CasClient.Client.Extensions;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Mvc.Filters;

namespace NGM.CasClient.Filters {
    [UsedImplicitly]
    public class CASAuthorizationFilter : FilterProvider, IAuthorizationFilter {
        private readonly ICASClient _casClient;
        private readonly ICasServices _casServices;
        private readonly IRequestEvaluator _requestEvaluator;

        public CASAuthorizationFilter(
            ICASClient casClient,
            ICasServices casServices,
            IRequestEvaluator requestEvaluator) {
            _casClient = casClient;
            _casServices = casServices;
            _requestEvaluator = requestEvaluator;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public void OnAuthorization(AuthorizationContext filterContext) {
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

            if (_requestEvaluator.GetRequestHasCasTicket(context)) {
                Logger.Information("Processing Proxy Callback request");
                _casClient.ProcessTicketValidation(context);
            }

            Logger.Debug("Starting AuthenticateRequest for " + request.RawUrl);
            _casClient.ProcessRequestAuthentication(context);
            Logger.Debug("Ending AuthenticateRequest for " + request.RawUrl);
        }
    }
}