using System.Collections.Generic;
using System.ServiceModel;

namespace Contoso.Billing
{
    [ServiceContract(Namespace = "http://contoso.com/billing/v1")]
    public interface IBillingService
    {
        [OperationContract]
        InvoiceResult CreateInvoice(int customerId, decimal amount, string currency);

        [OperationContract]
        InvoiceResult GetInvoice(int invoiceId);

        [OperationContract]
        List<InvoiceResult> ListInvoicesForCustomer(int customerId);

        [OperationContract]
        bool VoidInvoice(int invoiceId, string reason);
    }

    [DataContract]
    public class InvoiceResult
    {
        [DataMember] public int InvoiceId { get; set; }
        [DataMember] public int CustomerId { get; set; }
        [DataMember] public decimal Amount { get; set; }
        [DataMember] public string Currency { get; set; }
        [DataMember] public string Status { get; set; }
        [DataMember] public System.DateTime CreatedUtc { get; set; }
    }
}
