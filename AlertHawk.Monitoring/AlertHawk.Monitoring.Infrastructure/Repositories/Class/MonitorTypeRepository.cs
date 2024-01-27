using System.Data;
using System.Data.SqlClient;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

public class MonitorTypeRepository : RepositoryBase, IMonitorTypeRepository
{
    private readonly string _connstring;

    public MonitorTypeRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task<IEnumerable<MonitorType>> GetMonitorType()
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"SELECT Id, Name FROM [MonitorType]";
        return await db.QueryAsync<MonitorType>(sql, commandType: CommandType.Text);
    }
}