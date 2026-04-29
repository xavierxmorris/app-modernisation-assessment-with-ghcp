using System;
using System.Web;

namespace Contoso.EmployeePortal
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // Route registration would go here.
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            HttpContext.Current.Session["StartedUtc"] = DateTime.UtcNow;
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var ex = Server.GetLastError();
            System.Diagnostics.EventLog.WriteEntry("EmployeePortal",
                "Unhandled error: " + ex,
                System.Diagnostics.EventLogEntryType.Error);
        }
    }
}
