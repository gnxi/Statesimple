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

            var machine = new StateMachine<string, char>(off);

            machine.Configure(off).EventTransitionTo(' ', on).OnEnter(() => Console.WriteLine("State is on"));
            machine.Configure(on).EventTransitionTo(' ', off).OnEnter(() => Console.WriteLine("State is off"));
            machine.OnUnhandledEvent((state, evt) => {});

            Console.WriteLine("Press <space> to toggle the switch and q to quit.");

            for(char c = Console.ReadKey(true).KeyChar; c != 'q'; c = Console.ReadKey(true).KeyChar)
            {
                await machine.TriggerEventAsync(c);
            }
        }
    }
}
