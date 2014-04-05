using Orchard.ContentManagement;
using Orchard.ContentManagement.Records;

namespace NGM.CasClient.Models {
    public class CASSettingsPart : ContentPart<CASSettingsPartRecord> {
        public bool ProcessIncomingSingleSignOutRequests {
            get { return Record.ProcessIncomingSingleSignOutRequests; }
            set { Record.ProcessIncomingSingleSignOutRequests = value; }
        }

        public string ProxyCallbackParameterName {
            get { return Record.ProxyCallbackParameterName; }
            set { Record.ProxyCallbackParameterName = value; }
        }

        public string GatewayParameterName {
            get { return Record.GatewayParameterName; }
            set { Record.GatewayParameterName = value; }
        }

        public string ArtifactParameterName {
            get { return Record.ArtifactParameterName; }
            set { Record.ArtifactParameterName = value; }
        }

        public string GatewayStatusCookieName {
            get { return Record.GatewayStatusCookieName; }
            set { Record.GatewayStatusCookieName = value; }
        }

        public string TicketValidatorName {
            get { return Record.TicketValidatorName; }
            set { Record.TicketValidatorName = value; }
        }

        public string CasServerUrlPrefix {
            get { return Record.CasServerUrlPrefix; }
            set { Record.CasServerUrlPrefix = value; }
        }

        public string CookiesRequiredUrl {
            get { return Record.CookiesRequiredUrl; }
            set { Record.CookiesRequiredUrl = value; }
        }

        public bool Gateway {
            get { return Record.Gateway; }
            set { Record.Gateway = value; }
        }

        public bool Renew {
            get { return Record.Renew; }
            set { Record.Renew = value; }
        }

        public string FormsLoginUrl {
            get { return Record.FormsLoginUrl; }
            set { Record.FormsLoginUrl = value; }
        }

        public string NotAuthorizedUrl {
            get { return Record.NotAuthorizedUrl; }
            set { Record.NotAuthorizedUrl = value; }
        }

        public string ServiceTicketManager {
            get { return Record.ServiceTicketManager; }
            set { Record.ServiceTicketManager = value; }
        }

        public string ProxyTicketManager {
            get { return Record.ProxyTicketManager; }
            set { Record.ProxyTicketManager = value; }
        }

        public long TicketTimeTolerance {
            get { return Record.TicketTimeTolerance; }
            set { Record.TicketTimeTolerance = value; }
        }

        public bool IsConfigured() {
            if (string.IsNullOrWhiteSpace(ProxyCallbackParameterName))
                return false;
            if (string.IsNullOrWhiteSpace(GatewayParameterName))
                return false;
            if (string.IsNullOrWhiteSpace(ArtifactParameterName))
                return false;
            if (string.IsNullOrWhiteSpace(GatewayStatusCookieName))
                return false;
            if (string.IsNullOrWhiteSpace(TicketValidatorName))
                return false;
            if (string.IsNullOrWhiteSpace(CasServerUrlPrefix))
                return false;
            if (TicketTimeTolerance == 0)
                return false;

            return true;
        }
    }

    public class CASSettingsPartRecord : ContentPartRecord {
        public virtual bool ProcessIncomingSingleSignOutRequests { get; set; }
        public virtual string TicketValidatorName { get; set; }
        public virtual string ProxyCallbackParameterName { get; set; }
        public virtual string GatewayParameterName { get; set; }
        public virtual string ArtifactParameterName { get; set; }
        public virtual string GatewayStatusCookieName { get; set; }
        public virtual string CasServerUrlPrefix { get; set; }
        public virtual string CookiesRequiredUrl { get; set; }
        public virtual bool Gateway { get; set; }
        public virtual bool Renew { get; set; }
        public virtual string FormsLoginUrl { get; set; }
        public virtual string NotAuthorizedUrl { get; set; }
        public virtual string ServiceTicketManager { get; set; }
        public virtual string ProxyTicketManager { get; set; }
        public virtual long TicketTimeTolerance { get; set; }
    }
}