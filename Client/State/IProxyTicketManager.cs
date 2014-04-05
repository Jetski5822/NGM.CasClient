using Orchard;

namespace NGM.CasClient.Client.State {
    ///<summary>
    /// Defines the interface for a ProxyTicketManager implementation.  ProxyTicketManagers
    /// are responsible for temporary storage of state information relating to Proxy Tickets.
    /// For active-active clustered/web-farm configurations, the state must be stored in a 
    /// persistent storage mechanism that is accessible from any node or server that handles
    /// web requests.
    ///</summary>
    /// <author>Scott Holodak</author>
    public interface IProxyTicketManager : IDependency {
        /// <summary>
        /// You retrieve CasAuthentication properties in the constructor or else you will cause 
        /// a StackOverflow.  CasAuthentication.Initialize() will call Initialize() on all 
        /// relevant controls when its initialization is complete.  In Initialize(), you can 
        /// retrieve properties from CasAuthentication.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Removes expired PGTIOU-PGT from the ticket store
        /// </summary>
        void RemoveExpiredMappings();

        /// <summary>
        /// Method to save the ProxyGrantingTicket to the backing storage facility.
        /// </summary>
        /// <param name="proxyGrantingTicketIou">used as the key</param>
        /// <param name="proxyGrantingTicket">used as the value</param>
        void InsertProxyGrantingTicketMapping(string proxyGrantingTicketIou, string proxyGrantingTicket);

        /// <summary>
        /// Method to retrieve a ProxyGrantingTicket based on the
        /// ProxyGrantingTicketIou.  Implementations are not guaranteed to
        /// return the same result if retieve is called twice with the same 
        /// proxyGrantingTicketIou.
        /// </summary>
        /// <param name="proxyGrantingTicketIou">used as the key</param>
        /// <returns>the ProxyGrantingTicket Id or null if it can't be found</returns>
        string GetProxyGrantingTicket(string proxyGrantingTicketIou);
    }
}