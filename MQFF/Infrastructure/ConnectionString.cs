using sourcelist.Infrastructure;

namespace sourcelist.Provider
{
    public class ConnectionString : IConnectionString
    {
        private readonly IConfiguration _configuration;
        public ConnectionString(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection") ?? "";
        }
    }
}
