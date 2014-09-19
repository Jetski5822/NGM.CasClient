using NGM.CasClient.Client.Security;

namespace NGM.CasClient.Services {
    public class DefaultCasIdentityRetriever : ICasIdentityRetriever {
        public string GetId(CasPrincipal user) {
            return user.Identity.Name;
        }
    }
}