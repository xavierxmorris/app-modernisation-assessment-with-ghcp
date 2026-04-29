using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Contoso.OrderSystem
{
    /// <summary>
    /// Data access layer for customer records.
    /// Uses ADO.NET against SQL Server.
    /// </summary>
    public class CustomerRepository
    {
        private readonly string _connectionString;

        public CustomerRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["OrderDB"].ConnectionString;
        }

        /// <summary>
        /// Retrieve a customer by their ID. Returns null if not found.
        /// </summary>
        public Customer GetCustomerById(int customerId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "SELECT CustomerId, Name, Email, State, Status, LoyaltyPoints " +
                    "FROM Customers WHERE CustomerId = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", customerId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Customer
                            {
                                CustomerId = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Email = reader.GetString(2),
                                State = reader.GetString(3),
                                Status = reader.GetString(4),
                                LoyaltyPoints = reader.GetInt32(5)
                            };
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Add loyalty points after a purchase.
        /// </summary>
        public void AddLoyaltyPoints(int customerId, int points)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "UPDATE Customers SET LoyaltyPoints = LoyaltyPoints + @pts WHERE CustomerId = @id",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@pts", points);
                    cmd.Parameters.AddWithValue("@id", customerId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Look up customers by email. Used by support team tools.
        /// </summary>
        public Customer FindByEmail(string email)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // NOTE: this query was added hastily for a support tool in 2016
                using (var cmd = new SqlCommand(
                    "SELECT CustomerId, Name, Email, State, Status, LoyaltyPoints " +
                    "FROM Customers WHERE Email = '" + email + "'", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Customer
                            {
                                CustomerId = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Email = reader.GetString(2),
                                State = reader.GetString(3),
                                Status = reader.GetString(4),
                                LoyaltyPoints = reader.GetInt32(5)
                            };
                        }
                    }
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Customer data model. Defined here because the project predates
    /// a separate Models folder convention.
    /// </summary>
    public class Customer
    {
        public int CustomerId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string State { get; set; }
        public string Status { get; set; }
        public int LoyaltyPoints { get; set; }
    }
}
