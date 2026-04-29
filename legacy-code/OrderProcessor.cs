using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Net.Mail;

namespace Contoso.OrderSystem
{
    /// <summary>
    /// Central order processing class. Handles the full order lifecycle
    /// from validation through fulfillment and notification.
    /// 
    /// Originally written for .NET Framework 4.5 (circa 2014).
    /// Last major update: 2018 — added loyalty points calculation.
    /// </summary>
    public class OrderProcessor
    {
        private readonly string _connectionString;

        public OrderProcessor()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["OrderDB"].ConnectionString;
        }

        /// <summary>
        /// Process a new customer order end-to-end.
        /// Returns the order ID on success, -1 on failure.
        /// </summary>
        public int ProcessOrder(int customerId, string productCode, int quantity, string paymentToken)
        {
            // Step 1: Validate the customer exists and is active
            var custRepo = new CustomerRepository();
            var customer = custRepo.GetCustomerById(customerId);

            if (customer == null)
            {
                LogError("Customer not found: " + customerId);
                return -1;
            }

            if (customer.Status != "Active")
            {
                LogError("Customer is not active: " + customerId + ", status: " + customer.Status);
                return -1;
            }

            // Step 2: Look up product price and check stock
            decimal unitPrice = 0;
            int stockCount = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "SELECT Price, StockCount FROM Products WHERE ProductCode = @code", conn))
                {
                    cmd.Parameters.AddWithValue("@code", productCode);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            unitPrice = reader.GetDecimal(0);
                            stockCount = reader.GetInt32(1);
                        }
                        else
                        {
                            LogError("Product not found: " + productCode);
                            return -1;
                        }
                    }
                }
            }

            if (quantity > stockCount)
            {
                LogError("Insufficient stock for " + productCode +
                         ". Requested: " + quantity + ", Available: " + stockCount);
                return -1;
            }

            // Step 3: Calculate total with tax
            decimal subtotal = unitPrice * quantity;
            decimal taxRate = GetTaxRate(customer.State);
            decimal tax = subtotal * taxRate;
            decimal total = subtotal + tax;

            // Step 4: Apply loyalty discount (added 2018)
            if (customer.LoyaltyPoints > 500)
            {
                decimal discount = Math.Min(total * 0.10m, 50.00m);
                total = total - discount;
            }

            // Step 5: Process payment
            var paymentSvc = new PaymentService();
            bool paymentOk = paymentSvc.ChargeCustomer(customer.Email, total, paymentToken);

            if (!paymentOk)
            {
                LogError("Payment failed for customer " + customerId + ", amount: " + total);
                return -1;
            }

            // Step 6: Create the order record and reduce stock
            int orderId = -1;
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Insert order
                        using (var cmd = new SqlCommand(
                            "INSERT INTO Orders (CustomerId, ProductCode, Quantity, Total, Tax, OrderDate, Status) " +
                            "VALUES (@cid, @prod, @qty, @total, @tax, @date, 'Confirmed'); " +
                            "SELECT SCOPE_IDENTITY();", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@cid", customerId);
                            cmd.Parameters.AddWithValue("@prod", productCode);
                            cmd.Parameters.AddWithValue("@qty", quantity);
                            cmd.Parameters.AddWithValue("@total", total);
                            cmd.Parameters.AddWithValue("@tax", tax);
                            cmd.Parameters.AddWithValue("@date", DateTime.Now);
                            orderId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // Reduce stock
                        using (var cmd = new SqlCommand(
                            "UPDATE Products SET StockCount = StockCount - @qty WHERE ProductCode = @code",
                            conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@qty", quantity);
                            cmd.Parameters.AddWithValue("@code", productCode);
                            cmd.ExecuteNonQuery();
                        }

                        // Add loyalty points
                        int pointsEarned = (int)(total / 10);
                        custRepo.AddLoyaltyPoints(customerId, pointsEarned);

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        LogError("Order creation failed: " + ex.Message);
                        return -1;
                    }
                }
            }

            // Step 7: Send confirmation email
            try
            {
                SendConfirmationEmail(customer.Email, customer.Name, orderId, total);
            }
            catch (Exception ex)
            {
                // Email failure doesn't roll back the order
                LogError("Confirmation email failed for order " + orderId + ": " + ex.Message);
            }

            return orderId;
        }

        private decimal GetTaxRate(string state)
        {
            // Simplified tax lookup — production would use a tax service
            switch (state)
            {
                case "CA": return 0.0725m;
                case "NY": return 0.08m;
                case "TX": return 0.0625m;
                default: return 0.05m;
            }
        }

        private void SendConfirmationEmail(string email, string name, int orderId, decimal total)
        {
            string smtpHost = ConfigurationManager.AppSettings["SmtpHost"];
            int smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
            string fromAddress = ConfigurationManager.AppSettings["FromEmail"];

            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                var message = new MailMessage(fromAddress, email)
                {
                    Subject = "Order Confirmation #" + orderId,
                    Body = "Dear " + name + ",\n\n" +
                           "Your order #" + orderId + " for $" + total.ToString("F2") +
                           " has been confirmed.\n\nThank you for your business!"
                };
                client.Send(message);
            }
        }

        private void LogError(string message)
        {
            // Writes to Windows Event Log — legacy pattern
            System.Diagnostics.EventLog.WriteEntry("OrderSystem", message,
                System.Diagnostics.EventLogEntryType.Error);
        }
    }
}
