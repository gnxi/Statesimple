// Copyright (c) Statesimple.com. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Statesimple;
using System;
using System.Threading.Tasks;

namespace onoff
{
    class Program
    {
        static async Task Main()
        {
            string on = "On", off = "Off";
            char space = ' ';

            var machine = new StateMachine<string, char>(off);

            machine.Configure(off).EventTransitionTo(space, on);
            machine.Configure(on).EventTransitionTo(space, off);
            machine.OnUnhandledEvent((state, evt) => throw new Exception($"Cannot handle '{evt}' in state '{state}"));

            Console.WriteLine("Press <space> to toggle the switch and q to quit.");

            for(char c = Console.ReadKey(true).KeyChar; c != 'q'; c = Console.ReadKey(true).KeyChar)
            {
                try
                {
                    await machine.TriggerEventAsync(c);
                    Console.WriteLine("State is " + machine.State);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message);
                }
            }
        }
    }
}
