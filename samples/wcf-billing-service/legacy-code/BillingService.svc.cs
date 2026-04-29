using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.Mail;
using System.ServiceModel;

namespace Contoso.Billing
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class BillingService : IBillingService
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["BillingDB"].ConnectionString;

        private readonly string _smtpHost =
            ConfigurationManager.AppSettings["SmtpHost"];

        public InvoiceResult CreateInvoice(int customerId, decimal amount, string currency)
        {
            // Inline SQL — vulnerable, also tightly coupled to SqlClient.
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var sql = "INSERT INTO Invoices (CustomerId, Amount, Currency, Status, CreatedUtc) " +
                          "OUTPUT INSERTED.InvoiceId " +
                          "VALUES (@cid, @amt, '" + currency + "', 'OPEN', GETUTCDATE())";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@cid", customerId);
                    cmd.Parameters.AddWithValue("@amt", amount);
                    var id = (int)cmd.ExecuteScalar();
                    NotifyByEmail(customerId, id, amount, currency);
                    return new InvoiceResult
                    {
                        InvoiceId = id,
                        CustomerId = customerId,
                        Amount = amount,
                        Currency = currency,
                        Status = "OPEN",
                        CreatedUtc = DateTime.UtcNow
                    };
                }
            }
        }

        public InvoiceResult GetInvoice(int invoiceId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "SELECT InvoiceId, CustomerId, Amount, Currency, Status, CreatedUtc " +
                    "FROM Invoices WHERE InvoiceId = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", invoiceId);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        return new InvoiceResult
                        {
                            InvoiceId = r.GetInt32(0),
                            CustomerId = r.GetInt32(1),
                            Amount = r.GetDecimal(2),
                            Currency = r.GetString(3),
                            Status = r.GetString(4),
                            CreatedUtc = r.GetDateTime(5)
                        };
                    }
                }
            }
        }

        public List<InvoiceResult> ListInvoicesForCustomer(int customerId)
        {
            var list = new List<InvoiceResult>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "SELECT InvoiceId, CustomerId, Amount, Currency, Status, CreatedUtc " +
                    "FROM Invoices WHERE CustomerId = @cid ORDER BY CreatedUtc DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@cid", customerId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new InvoiceResult
                            {
                                InvoiceId = r.GetInt32(0),
                                CustomerId = r.GetInt32(1),
                                Amount = r.GetDecimal(2),
                                Currency = r.GetString(3),
                                Status = r.GetString(4),
                                CreatedUtc = r.GetDateTime(5)
                            });
                        }
                    }
                }
            }
            return list;
        }

        public bool VoidInvoice(int invoiceId, string reason)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "UPDATE Invoices SET Status='VOID', VoidReason=@r WHERE InvoiceId=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", invoiceId);
                    cmd.Parameters.AddWithValue("@r", reason);
                    var rows = cmd.ExecuteNonQuery();
                    if (rows == 0) return false;
                    EventLog.WriteEntry("BillingService",
                        "Voided invoice " + invoiceId + " reason=" + reason,
                        EventLogEntryType.Information);
                    return true;
                }
            }
        }

        private void NotifyByEmail(int customerId, int invoiceId, decimal amount, string currency)
        {
            try
            {
                using (var client = new SmtpClient(_smtpHost, 25))
                {
                    var msg = new MailMessage(
                        "billing@contoso.com",
                        "customer-" + customerId + "@example.com",
                        "New invoice #" + invoiceId,
                        "Amount: " + amount + " " + currency);
                    client.Send(msg);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("BillingService",
                    "SMTP failed for invoice " + invoiceId + ": " + ex.Message,
                    EventLogEntryType.Error);
            }
        }
    }
}
