using NGM.CasClient.Client.Utils;
using NGM.CasClient.Client.Validation.TicketValidator;
using Orchard;

namespace NGM.CasClient.Client.Factories {
    public interface ITicketValidatorFactory : IDependency {
        ITicketValidator TicketValidator { get; }
    }

    public class TicketValidatorFactory : ITicketValidatorFactory {
        private readonly ICasServices _casServices;
        private readonly IUrlUtil _urlUtil;

        public TicketValidatorFactory(ICasServices casServices,
            IUrlUtil urlUtil) {
            _casServices = casServices;
            _urlUtil = urlUtil;
        }

        public ITicketValidator TicketValidator {
            get{
                switch (_casServices.Settings.TicketValidatorName) {
                    case "Cas10": return new Cas10TicketValidator(_casServices, _urlUtil);
                    case "Cas20": return new Cas20ServiceTicketValidator(_casServices, _urlUtil);
                    case "Saml11": return new Saml11TicketValidator(_casServices, _urlUtil);
                }

                return null;
            }
        }
    }
}