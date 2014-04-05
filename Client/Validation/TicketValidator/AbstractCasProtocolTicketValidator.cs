using NGM.CasClient.Client.Utils;

namespace NGM.CasClient.Client.Validation.TicketValidator {
    public abstract class AbstractCasProtocolTicketValidator : AbstractUrlTicketValidator {
        private const string CAS_ARTIFACT_PARAM = "ticket";
        private const string CAS_SERVICE_PARAM = "service";

        protected AbstractCasProtocolTicketValidator(ICasServices casServices, IUrlUtil urlUtil)
            : base(casServices, urlUtil) {
        }

        public override string ArtifactParameterName {
            get { return CAS_ARTIFACT_PARAM; }
        }

        public override string ServiceParameterName {
            get { return CAS_SERVICE_PARAM; }
        }
    }
}