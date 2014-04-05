using System.IO;
using NGM.CasClient.Client.Security;
using NGM.CasClient.Client.Utils;

namespace NGM.CasClient.Client.Validation.TicketValidator {
    /// <summary>
    /// CAS 1.0 Ticket Validator
    /// </summary>
    /// <remarks>
    /// This is the .Net port of org.jasig.cas.client.validation.Cas10TicketValidator
    /// </remarks>
    /// <author>Scott Battaglia</author>
    /// <author>William G. Thompson, Jr. (.Net)</author>
    /// <author>Marvin S. Addison</author>
    /// <author>Scott Holodak (.Net)</author>
    public class Cas10TicketValidator : AbstractCasProtocolTicketValidator {
        #region Properties

        public Cas10TicketValidator(ICasServices casServices, IUrlUtil urlUtil)
            : base(casServices, urlUtil) {
        }

        /// <summary>
        /// The endpoint of the validation URL.  Should be relative (i.e. not start with a "/").
        /// i.e. validate or serviceValidate.
        /// </summary>
        public override string UrlSuffix {
            get {
                return "validate";
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Performs Cas10TicketValidator initialization.
        /// </summary>
        public override void Initialize() { /* Do nothing */ }

        /// <summary>
        /// Parses the response from the server into a CAS Assertion and includes this in
        /// a CASPrincipal.
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
            if (response == null || !response.StartsWith("yes")) {
                throw new TicketValidationException("CAS Server could not validate ticket.");
            }

            try {
                StringReader reader = new StringReader(response);
                reader.ReadLine();
                string name = reader.ReadLine();
                return new CasPrincipal(new Assertion(name));
            }
            catch (IOException e) {
                throw new TicketValidationException("CAS Server response could not be parsed.", e);
            }
        }
        #endregion
    }
}