using Stateless;
using Stateless.Graph;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TelephoneCallExample
{
    public class PhoneCallSaga
    {
        enum Trigger
        {
            CallDialed,
            CallConnected,
            LeftMessage,
            PlacedOnHold,
            TakenOffHold,
            PhoneHurledAgainstWall,
            MuteMicrophone,
            UnmuteMicrophone,
            SetVolume
        }

        public enum State
        {
            OffHook,
            Ringing,
            Connected,
            OnHold,
            PhoneDestroyed
        }

        State _state = State.OffHook;

        Guid Id { get; } = Guid.NewGuid();

        StateMachine<State, Trigger> _machine;
        StateMachine<State, Trigger>.TriggerWithParameters<int> _setVolumeTrigger;

        StateMachine<State, Trigger>.TriggerWithParameters<string> _setCalleeTrigger;

        public PhoneCall Model { get; internal set; } = new PhoneCall();
        AppDbContext _dbContext;

        public PhoneCallSaga(Guid id, AppDbContext dbContext) : this(dbContext)
        {
            // updating saga
            var existingRecord = dbContext.Sagas.Find(id) ?? throw new KeyNotFoundException();

            Id = id;
            _state = existingRecord.State;

            var phoneCall = dbContext.PhoneCalls.Where(c => c.CorrelationId == id).FirstOrDefault() ?? throw new KeyNotFoundException();
            
            Init(phoneCall);
        }

        public PhoneCallSaga(string caller, AppDbContext dbContext) : this(dbContext)
        {
            var phoneCall = new PhoneCall()
            {
                CallerNumber = caller
            };

            Init(phoneCall);
        }

        public void Init(PhoneCall phoneCall)
        {
            Model = phoneCall;
            Model.CorrelationId = Id;

            StoreState();
        }

        private PhoneCallSaga(AppDbContext dbContext)
        {
            _dbContext = dbContext;

            _machine = new StateMachine<State, Trigger>(() => _state, s => _state = s);

            _setVolumeTrigger = _machine.SetTriggerParameters<int>(Trigger.SetVolume);
            _setCalleeTrigger = _machine.SetTriggerParameters<string>(Trigger.CallDialed);

            _machine.Configure(State.OffHook)
                .Permit(Trigger.CallDialed, State.Ringing);

            _machine.Configure(State.Ringing)
                .OnEntryFrom(_setCalleeTrigger, callee => OnDialed(callee), "Caller number to call")
                .Permit(Trigger.CallConnected, State.Connected)
                .Permit(Trigger.PhoneHurledAgainstWall, State.PhoneDestroyed);

            _machine.Configure(State.Connected)
                .OnEntry(t => StartCallTimer())
                .OnExit(t => StopCallTimer())
                .InternalTransition(Trigger.MuteMicrophone, t => OnMute())
                .InternalTransition(Trigger.UnmuteMicrophone, t => OnUnmute())
                .InternalTransition<int>(_setVolumeTrigger, (volume, t) => OnSetVolume(volume))
                .Permit(Trigger.LeftMessage, State.OffHook)
                .Permit(Trigger.PlacedOnHold, State.OnHold)
                .Permit(Trigger.PhoneHurledAgainstWall, State.PhoneDestroyed);

            _machine.Configure(State.OnHold)
                .SubstateOf(State.Connected)
                .Permit(Trigger.TakenOffHold, State.Connected)
                .Permit(Trigger.PhoneHurledAgainstWall, State.PhoneDestroyed);

            _machine.OnTransitioned(t => OnTransitioned(t));
        }

        private void OnTransitioned(StateMachine<State, Trigger>.Transition t)
        {
            Console.WriteLine($"OnTransitioned: {t.Source} -> {t.Destination} via {t.Trigger}({string.Join(", ", t.Parameters)})");
            StoreState();
        }

        public PhoneCallSagaTransaction GetTransaction()
        {
            return new PhoneCallSagaTransaction()
            {
                Id = Id,
                State = _state
            };
        }

        void StoreModel()
        {
            Console.WriteLine($"Storing model {Model.Id}");

            var existingPhoneCall = _dbContext.PhoneCalls.Find(Model.Id);

            if (existingPhoneCall == null)
            {
                // Insert a new record
                _dbContext.PhoneCalls.Add(Model);
            }
            else
            {
                // Update the existing record
                _dbContext.Entry(existingPhoneCall).CurrentValues.SetValues(Model);
            }

            _dbContext.SaveChanges();
        }

        void StoreState()
        {
            // updating saga
            var transaction = GetTransaction();
            var existingRecord = _dbContext.Sagas.Find(transaction.Id);

            if (existingRecord == null)
            {
                // Insert a new record
                _dbContext.Sagas.Add(transaction);
            }
            else
            {
                // Update the existing record
                _dbContext.Entry(existingRecord).CurrentValues.SetValues(transaction);
            }

            _dbContext.SaveChanges();
        }

        void OnSetVolume(int volume)
        {
            Console.WriteLine("Volume set to " + volume + "!");
        }

        void OnUnmute()
        {
            Console.WriteLine("Microphone unmuted!");
        }

        void OnMute()
        {
            Console.WriteLine("Microphone muted!");
        }

        void OnDialed(string receiverNumber)
        {
            Model.ReceiverNumber = receiverNumber;
            StoreModel();
            Console.WriteLine("[Phone Call] placed for : [{0}]", Model.ReceiverNumber);
        }

        void StartCallTimer()
        {
            Console.WriteLine("[Timer:] Call started at {0}", DateTime.Now);
        }

        void StopCallTimer()
        {
            Console.WriteLine("[Timer:] Call ended at {0}", DateTime.Now);
        }

        public void Mute()
        {
            _machine.Fire(Trigger.MuteMicrophone);
        }

        public void Unmute()
        {
            _machine.Fire(Trigger.UnmuteMicrophone);
        }

        public void SetVolume(int volume)
        {
            _machine.Fire(_setVolumeTrigger, volume);
        }

        public void Print()
        {
            Console.WriteLine("[{1}] placed call and [Status:] {0}", _machine.State, Model.CallerNumber);
        }

        public void Dial(string receiver)
        {
            _machine.Fire(_setCalleeTrigger, receiver);
        }

        public void Connected()
        {
            _machine.Fire(Trigger.CallConnected);
        }

        public void Hold()
        {
            _machine.Fire(Trigger.PlacedOnHold);
        }

        public void Resume()
        {
            _machine.Fire(Trigger.TakenOffHold);
        }

        public void Smash()
        {
            _machine.Fire(Trigger.PhoneHurledAgainstWall);
        }

        public string ToDotGraph()
        {
            return UmlDotGraph.Format(_machine.GetInfo());
        }
    }
}