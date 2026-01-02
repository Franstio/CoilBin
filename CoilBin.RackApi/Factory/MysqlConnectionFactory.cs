using MySql.Data.MySqlClient;

namespace CoilBin.RackApi.Factory
{
    public class MysqlConnectionFactory : IDBFactory<MySqlConnection>
    {
        private readonly string ConnectionString;
        public MysqlConnectionFactory(IConfiguration config)
        {
            ConnectionString = config.GetConnectionString("RackDb")!;
        }

        public MySqlConnection Create()
        {
            return new MySqlConnection(ConnectionString);
        }
    }
}
