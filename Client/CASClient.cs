using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Xml;
using NGM.CasClient.Client.Configuration;
using NGM.CasClient.Client.Extensions;
using NGM.CasClient.Client.Factories;
using NGM.CasClient.Client.Security;
using NGM.CasClient.Client.Utils;
using NGM.CasClient.Client.Validation;
using NGM.CasClient.Client.Validation.Schema.Cas20;
using NGM.CasClient.Client.Validation.TicketValidator;
using Orchard.Environment.Configuration;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Security;
using Orchard.Services;
using Orchard.Validation;

namespace NGM.CasClient.Client {
    public class CASClient : ICASClient {
        #region Constants
        private const string XML_SESSION_INDEX_ELEMENT_NAME = "samlp:SessionIndex";
        private const string PARAM_PROXY_GRANTING_TICKET_IOU = "pgtIou";
        private const string PARAM_PROXY_GRANTING_TICKET = "pgtId";
        #endregion

        // XML Reader Settings for SAML parsing.
        private static readonly XmlReaderSettings XmlReaderSettings = new XmlReaderSettings {
            ConformanceLevel = ConformanceLevel.Auto,
            IgnoreWhitespace = true
        };

        // XML Name Table for namespace resolution in SSO SAML Parsing routine
        private readonly NameTable _xmlNameTable = new NameTable();

        /// XML Namespace Manager for namespace resolution in SSO SAML Parsing routine
        private readonly XmlNamespaceManager _xmlNamespaceManager;

        private readonly ShellSettings _settings;
        private readonly ITicketValidatorFactory _ticketValidatorFactory;
        private readonly IRequestEvaluator _requestEvaluator;
        private readonly IClock _clock;
        private readonly IUrlUtil _urlUtil;
        private readonly IAuthenticationService _authenticationService;
        private readonly ICasServices _casServices;

        public CASClient(
            ShellSettings settings, 
            ITicketValidatorFactory ticketValidatorFactory,
            IRequestEvaluator requestEvaluator,
            IClock clock,
            IUrlUtil urlUtil,
            IAuthenticationService authenticationService,
            ICasServices casServices) {
            _settings = settings;
            _ticketValidatorFactory = ticketValidatorFactory;
            _requestEvaluator = requestEvaluator;
            _clock = clock;
            _urlUtil = urlUtil;
            _authenticationService = authenticationService;
            _casServices = casServices;

            _xmlNamespaceManager = new XmlNamespaceManager(_xmlNameTable);
            _xmlNamespaceManager.AddNamespace("cas", "http://www.yale.edu/tp/cas");
            _xmlNamespaceManager.AddNamespace("saml", "urn: oasis:names:tc:SAML:1.0:assertion");
            _xmlNamespaceManager.AddNamespace("saml2", "urn: oasis:names:tc:SAML:1.0:assertion");
            _xmlNamespaceManager.AddNamespace("samlp", "urn: oasis:names:tc:SAML:1.0:protocol");

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        /// <summary>
        /// Obtain a Proxy ticket and redirect to the foreign service url with 
        /// that ticket included in the url.  The foreign service must be configured 
        /// to accept the ticket.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="url">The foreign service to redirect to</param>
        /// <exception cref="ArgumentNullException">The url supplied is null</exception>
        /// <exception cref="ArgumentException">The url supplied is empty</exception>
        public void ProxyRedirect(HttpContextBase httpContext, string url) {
            ProxyRedirect(httpContext, url, "ticket", false);
        }

        /// <summary>
        /// Obtain a Proxy ticket and redirect to the foreign service url with 
        /// that ticket included in the url.  The foreign service must be configured 
        /// to accept the ticket.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="url">The foreign service to redirect to</param>
        /// <param name="endResponse">
        ///     Boolean indicating whether or not to short circuit the remaining request 
        ///     pipeline events
        /// </param>
        /// <exception cref="ArgumentNullException">The url supplied is null</exception>
        /// <exception cref="ArgumentException">The url supplied is empty</exception>
        public void ProxyRedirect(HttpContextBase httpContext, string url, bool endResponse) {
            ProxyRedirect(httpContext, url, "ticket", endResponse);
        }

        /// <summary>
        /// Obtain a Proxy ticket and redirect to the foreign service url with 
        /// that ticket included in the url.  The foreign service must be configured 
        /// to accept the ticket.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="url">The foreign service to redirect to</param>
        /// <param name="proxyTicketUrlParameter">
        ///     The ticket parameter to include in the remote service Url.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The url or proxyTicketUrlParameter supplied is null
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The url or proxyTicketUrlParametersupplied is empty
        /// </exception>
        public void ProxyRedirect(HttpContextBase httpContext, string url, string proxyTicketUrlParameter) {
            ProxyRedirect(httpContext, url, proxyTicketUrlParameter, false);
        }

        /// <summary>
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="url">The foreign service to redirect to</param>
        /// <param name="proxyTicketUrlParameter">
        ///     The ticket parameter to include in the remote service Url.
        /// </param>
        /// <param name="endResponse">
        ///     Boolean indicating whether or not to short circuit the remaining request 
        ///     pipeline events
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The url or proxyTicketUrlParameter supplied is null
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The url or proxyTicketUrlParametersupplied is empty
        /// </exception>
        public void ProxyRedirect(HttpContextBase httpContext, string url, string proxyTicketUrlParameter, bool endResponse) {
            Argument.ThrowIfNullOrEmpty(url, "url", "url parameter cannot be null or empty.");
            Argument.ThrowIfNull(proxyTicketUrlParameter, "proxyTicketUrlParameter", "proxyTicketUrlParameter parameter cannot be null or empty.");

            HttpResponseBase response = httpContext.Response;

            string proxyRedirectUrl = _urlUtil.GetProxyRedirectUrl(url, proxyTicketUrlParameter, (resolvedUrl) => {
                return GetProxyTicketIdFor(httpContext, resolvedUrl);
            });

            response.Redirect(proxyRedirectUrl, endResponse);
        }

        /// <summary>
        /// Attempts to connect to the CAS server to retrieve a proxy ticket 
        /// for the target URL specified.
        /// </summary>
        /// <remarks>
        /// Problems retrieving proxy tickets are generally caused by SSL misconfiguration.
        /// The CAS server must be configured to trust the SSL certificate on the web application's 
        /// server.  The CAS server will attempt to establish an SSL connection to this web 
        /// application server to confirm that the proxy ticket request is legitimate.  If the 
        /// server does not trust the SSL certificate or the certificate authority/chain of the SSL
        /// certificate, the request will fail.
        /// </remarks>
        /// <param name="httpContext"></param>
        /// <param name="targetServiceUrl">The target Url to obtain a proxy ticket for</param>
        /// <returns>
        /// A proxy ticket for the target Url or an empty string if the request failed.
        /// </returns>
        public string GetProxyTicketIdFor(HttpContextBase httpContext, string targetServiceUrl) {
            Argument.ThrowIfNullOrEmpty(targetServiceUrl, "targetServiceUrl", "targetServiceUrl parameter cannot be null or empty.");

            if (_casServices.ServiceTicketManager == null) {
                LogAndThrowConfigurationException("Proxy authentication requires a ServiceTicketManager");
            }

            FormsAuthenticationTicket formsAuthTicket = GetFormsAuthenticationTicket(httpContext);

            if (formsAuthTicket == null) {
                LogAndThrowOperationException("The request is not authenticated (does not have a CAS Service or Proxy ticket).");
            }

            if (string.IsNullOrEmpty(formsAuthTicket.UserData)) {
                LogAndThrowOperationException("The request does not have a CAS Service Ticket.");
            }

            CasAuthenticationTicket casTicket = _casServices.ServiceTicketManager.GetTicket(formsAuthTicket.UserData);

            if (casTicket == null) {
                LogAndThrowOperationException("The request does not have a valid CAS Service Ticket.");
            }

            string proxyTicketResponse = null;
            try {
                string proxyUrl = _urlUtil.ConstructProxyTicketRequestUrl(casTicket.ProxyGrantingTicket, targetServiceUrl);
                proxyTicketResponse = HttpUtil.PerformHttpGet(proxyUrl, true);
            }
            catch {
                LogAndThrowOperationException("Unable to obtain CAS Proxy Ticket.");
            }

            if (String.IsNullOrEmpty(proxyTicketResponse)) {
                LogAndThrowOperationException("Unable to obtain CAS Proxy Ticket (response was empty)");
            }

            string proxyTicket = null;
            try {
                ServiceResponse serviceResponse = ServiceResponse.ParseResponse(proxyTicketResponse);
                if (serviceResponse.IsProxySuccess) {
                    ProxySuccess success = (ProxySuccess)serviceResponse.Item;
                    if (!String.IsNullOrEmpty(success.ProxyTicket)) {
                        Logger.Information("Proxy success: {0}", success.ProxyTicket);
                    }
                    proxyTicket = success.ProxyTicket;
                }
                else {
                    ProxyFailure failure = (ProxyFailure)serviceResponse.Item;
                    if (!String.IsNullOrEmpty(failure.Message) && !String.IsNullOrEmpty(failure.Code)) {
                        Logger.Information("Proxy failure: {0} ({1})", failure.Message, failure.Code);
                    }
                    else if (!String.IsNullOrEmpty(failure.Message)) {
                        Logger.Information("Proxy failure: {0}", failure.Message);
                    }
                    else if (!String.IsNullOrEmpty(failure.Code)) {
                        Logger.Information("Proxy failure: Code {0}", failure.Code);
                    }
                }
            }
            catch (InvalidOperationException) {
                LogAndThrowOperationException("CAS Server response does not conform to CAS 2.0 schema");
            }
            return proxyTicket;
        }

        /// <summary>
        /// Redirects the current request to the CAS Login page
        /// </summary>
        public RedirectResult RedirectToLoginPage() {
            return RedirectToLoginPage(_casServices.Settings.Renew);
        }

        /// <summary>
        /// Redirects the current request to the Login page and requires renewed
        /// CAS credentials
        /// </summary>
        public RedirectResult RedirectToLoginPage(bool forceRenew) {
            string redirectUrl = _urlUtil.ConstructLoginRedirectUrl(false, forceRenew);
            Logger.Information("Redirecting to {0}", redirectUrl);
            return new RedirectResult(redirectUrl);
        }

        /// <summary>
        /// Redirects the current request to the Cookies Required page
        /// </summary>
        public RedirectResult RedirectToCookiesRequiredPage() {
            return new RedirectResult(_urlUtil.ResolveUrl(_casServices.Settings.CookiesRequiredUrl));
        }

        /// <summary>
        /// Redirects the current request to the Not Authorized page
        /// </summary>
        public RedirectResult RedirectToNotAuthorizedPage() {
            return new RedirectResult(_urlUtil.ResolveUrl(_casServices.Settings.NotAuthorizedUrl));
        }

        /// <summary>
        /// Redirects the current request back to the requested page without
        /// the CAS ticket artifact in the URL.
        /// </summary>
        public RedirectResult RedirectFromLoginCallback(HttpContextBase httpContext, ActionResult result) {
            HttpRequestBase request = httpContext.Request;
            if (_requestEvaluator.GetRequestHasGatewayParameter(httpContext)) {
                // TODO: Only set Success if request is authenticated?  Otherwise Failure.  
                // Doesn't make a difference from a security perspective, but may be clearer for users
                SetGatewayStatusCookie(httpContext, GatewayStatus.Success);
            }

            return new RedirectResult(_urlUtil.RemoveCasArtifactsFromUrl(request.Url.PathAndQuery));
        }

        /// <summary>
        /// Redirects the current request back to the requested page without
        /// the gateway callback artifact in the URL.
        /// </summary>
        public RedirectResult RedirectFromFailedGatewayCallback(HttpContextBase httpContext) {
            HttpRequestBase request = httpContext.Request;
            SetGatewayStatusCookie(httpContext, GatewayStatus.Failed);

            string urlWithoutCasArtifact = _urlUtil.RemoveCasArtifactsFromUrl(request.Url.AbsoluteUri);
            return new RedirectResult(urlWithoutCasArtifact);
        }

        /// <summary>
        /// Attempt to perform a CAS gateway authentication.  This causes a transparent
        /// redirection out to the CAS server and back to the requesting page with or 
        /// without a CAS service ticket.  If the user has already authenticated for 
        /// another service against the CAS server and the CAS server supports Single 
        /// Sign On, this will result in the user being automatically authenticated.
        /// Otherwise, the user will remain anonymous.
        /// </summary>
        /// <param name="ignoreGatewayStatusCookie">
        /// The Gateway Status Cookie reflects whether a gateway authentication has 
        /// already been attempted, in which case the redirection is generally 
        /// unnecessary.  This property allows you to override the behavior and 
        /// perform a redirection regardless of whether it has already been attempted.
        /// </param>
        public RedirectResult GatewayAuthenticate(HttpContextBase httpContext, bool ignoreGatewayStatusCookie) {
            if (!ignoreGatewayStatusCookie) {
                if (GetGatewayStatus(httpContext) != GatewayStatus.NotAttempted) {
                    return null;
                }
            }

            SetGatewayStatusCookie(httpContext, GatewayStatus.Attempting);

            string redirectUrl = _urlUtil.ConstructLoginRedirectUrl(true, false);
            Logger.Information("Performing gateway redirect to {0}", redirectUrl);
            return new RedirectResult(redirectUrl);
        }

        /// <summary>
        /// Logs the user out of the application and attempts to perform a Single Sign 
        /// Out against the CAS server.  If the CAS server is configured to support 
        /// Single Sign Out, this will prevent users from gateway authenticating 
        /// to other services.  The CAS server will attempt to notify any other 
        /// applications to revoke the session.  Each of the applications must be 
        /// configured to maintain session state on the server.  In the case of 
        /// ASP.NET web applications using DotNetCasClient, this requires defining a 
        /// serviceTicketManager.  The configuration for other client types (Java, 
        /// PHP) varies based on the client implementation.  Consult the Jasig wiki
        /// for more details.
        /// </summary>
        public void SingleSignOut(HttpContextBase httpContext) {
            HttpResponseBase response = httpContext.Response;

            // Necessary for ASP.NET MVC Support.
            if (_authenticationService.GetAuthenticatedUser() != null) {
                ClearAuthCookie(httpContext);
                string singleSignOutRedirectUrl = _urlUtil.ConstructSingleSignOutRedirectUrl();

                // Leave endResponse as true.  This will throw a handled ThreadAbortException
                // but it is necessary to support SingleSignOut in ASP.NET MVC applications.
                response.Redirect(singleSignOutRedirectUrl, true);
            }
        }

        /// <summary>
        /// Process SingleSignOut requests originating from another web application by removing the ticket 
        /// from the ServiceTicketManager (assuming one is configured).  Without a ServiceTicketManager
        /// configured, this method will not execute and this web application cannot respect external 
        /// SingleSignOut requests.
        /// </summary>
        /// <returns>
        /// Boolean indicating whether the request was a SingleSignOut request, regardless of
        /// whether or not the request actually required processing (non-existent/already expired).
        /// </returns>
        public ActionResult ProcessSingleSignOutRequest(HttpContextBase httpContext) {
            HttpRequestBase request = httpContext.Request;

            Logger.Debug("Examining request for single sign-out signature");

            if (request.HttpMethod == "POST" && request.Form["logoutRequest"] != null) {
                Logger.Debug("Attempting to get CAS service ticket from request");
                // TODO: Should we be checking to make sure that this special POST is coming from a trusted source?
                //       It would be tricky to do this by IP address because there might be a white list or something.

                string casTicket = ExtractSingleSignOutTicketFromSamlResponse(request.Params["logoutRequest"]);
                if (!String.IsNullOrEmpty(casTicket)) {
                    Logger.Information("Processing single sign-out request for {0}", casTicket);
                    _casServices.ServiceTicketManager.RevokeTicket(casTicket);
                    Logger.Debug("Successfully removed {0}", casTicket);

                    return new HttpStatusCodeResult(HttpStatusCode.OK, "Revoked Ticket");
                }
            }

            return null;
        }

        /// <summary>
        /// Process a Proxy Callback request from the CAS server.  Proxy Callback requests occur as a part
        /// of a proxy ticket request.  When the web application requests a proxy ticket for a third party
        /// service from the CAS server, the CAS server attempts to connect back to the web application 
        /// over an HTTPS connection.  The success of this callback is essential for the proxy ticket 
        /// request to succeed.  Failures are generally caused by SSL configuration errors.  See the 
        /// description of the SingleSignOut method for more details.  Assuming the SSL configuration is 
        /// correct, this method is responsible for handling the callback from the CAS server.  For 
        /// more details, see the CAS protocol specification.
        /// </summary>
        /// <returns>
        /// A Boolean indicating whether or not the proxy callback request is valid and mapped to a valid,
        /// outstanding Proxy Granting Ticket IOU.
        /// </returns>
        public ActionResult ProcessProxyCallbackRequest(HttpContextBase httpContext) {
            HttpRequestBase request = httpContext.Request;

            string proxyGrantingTicketIou = request.Params[PARAM_PROXY_GRANTING_TICKET_IOU];
            string proxyGrantingTicket = request.Params[PARAM_PROXY_GRANTING_TICKET];
            if (String.IsNullOrEmpty(proxyGrantingTicket)) {
                Logger.Information("Invalid request - {0} parameter not found", PARAM_PROXY_GRANTING_TICKET);
                return null;
            }
            else if (String.IsNullOrEmpty(proxyGrantingTicketIou)) {
                Logger.Information("Invalid request - {0} parameter not found", PARAM_PROXY_GRANTING_TICKET_IOU);
                return null;
            }

            Logger.Information("Recieved proxyGrantingTicketId [{0}] for proxyGrantingTicketIou [{1}]", proxyGrantingTicket, proxyGrantingTicketIou);

            _casServices.ProxyTicketManager.InsertProxyGrantingTicketMapping(proxyGrantingTicketIou, proxyGrantingTicket);

            // TODO: Consider creating a DotNetCasClient.Validation.Schema.Cas20.ProxySuccess object and serializing it.

            return new ContentResult {
                Content = "<casClient:proxySuccess xmlns:casClient=\"http://www.yale.edu/tp/casClient\" />",
                ContentEncoding = new UTF8Encoding(),
                ContentType = "application/xml"
            };
        }

        /// <summary>
        /// Validates a ticket contained in the URL, presumably generated by
        /// the CAS server after a successful authentication.  The actual ticket
        /// validation is performed by the configured TicketValidator 
        /// (i.e., CAS 1.0, CAS 2.0, SAML 1.0).  If the validation succeeds, the
        /// request is authenticated and a FormsAuthenticationCookie and 
        /// corresponding CasAuthenticationTicket are created for the purpose of 
        /// authenticating subsequent requests (see ProcessTicketValidation 
        /// method).  If the validation fails, the authentication status remains 
        /// unchanged (generally the user is and remains anonymous).
        /// </summary>
        public void ProcessTicketValidation(HttpContextBase httpContext) {
            HttpApplication app = httpContext.ApplicationInstance;
            HttpRequestBase request = httpContext.Request;

            string ticket = request[_casServices.Settings.ArtifactParameterName];

            try {
                // Attempt to authenticate the ticket and resolve to an ICasPrincipal
                var principal = TicketValidator.Validate(ticket);

                // Save the ticket in the FormsAuthTicket.  Encrypt the ticket and send it as a cookie. 
                var casTicket = new CasAuthenticationTicket(
                    ticket,
                    _urlUtil.RemoveCasArtifactsFromUrl(request.Url.AbsoluteUri),
                    request.UserHostAddress,
                    principal.Assertion,
                    _clock.UtcNow
                    );

                if (_casServices.ProxyTicketManager != null && !string.IsNullOrEmpty(principal.ProxyGrantingTicket)) {
                    casTicket.ProxyGrantingTicketIou = principal.ProxyGrantingTicket;
                    casTicket.Proxies.AddRange(principal.Proxies);
                    string proxyGrantingTicket = _casServices.ProxyTicketManager.GetProxyGrantingTicket(casTicket.ProxyGrantingTicketIou);
                    if (!string.IsNullOrEmpty(proxyGrantingTicket)) {
                        casTicket.ProxyGrantingTicket = proxyGrantingTicket;
                    }
                }

                // TODO: Check the last 2 parameters.  We want to take the from/to dates from the FormsAuthenticationTicket.  
                // However, we may need to do some clock drift correction.
                FormsAuthenticationTicket formsAuthTicket = CreateFormsAuthenticationTicket(
                    principal.Identity.Name,
                    ticket, 
                    null, 
                    null);

                SetAuthCookie(httpContext, formsAuthTicket);

                // Also save the ticket in the server store (if configured)
                if (_casServices.ServiceTicketManager != null) {
                    _casServices.ServiceTicketManager.UpdateTicketExpiration(casTicket, formsAuthTicket.Expiration);
                }

                // Jump directly to EndRequest.  Don't allow the Page and/or Handler to execute.
                // EndRequest will redirect back without the ticket in the URL
                app.CompleteRequest();
                return;
            }
            catch (TicketValidationException e) {
                // Leave principal null.  This might not have been a CAS service ticket.
                Logger.Error(e, "Ticket validation error: {0}", e);
            }
        }

        /// <summary>
        /// Attempts to authenticate requests subsequent to the initial authentication
        /// request (handled by ProcessTicketValidation).  This method looks for a 
        /// FormsAuthenticationCookie containing a FormsAuthenticationTicket and attempts
        /// to confirms its validitiy.  It either contains the CAS service ticket or a 
        /// reference to a CasAuthenticationTicket stored in the ServiceTicketManager 
        /// (if configured).  If it succeeds, the context.User and Thread.CurrentPrincipal 
        /// are set with a ICasPrincipal and the current request is considered 
        /// authenticated.  Otherwise, the current request is effectively anonymous.
        /// </summary>
        public void ProcessRequestAuthentication(HttpContextBase httpContext) {
            // Look for a valid FormsAuthenticationTicket encrypted in a cookie.
            CasAuthenticationTicket casTicket = null;
            FormsAuthenticationTicket formsAuthenticationTicket = GetFormsAuthenticationTicket(httpContext);
            if (formsAuthenticationTicket != null) {
                ICasPrincipal principal;
                if (_casServices.ServiceTicketManager != null) {
                    string serviceTicket = formsAuthenticationTicket.UserData;
                    casTicket = _casServices.ServiceTicketManager.GetTicket(serviceTicket);
                    if (casTicket != null) {
                        IAssertion assertion = casTicket.Assertion;

                        if (!_casServices.ServiceTicketManager.VerifyClientTicket(casTicket)) {
                            Logger.Warning("CasAuthenticationTicket failed verification: {0}", casTicket);

                            // Deletes the invalid FormsAuthentication cookie from the client.
                            ClearAuthCookie(httpContext);
                            _casServices.ServiceTicketManager.RevokeTicket(serviceTicket);

                            // Don't give this request a User/Principal.  Remove it if it was created
                            // by the underlying FormsAuthenticationModule or another module.
                            principal = null;
                        }
                        else {
                            if (_casServices.ProxyTicketManager != null && 
                                !string.IsNullOrEmpty(casTicket.ProxyGrantingTicketIou) && 
                                string.IsNullOrEmpty(casTicket.ProxyGrantingTicket)) {

                                string proxyGrantingTicket = _casServices.ProxyTicketManager.GetProxyGrantingTicket(casTicket.ProxyGrantingTicketIou);
                                if (!string.IsNullOrEmpty(proxyGrantingTicket)) {
                                    casTicket.ProxyGrantingTicket = proxyGrantingTicket;
                                }
                            }

                            principal = new CasPrincipal(assertion);
                        }
                    }
                    else {
                        if (httpContext.User != null &&
                            httpContext.User.Identity is FormsIdentity &&
                            _authenticationService.GetAuthenticatedUser() != null) {
                            return;
                        }

                        // This didn't resolve to a ticket in the TicketStore.  Revoke it.
                        ClearAuthCookie(httpContext);
                        Logger.Debug("Revoking ticket {0}", serviceTicket);
                        _casServices.ServiceTicketManager.RevokeTicket(serviceTicket);

                        // Don't give this request a User/Principal.  Remove it if it was created
                        // by the underlying FormsAuthenticationModule or another module.
                        principal = null;
                    }
                }
                else {
                    principal = new CasPrincipal(new Assertion(formsAuthenticationTicket.Name));
                }

                httpContext.User = principal;
                Thread.CurrentPrincipal = principal;

                if (principal == null) {
                    // Remove the cookie from the client
                    ClearAuthCookie(httpContext);
                }
                else {
                    // Extend the expiration of the cookie if FormsAuthentication is configured to do so.
                    if (FormsAuthentication.SlidingExpiration) {
                        FormsAuthenticationTicket newTicket = FormsAuthentication.RenewTicketIfOld(formsAuthenticationTicket);
                        if (newTicket != null && newTicket != formsAuthenticationTicket) {
                            SetAuthCookie(httpContext, newTicket);
                            if (_casServices.ServiceTicketManager != null) {
                                _casServices.ServiceTicketManager.UpdateTicketExpiration(casTicket, newTicket.Expiration);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to set the GatewayStatus client cookie.  If the cookie is not
        /// present and equal to GatewayStatus.Attempting when a CAS Gateway request
        /// comes in (indicated by the presence of the 'gatewayParameterName' 
        /// defined in web.config appearing in the URL), the server knows that the 
        /// client is not accepting session cookies and will optionally redirect 
        /// the user to the 'cookiesRequiredUrl' (also defined in web.config).  If
        /// 'cookiesRequiredUrl' is not defined but 'gateway' is, every page request
        /// will result in a round-trip to the CAS server.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="gatewayStatus">The GatewayStatus to attempt to store</param>
        public void SetGatewayStatusCookie(HttpContextBase httpContext, GatewayStatus gatewayStatus) {
            var cookie = new HttpCookie(_casServices.Settings.GatewayStatusCookieName, gatewayStatus.ToString()) {
                HttpOnly = false,
                Secure = false,
                Path = FormsAuthentication.FormsCookiePath
            };

            if (!String.IsNullOrEmpty(_settings.RequestUrlPrefix)) {
                cookie.Path = GetCookiePath(httpContext);
            }

            if (FormsAuthentication.CookieDomain != null) {
                cookie.Domain = FormsAuthentication.CookieDomain;
            }

            // Add it to the request collection for later processing during this request
            httpContext.Request.Cookies.Remove(_casServices.Settings.GatewayStatusCookieName);
            httpContext.Request.Cookies.Add(cookie);

            // Add it to the response collection for delivery to client
            httpContext.Response.Cookies.Add(cookie);
        }

        /// <summary>
        /// Retrieves the GatewayStatus from the client cookie.
        /// </summary>
        /// <returns>
        /// The GatewayStatus stored in the cookie if present, otherwise 
        /// GatewayStatus.NotAttempted.
        /// </returns>
        public GatewayStatus GetGatewayStatus(HttpContextBase httpContext) {
            HttpCookie cookie = httpContext.Request.Cookies[_casServices.Settings.GatewayStatusCookieName];

            GatewayStatus status;

            if (cookie != null && !string.IsNullOrEmpty(cookie.Value)) {
                try {
                    // Parse the value out of the cookie
                    status = (GatewayStatus)Enum.Parse(typeof(GatewayStatus), cookie.Value);
                }
                catch (ArgumentException) {
                    // If the cookie contains an invalid value, clear the cookie 
                    // and return GatewayStatus.NotAttempted
                    SetGatewayStatusCookie(httpContext, GatewayStatus.NotAttempted);
                    status = GatewayStatus.NotAttempted;
                }
            }
            else {
                // Use the default value GatewayStatus.NotAttempted
                status = GatewayStatus.NotAttempted;
            }

            return status;
        }

        /// <summary>
        /// Sends a blank and expired FormsAuthentication cookie to the 
        /// client response.  This effectively removes the FormsAuthentication
        /// cookie and revokes the FormsAuthenticationTicket.  It also removes
        /// the cookie from the current Request object, preventing subsequent 
        /// code from being able to access it during the execution of the 
        /// current request.
        /// </summary>
        public void ClearAuthCookie(HttpContextBase httpContext) {
            // Don't let anything see the incoming cookie 
            httpContext.Request.Cookies.Remove(FormsAuthentication.FormsCookieName);

            // Signout of Orchard too... Clearing the cookie might leave state that is unacceptable.
            _authenticationService.SignOut();
        }

        /// <summary>
        /// Encrypts a FormsAuthenticationTicket in an HttpCookie (using 
        /// GetAuthCookie) and includes it in the response.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="clientTicket">The FormsAuthenticationTicket to encode</param>
        public void SetAuthCookie(HttpContextBase httpContext, FormsAuthenticationTicket clientTicket) {
            if (!httpContext.Request.IsSecureConnection && FormsAuthentication.RequireSSL) {
                throw new HttpException("Connection not secure while creating secure cookie");
            }

            var authCookie = GetAuthCookie(httpContext, clientTicket);

            httpContext.Request.Cookies.Remove(FormsAuthentication.FormsCookieName);
            httpContext.Request.Cookies.Add(authCookie);
            httpContext.Response.Cookies.Add(authCookie);
        }

        /// <summary>
        /// Creates an HttpCookie containing an encrypted FormsAuthenticationTicket,
        /// which in turn contains a CAS service ticket.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="ticket">The FormsAuthenticationTicket to encode</param>
        /// <returns>An HttpCookie containing the encrypted FormsAuthenticationTicket</returns>
        public HttpCookie GetAuthCookie(HttpContextBase httpContext, FormsAuthenticationTicket ticket) {
            string str = FormsAuthentication.Encrypt(ticket);

            if (String.IsNullOrEmpty(str)) {
                throw new HttpException("Unable to encrypt cookie ticket");
            }

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, str) {
                HttpOnly = true,
                Secure = FormsAuthentication.RequireSSL,
                Path = FormsAuthentication.FormsCookiePath
            };

            if (!String.IsNullOrEmpty(_settings.RequestUrlPrefix)) {
                cookie.Path = GetCookiePath(httpContext);
            }

            if (FormsAuthentication.CookieDomain != null) {
                cookie.Domain = FormsAuthentication.CookieDomain;
            }

            if (ticket.IsPersistent) {
                cookie.Expires = ticket.Expiration;
            }

            return cookie;
        }

        /// <summary>
        /// Creates a FormsAuthenticationTicket for storage on the client.
        /// The UserData field contains the CAS Service Ticket which can be 
        /// used by the server-side ServiceTicketManager to retrieve additional 
        /// details about the ticket (e.g. assertions)
        /// </summary>
        /// <param name="netId">User associated with the ticket</param>
        /// <param name="serviceTicket">CAS service ticket</param>
        /// <param name="validFromDate">Ticket valid from date</param>
        /// <param name="validUntilDate">Ticket valid too date</param>
        /// <returns>Instance of a FormsAuthenticationTicket</returns>
        public FormsAuthenticationTicket CreateFormsAuthenticationTicket(
            string netId,
            string serviceTicket,
            DateTime? validFromDate,
            DateTime? validUntilDate) {

            Logger.Debug("Creating FormsAuthenticationTicket for {0}", serviceTicket);

            DateTime fromDate = validFromDate.HasValue ? validFromDate.Value : _clock.UtcNow;
            DateTime toDate = validUntilDate.HasValue ? validUntilDate.Value : fromDate.Add(TimeSpan.FromDays(30));

            var ticket = new FormsAuthenticationTicket(
                2 /*version*/,
                netId,
                fromDate,
                toDate,
                false,
                serviceTicket,
                FormsAuthentication.FormsCookiePath);

            return ticket;
        }

        /// <summary>
        /// Looks for a FormsAuthentication cookie and attempts to
        /// parse a valid, non-expired FormsAuthenticationTicket.
        /// It ensures that the UserData field has a value (presumed
        /// to be a CAS Service Ticket).
        /// </summary>
        /// <returns>
        /// Returns the FormsAuthenticationTicket contained in the 
        /// cookie or null if any issues are encountered.
        /// </returns>
        public FormsAuthenticationTicket GetFormsAuthenticationTicket(HttpContextBase httpContext) {
            HttpCookie cookie = httpContext.Request.Cookies[FormsAuthentication.FormsCookieName];

            if (cookie == null) {
                return null;
            }

            if ((cookie.Expires != DateTime.MinValue && cookie.Expires < _clock.UtcNow) ||
                String.IsNullOrEmpty(cookie.Value)) {
                ClearAuthCookie(httpContext);
                return null;
            }

            try {
                var formsAuthTicket = FormsAuthentication.Decrypt(cookie.Value);

                if (formsAuthTicket == null ||
                    formsAuthTicket.Expired ||
                    String.IsNullOrEmpty(formsAuthTicket.UserData)) {
                    ClearAuthCookie(httpContext);
                    return null;
                }

                return formsAuthTicket;
            }
            catch {
                ClearAuthCookie(httpContext);
            }

            return null;
        }

        /// <summary>
        /// Extracts the CAS ticket from the SAML message supplied.
        /// </summary>
        /// <param name="xmlAsString">SAML message from CAS server</param>
        /// <returns>The CAS ticket contained in SAML message</returns>
        private string ExtractSingleSignOutTicketFromSamlResponse(string xmlAsString) {
            XmlParserContext xmlParserContext = new XmlParserContext(null, _xmlNamespaceManager, null, XmlSpace.None);

            string elementText = null;
            if (!String.IsNullOrEmpty(xmlAsString) && !String.IsNullOrEmpty(XML_SESSION_INDEX_ELEMENT_NAME)) {
                using (TextReader textReader = new StringReader(xmlAsString)) {
                    XmlReader reader = XmlReader.Create(textReader, XmlReaderSettings, xmlParserContext);
                    bool foundElement = reader.ReadToFollowing(XML_SESSION_INDEX_ELEMENT_NAME);
                    if (foundElement) {
                        elementText = reader.ReadElementString();
                    }

                    reader.Close();
                }
            }
            return elementText;
        }

        private void LogAndThrowConfigurationException(string message)
        {
            Logger.Error(message);
            throw new CasConfigurationException(message);
        }

        private void LogAndThrowOperationException(string message)
        {
            Logger.Error(message);
            throw new InvalidOperationException(message);
        }

        private string GetCookiePath(HttpContextBase httpContext) {
            var cookiePath = httpContext.Request.ApplicationPath;
            if (cookiePath != null && cookiePath.Length > 1) {
                cookiePath += '/';
            }

            cookiePath += _settings.RequestUrlPrefix;

            return cookiePath;
        }

        internal ITicketValidator TicketValidator {
            get {
                return _ticketValidatorFactory.TicketValidator;
            }
        }
    }
}