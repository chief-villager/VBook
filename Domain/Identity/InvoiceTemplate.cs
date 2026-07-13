using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bookkeeping.Domain.Common;

namespace Bookkeeping.Domain.Identity
{
    public sealed class InvoiceTemplate
    {
        public BusinessId BusinessId { get; private set; }
        public string LogoUrl { get; private set; } = string.Empty;
        public string BusinessName { get; private set; } = string.Empty;
        public string AccountNumber { get; private set; } = string.Empty;
        public string BankName { get; private set; } = string.Empty;
        public string Terms { get; private set; } = string.Empty;

        private InvoiceTemplate() { } // EF

       
        internal InvoiceTemplate(BusinessId businessId, string logoUrl, string businessName,
        string accountNumber, string bankName, string terms)
        {
            BusinessId = businessId;
            LogoUrl = logoUrl;
            BusinessName = businessName;
            AccountNumber = accountNumber;
            BankName = bankName;
            Terms = terms;
        }


    }
}