using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;

namespace Contoso.OrderSystem
{
    /// <summary>
    /// Handles payment processing through the QuickPay gateway.
    /// Originally integrated in 2015.
    /// </summary>
    public class PaymentService
    {
        /// <summary>
        /// Charge a customer via the external payment gateway.
        /// Returns true if the charge succeeded.
        /// </summary>
        public bool ChargeCustomer(string customerEmail, decimal amount, string paymentToken)
        {
            string gatewayUrl = ConfigurationManager.AppSettings["PaymentGatewayUrl"];
            string merchantId = ConfigurationManager.AppSettings["MerchantId"];
            string merchantSecret = ConfigurationManager.AppSettings["MerchantSecret"];

            // Build the POST body expected by QuickPay v2 API
            string postData = string.Format(
                "merchant_id={0}&secret={1}&email={2}&amount={3}&token={4}&currency=USD",
                merchantId,
                merchantSecret,
                Uri.EscapeDataString(customerEmail),
                amount.ToString("F2"),
                Uri.EscapeDataString(paymentToken));

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(gatewayUrl);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Timeout = 30000; // 30 seconds

                byte[] data = Encoding.UTF8.GetBytes(postData);
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (var reader = new StreamReader(response.GetResponseStream()))
                        {
                            string body = reader.ReadToEnd();
                            // QuickPay returns "APPROVED" or "DECLINED:reason"
                            return body.StartsWith("APPROVED");
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                System.Diagnostics.EventLog.WriteEntry("OrderSystem",
                    "Payment gateway error: " + ex.Message,
                    System.Diagnostics.EventLogEntryType.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.EventLog.WriteEntry("OrderSystem",
                    "Unexpected payment error: " + ex.Message,
                    System.Diagnostics.EventLogEntryType.Error);
            }

            return false;
        }

        /// <summary>
        /// Issue a refund for a previous charge. Added in 2019.
        /// </summary>
        public bool RefundCharge(string transactionId, decimal amount)
        {
            string gatewayUrl = ConfigurationManager.AppSettings["PaymentGatewayUrl"] + "/refund";
            string merchantId = ConfigurationManager.AppSettings["MerchantId"];
            string merchantSecret = ConfigurationManager.AppSettings["MerchantSecret"];

            string postData = string.Format(
                "merchant_id={0}&secret={1}&transaction_id={2}&amount={3}",
                merchantId, merchantSecret, transactionId, amount.ToString("F2"));

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(gatewayUrl);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Timeout = 30000;

                byte[] data = Encoding.UTF8.GetBytes(postData);
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        string body = reader.ReadToEnd();
                        return body.StartsWith("REFUNDED");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.EventLog.WriteEntry("OrderSystem",
                    "Refund error: " + ex.Message,
                    System.Diagnostics.EventLogEntryType.Error);
            }

            return false;
        }
    }
}
