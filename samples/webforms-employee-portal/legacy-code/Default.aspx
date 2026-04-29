<%@ Page Language="C#" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs"
    Inherits="Contoso.EmployeePortal.Default" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Contoso Employee Portal</title>
</head>
<body>
    <form id="form1" runat="server">
        <h1>Employees (<asp:Literal ID="DepartmentLabel" runat="server" />)</h1>

        <asp:DropDownList ID="DepartmentDropDown" runat="server"
                          AutoPostBack="true"
                          OnSelectedIndexChanged="DepartmentDropDown_SelectedIndexChanged" />

        <asp:GridView ID="EmployeeGrid" runat="server"
                      AutoGenerateColumns="false"
                      DataKeyNames="EmployeeId"
                      OnRowDataBound="EmployeeGrid_RowDataBound"
                      OnRowCommand="EmployeeGrid_RowCommand">
            <Columns>
                <asp:BoundField DataField="EmployeeId" HeaderText="ID" />
                <asp:BoundField DataField="FullName"   HeaderText="Name" />
                <asp:BoundField DataField="Email"      HeaderText="Email" />
                <asp:BoundField DataField="Salary"     HeaderText="Salary" DataFormatString="{0:C}" />
                <asp:ButtonField CommandName="ViewDetails" ButtonType="Link" Text="Details" />
            </Columns>
        </asp:GridView>

        <asp:Label ID="StatusLabel" runat="server" />
    </form>
</body>
</html>
