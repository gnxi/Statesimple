using Statesimple;
using System;
using System.Threading.Tasks;

namespace PhoneCall
{
    class PhoneCallStateMachine
    {
        const int _phoneNumberLength = 9;
        readonly StateMachine<CallState, CallEvent> _machine;
        public Task ProcessEventAsync(CallEvent evt, params object[] parameters) => _machine.ProcessEventAsync(evt, parameters);
        public PhoneCallStateMachine(PhoneCall phoneCall)
        {
            _machine = new StateMachine<CallState, CallEvent>(() => phoneCall.State, state => phoneCall.State = state);

            _machine.Configure(CallState.Idle)
                .EventTransitionTo(CallEvent.CallerHookOff, CallState.Dialtone)
            ;
            _machine.Configure(CallState.Dialtone)
                .OnEnter(() => Console.WriteLine($"{phoneCall.Caller}: dialtone"))
                .EventTransitionTo(CallEvent.DigitsEntered, CallState.Dialing)
                .EventTransitionTo(CallEvent.CallerHookOn, CallState.Disconnected)
            ;
            _machine.Configure(CallState.Dialing)
                .OnEnter<string>(CallEvent.DigitsEntered, async digits =>
                {
                    Console.WriteLine($"{phoneCall.Caller}: entered {digits}");
                    phoneCall.Callee += digits;
                    if (phoneCall.Callee.Length > _phoneNumberLength)
                        phoneCall.Callee = phoneCall.Callee.Substring(0, _phoneNumberLength);
                    if (phoneCall.Callee.Length == _phoneNumberLength)
                        await _machine.ProcessEventAsync(CallEvent.CalleeNumberComplete);
                })
                .EventTransitionToSelf(CallEvent.DigitsEntered)
                .EventTransitionTo(CallEvent.CalleeNumberComplete, CallState.Ringing)
                .EventTransitionTo(CallEvent.CallerHookOn, CallState.Disconnected)
            ;
            _machine.Configure(CallState.Ringing)
                .OnEnter(() => Console.WriteLine($"{phoneCall.Caller}: ringing {phoneCall.Callee}"))
                .EventTransitionTo(CallEvent.CallerHookOn, CallState.Disconnected)
                .EventTransitionTo(CallEvent.CalleeHookOff, CallState.Connected)
            ;
            _machine.Configure(CallState.Connected)
                .OnEnter(() => 
                {
                    Console.WriteLine($"{phoneCall.Caller}: connected to {phoneCall.Callee}");
                    phoneCall.StartTime = DateTime.Now;
                })
                .OnExit(() => phoneCall.StopTime = DateTime.Now)
                .EventTransitionTo(CallEvent.CallerHookOn, CallState.Disconnected)
                .EventTransitionTo(CallEvent.CalleeHookOn, CallState.Disconnected)
            ;
            _machine.Configure(CallState.Disconnected)
                .OnEnter(CallEvent.CalleeHookOn, () => Console.WriteLine($"{phoneCall.Caller}: disconnected from {phoneCall.Callee} callee onhook duration {phoneCall.Duration.TotalSeconds} sec"))
                .OnEnter(CallEvent.CallerHookOn, () => Console.WriteLine($"{phoneCall.Caller}: disconnected from {phoneCall.Callee} caller onhook duration {phoneCall.Duration.TotalSeconds} sec"))
            ;

            _machine.OnUnhandledEvent((evt, state) => Console.WriteLine($"{phoneCall.Caller}: cannot handle {evt} in state {state}"));
        }
    }
}
