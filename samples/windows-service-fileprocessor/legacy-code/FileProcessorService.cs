using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Timers;

namespace Contoso.FileProcessor
{
    public class FileProcessorService : ServiceBase
    {
        private Timer _timer;
        private MessageQueueListener _queueListener;
        private readonly string _inboxPath = ConfigurationManager.AppSettings["InboxPath"];
        private readonly string _archivePath = ConfigurationManager.AppSettings["ArchivePath"];
        private readonly int _pollSeconds =
            int.Parse(ConfigurationManager.AppSettings["PollSeconds"] ?? "30");

        public FileProcessorService()
        {
            ServiceName = "Contoso.FileProcessor";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            EventLog.WriteEntry("FileProcessor", "Service starting. Inbox=" + _inboxPath,
                EventLogEntryType.Information);

            Directory.CreateDirectory(_archivePath);

            _queueListener = new MessageQueueListener();
            _queueListener.Start();

            _timer = new Timer(_pollSeconds * 1000);
            _timer.Elapsed += (s, e) => ScanInbox();
            _timer.Start();
        }

        protected override void OnStop()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _queueListener?.Stop();
            EventLog.WriteEntry("FileProcessor", "Service stopped.",
                EventLogEntryType.Information);
        }

        private void ScanInbox()
        {
            try
            {
                foreach (var file in Directory.GetFiles(_inboxPath, "*.csv"))
                {
                    ProcessFile(file);
                    var dest = Path.Combine(_archivePath, Path.GetFileName(file));
                    File.Move(file, dest);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("FileProcessor",
                    "Inbox scan failed: " + ex,
                    EventLogEntryType.Error);
            }
        }

        private void ProcessFile(string path)
        {
            // Synchronous I/O on the timer thread; no cancellation; no retry.
            var lines = File.ReadAllLines(path);
            EventLog.WriteEntry("FileProcessor",
                "Processed " + lines.Length + " rows from " + Path.GetFileName(path),
                EventLogEntryType.Information);
        }
    }
}
