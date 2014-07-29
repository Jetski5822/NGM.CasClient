using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using NGM.CasClient.Client;
using NGM.CasClient.Client.Extensions;
using NGM.CasClient.Filters.WebApi;
using Orchard;
using Orchard.Localization;
using Orchard.Logging;

namespace NGM.CasClient.Filters {
    public class CasWebApiActionFilter : WebApiActionFilter {
        private readonly IRequestEvaluator _requestEvaluator;
        private readonly ICasServices _casServices;
        private readonly ICASActionFilter _casActionFilter;

        public CasWebApiActionFilter(
            IRequestEvaluator requestEvaluator,
            ICasServices casServices,
            ICASActionFilter casActionFilter) {
            _requestEvaluator = requestEvaluator;
            _casServices = casServices;
            _casActionFilter = casActionFilter;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public override void OnActionExecuting(HttpActionContext actionContext) {
            if (!_casServices.Settings.IsConfigured()) {
                Logger.Debug("CAS is not configured correctly");
                return;
            }

            var workContext = actionContext.ControllerContext.GetWorkContext();
            var httpContext = workContext.HttpContext;

            Logger.Debug("Starting BeginRequest for {0}", httpContext.Request.RawUrl);
            
            _casActionFilter.OnActionExecuting(workContext);

            Logger.Debug("Ending BeginRequest for {0}", httpContext.Request.RawUrl);
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext) {
            if (!_casServices.Settings.IsConfigured()) {
                Logger.Debug("CAS is not configured correctly");
                return;
            }
            
            var workContext = actionExecutedContext.ActionContext.ControllerContext.GetWorkContext();
            var httpContext = workContext.HttpContext;

            if (!_requestEvaluator.GetRequestIsAppropriateForCasAuthentication(httpContext)) {
                Logger.Debug("No EndRequest processing for {0}", httpContext.Request.RawUrl);
                return;
            }

            Logger.Debug("Starting EndRequest for {0}", httpContext.Request.RawUrl);

            _casActionFilter.OnActionExecuted(workContext);

            Logger.Debug("Ending EndRequest for {0}", httpContext.Request.RawUrl);
        }
    }
}