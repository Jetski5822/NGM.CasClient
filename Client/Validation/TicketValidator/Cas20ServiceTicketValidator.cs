using System;
using System.Web;
using NGM.CasClient.Client.Security;
using NGM.CasClient.Client.Utils;
using NGM.CasClient.Client.Validation.Schema.Cas20;

namespace NGM.CasClient.Client.Validation.TicketValidator {
    /// <summary>
    /// CAS 2.0 Ticket Validator
    /// </summary>
    /// <remarks>
    /// This is the .Net port of org.jasig.cas.client.validation.Cas20ServiceTicketValidator.
    /// </remarks>
    /// <author>Scott Battaglia</author>
    /// <author>Catherine D. Winfrey (.Net)</author>
    /// <author>William G. Thompson, Jr. (.Net)</author>
    /// <author>Marvin S. Addison</author>
    /// <author>Scott Holodak (.Net)</author>
    public class Cas20ServiceTicketValidator : AbstractCasProtocolTicketValidator {

        #region Properties

        public Cas20ServiceTicketValidator(ICasServices casServices, IUrlUtil urlUtil)
            : base(casServices, urlUtil) {
        }

        /// <summary>
        /// The endpoint of the validation URL.  Should be relative (i.e. not start with a "/").
        /// i.e. validate or serviceValidate.
        /// </summary>
        public override string UrlSuffix {
            get {
                if (CASServices.ProxyTicketManager != null) {
                    return "proxyValidate";
                }
                else {
                    return "serviceValidate";
                }
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Performs Cas20ServiceTicketValidator initialization.
        /// </summary>
        public override void Initialize() {
            if (CASServices.ProxyTicketManager != null) {
                CustomParameters.Add("pgtUrl", HttpUtility.UrlEncode(UrlUtil.ConstructProxyCallbackUrl()));
            }
        }

        /// <summary>
        /// Parses the response from the server into a CAS Assertion and includes this in
        /// a CASPrincipal.
        /// <remarks>
        /// Parsing of a &lt;cas:attributes&gt; element is <b>not</b> supported.  The official
        /// CAS 2.0 protocol does include this feature.  If attributes are needed,
        /// SAML must be used.
        /// </remarks>
        /// </summary>
        /// <param name="response">the response from the server, in any format.</param>
        /// <param name="ticket">The ticket used to generate the validation response</param>
        /// <returns>
        /// a Principal backed by a CAS Assertion, if one could be created from the response.
        /// </returns>
        /// <exception cref="TicketValidationException">
        /// Thrown if creation of the Assertion fails.
        /// </exception>
        protected override ICasPrincipal ParseResponseFromServer(string response, string ticket) {
            if (String.IsNullOrEmpty(response)) {
                throw new TicketValidationException("CAS Server response was empty.");
            }

            ServiceResponse serviceResponse;
            try {
                serviceResponse = ServiceResponse.ParseResponse(response);
            }
            catch (InvalidOperationException) {
                throw new TicketValidationException("CAS Server response does not conform to CAS 2.0 schema");
            }

            if (serviceResponse.IsAuthenticationSuccess) {
                AuthenticationSuccess authSuccessResponse = (AuthenticationSuccess)serviceResponse.Item;

                if (String.IsNullOrEmpty(authSuccessResponse.User)) {
                    throw new TicketValidationException(string.Format("CAS Server response parse failure: missing 'cas:user' element."));
                }

                string proxyGrantingTicketIou = authSuccessResponse.ProxyGrantingTicket;

                if (CASServices.ProxyTicketManager != null && !string.IsNullOrEmpty(proxyGrantingTicketIou)) {
                    string proxyGrantingTicket = CASServices.ProxyTicketManager.GetProxyGrantingTicket(proxyGrantingTicketIou);
                    CASServices.ProxyTicketManager.InsertProxyGrantingTicketMapping(proxyGrantingTicketIou, proxyGrantingTicket);
                }

                if (authSuccessResponse.Proxies != null && authSuccessResponse.Proxies.Length > 0) {
                    return new CasPrincipal(new Assertion(authSuccessResponse.User), proxyGrantingTicketIou, authSuccessResponse.Proxies);
                }
                else {
                    return new CasPrincipal(new Assertion(authSuccessResponse.User), proxyGrantingTicketIou);
                }
            }

            if (serviceResponse.IsAuthenticationFailure) {
                try {
                    AuthenticationFailure authFailureResponse = (AuthenticationFailure)serviceResponse.Item;
                    throw new TicketValidationException(authFailureResponse.Message, authFailureResponse.Code);
                }
                catch {
                    throw new TicketValidationException("CAS ticket could not be validated.");
                }
            }

            if (serviceResponse.IsProxySuccess) {
                throw new TicketValidationException("Unexpected service validate response: ProxySuccess");
            }

            if (serviceResponse.IsProxyFailure) {
                throw new TicketValidationException("Unexpected service validate response: ProxyFailure");
            }

            throw new TicketValidationException("Failed to validate CAS ticket.");
        }
        #endregion
    }
}