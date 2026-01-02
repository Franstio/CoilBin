using CoilBin.RackApi.Factory;
using CoilBin.RackApi.Models;
using Dapper;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Bcpg;
using System.Data;

namespace CoilBin.RackApi.Services
{
    public class RackSaleableServices
    {
        private readonly IDBFactory<MySqlConnection> dbFactory;
        private readonly string TimbanganURL;
        private readonly IHttpClientFactory clientFactory;
        public RackSaleableServices(IDBFactory<MySqlConnection> _dbFactory,IConfiguration config,IHttpClientFactory factoryHttp)
        {
            dbFactory = _dbFactory;
            TimbanganURL = config.GetSection("Timbangan").Value!;
            clientFactory = factoryHttp;
        }

        public async Task<IEnumerable<RackModel>> GetRackList()
        {
            using (IDbConnection dbConnection = dbFactory.Create())
            {
                dbConnection.Open();

                string query = "Select rackId,name,clientId,weight,weightbin,wasteId,address,value,max_weight,line,sensor from rack";
                return await dbConnection.QueryAsync<RackModel>(query);
            }
        }

        public async Task<bool> Login(string password)
        {
            using (IDbConnection dbConnection = dbFactory.Create())
            {
                dbConnection.Open();

                string query = "Select name from admin where password=@password";
                var data=  await dbConnection.QueryAsync<AdminModel>(query,new {password});
                return data.Any();
            }
        }

        public async Task<WasteModel?> GetWaste(string name)
        {
            using (IDbConnection dbConnection = dbFactory.Create())
            {
                dbConnection.Open();
                string query = "Select wasteId,name from waste where name=@name";
                return await dbConnection.QueryFirstOrDefaultAsync<WasteModel>(query, new { name });
            }
        }
        public async Task<ContainerModel?> GetContainer(string name)
        {
            using (IDbConnection dbConnection = dbFactory.Create())
            {
                dbConnection.Open();
                string query = "Select containerId,name,station,IdWaste,weightbin,status,type,line  from container where name=@name";
                return await dbConnection.QueryFirstOrDefaultAsync<ContainerModel>(query, new { name });
            }
        }
        public async Task<RackModel?> GetRack(string search)
        {
            using (IDbConnection dbConnection = dbFactory.Create())
            {
                dbConnection.Open();
                string query = "Select rackId,name,clientId,weight,weightbin,wasteId,address,value,max_weight,line,sensor from rack where name=@search or line=@search";
                return await dbConnection.QueryFirstOrDefaultAsync<RackModel>(query, new { search });
            }
        }
        public async Task<RackModel?> GetRack(int id)
        {
            using (IDbConnection dbConnection = dbFactory.Create())
            {
                dbConnection.Open();
                string query = "Select rackId,name,clientId,weight,weightbin,wasteId,address,value,max_weight,line,sensor from rack where rackId=@id";
                return await dbConnection.QueryFirstOrDefaultAsync<RackModel>(query, new { id });
            }
        }
        public async Task<IEnumerable<RackModel>> GetRacks(string search)
        {
            using (IDbConnection dbConnection = dbFactory.Create())
            {
                dbConnection.Open();
                string query = "Select rackId,name,clientId,weight,weightbin,wasteId,address,value,max_weight,line,sensor from rack where name=@search or line=@search";
                return await dbConnection.QueryAsync<RackModel>(query, new { search });
            }
        }
        public async Task<TransactionModel> SaveTransaksi(TransactionModel model)
        {
            using (IDbConnection dbConnection = dbFactory.Create())
            {
                dbConnection.Open();
                string query = "Insert into transaction(id,badgeId,idContainer,idWaste,type,weight,recordDate,idqrmachine) values(@id,@badgeId,@idContainer,@idWaste,@type,@weight,@recordDate,@idqrmachine)";
                var res = await dbConnection.ExecuteAsync(query, model);
                return model;
            }
        }
        public async Task<EmployeeTimbanganModel> SaveEmployee(EmployeeTimbanganModel model)
        {
            using (IDbConnection dbConnection = dbFactory.Create())
            {
                dbConnection.Open();
                string query = "Insert into employee(badgeId,username,active) values(@badgeno,@employeename,1)";
                var res = await dbConnection.ExecuteAsync(query, model);
                return model;
            }
        }
        public async Task UpdateRackWeight(int rackId, decimal weight)
        {
            using (IDbConnection dbConnection = dbFactory.Create())
            {
                dbConnection.Open();
                string query = "update rack set weight=@weight where rackId=@rackId";
                var res = await dbConnection.ExecuteAsync(query, new {rackId,weight});
            }
        }
        public async Task<IEnumerable<EmployeeModel>> GetEmployees()
        {
            using (IDbConnection dbConnection = dbFactory.Create())
            {
                dbConnection.Open();
                string query = "Select badgeId,username,active from employee";
                var res = await dbConnection.QueryAsync<EmployeeModel>(query);
                return res;
            }
        }

        public async Task SyncEmployeeTimbangan()
        {
            using (var client = clientFactory.CreateClient())
            {
                IEnumerable<EmployeeTimbanganModel> data = [];
                try
                {
                    var emp = await GetEmployees();
                    UriBuilder uri = new UriBuilder(TimbanganURL);
                    uri.Path = "/employee";
                    var res = await client.GetFromJsonAsync<EmployeeTimbanganModel[]>(uri.ToString());
                    data = res!.Where(x=>!emp.Any(z=>z.badgeId==x.badgeno));
                    foreach (var empData in data!)
                    {
                        try
                        {
                            await SaveEmployee(empData);
                        }
                        catch { }
                    }
                }
                catch (Exception e)
                {
                    
                }
            }
        }

    }
}
