﻿using System;
using System.Globalization;
using System.Web;
using NGM.CasClient.Client.Utils;
using Orchard;
using Orchard.Security;

namespace NGM.CasClient.Client.Extensions {
    public interface IRequestEvaluator : IDependency {
        /// <summary>
        /// Determines whether the request has a CAS ticket in the URL
        /// </summary>
        /// <returns>True if the request URL contains a CAS ticket, otherwise False</returns>
        bool GetRequestHasCasTicket(WorkContext context);

        /// <summary>
        /// Determines whether the request is a return request from the 
        /// CAS server containing a CAS ticket
        /// </summary>
        /// <returns>True if the request URL contains a CAS ticket, otherwise False</returns>
        bool GetRequestIsCasAuthenticationResponse(WorkContext context);

        /// <summary>
        /// Determines whether the request contains the GatewayParameterName defined in 
        /// web.config or the default value 'gatewayResponse'
        /// </summary>
        /// <returns>True if the request contains the GatewayParameterName, otherwise False</returns>
        bool GetRequestHasGatewayParameter(WorkContext context);

        /// <summary>
        /// Determines whether the request is an inbound proxy callback verifications 
        /// from the CAS server
        /// </summary>
        /// <returns>True if the request is a proxy callback verificiation, otherwise False</returns>
        bool GetRequestIsProxyResponse(WorkContext context);

        /// <summary>
        /// Determines whether the current request requires a Gateway authentication redirect
        /// </summary>
        /// <returns>True if the request requires Gateway authentication, otherwise False</returns>
        bool GetRequestRequiresGateway(WorkContext context, GatewayStatus status);

        /// <summary>
        /// Determines whether the user's browser refuses to accept session cookies
        /// </summary>
        /// <returns>True if the browser does not allow session cookies, otherwise False</returns>
        bool GetUserDoesNotAllowSessionCookies(WorkContext context, GatewayStatus status);

        /// <summary>
        /// Determines whether the current request is unauthorized
        /// </summary>
        /// <returns>True if the request is unauthorized, otherwise False</returns>
        bool GetRequestIsUnauthorized(WorkContext context);

        /// <summary>
        /// Determines whether the current request is unauthenticated
        /// </summary>
        /// <returns>True if the request is unauthenticated, otherwise False</returns>
        bool GetRequestIsUnAuthenticated(WorkContext context);

        /// <summary>
        /// Determines whether the current request will be redirected to the 
        /// CAS login page
        /// </summary>
        /// <returns>True if the request will be redirected, otherwise False.</returns>
        bool GetResponseIsCasLoginRedirect(WorkContext context);

        /// <summary>
        /// Determines whether the request is a CAS Single Sign Out request
        /// </summary>
        /// <returns>True if the request is a CAS Single Sign Out request, otherwise False</returns>
        bool GetRequestIsCasSingleSignOut(WorkContext context);

        bool GetRequestIsAppropriateForCasAuthentication(WorkContext context);
    }

    public class RequestEvaluator : IRequestEvaluator {
        // The requested content types that are appropriate for 
        // authentication-related redirections
        private static readonly string[] AppropriateContentTypes = {
            "text/plain",
            "text/html"
        };

        // The built in ASP.NET handler files that are inappropriate
        // for authentication-related redirections.
        private static readonly string[] BuiltInHandlers = {
            "trace.axd",
            "webresource.axd"        
        };

        private readonly IUrlUtil _urlUtil;
        private readonly IAuthenticationService _authenticationService;
        private readonly ICasServices _casServices;

        public RequestEvaluator(
            IUrlUtil urlUtil,
            IAuthenticationService authenticationService,
            ICasServices casServices) {
            _urlUtil = urlUtil;
            _authenticationService = authenticationService;
            _casServices = casServices;
        }

        /// <summary>
        /// Determines whether the request has a CAS ticket in the URL
        /// </summary>
        /// <returns>True if the request URL contains a CAS ticket, otherwise False</returns>
        public bool GetRequestHasCasTicket(WorkContext context) {
            HttpRequestBase request = context.HttpContext.Request;

            bool result =
            (
                request[_casServices.Settings.ArtifactParameterName] != null &&
                !String.IsNullOrEmpty(request[_casServices.Settings.ArtifactParameterName])
            );

            return result;
        }

        /// <summary>
        /// Determines whether the request is a return request from the 
        /// CAS server containing a CAS ticket
        /// </summary>
        /// <returns>True if the request URL contains a CAS ticket, otherwise False</returns>
        public bool GetRequestIsCasAuthenticationResponse(WorkContext context) {
            HttpRequestBase request = context.HttpContext.Request;

            int artifactIndex = request.Url.AbsoluteUri.IndexOf(_casServices.Settings.ArtifactParameterName);

            bool result =
            (
                GetRequestHasCasTicket(context) &&
                artifactIndex > 0 &&
                (
                    request.Url.AbsoluteUri[artifactIndex - 1] == '?' ||
                    request.Url.AbsoluteUri[artifactIndex - 1] == '&'
                )
            );

            return result;
        }

        /// <summary>
        /// Determines whether the request contains the GatewayParameterName defined in 
        /// web.config or the default value 'gatewayResponse'
        /// </summary>
        /// <returns>True if the request contains the GatewayParameterName, otherwise False</returns>
        public bool GetRequestHasGatewayParameter(WorkContext context) {
            HttpRequestBase request = context.HttpContext.Request;

            bool requestContainsGatewayParameter = !String.IsNullOrEmpty(request.QueryString[_casServices.Settings.GatewayParameterName]);
            bool gatewayParameterValueIsTrue = (request.QueryString[_casServices.Settings.GatewayParameterName] == "true");

            bool result =
            (
               requestContainsGatewayParameter &&
               gatewayParameterValueIsTrue
            );

            return result;
        }

        /// <summary>
        /// Determines whether the request is an inbound proxy callback verifications 
        /// from the CAS server
        /// </summary>
        /// <returns>True if the request is a proxy callback verificiation, otherwise False</returns>
        public bool GetRequestIsProxyResponse(WorkContext context) {
            HttpRequestBase request = context.HttpContext.Request;

            bool requestContainsProxyCallbackParameter = !String.IsNullOrEmpty(request.QueryString[_casServices.Settings.ProxyCallbackParameterName]);
            bool proxyCallbackParameterValueIsTrue = (request.QueryString[_casServices.Settings.ProxyCallbackParameterName] == "true");

            bool result =
            (
               requestContainsProxyCallbackParameter &&
               proxyCallbackParameterValueIsTrue
            );

            return result;
        }

        /// <summary>
        /// Determines whether the current request requires a Gateway authentication redirect
        /// </summary>
        /// <returns>True if the request requires Gateway authentication, otherwise False</returns>
        public bool GetRequestRequiresGateway(WorkContext context, GatewayStatus status) {
            HttpRequestBase request = context.HttpContext.Request;

            bool gatewayEnabled = _casServices.Settings.Gateway;
            bool gatewayWasNotAttempted = (status == GatewayStatus.NotAttempted);
            bool requestDoesNotHaveGatewayParameter = !GetRequestHasGatewayParameter(context);
            bool cookiesRequiredUrlIsDefined = !string.IsNullOrEmpty(_casServices.Settings.CookiesRequiredUrl);
            bool requestIsNotCookiesRequiredUrl = !GetRequestIsCookiesRequiredUrl(context);
            bool notAuthorizedUrlIsDefined = !String.IsNullOrEmpty(_casServices.Settings.NotAuthorizedUrl);
            bool requestIsNotAuthorizedUrl = notAuthorizedUrlIsDefined && request.RawUrl.StartsWith(_urlUtil.ResolveUrl(_casServices.Settings.NotAuthorizedUrl), true, CultureInfo.InvariantCulture);

            bool result =
            (
                gatewayEnabled &&
                gatewayWasNotAttempted &&
                requestDoesNotHaveGatewayParameter &&
                cookiesRequiredUrlIsDefined &&
                requestIsNotCookiesRequiredUrl &&
                !requestIsNotAuthorizedUrl
            );

            return result;
        }

        /// <summary>
        /// Determines whether the user's browser refuses to accept session cookies
        /// </summary>
        /// <returns>True if the browser does not allow session cookies, otherwise False</returns>
        public bool GetUserDoesNotAllowSessionCookies(WorkContext context, GatewayStatus status) {
            // If the request has a gateway parameter but the cookie does not
            // reflect the fact that gateway was attempted, then cookies must
            // be disabled.
            bool gatewayEnabled = _casServices.Settings.Gateway;
            bool gatewayWasNotAttempted = (status == GatewayStatus.NotAttempted);
            bool requestHasGatewayParameter = GetRequestHasGatewayParameter(context);
            bool cookiesRequiredUrlIsDefined = !string.IsNullOrEmpty(_casServices.Settings.CookiesRequiredUrl);
            bool requestIsNotCookiesRequiredUrl = cookiesRequiredUrlIsDefined && !GetRequestIsCookiesRequiredUrl(context);

            bool result =
            (
                gatewayEnabled &&
                gatewayWasNotAttempted &&
                requestHasGatewayParameter &&
                requestIsNotCookiesRequiredUrl
            );

            return result;
        }

        /// <summary>
        /// Determines whether the current request is unauthorized
        /// </summary>
        /// <returns>True if the request is unauthorized, otherwise False</returns>
        public bool GetRequestIsUnauthorized(WorkContext context) {
            HttpResponseBase response = context.HttpContext.Response;

            bool responseIsBeingRedirected = (response.StatusCode == 302);
            bool userIsAuthenticated = GetUserIsAuthenticated();
            bool responseIsCasLoginRedirect = GetResponseIsCasLoginRedirect(context);

            bool result =
            (
               responseIsBeingRedirected &&
               userIsAuthenticated &&
               responseIsCasLoginRedirect
            );

            return result;
        }

        /// <summary>
        /// Determines whether the current request is unauthenticated
        /// </summary>
        /// <returns>True if the request is unauthenticated, otherwise False</returns>
        public bool GetRequestIsUnAuthenticated(WorkContext context) {
            bool userIsNotAuthenticated = !GetUserIsAuthenticated();
            bool responseIsCasLoginRedirect = GetResponseIsCasLoginRedirect(context);

            bool result =
            (
                userIsNotAuthenticated &&
                responseIsCasLoginRedirect
            );

            return result;
        }

        /// <summary>
        /// Determines whether the current request will be redirected to the 
        /// CAS login page
        /// </summary>
        /// <returns>True if the request will be redirected, otherwise False.</returns>
        public bool GetResponseIsCasLoginRedirect(WorkContext context) {
            HttpResponseBase response = context.HttpContext.Response;

            bool requestDoesNotHaveCasTicket = !GetRequestHasCasTicket(context);
            bool responseIsBeingRedirected = (response.StatusCode == 302);
            bool responseRedirectsToFormsLoginUrl = !String.IsNullOrEmpty(response.RedirectLocation) && response.RedirectLocation.StartsWith(_casServices.Settings.FormsLoginUrl);

            bool result =
            (
               requestDoesNotHaveCasTicket &&
               responseIsBeingRedirected &&
               responseRedirectsToFormsLoginUrl
            );

            return result;
        }

        /// <summary>
        /// Determines whether the request is a CAS Single Sign Out request
        /// </summary>
        /// <returns>True if the request is a CAS Single Sign Out request, otherwise False</returns>
        public bool GetRequestIsCasSingleSignOut(WorkContext context) {
            HttpRequestBase request = context.HttpContext.Request;

            bool requestIsFormPost = (request.RequestType == "POST");
            bool haveLogoutRequest = !string.IsNullOrEmpty(request.Params["logoutRequest"]);

            bool result =
            (
                requestIsFormPost &&
                haveLogoutRequest
            );

            return result;
        }

        /// <summary>
        /// Determines whether the User associated with the request has been 
        /// defined and is authenticated.
        /// </summary>
        /// <returns>True if the request has an authenticated User, otherwise False</returns>
        private bool GetUserIsAuthenticated() {
            return _authenticationService.GetAuthenticatedUser() != null;
        }

        /// <summary>
        /// Determines whether the request is for the CookiesRequiredUrl defined in web.config
        /// </summary>
        /// <returns>True if the request is to the CookiesRequiredUrl, otherwise False</returns>
        private bool GetRequestIsCookiesRequiredUrl(WorkContext context) {
            HttpRequestBase request = context.HttpContext.Request;

            bool cookiesRequiredUrlIsDefined = !String.IsNullOrEmpty(_casServices.Settings.CookiesRequiredUrl);
            bool requestIsCookiesRequiredUrl = cookiesRequiredUrlIsDefined && request.RawUrl.StartsWith(_urlUtil.ResolveUrl(_casServices.Settings.CookiesRequiredUrl), true, CultureInfo.InvariantCulture);

            bool result =
            (
                requestIsCookiesRequiredUrl
            );

            return result;
        }

        /// <summary>
        /// Determines whether the request is appropriate for CAS authentication.
        /// Generally, this is true for most requests except those for images,
        /// style sheets, javascript files and anything generated by the built-in
        /// ASP.NET handlers (i.e., web resources, trace handler).
        /// </summary>
        /// <returns>True if the request is appropriate for CAS authentication, otherwise False</returns>
        public bool GetRequestIsAppropriateForCasAuthentication(WorkContext workContext) {
            HttpRequestBase request = workContext.HttpContext.Request;
            HttpResponseBase response = workContext.HttpContext.Response;

            string contentType = response.ContentType.ToLowerInvariant();
            string fileName = request.Url.Segments[request.Url.Segments.Length - 1];

            bool contentTypeIsEligible = false;
            bool fileNameIsEligible = true;

            foreach (string appropriateContentType in AppropriateContentTypes) {
                if (string.Compare(contentType, appropriateContentType, true, CultureInfo.InvariantCulture) == 0) {
                    contentTypeIsEligible = true;
                    break;
                }
            }

            foreach (string builtInHandler in BuiltInHandlers) {
                if (string.Compare(fileName, builtInHandler, true, CultureInfo.InvariantCulture) == 0) {
                    fileNameIsEligible = false;
                    break;
                }
            }

            // TODO: Should this be || instead of && ?
            return (contentTypeIsEligible && fileNameIsEligible);
        }        
    }
}