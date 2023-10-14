using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelephoneCallExample;

namespace Orchestrator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PhoneCallsController : ControllerBase
    {
        private readonly ILogger<PhoneCallsController> _logger;

        private readonly AppDbContext _dbContext;

        public PhoneCallsController(ILogger<PhoneCallsController> logger, AppDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpGet(Name = "GetPhoneCallSaga")]
        public PhoneCallSagaTransaction Get(Guid id)
        {
            return _dbContext.Sagas.Find(id)!;
        }

        [HttpPost(Name = "StartPhoneCall")]
        public PhoneCallSagaTransaction Post(StartPhoneCallDto startCall)
        {
            var phoneCall = new PhoneCallSaga(startCall.CallerName, _dbContext);

            phoneCall.Dial(startCall.ReceiverNumber);

            return phoneCall.GetTransaction();
        }

        [HttpDelete(Name = "TerminatePhoneCall")]
        public void Delete(Guid id)
        {
            var phoneCall = new PhoneCallSaga(id, _dbContext);

            phoneCall.Smash();
        }
    }
}