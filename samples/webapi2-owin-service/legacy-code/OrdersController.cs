using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Contoso.OrderApi
{
    public class OrdersController : ApiController
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["OrderDB"].ConnectionString;

        // GET api/orders/5
        [HttpGet]
        [Route("api/orders/{id}")]
        public IHttpActionResult Get(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "SELECT OrderId, CustomerId, Total, Status FROM Orders WHERE OrderId = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return NotFound();
                        return Ok(new
                        {
                            OrderId = r.GetInt32(0),
                            CustomerId = r.GetInt32(1),
                            Total = r.GetDecimal(2),
                            Status = r.GetString(3)
                        });
                    }
                }
            }
        }

        // GET api/orders?customer=alice
        [HttpGet]
        [Route("api/orders")]
        public IHttpActionResult List(string customer)
        {
            var rows = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // SQL injection: customer concatenated into LIKE clause.
                var sql = "SELECT OrderId, CustomerId, Total FROM Orders " +
                          "WHERE CustomerName LIKE '%" + customer + "%'";
                using (var cmd = new SqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        rows.Add(new { OrderId = r.GetInt32(0), CustomerId = r.GetInt32(1), Total = r.GetDecimal(2) });
                    }
                }
            }
            return Ok(rows);
        }

        // POST api/orders/{id}/notify
        [HttpPost]
        [Route("api/orders/{id}/notify")]
        public IHttpActionResult NotifyDownstream(int id)
        {
            var url = ConfigurationManager.AppSettings["DownstreamUrl"];
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url + "?orderId=" + id);
                req.Method = "POST";
                req.ContentLength = 0;
                using (var resp = (HttpWebResponse)req.GetResponse())
                {
                    return Content((HttpStatusCode)resp.StatusCode, "ok");
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("OrderApi",
                    "Downstream notify failed for " + id + ": " + ex.Message,
                    EventLogEntryType.Error);
                return InternalServerError(ex);
            }
        }
    }
}
