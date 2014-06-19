using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Mvc;
using JetBrains.Annotations;
using NGM.CasClient.Client;
using NGM.CasClient.Client.Extensions;
using Orchard;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Mvc.Filters;
using Orchard.WebApi.Filters;

namespace NGM.CasClient.Filters {
    [UsedImplicitly]
    public class CasAuthorizationFilter : FilterProvider, IAuthorizationFilter, IHttpAuthorizationFilter {
        private readonly ICASClient _casClient;
        private readonly ICasServices _casServices;
        private readonly IRequestEvaluator _requestEvaluator;

        public CasAuthorizationFilter(
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

            var workContext = filterContext.RequestContext.GetWorkContext();

            ProcessAuthorization(workContext);
        }

        public bool AllowMultiple {
            get { return false; }
        }

        public Task<HttpResponseMessage> ExecuteAuthorizationFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken,
            Func<Task<HttpResponseMessage>> continuation) {

                var workContext = actionContext.ControllerContext.GetWorkContext();

                ProcessAuthorization(workContext);

                return continuation();
        }

        private void ProcessAuthorization(WorkContext workContext) {
            if (!_requestEvaluator.GetRequestIsAppropriateForCasAuthentication(workContext)) {
                Logger.Debug("No EndRequest processing for " + workContext.HttpContext.Request.RawUrl);
                return;
            }

            if (_requestEvaluator.GetRequestHasCasTicket(workContext)) {
                Logger.Information("Processing Proxy Callback request");
                _casClient.ProcessTicketValidation(workContext);
            }

            Logger.Debug("Starting AuthenticateRequest for " + workContext.HttpContext.Request.RawUrl);
            _casClient.ProcessRequestAuthentication(workContext);
            Logger.Debug("Ending AuthenticateRequest for " + workContext.HttpContext.Request.RawUrl);
        }
    }
}