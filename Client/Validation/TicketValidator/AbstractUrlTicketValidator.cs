using System;
using System.Collections.Specialized;
using NGM.CasClient.Client.Security;
using NGM.CasClient.Client.Utils;
using Orchard.Logging;

namespace NGM.CasClient.Client.Validation.TicketValidator {
    /// <summary>
    /// Abstract validator implementation for tickets that are validated against
    /// an Http server.
    /// </summary>
    /// <remarks>
    /// This is the .Net port of 
    ///   org.jasig.cas.client.validation.AbstractUrlBasedTicketValidator
    /// </remarks>
    /// <author>Scott Battaglia</author>
    /// <author>William G. Thompson, Jr. (.Net)</author>
    /// <author>Marvin S. Addison</author>
    /// <author>Scott Holodak (.Net)</author>
    public abstract class AbstractUrlTicketValidator : ITicketValidator {
        protected readonly ICasServices CASServices;
        protected readonly IUrlUtil UrlUtil;

        protected AbstractUrlTicketValidator(ICasServices casServices, 
            IUrlUtil urlUtil) {
            CASServices = casServices;
            UrlUtil = urlUtil;
        }

        #region Fields
        private NameValueCollection _customParameters;
        #endregion

        #region Properties
        /// <summary>
        /// Custom parameters to pass to the validation URL.
        /// </summary>        
        public NameValueCollection CustomParameters {
            get {
                if (_customParameters == null) {
                    _customParameters = new NameValueCollection();
                }
                return _customParameters;
            }
        }

        /// <summary>
        /// The endpoint of the validation URL.  Should be relative (i.e. not start with a "/").
        /// i.e. validate or serviceValidate.
        /// <list>
        ///   <item>CAS 1.0:  validate</item>
        ///   <item>CAS 2.0:  serviceValidate or proxyValidate</item>
        ///   <item>SAML 1.1: samlValidate</item>
        /// </list>
        /// </summary>
        public abstract string UrlSuffix {
            get;
        }

        /// <summary>
        /// The protocol-specific name of the request parameter containing the artifact/ticket.
        /// </summary>
        public abstract string ArtifactParameterName {
            get;
        }

        /// <summary>
        /// The protocol-specific name of the request parameter containing the service identifier.
        /// </summary>
        public abstract string ServiceParameterName {
            get;
        }
        #endregion

        #region Abstract Methods
        /// <summary>
        /// Perform any initialization required for the UrlTicketValidator implementation.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Parses the response from the server into a CAS Assertion and includes
        /// this in a CASPrincipal.
        /// </summary>
        /// <param name="response">
        /// the response from the server, in any format.
        /// </param>
        /// <param name="ticket">The ticket used to generate the validation response</param>
        /// <returns>
        /// a Principal backed by a CAS Assertion, if one could be parsed from the
        /// response.
        /// </returns>
        /// <exception cref="TicketValidationException">
        /// Thrown if creation of the Assertion fails.
        /// </exception>
        protected abstract ICasPrincipal ParseResponseFromServer(string response, string ticket);
        #endregion

        #region Concrete Methods
        /// <summary>
        /// Default implementation that performs an HTTP GET request to the validation URL
        /// supplied with the supplied ticket and returns the response body as a string.
        /// </summary>
        /// <param name="validationUrl">The validation URL to request</param>
        /// <param name="ticket">The ticket parameter to pass to the URL</param>
        /// <returns></returns>
        protected virtual string RetrieveResponseFromServer(string validationUrl, string ticket) {
            return HttpUtil.PerformHttpGet(validationUrl, true);
        }

        /// <summary>
        /// Attempts to validate a ticket for the provided service.
        /// </summary>
        /// <param name="ticket">the ticket to validate</param>
        /// <returns>
        /// The ICasPrincipal backed by the CAS Assertion included in the response
        /// from the CAS server for a successful ticket validation.
        /// </returns>
        /// <exception cref="TicketValidationException">
        /// Thrown if ticket validation fails.
        /// </exception>
        public ICasPrincipal Validate(string ticket) {
            string validationUrl = UrlUtil.ConstructValidateUrl(ticket, CASServices.Settings.Gateway, CASServices.Settings.Renew, CustomParameters);
            CASServices.Logger.Debug("Constructed validation URL " + validationUrl);

            string serverResponse;
            try {
                serverResponse = RetrieveResponseFromServer(validationUrl, ticket);
            }
            catch (Exception e) {
                CASServices.Logger.Information("Ticket validation failed: " + e);
                throw new TicketValidationException("CAS server ticket validation threw an Exception", e);
            }

            if (serverResponse == null) {
                CASServices.Logger.Warning("CAS server returned no response");
                throw new TicketValidationException("The CAS server returned no response.");
            }

            CASServices.Logger.Debug("Ticket validation response:{0}{1}", Environment.NewLine, serverResponse);

            return ParseResponseFromServer(serverResponse, ticket);
        }
        #endregion
    }
}