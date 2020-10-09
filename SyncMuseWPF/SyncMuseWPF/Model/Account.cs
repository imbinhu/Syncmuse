using System;
using System.Collections.Generic;
using System.Text;

namespace SyncMuseWPF.Model
{
    class Account
    {
        public string Domain { get; }
        public ApiMethods Api { get; }
        public Account(string domain, ApiMethods api)
        {
            this.Domain = domain;
            this.Api = api;
        }
    }
}
