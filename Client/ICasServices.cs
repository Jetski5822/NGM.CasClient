using NGM.CasClient.Client.State;
using NGM.CasClient.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Logging;
using Orchard.Services;

namespace NGM.CasClient.Client {
    public interface ICasServices : IDependency {
        CASSettingsPart Settings { get; }
        IServiceTicketManager ServiceTicketManager { get; }
        IProxyTicketManager ProxyTicketManager { get; }
        ILogger Logger { get; }
        IClock Clock { get; }
    }

    public class CasServices : ICasServices {
        private readonly IServiceTicketManagerFactory _serviceTicketManagerFactory;
        private readonly IProxyTicketManagerFactory _proxyTicketManagerFactory;
        private readonly IOrchardServices _orchardServices;
        private readonly IClock _clock;

        public CasServices(IServiceTicketManagerFactory serviceTicketManagerFactory,
            IProxyTicketManagerFactory proxyTicketManagerFactory,
            IOrchardServices orchardServices,
            IClock clock) {
            _serviceTicketManagerFactory = serviceTicketManagerFactory;
            _proxyTicketManagerFactory = proxyTicketManagerFactory;
            _orchardServices = orchardServices;
            _clock = clock;

            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public IClock Clock {
            get { return _clock; }
        }

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