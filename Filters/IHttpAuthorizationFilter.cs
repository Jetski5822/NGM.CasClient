using Orchard.WebApi.Filters;

namespace NGM.CasClient.Filters {
    public interface IHttpAuthorizationFilter : System.Web.Http.Filters.IAuthorizationFilter, IApiFilterProvider {
    }
}