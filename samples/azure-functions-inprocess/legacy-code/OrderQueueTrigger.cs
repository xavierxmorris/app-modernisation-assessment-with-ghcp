using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Contoso.OrderFunctions
{
    public class OrderQueueTrigger
    {
        private readonly IConfiguration _config;

        public OrderQueueTrigger(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("ProcessOrderEvent")]
        public async Task Run(
            [ServiceBusTrigger("order-events", Connection = "ServiceBusConnection")]
            string messageBody,
            ILogger log)
        {
            log.LogInformation("Received message: {Body}", messageBody);

            // .NET Framework-style synchronous DB write embedded in async method.
            using (var conn = new System.Data.SqlClient.SqlConnection(
                _config.GetConnectionString("OrderDB")))
            {
                conn.Open();
                using (var cmd = new System.Data.SqlClient.SqlCommand(
                    "INSERT INTO OrderEvents (Payload, ReceivedUtc) VALUES (@p, GETUTCDATE())", conn))
                {
                    cmd.Parameters.AddWithValue("@p", messageBody);
                    cmd.ExecuteNonQuery();
                }
            }

            await Task.CompletedTask;
        }

        [FunctionName("RetryDeadLetter")]
        public void RetryDeadLetter(
            [TimerTrigger("0 */5 * * * *")] TimerInfo timer,
            ILogger log)
        {
            log.LogInformation("Dead-letter retry sweep at {When}", DateTime.UtcNow);
            // Implementation omitted for brevity.
        }
    }
}
