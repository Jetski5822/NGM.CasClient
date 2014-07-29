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
    public class CasMvcActionFilter : FilterProvider, IActionFilter {
        private readonly IRequestEvaluator _requestEvaluator;
        private readonly ICasServices _casServices;
        private readonly ICASActionFilter _casActionFilter;

        public CasMvcActionFilter(
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

        public void OnActionExecuting(ActionExecutingContext filterContext) {
            if (!_casServices.Settings.IsConfigured()) {
                Logger.Debug("CAS is not configured correctly");
                return;
            }
            
            var workContext = filterContext.RequestContext.GetWorkContext();
            var httpContext = workContext.HttpContext;

            Logger.Debug("Starting BeginRequest for {0}", httpContext.Request.RawUrl);

            ActionResult redirectRequest = _casActionFilter.OnActionExecuting(workContext);

            if (redirectRequest != null)
                filterContext.Result = redirectRequest;

            Logger.Debug("Ending BeginRequest for {0}", httpContext.Request.RawUrl);
        }

        public void OnActionExecuted(ActionExecutedContext filterContext) {
            if (!_casServices.Settings.IsConfigured()) {
                Logger.Debug("CAS is not configured correctly");
                return;
            }

            var workContext = filterContext.RequestContext.GetWorkContext();
            var httpContext = workContext.HttpContext;

            if (!_requestEvaluator.GetRequestIsAppropriateForCasAuthentication(httpContext)) {
                Logger.Debug("No EndRequest processing for {0}", httpContext.Request.RawUrl);
                return;
            }

            Logger.Debug("Starting EndRequest for {0}", httpContext.Request.RawUrl);

            ActionResult redirectRequest = _casActionFilter.OnActionExecuted(workContext);

            if (redirectRequest != null)
                filterContext.Result = redirectRequest;

            Logger.Debug("Ending EndRequest for {0}", httpContext.Request.RawUrl);
        }
    }
}