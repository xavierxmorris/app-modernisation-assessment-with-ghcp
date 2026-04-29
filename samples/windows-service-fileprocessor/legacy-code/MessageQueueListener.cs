using System;
using System.Configuration;
using System.Diagnostics;
using System.Messaging;
using System.Threading;

namespace Contoso.FileProcessor
{
    public class MessageQueueListener
    {
        private readonly string _queuePath =
            ConfigurationManager.AppSettings["MsmqPath"] ?? @".\private$\fileprocessor";
        private MessageQueue _queue;
        private Thread _worker;
        private volatile bool _running;

        public void Start()
        {
            if (!MessageQueue.Exists(_queuePath))
            {
                MessageQueue.Create(_queuePath, transactional: true);
            }

            _queue = new MessageQueue(_queuePath);
            _queue.Formatter = new XmlMessageFormatter(new[] { typeof(string) });

            _running = true;
            _worker = new Thread(Loop) { IsBackground = true };
            _worker.Start();
        }

        public void Stop()
        {
            _running = false;
            try { _queue?.Close(); } catch { /* swallow */ }
        }

        private void Loop()
        {
            while (_running)
            {
                try
                {
                    using (var tx = new MessageQueueTransaction())
                    {
                        tx.Begin();
                        var msg = _queue.Receive(TimeSpan.FromSeconds(5), tx);
                        var body = (string)msg.Body;
                        EventLog.WriteEntry("FileProcessor",
                            "Received MSMQ message: " + body,
                            EventLogEntryType.Information);
                        tx.Commit();
                    }
                }
                catch (MessageQueueException mqex)
                    when (mqex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    // expected when queue is idle
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry("FileProcessor",
                        "MSMQ loop error: " + ex,
                        EventLogEntryType.Error);
                    Thread.Sleep(2000);
                }
            }
        }
    }
}
