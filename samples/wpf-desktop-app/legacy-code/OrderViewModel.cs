using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;

namespace Contoso.OrderClient
{
    public class OrderViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<OrderRow> Orders { get; } = new ObservableCollection<OrderRow>();

        private string _status = "Idle";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public void LoadOrdersSync()
        {
            Status = "Loading...";
            Orders.Clear();
            var url = App.ApiBaseUrl + "/orders";
            var apiKey = ConfigurationManager.AppSettings["ApiKey"];

            try
            {
                // Synchronous WebRequest on the UI thread — freezes the window.
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Headers["X-Api-Key"] = apiKey;
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var reader = new StreamReader(resp.GetResponseStream()))
                {
                    var body = reader.ReadToEnd();
                    // Real code would JSON-parse here.
                    Orders.Add(new OrderRow { OrderId = 1, Total = 99.95m, Status = body.Length > 0 ? "OK" : "EMPTY" });
                }
                Status = "Loaded " + Orders.Count + " orders";
            }
            catch (Exception ex)
            {
                Status = "Error: " + ex.Message;
                System.Diagnostics.EventLog.WriteEntry("OrderClient",
                    "Load failed: " + ex,
                    System.Diagnostics.EventLogEntryType.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class OrderRow
    {
        public int OrderId { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
    }
}
