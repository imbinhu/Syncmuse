using System;
using System.Collections.Generic;
using System.Text;

namespace SyncMuseWPF.Model
{
    class Account
    {
        public string Domain { get; }
        public Account(string domain)
        {
            this.Domain = domain;
        }
    }
}
