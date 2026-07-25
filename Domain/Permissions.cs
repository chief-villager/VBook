using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bookkeeping.Domain
{
    public static class Permissions
    {
        public static class Invoices
        {
            public const string Read = "Invoices.Read";
            public const string Create = "Invoices.Create";
            public const string Update = "Invoices.Update";
            public const string Delete = "Invoices.Delete";
        }

        public static class Users
        {
            public const string Read = "Users.Read";
            public const string Create = "Users.Create";
            public const string Delete = "Users.Delete";
        }
    }
}