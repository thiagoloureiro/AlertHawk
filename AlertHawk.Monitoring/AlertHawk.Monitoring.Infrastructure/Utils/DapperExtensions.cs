using Dapper;
using Microsoft.Data.SqlClient;
using Polly;
using Polly.Retry;
using System.ComponentModel;
using System.Data;

namespace AlertHawk.Monitoring.Infrastructure.Utils
{
    public static class DapperExtensions
    {
        private static readonly IEnumerable<TimeSpan> RetryTimes =
        [
            TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(3)
        ];

        private static readonly AsyncRetryPolicy RetryPolicy = Policy
                                                         .Handle<SqlException>(SqlServerTransientExceptionDetector.ShouldRetryOn)
                                                         .Or<TimeoutException>()
                                                         .OrInner<Win32Exception>(SqlServerTransientExceptionDetector.ShouldRetryOn)
                                                         .WaitAndRetryAsync(RetryTimes,
                                                                        (exception, timeSpan, retryCount, context) =>
                                                                        {
                                                                            Console.WriteLine("WARNING: Error talking to the Database, will retry after {RetryTimeSpan}. Retry attempt {RetryCount}");
                                                                            //LogTo.Warning(
                                                                            //    exception,
                                                                            //    "WARNING: Error talking to ReportingDb, will retry after {RetryTimeSpan}. Retry attempt {RetryCount}",
                                                                            //    timeSpan,
                                                                            //    retryCount
                                                                            // );
                                                                        });

        public static async Task<int> ExecuteAsyncWithRetry(this IDbConnection cnn, string sql, object param = null,
                                                            IDbTransaction transaction = null, int? commandTimeout = null,
                                                            CommandType? commandType = null) =>
            await RetryPolicy.ExecuteAsync(async () => await cnn.ExecuteAsync(sql, param, transaction, commandTimeout, commandType));

        public static async Task<IEnumerable<T>> QueryAsyncWithRetry<T>(this IDbConnection cnn, string sql, object param = null,
                                                                        IDbTransaction transaction = null, int? commandTimeout = null,
                                                                        CommandType? commandType = null) =>
            await RetryPolicy.ExecuteAsync(async () => await cnn.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType));
    }
}