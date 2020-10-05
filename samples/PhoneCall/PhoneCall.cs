using System;

namespace PhoneCall
{
    enum CallState
    {
        Idle,
        Dialtone,
        Dialing,
        Ringing,
        Connected,
        Disconnected
    }
    enum CallEvent
    {
        CalleeHookOff,
        CalleeHookOn,
        CalleeNumberComplete,
        CallerHookOff,
        CallerHookOn,
        DigitsEntered,
    }
    class PhoneCall
    {
        public string Caller { get; set; }
        public string Callee { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime StopTime { get; set; }
        public CallState State { get; set; } = CallState.Idle;
        public TimeSpan Duration => StartTime != default ? StopTime - StartTime : TimeSpan.FromSeconds(0);
        public PhoneCall(string caller) => Caller = caller;
    }
}
