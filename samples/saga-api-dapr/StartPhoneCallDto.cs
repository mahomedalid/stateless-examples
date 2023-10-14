using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Stateless;
using Stateless.Graph;

namespace TelephoneCallExample
{
    public class StartPhoneCallDto
    {
        public string CallerName { get; set; }
        public string CallerNumber { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverNumber { get; set; }
    }
}