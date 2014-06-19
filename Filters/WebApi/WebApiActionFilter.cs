using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Orchard.Validation;
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

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to intercept all exceptions")]
        Task<HttpResponseMessage> IActionFilter.ExecuteActionFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation) {
            if (actionContext == null) {
                Argument.ThrowIfNull(actionContext, "actionContext");
            }
            if (continuation == null) {
                Argument.ThrowIfNull(continuation, "continuation");
            }

            try {
                OnActionExecuting(actionContext);
            }
            catch (Exception e) {
                return TaskHelpers.FromError<HttpResponseMessage>(e);
            }

            if (actionContext.Response != null) {
                return TaskHelpers.FromResult(actionContext.Response);
            }

            Task<HttpResponseMessage> internalTask = continuation();
            bool calledOnActionExecuted = false;

            return internalTask
                .Then(response => {
                    calledOnActionExecuted = true;
                    Tuple<HttpResponseMessage, Exception> result = CallOnActionExecuted(actionContext, response: response);
                    return result.Item1 != null ? TaskHelpers.FromResult(result.Item1) : TaskHelpers.FromError<HttpResponseMessage>(result.Item2);
                }, cancellationToken)
                .Catch<HttpResponseMessage>(info => {
                    // If we've already called OnActionExecuted, that means this catch is running because
                    // OnActionExecuted threw an exception, so we just want to re-throw the exception rather
                    // that calling OnActionExecuted again. We also need to reset the response to forget about it
                    // since a filter threw an exception.
                    if (calledOnActionExecuted) {
                        actionContext.Response = null;
                        return info.Throw();
                    }

                    Tuple<HttpResponseMessage, Exception> result = CallOnActionExecuted(actionContext, exception: info.Exception);
                    return result.Item1 != null ? info.Handled(result.Item1) : info.Throw(result.Item2);
                }, cancellationToken);
        }

        private Tuple<HttpResponseMessage, Exception> CallOnActionExecuted(HttpActionContext actionContext, HttpResponseMessage response = null, Exception exception = null) {
            Contract.Assert(actionContext != null);
            Contract.Assert(response != null || exception != null);

            HttpActionExecutedContext executedContext = new HttpActionExecutedContext(actionContext, exception) { Response = response };

            OnActionExecuted(executedContext);

            if (executedContext.Response != null) {
                return new Tuple<HttpResponseMessage, Exception>(executedContext.Response, null);
            }
            if (executedContext.Exception != null) {
                return new Tuple<HttpResponseMessage, Exception>(null, executedContext.Exception);
            }

            throw new Exception("");// Error.InvalidOperation(SRResources.ActionFilterAttribute_MustSupplyResponseOrException, GetType().Name);
        }
    }
}