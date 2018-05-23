// Copyright (c) Statesimple.com. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Statesimple
{
    interface IStateEvent<EVENT>
    {
        bool IsMatch(EVENT evt, object[] parameters);
        Task ProcessAsync(object[] parameters);
    }
    class StateEvent<EVENT> : IStateEvent<EVENT>
    {
        public EVENT Event { get; set; }
        public Func<Task> OnStateEventAsync { get; set; }
        public bool IsMatch(EVENT evt, object[] parameters)
        {
            return EqualityComparer<EVENT>.Default.Equals(Event, evt) &&
                    parameters.Length == 0;
        }
        public async Task ProcessAsync(object[] parameters)
        {
            await OnStateEventAsync();
        }
    }
    class StateEvent<EVENT, P1> : IStateEvent<EVENT>
    {
        public EVENT Event { get; set; }
        public Func<P1, Task> OnStateEventAsync { get; set; }
        public bool IsMatch(EVENT evt, object[] parameters)
        {
            return EqualityComparer<EVENT>.Default.Equals(Event, evt) &&
                    parameters.Length == 1 &&
                    typeof(P1).IsAssignableFrom(parameters[0].GetType());
        }
        public async Task ProcessAsync(object[] parameters)
        {
            await OnStateEventAsync((P1)parameters[0]);
        }
    }
    class StateEvent<EVENT, P1, P2> : IStateEvent<EVENT>
    {
        public EVENT Event { get; set; }
        public Func<P1, P2, Task> OnStateEventAsync { get; set; }
        public bool IsMatch(EVENT evt, object[] parameters)
        {
            return EqualityComparer<EVENT>.Default.Equals(Event, evt) &&
                    parameters.Length == 2 &&
                    typeof(P1).IsAssignableFrom(parameters[0].GetType()) &&
                    typeof(P2).IsAssignableFrom(parameters[1].GetType());
        }
        public async Task ProcessAsync(object[] parameters)
        {
            await OnStateEventAsync((P1)parameters[0], (P2)parameters[1]);
        }
    }
    class StateEvent<EVENT, P1, P2, P3> : IStateEvent<EVENT>
    {
        public EVENT Event { get; set; }
        public Func<P1, P2, P3, Task> OnStateEventAsync { get; set; }
        public bool IsMatch(EVENT evt, object[] parameters)
        {
            return EqualityComparer<EVENT>.Default.Equals(Event, evt) &&
                    parameters.Length == 3 &&
                    typeof(P1).IsAssignableFrom(parameters[0].GetType()) &&
                    typeof(P2).IsAssignableFrom(parameters[1].GetType()) &&
                    typeof(P3).IsAssignableFrom(parameters[2].GetType());
        }
        public async Task ProcessAsync(object[] parameters)
        {
            await OnStateEventAsync((P1)parameters[0], (P2)parameters[1], (P3)parameters[2]);
        }
    }
}
