using System;
using System.Collections.Generic;
using System.Xml;
using NGM.CasClient.Client.Security;
using Orchard.Logging;
using Orchard.Services;

namespace NGM.CasClient.Client.Validation {
    /// <summary>
    /// Represents a CAS SAML 1.1 response from a CAS server, using Xml parsing to
    /// populate the object.
    /// </summary>
    class CasSaml11Response {
        private readonly ILogger _logger;
        private readonly IClock _clock;

        #region Fields
        // The SAML 1.1 Assertion namespace
        const string SAML11_ASSERTION_NAMESPACE = "urn:oasis:names:tc:SAML:1.0:assertion";

        // Tolerance ticks for checking the current time against the SAML
        // Assertion valid times.
        private readonly long _toleranceTicks = 1000L * TimeSpan.TicksPerMillisecond;
        #endregion

        #region Properties
        /// <summary>
        ///  Whether a valid SAML Assertion was found for processing
        /// </summary>
        public bool HasCasSamlAssertion { get; private set; }

        /// <summary>
        ///  The JaSig CAS ICasPrincipal assertion built from the received CAS
        ///  SAML 1.1 response
        /// </summary>
        public ICasPrincipal CasPrincipal { get; private set; }
        #endregion

        #region Constructor

        /// <summary>
        /// Creates a CasSaml11Response from the response returned by the CAS server.
        /// The SAMLAssertion processed is the first valid SAML Asssertion found in
        /// the server response.
        /// </summary>
        /// <param name="response">
        /// the xml for the SAML 1.1 response received in response to the
        /// samlValidate query to the CAS server
        /// </param>
        /// <param name="tolerance">
        /// Tolerance milliseconds for checking the current time against the SAML
        /// Assertion valid times.
        /// </param>
        /// <param name="logger"></param>
        /// <param name="clock"></param>
        public CasSaml11Response(string response, long tolerance, ILogger logger, IClock clock) {
            _logger = logger;
            _clock = clock;

            _toleranceTicks = tolerance * TimeSpan.TicksPerMillisecond;
            HasCasSamlAssertion = false;
            ProcessValidAssertion(response);
            HasCasSamlAssertion = true;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Initializes the CAS IAssertion for this instance from the first valid
        /// Assertion node in the CAS server response.
        /// </summary>
        /// <exception cref="TicketValidationException">
        /// Thrown when data problems are encountered parsing
        /// the CAS server response that contains the Assertion, such as
        /// no valid Assertion found or no Authentication statment found in the
        /// the valid Assertion.
        /// </exception>
        private void ProcessValidAssertion(string response) {
            _logger.Debug("Unmarshalling SAML response");
            var document = new XmlDocument();
            document.LoadXml(response);

            var nsmgr = new XmlNamespaceManager(document.NameTable);
            nsmgr.AddNamespace("assertion", SAML11_ASSERTION_NAMESPACE);

            if (document.DocumentElement == null) {
                _logger.Debug("No assertions found in SAML response.");
                throw new TicketValidationException("No valid assertions found in the CAS response.");
            }

            var assertions = document.DocumentElement.SelectNodes("descendant::assertion:Assertion", nsmgr);

            if (assertions == null || assertions.Count <= 0) {
                _logger.Debug("No assertions found in SAML response.");
                throw new TicketValidationException("No assertions found.");
            }

            var currentTicks = _clock.UtcNow.Ticks;

            foreach (XmlNode assertionNode in assertions) {
                XmlNode conditionsNode = assertionNode.SelectSingleNode("descendant::assertion:Conditions", nsmgr);
                if (conditionsNode == null) {
                    continue;
                }

                DateTime notBefore;
                DateTime notOnOrAfter;
                try {
                    notBefore = SamlUtils.GetAttributeValueAsDateTime(conditionsNode, "NotBefore");
                    notOnOrAfter = SamlUtils.GetAttributeValueAsDateTime(conditionsNode, "NotOnOrAfter");
                    if (!SamlUtils.IsValidAssertion(_logger, notBefore, notOnOrAfter, currentTicks, _toleranceTicks)) {
                        continue;
                    }
                }
                catch (Exception) {
                    continue;
                }

                XmlNode authenticationStmtNode = assertionNode.SelectSingleNode("descendant::assertion:AuthenticationStatement", nsmgr);
                if (authenticationStmtNode == null) {
                    _logger.Debug("No AuthenticationStatement found in SAML response.");
                    throw new TicketValidationException("No AuthenticationStatement found in the CAS response.");
                }

                XmlNode nameIdentifierNode = assertionNode
                    .SelectSingleNode("child::assertion:AuthenticationStatement/child::assertion:Subject/child::assertion:NameIdentifier", nsmgr);
                if (nameIdentifierNode == null) {
                    _logger.Debug("No NameIdentifier found in SAML response.");
                    throw new TicketValidationException("No NameIdentifier found in AuthenticationStatement of the CAS response.");
                }

                string subject = nameIdentifierNode.FirstChild.Value;

                XmlNode attributeStmtNode = assertionNode.SelectSingleNode("descendant::assertion:AttributeStatement", nsmgr);
                if (attributeStmtNode != null) {
                    IDictionary<string, IList<string>> personAttributes =
                        SamlUtils.GetAttributesFor(_logger, attributeStmtNode, nsmgr, subject);

                    CasPrincipal = new CasPrincipal(new Assertion(subject, notBefore, notOnOrAfter, personAttributes), null, null);
                }
                else {
                    CasPrincipal = new CasPrincipal(new Assertion(subject, notBefore, notOnOrAfter), null, null);
                }

                return;
            }

            _logger.Debug("No assertions found in SAML response.");
            throw new TicketValidationException("No valid assertions found in the CAS response.");
        }
        #endregion
    }
}