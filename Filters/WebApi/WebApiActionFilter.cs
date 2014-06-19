using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Orchard.WebApi.Filters;

namespace NGM.CasClient.Filters.WebApi {
    public abstract class WebApiActionFilter : IApiFilterProvider, IActionFilter {
        public bool AllowMultiple {
            get { return false; }
        }

        public virtual void OnActionExecuting(HttpActionContext actionContext) {
        }

        public virtual void OnActionExecuted(HttpActionExecutedContext actionExecutedContext) {
        }

        public Task<HttpResponseMessage> ExecuteActionFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken,
        Func<Task<HttpResponseMessage>> continuation) {
            OnActionExecuting(actionContext);

            if (actionContext.Response != null) {
                return Task.Factory.StartNew(() => { return actionContext.Response; });
            }

            HttpActionExecutedContext executedContext;

            try {
                var response = continuation();
                executedContext = new HttpActionExecutedContext(actionContext, null) {
                    Response = response.Result
                };
            }
            catch (Exception exception) {
                executedContext = new HttpActionExecutedContext(actionContext, exception);
            }

            OnActionExecuted(executedContext);
            return Task.Factory.StartNew(() => { return executedContext.Response; });
        }
    }
}