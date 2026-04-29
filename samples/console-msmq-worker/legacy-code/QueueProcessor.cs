using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Messaging;

namespace Contoso.QueueWorker
{
    public class QueueProcessor
    {
        private readonly string _queuePath;
        private readonly string _dbConn;
        private readonly MessageQueue _queue;

        public QueueProcessor(string queuePath, string dbConn)
        {
            _queuePath = queuePath;
            _dbConn = dbConn;

            if (!MessageQueue.Exists(_queuePath))
            {
                MessageQueue.Create(_queuePath, transactional: true);
            }

            _queue = new MessageQueue(_queuePath);
            _queue.Formatter = new XmlMessageFormatter(new[] { typeof(OrderEvent) });
        }

        public void PumpOnce()
        {
            using (var tx = new MessageQueueTransaction())
            {
                tx.Begin();
                Message msg;
                try
                {
                    msg = _queue.Receive(TimeSpan.FromSeconds(5), tx);
                }
                catch (MessageQueueException mq)
                    when (mq.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    tx.Abort();
                    return;
                }

                var evt = (OrderEvent)msg.Body;
                PersistOrderEvent(evt);
                tx.Commit();
            }
        }

        private void PersistOrderEvent(OrderEvent evt)
        {
            using (var conn = new SqlConnection(_dbConn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "INSERT INTO OrderEvents (OrderId, EventType, OccurredUtc) " +
                    "VALUES (@id, @type, @t)", conn))
                {
                    cmd.Parameters.AddWithValue("@id", evt.OrderId);
                    cmd.Parameters.AddWithValue("@type", evt.EventType);
                    cmd.Parameters.AddWithValue("@t", evt.OccurredUtc);
                    cmd.ExecuteNonQuery();
                }
            }
            EventLog.WriteEntry("QueueWorker",
                "Persisted " + evt.EventType + " for order " + evt.OrderId,
                EventLogEntryType.Information);
        }

        public void Shutdown()
        {
            try { _queue?.Close(); } catch { /* swallow */ }
        }
    }

    [Serializable]
    public class OrderEvent
    {
        public int OrderId { get; set; }
        public string EventType { get; set; }
        public DateTime OccurredUtc { get; set; }
    }
}
