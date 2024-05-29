using Dapper;
using System.Data;
using static Dapper.SqlMapper;
using System.Data.SqlClient;
using MySqlConnector;
using MyHub.Controllers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace MyHub
{
    //17-05-2024 by Periya Samy P CHC1761
    public class DapperSql
    {

        #region Insert Query
        public static async Task<int> Execute(string sql, object parameters = null)
        {
            using (IDbConnection dbConnection = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                  return await  dbConnection.ExecuteAsync(sql, parameters);
            }
        }
        #endregion

        #region Select Query
        public static async Task<IEnumerable<T>> SelectQuery<T>(string sql, object parameters = null )
        {
            using (IDbConnection dbConnection = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                return await dbConnection.QueryAsync<T>(sql, parameters);
            }
        }
        #endregion

        #region Select 2 Table
        public async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>> SelectQuery<T1,T2>(string sql, object parameters = null )
        {
            using (IDbConnection dbConnection = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                using (var result = await dbConnection.QueryMultipleAsync(sql, parameters))
                {
                    var result1 = result.Read<T1>();
                    var result2 = result.Read<T2>();
                    return Tuple.Create(result1,result2);
                }
            }
        }
        #endregion

        #region Select With Count
        public async Task<Tuple<long, IEnumerable<dynamic>>> 
            SelectQueryWithCount<T1, T2>(string sql, object parameters = null )
        {
            using (IDbConnection dbConnection = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                using (var result = await dbConnection.QueryMultipleAsync(sql, parameters))
                {
                    var TotalCount = (long)result.Read<dynamic>().SingleOrDefault();
                    var dataList = result.Read<dynamic>();
                    return Tuple.Create(TotalCount, dataList);
                }
            }
        }
        #endregion

        #region Stored Procudure
        public async Task<Tuple<long, IEnumerable<dynamic>>>  ExecuteProcedureWithCount<T1, T2>
            (string ProcedureName, object parameters = null , CommandType commandType = CommandType.StoredProcedure)
        {
            using (IDbConnection dbConnection = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                using (var result = await dbConnection.QueryMultipleAsync(ProcedureName, parameters, commandType: commandType))
                {
                    var totalCount = result.Read<long>().SingleOrDefault();
                    var dataList = result.Read<dynamic>();
                    return Tuple.Create(totalCount, dataList);
                }
            }
        }
        #endregion


        public async Task<IEnumerable<T>> ExecuteProcedure<T>
           (string ProcedureName, object parameters = null , CommandType commandType = CommandType.StoredProcedure)
        {
            using (IDbConnection dbConnection = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                return await dbConnection.QueryAsync<T>(ProcedureName, parameters, commandType: commandType);
            }
        }
        public async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>>  ExecuteProcedure<T1, T2>
            (string ProcedureName, object parameters = null , CommandType commandType = CommandType.StoredProcedure)
        {
            using (IDbConnection dbConnection = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                using (var result = await dbConnection.QueryMultipleAsync(ProcedureName, parameters, commandType: commandType))
                {
                    var result1 = result.Read<T1>();
                    var result2 = result.Read<T2>();
                    return Tuple.Create(result1, result2);
                }
            }
        }
        public async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>  ExecuteProcedure<T1, T2, T3>
            (string ProcedureName, object parameters = null , CommandType commandType = CommandType.StoredProcedure)
        {
            using (IDbConnection dbConnection = new MySqlConnection(MyGlobal.GetConnectionString()))
            {
                using (var result = await dbConnection.QueryMultipleAsync(ProcedureName, parameters, commandType: commandType))
                {
                    var result1 = result.Read<T1>();
                    var result2 = result.Read<T2>();
                    var result3 = result.Read<T3>();
                    return Tuple.Create(result1, result2,result3);
                }
            }
        }
       
    }
}
