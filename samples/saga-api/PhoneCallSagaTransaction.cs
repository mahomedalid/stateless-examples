using Stateless;
using Stateless.Graph;
using System.ComponentModel.DataAnnotations;

namespace TelephoneCallExample
{
    public class PhoneCallSagaTransaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public PhoneCallSaga.State State { get; set; }
   }
}