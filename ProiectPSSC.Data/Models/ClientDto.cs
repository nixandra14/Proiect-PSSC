﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProiectPSSC.Data.Models
{
    public class ClientDto
    {
        public int ClientId { get; set; }
        public string ClientEmail { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string CardDetails { get; set; }
    }
}
