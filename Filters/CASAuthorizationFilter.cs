using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Mvc;
using JetBrains.Annotations;
using NGM.CasClient.Client;
using NGM.CasClient.Client.Extensions;
using Orchard;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Mvc.Filters;

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

            ProcessAuthorization(workContext.HttpContext);
        }

        public bool AllowMultiple {
            get { return false; }
        }

        public Task<HttpResponseMessage> ExecuteAuthorizationFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken,
            Func<Task<HttpResponseMessage>> continuation) {

                var workContext = actionContext.ControllerContext.GetWorkContext();

                ProcessAuthorization(workContext.HttpContext);

                return continuation();
        }

        private void ProcessAuthorization(HttpContextBase httpContext) {
            if (!_requestEvaluator.GetRequestIsAppropriateForCasAuthentication(httpContext)) {
                Logger.Debug("No EndRequest processing for {0}", httpContext.Request.RawUrl);
                return;
            }

            if (_requestEvaluator.GetRequestHasCasTicket(httpContext)) {
                Logger.Information("Processing Proxy Callback request");
                _casClient.ProcessTicketValidation(httpContext);
            }

            Logger.Debug("Starting AuthenticateRequest for {0}", httpContext.Request.RawUrl);
            _casClient.ProcessRequestAuthentication(httpContext);
            Logger.Debug("Ending AuthenticateRequest for {0}", httpContext.Request.RawUrl);
        }
    }
}