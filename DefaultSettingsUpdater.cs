using JetBrains.Annotations;
using NGM.CasClient.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Environment;
using Orchard.Environment.Extensions.Models;

namespace NGM.CasClient {
    [UsedImplicitly]
    public class DefaultSettingsUpdater : IFeatureEventHandler {
        private readonly IOrchardServices _orchardServices;

        public DefaultSettingsUpdater(IOrchardServices orchardServices) {
            _orchardServices = orchardServices;
        }

        public void Installing(Feature feature) {
        }

        public void Installed(Feature feature) {
        }

        public void Enabling(Feature feature) {
        }

        public void Enabled(Feature feature) {
            if (feature.Descriptor.Id != "NGM.CasClient")
                return;

            var settings = _orchardServices.WorkContext.CurrentSite.As<CASSettingsPart>();
            settings.ProcessIncomingSingleSignOutRequests = true;
            settings.ProxyCallbackParameterName = "proxyResponse";
            settings.GatewayParameterName = "gatewayResponse";
            settings.ArtifactParameterName = "ticket";
            settings.GatewayStatusCookieName = "cas_gateway_status";
            settings.TicketValidatorName = "Saml11";
            settings.ServiceTicketManager = "CacheServiceTicketManager";
            settings.ProxyTicketManager = "CacheProxyTicketManager";
            settings.TicketTimeTolerance = 5000;
        }

        public void Disabling(Feature feature) {
        }

        public void Disabled(Feature feature) {
        }

        public void Uninstalling(Feature feature) {
        }

        public void Uninstalled(Feature feature) {
        }
    }
}