using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using TelephoneCallExample;

namespace Orchestrator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PhoneCallsController : ControllerBase
    {
        private readonly ILogger<PhoneCallsController> _logger;

        private readonly DaprClient _daprClient;

        public PhoneCallsController(ILogger<PhoneCallsController> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;
        }

        [HttpGet(Name = "GetPhoneCallSaga")]
        public async Task<PhoneCallSagaTransaction> Get(Guid id)
        {
            return await _daprClient.GetStateAsync<PhoneCallSagaTransaction>(PhoneCallSaga.StoreNameSagas, id.ToString());
        }

        [Topic(pubsubName: "phonecallspubsub", name: "dial")]
        [HttpPost(Name = "StartPhoneCall")]
        public PhoneCallSagaTransaction Post([FromBody] StartPhoneCallDto startCall)
        {
            var phoneCall = new PhoneCall() { CallerNumber =  startCall.CallerNumber };
            var saga = new PhoneCallSaga(phoneCall, _daprClient);

            saga.Dial(startCall.ReceiverNumber);

            return saga.GetTransaction();
        }

        [Topic(pubsubName: "phonecallspubsub", name: "terminate")]
        [HttpDelete(Name = "TerminatePhoneCall")]
        public async Task Delete(Guid id)
        {
            var saga = await PhoneCallSaga.GetInstance(id, _daprClient);

            saga.Smash();
        }
    }
}