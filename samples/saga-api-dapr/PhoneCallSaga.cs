using Dapr.Client;
using Stateless;
using Stateless.Graph;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

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
        DaprClient _daprClient;

        public const string StoreNameSagas = "saga";

        public const string StoreNamePhoneCalls = "phonecalls";

        public PhoneCallSaga(PhoneCallSagaTransaction transaction, PhoneCall phoneCall, DaprClient daprClient) : this(phoneCall, daprClient)
        {
            Id = transaction.Id;
            _state = transaction.State;
        }

        public PhoneCallSaga(PhoneCall phoneCall, DaprClient daprClient) : this(daprClient)
        {
            Model = phoneCall;
        }

        public static PhoneCallSaga GetInstance(string caller, DaprClient daprClient)
        {
            var phoneCall = new PhoneCall()
            {
                CallerNumber = caller
            };

            return new PhoneCallSaga(phoneCall, daprClient);
        }

        public static async Task<PhoneCallSaga> GetInstance(Guid id, DaprClient daprClient)
        {
            var transaction = await daprClient.GetStateAsync<PhoneCallSagaTransaction>(StoreNameSagas, id.ToString()) 
                ?? throw new KeyNotFoundException();

            var phoneCall = await daprClient.GetStateAsync<PhoneCall>(StoreNamePhoneCalls, id.ToString())
                ?? throw new KeyNotFoundException();

            return new PhoneCallSaga(transaction, phoneCall, daprClient);
        }

        private PhoneCallSaga(DaprClient daprClient)
        {
            _daprClient = daprClient;

            _machine = new StateMachine<State, Trigger>(() => _state, s => _state = s);

            _setVolumeTrigger = _machine.SetTriggerParameters<int>(Trigger.SetVolume);
            _setCalleeTrigger = _machine.SetTriggerParameters<string>(Trigger.CallDialed);

            _machine.Configure(State.OffHook)
                .Permit(Trigger.CallDialed, State.Ringing);

            _machine.Configure(State.Ringing)
                .OnEntryFrom(_setCalleeTrigger, async callee => await OnDialed(callee), "Caller number to call")
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

            _machine.OnTransitionedAsync(t => OnTransitionedAsync(t));
        }

        private async Task OnTransitionedAsync(StateMachine<State, Trigger>.Transition t)
        {
            Console.WriteLine($"OnTransitioned: {t.Source} -> {t.Destination} via {t.Trigger}({string.Join(", ", t.Parameters)})");
            //_daprClient.PublishEventAsync();
            await _daprClient.SaveStateAsync(StoreNameSagas, GetTransaction().Id.ToString(), GetTransaction());
        }

        public PhoneCallSagaTransaction GetTransaction()
        {
            return new PhoneCallSagaTransaction()
            {
                Id = Id,
                State = _state
            };
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

        async Task OnDialed(string receiverNumber)
        {
            Model.ReceiverNumber = receiverNumber;
            await _daprClient.SaveStateAsync(StoreNamePhoneCalls, Model.Id.ToString(), Model);
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