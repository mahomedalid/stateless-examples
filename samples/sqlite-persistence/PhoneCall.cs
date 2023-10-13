using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Stateless;
using Stateless.Graph;

namespace TelephoneCallExample
{
    public class PhoneCall
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? CallerName { get; set; }
        public string? CallerNumber { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverNumber { get; set; }
        public DateTime CallStartTime { get; set; } = DateTime.Now;
        public TimeSpan CallDuration { get; set; }
        public bool IsMissedCall { get; set; }
    }
}