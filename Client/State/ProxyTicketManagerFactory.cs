using System;
using System.Collections.Generic;
using System.Linq;
using NGM.CasClient.Client.Configuration;
using Orchard;
using Orchard.Localization;
using Orchard.Logging;

namespace NGM.CasClient.Client.State {
    public interface IProxyTicketManagerFactory : IDependency {
        IProxyTicketManager GetProxyTicketManager(string name);
    }

    public class ProxyTicketManagerFactory : IProxyTicketManagerFactory {
        private readonly IEnumerable<IProxyTicketManagerWrapper> _proxyTicketManagerWrappers;

        public ProxyTicketManagerFactory(IEnumerable<IProxyTicketManagerWrapper> proxyTicketManagerWrappers) {
            _proxyTicketManagerWrappers = proxyTicketManagerWrappers;

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public IProxyTicketManager GetProxyTicketManager(string name) {
            if (String.IsNullOrEmpty(name)) {
                // Web server cannot maintain ticket state, verify tickets, perform SSO, etc.
                return null;
            }

            var wrapper = _proxyTicketManagerWrappers.SingleOrDefault(x => System.String.CompareOrdinal(name, x.Name) == 0);

            if (wrapper != null) {
                return wrapper;
            }

            Logger.Error("Unknown service ticket manager provider: {0}", name);
            throw new CasConfigurationException("Unknown service ticket manager provider: " + name);
        }
    }
}