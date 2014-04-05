using System;
using System.Web.Caching;
using Orchard.Mvc;
using Orchard.Services;

namespace NGM.CasClient.Client.State {
    public interface IProxyTicketManagerWrapper : IProxyTicketManager {
        string Name { get; }
    }

    public class CacheProxyTicketManager : IProxyTicketManagerWrapper {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IClock _clock;

        public CacheProxyTicketManager(IHttpContextAccessor httpContextAccessor,
            IClock clock) {
            _httpContextAccessor = httpContextAccessor;
            _clock = clock;
        }

        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromSeconds(180);

        /// <summary>
        /// You retrieve CasAuthentication properties in the constructor or else you will cause 
        /// a StackOverflow.  CasAuthentication.Initialize() will call Initialize() on all 
        /// relevant controls when its initialization is complete.  In Initialize(), you can 
        /// retrieve properties from CasAuthentication.
        /// </summary>
        public void Initialize() {
            // Do nothing
        }

        /// <summary>
        /// Removes expired PGTIOU-PGT from the ticket store
        /// </summary>
        public void RemoveExpiredMappings() {
            // No-op.  ASP.NET Cache provider removes expired entries automatically.
        }

        /// <summary>
        /// Method to save the ProxyGrantingTicket to the backing storage facility.
        /// </summary>
        /// <param name="proxyGrantingTicketIou">used as the key</param>
        /// <param name="proxyGrantingTicket">used as the value</param>
        public void InsertProxyGrantingTicketMapping(string proxyGrantingTicketIou, string proxyGrantingTicket) {
            _httpContextAccessor.Current().Cache.Insert(proxyGrantingTicketIou, proxyGrantingTicket, null, _clock.UtcNow.Add(DefaultExpiration), Cache.NoSlidingExpiration);
        }

        /// <summary>
        /// Method to retrieve a ProxyGrantingTicket based on the
        /// ProxyGrantingTicketIou.  Implementations are not guaranteed to
        /// return the same result if retieve is called twice with the same 
        /// proxyGrantingTicketIou.
        /// </summary>
        /// <param name="proxyGrantingTicketIou">used as the key</param>
        /// <returns>the ProxyGrantingTicket Id or null if it can't be found</returns>
        public string GetProxyGrantingTicket(string proxyGrantingTicketIou) {
            var context = _httpContextAccessor.Current();
            if (context.Cache[proxyGrantingTicketIou] != null && context.Cache[proxyGrantingTicketIou].ToString().Length > 0) {
                return context.Cache[proxyGrantingTicketIou].ToString();
            }

            return null;
        }

        public string Name {
            get { return "CacheProxyTicketManager"; }
        }
    }
}