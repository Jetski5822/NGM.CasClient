using NGM.CasClient.Client.Security;
using Orchard;

namespace NGM.CasClient.Services {
    public interface ICasIdentityRetriever : IDependency {
        string GetId(CasPrincipal user);
    }
}