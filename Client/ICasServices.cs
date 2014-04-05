using NGM.CasClient.Client.State;
using NGM.CasClient.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Logging;

namespace NGM.CasClient.Client {
    public interface ICasServices : IDependency {
        CASSettingsPart Settings { get; }
        IServiceTicketManager ServiceTicketManager { get; }
        IProxyTicketManager ProxyTicketManager { get; }
        ILogger Logger { get; }
    }

    public class CasServices : ICasServices {
        private readonly IServiceTicketManagerFactory _serviceTicketManagerFactory;
        private readonly IProxyTicketManagerFactory _proxyTicketManagerFactory;
        private readonly IOrchardServices _orchardServices;

        public CasServices(IServiceTicketManagerFactory serviceTicketManagerFactory,
            IProxyTicketManagerFactory proxyTicketManagerFactory,
            IOrchardServices orchardServices) {
            _serviceTicketManagerFactory = serviceTicketManagerFactory;
            _proxyTicketManagerFactory = proxyTicketManagerFactory;
            _orchardServices = orchardServices;

            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public CASSettingsPart Settings {
            get { return _orchardServices.WorkContext.CurrentSite.As<CASSettingsPart>(); }
        }

        public IServiceTicketManager ServiceTicketManager {
            get {
                return _serviceTicketManagerFactory.GetServiceTicketManager(Settings.ServiceTicketManager);
            }
        }

        public IProxyTicketManager ProxyTicketManager {
            get {
                return _proxyTicketManagerFactory.GetProxyTicketManager(Settings.ProxyTicketManager);
            }
        }
    }
}