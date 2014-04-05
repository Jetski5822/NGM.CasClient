using System.Collections.Generic;
using NGM.CasClient.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.UI.Admin.Notification;
using Orchard.UI.Notify;

namespace NGM.CasClient {
    public class MissingSettingsBanner : INotificationProvider {
        private readonly IOrchardServices _orchardServices;

        public MissingSettingsBanner(IOrchardServices orchardServices) {
            _orchardServices = orchardServices;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public IEnumerable<NotifyEntry> GetNotifications() {
            var casSettings = _orchardServices.WorkContext.CurrentSite.As<CASSettingsPart>();

            if (string.IsNullOrWhiteSpace(casSettings.ProxyCallbackParameterName)) {
                yield return new NotifyEntry {
                    Message = T("The CAS Server Proxy Callback Parameter Name."),
                    Type = NotifyType.Warning
                };
            }
            if (string.IsNullOrWhiteSpace(casSettings.GatewayParameterName)) {
                yield return new NotifyEntry {
                    Message = T("The CAS Server Gateway Parameter Name."),
                    Type = NotifyType.Warning
                };
            }
            if (string.IsNullOrWhiteSpace(casSettings.ArtifactParameterName)) {
                yield return new NotifyEntry {
                    Message = T("The CAS Server Artifact Parameter Name."),
                    Type = NotifyType.Warning
                };
            }
            if (string.IsNullOrWhiteSpace(casSettings.GatewayStatusCookieName)) {
                yield return new NotifyEntry {
                    Message = T("The CAS Server Gateway Status Cookie Name."),
                    Type = NotifyType.Warning
                };
            }
            if (string.IsNullOrWhiteSpace(casSettings.TicketValidatorName)) {
                yield return new NotifyEntry {
                    Message = T("The CAS Server Ticket Validator Name."),
                    Type = NotifyType.Warning
                };
            }
            if (string.IsNullOrWhiteSpace(casSettings.CasServerUrlPrefix)) {
                yield return new NotifyEntry {
                    Message = T("The CAS Server Url Prefix cannot be empty."), 
                    Type = NotifyType.Warning
                };
            }
            if (string.IsNullOrWhiteSpace(casSettings.FormsLoginUrl)) {
                yield return new NotifyEntry {
                    Message = T("The CAS Server Forms Login Url cannot be empty."), 
                    Type = NotifyType.Warning
                };
            }
            
        }
    }
}
