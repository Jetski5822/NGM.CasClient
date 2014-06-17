using System.Collections.Generic;
using System.Web.Http;
using NGM.CasClient.Client;
using NGM.CasClient.Client.Extensions;
using NGM.CasClient.Filters;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace NGM.CasClient {
    public class Routes : IHttpRouteProvider {
        private readonly IRequestEvaluator _requestEvaluator;
        private readonly ICasServices _casServices;
        private readonly ICASActionFilter _casActionFilter;
        private readonly ICASClient _casClient;

        public Routes(
            IRequestEvaluator requestEvaluator,
            ICasServices casServices,
            ICASActionFilter casActionFilter,
            ICASClient casClient) {
            _requestEvaluator = requestEvaluator;
            _casServices = casServices;
            _casActionFilter = casActionFilter;
            _casClient = casClient;
        }

        public void GetRoutes(ICollection<RouteDescriptor> routes) {
            foreach (var routeDescriptor in GetRoutes())
                routes.Add(routeDescriptor);
        }

        public IEnumerable<RouteDescriptor> GetRoutes() {
            GlobalConfiguration.Configuration.Filters.Add(new CasAuthorizationFilter(_casClient, _casServices, _requestEvaluator));
            GlobalConfiguration.Configuration.Filters.Add(new CasWebApiActionFilter(_requestEvaluator, _casServices, _casActionFilter));

            return new List<RouteDescriptor>();
        }
    }
}