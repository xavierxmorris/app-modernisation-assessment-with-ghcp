using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace Contoso.EmployeePortal
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadDepartments();
                Session["UserName"] = User?.Identity?.Name ?? "anonymous";
                BindEmployees(DepartmentDropDown.SelectedValue);
            }
        }

        private void LoadDepartments()
        {
            DepartmentDropDown.Items.Add(new ListItem("Engineering", "ENG"));
            DepartmentDropDown.Items.Add(new ListItem("Sales", "SAL"));
            DepartmentDropDown.Items.Add(new ListItem("HR", "HR"));
        }

        protected void DepartmentDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindEmployees(DepartmentDropDown.SelectedValue);
        }

        private void BindEmployees(string department)
        {
            DepartmentLabel.Text = department;

            var rows = new List<EmployeeRow>();
            var cs = ConfigurationManager.ConnectionStrings["HRDB"].ConnectionString;

            using (var conn = new SqlConnection(cs))
            {
                conn.Open();
                // Inline SQL — concatenates department string. Vulnerable.
                var sql = "SELECT EmployeeId, FullName, Email, Salary FROM Employees " +
                          "WHERE Department = '" + department + "'";
                using (var cmd = new SqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        rows.Add(new EmployeeRow
                        {
                            EmployeeId = r.GetInt32(0),
                            FullName = r.GetString(1),
                            Email = r.GetString(2),
                            Salary = r.GetDecimal(3)
                        });
                    }
                }
            }

            EmployeeGrid.DataSource = rows;
            EmployeeGrid.DataBind();
        }

        protected void EmployeeGrid_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var emp = (EmployeeRow)e.Row.DataItem;
                if (emp.Salary > 200000m)
                {
                    e.Row.BackColor = System.Drawing.Color.LightYellow;
                }
            }
        }

        protected void EmployeeGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ViewDetails")
            {
                var index = int.Parse((string)e.CommandArgument);
                var id = (int)EmployeeGrid.DataKeys[index].Value;
                Response.Redirect("EmployeeDetails.aspx?id=" + id);
            }
        }

        public class EmployeeRow
        {
            public int EmployeeId { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public decimal Salary { get; set; }
        }
    }
}
