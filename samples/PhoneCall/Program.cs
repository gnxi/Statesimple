using System;
using System.Linq;
using System.Threading.Tasks;

namespace PhoneCall
{
    class Program
    {
        static async Task Main(string[] args)
        {
            PhoneCall phoneCall = new PhoneCall("123456789");
            PhoneCallStateMachine machine = new PhoneCallStateMachine(phoneCall);

            (string cmd, CallEvent evt)[] commands = new[]
            {
                ("RF", CallEvent.CallerHookOff),
                ("RN", CallEvent.CallerHookOn),
                ("EF", CallEvent.CalleeHookOff),
                ("EN", CallEvent.CalleeHookOn)
            };

            Console.WriteLine("Commands:");
            foreach (var (cmd, evt) in commands)
                Console.WriteLine($"{cmd}={evt}");
            Console.WriteLine($"[0-9]={CallEvent.DigitsEntered}");

            string line;
            while (phoneCall.State != CallState.Disconnected && !string.IsNullOrEmpty(line = Console.ReadLine().ToUpper()))
            {
                if (commands.Any(x => x.cmd == line))
                    await machine.ProcessEventAsync(commands.First(x => x.cmd == line).evt);
                else if (line.All(c => char.IsDigit(c)))
                    await machine.ProcessEventAsync(CallEvent.DigitsEntered, line);
            }
        }
    }
}
