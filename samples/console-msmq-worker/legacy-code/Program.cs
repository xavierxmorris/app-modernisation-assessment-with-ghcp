using System;
using System.Configuration;
using System.Threading;

namespace Contoso.QueueWorker
{
    internal static class Program
    {
        private static readonly ManualResetEventSlim _shutdown = new ManualResetEventSlim(false);

        private static int Main(string[] args)
        {
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                _shutdown.Set();
            };

            var queuePath = ConfigurationManager.AppSettings["MsmqPath"]
                ?? @".\private$\order-events";
            var dbConn = ConfigurationManager.ConnectionStrings["OrderDB"].ConnectionString;

            Console.WriteLine("[QueueWorker] Starting. Queue=" + queuePath);

            var processor = new QueueProcessor(queuePath, dbConn);

            while (!_shutdown.IsSet)
            {
                try
                {
                    processor.PumpOnce();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("[QueueWorker] Pump error: " + ex);
                    Thread.Sleep(2000);
                }
            }

            processor.Shutdown();
            Console.WriteLine("[QueueWorker] Stopped.");
            return 0;
        }
    }
}
