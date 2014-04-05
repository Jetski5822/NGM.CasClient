using Orchard.Data.Migration;

namespace NGM.CasClient {
    public class UsersDataMigration : DataMigrationImpl {
        public int Create() {
            SchemaBuilder.CreateTable("CASSettingsPartRecord",
                table => table
                    .ContentPartRecord()
                    .Column<bool>("ProcessIncomingSingleSignOutRequests", c => c.WithDefault(true))
                    .Column<string>("ProxyCallbackParameterName", c => c.WithDefault("proxyResponse"))
                    .Column<string>("GatewayParameterName", c => c.WithDefault("gatewayResponse"))
                    .Column<string>("ArtifactParameterName", c => c.WithDefault("ticket"))
                    .Column<string>("GatewayStatusCookieName", c => c.WithDefault("cas_gateway_status"))
                    .Column<string>("TicketValidatorName", c => c.WithDefault("Cas10"))
                    .Column<string>("CasServerUrlPrefix")
                    .Column<string>("CookiesRequiredUrl")
                    .Column<bool>("Gateway")
                    .Column<bool>("Renew")
                    .Column<string>("FormsLoginUrl")
                    .Column<string>("NotAuthorizedUrl")
                    .Column<string>("ServiceTicketManager")
                    .Column<string>("ProxyTicketManager")
                    .Column<long>("TicketTimeTolerance", c => c.WithDefault(long.Parse("5000")))
                );

            return 1;
        }
    }
}