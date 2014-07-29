using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Orchard;

namespace NGM.CasClient.Client {
    public interface ICASClient : IDependency {
        /// <summary>
        /// Obtain a Proxy ticket and redirect to the foreign service url with 
        /// that ticket included in the url.  The foreign service must be configured 
        /// to accept the ticket.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="url">The foreign service to redirect to</param>
        /// <exception cref="ArgumentNullException">The url supplied is null</exception>
        /// <exception cref="ArgumentException">The url supplied is empty</exception>
        void ProxyRedirect(HttpContextBase httpContext, string url);

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
        void ProxyRedirect(HttpContextBase httpContext, string url, bool endResponse);

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
        void ProxyRedirect(HttpContextBase httpContext, string url, string proxyTicketUrlParameter);

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
        void ProxyRedirect(HttpContextBase httpContext, string url, string proxyTicketUrlParameter, bool endResponse);

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
        string GetProxyTicketIdFor(HttpContextBase httpContext, string targetServiceUrl);

        /// <summary>
        /// Redirects the current request to the CAS Login page
        /// </summary>
        RedirectResult RedirectToLoginPage();

        /// <summary>
        /// Redirects the current request to the Login page and requires renewed
        /// CAS credentials
        /// </summary>
        RedirectResult RedirectToLoginPage(bool forceRenew);

        /// <summary>
        /// Redirects the current request to the Cookies Required page
        /// </summary>
        RedirectResult RedirectToCookiesRequiredPage();

        /// <summary>
        /// Redirects the current request to the Not Authorized page
        /// </summary>
        RedirectResult RedirectToNotAuthorizedPage();

        /// <summary>
        /// Redirects the current request back to the requested page without
        /// the CAS ticket artifact in the URL.
        /// </summary>
        RedirectResult RedirectFromLoginCallback(HttpContextBase httpContext, ActionResult result);

        /// <summary>
        /// Redirects the current request back to the requested page without
        /// the gateway callback artifact in the URL.
        /// </summary>
        RedirectResult RedirectFromFailedGatewayCallback(HttpContextBase httpContext);

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
        RedirectResult GatewayAuthenticate(HttpContextBase httpContext, bool ignoreGatewayStatusCookie);

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
        void SingleSignOut(HttpContextBase httpContext);

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
        ActionResult ProcessSingleSignOutRequest(HttpContextBase httpContext);

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
        ActionResult ProcessProxyCallbackRequest(HttpContextBase httpContext);

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
        void ProcessTicketValidation(HttpContextBase httpContext);

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
        void ProcessRequestAuthentication(HttpContextBase httpContext);

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
        void SetGatewayStatusCookie(HttpContextBase httpContext, GatewayStatus gatewayStatus);

        /// <summary>
        /// Retrieves the GatewayStatus from the client cookie.
        /// </summary>
        /// <returns>
        /// The GatewayStatus stored in the cookie if present, otherwise 
        /// GatewayStatus.NotAttempted.
        /// </returns>
        GatewayStatus GetGatewayStatus(HttpContextBase httpContext);

        /// <summary>
        /// Sends a blank and expired FormsAuthentication cookie to the 
        /// client response.  This effectively removes the FormsAuthentication
        /// cookie and revokes the FormsAuthenticationTicket.  It also removes
        /// the cookie from the current Request object, preventing subsequent 
        /// code from being able to access it during the execution of the 
        /// current request.
        /// </summary>
        void ClearAuthCookie(HttpContextBase httpContext);

        /// <summary>
        /// Encrypts a FormsAuthenticationTicket in an HttpCookie (using 
        /// GetAuthCookie) and includes it in the response.
        /// </summary>
        /// <param name="clientTicket">The FormsAuthenticationTicket to encode</param>
        void SetAuthCookie(HttpContextBase httpContext, FormsAuthenticationTicket clientTicket);

        /// <summary>
        /// Creates an HttpCookie containing an encrypted FormsAuthenticationTicket,
        /// which in turn contains a CAS service ticket.
        /// </summary>
        /// <param name="ticket">The FormsAuthenticationTicket to encode</param>
        /// <returns>An HttpCookie containing the encrypted FormsAuthenticationTicket</returns>
        HttpCookie GetAuthCookie(HttpContextBase httpContext, FormsAuthenticationTicket ticket);

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
        FormsAuthenticationTicket GetFormsAuthenticationTicket(HttpContextBase httpContext);
    }
}