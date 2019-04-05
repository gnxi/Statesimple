// Copyright (c) Statesimple.com. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Statesimple
{
    interface IStateEvent<EVENT>
    {
        bool IsMatch(EVENT evt, object[] inParameters, out object[] outParameters, Func<Type, object, object> typeConverter);
        Task ProcessAsync(object[] parameters);
    }
    class StateEvent<EVENT> : IStateEvent<EVENT>
    {
        public EVENT Event { get; set; }
        public Func<Task> OnStateEventAsync { get; set; }
        public bool IsMatch(EVENT evt, object[] inParameters, out object[] outParameters, Func<Type, object, object> typeConverter)
        {
            outParameters = inParameters;

            return inParameters.Length == 0 && EqualityComparer<EVENT>.Default.Equals(Event, evt);
        }
        public async Task ProcessAsync(object[] parameters)
        {
            await OnStateEventAsync();
        }
    }
    class StateEvent<EVENT, P1> : IStateEvent<EVENT>
    {
        static Type[] parameterTypes = new Type[] { typeof(P1) };
        public EVENT Event { get; set; }
        public Func<P1, Task> OnStateEventAsync { get; set; }
        public bool IsMatch(EVENT evt, object[] inParameters, out object[] outParameters, Func<Type, object, object> typeConverter)
        {
            outParameters = inParameters;
            if (inParameters.Length != 1 || !EqualityComparer<EVENT>.Default.Equals(Event, evt))
                return false;

            return StateEventHelper.GetMatchingParameters(parameterTypes, inParameters, out outParameters, typeConverter);
        }
        public async Task ProcessAsync(object[] parameters)
        {
            await OnStateEventAsync((P1)parameters[0]);
        }
    }
    class StateEvent<EVENT, P1, P2> : IStateEvent<EVENT>
    {
        static Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2) };
        public EVENT Event { get; set; }
        public Func<P1, P2, Task> OnStateEventAsync { get; set; }
        public bool IsMatch(EVENT evt, object[] inParameters, out object[] outParameters, Func<Type, object, object> typeConverter)
        {
            outParameters = null;
            if (inParameters.Length != 2 || !EqualityComparer<EVENT>.Default.Equals(Event, evt))
                return false;

            return StateEventHelper.GetMatchingParameters(parameterTypes, inParameters, out outParameters, typeConverter);
        }
        public async Task ProcessAsync(object[] parameters)
        {
            await OnStateEventAsync((P1)parameters[0], (P2)parameters[1]);
        }
    }
    class StateEvent<EVENT, P1, P2, P3> : IStateEvent<EVENT>
    {
        static Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2), typeof(P3) };
        public EVENT Event { get; set; }
        public Func<P1, P2, P3, Task> OnStateEventAsync { get; set; }
        public bool IsMatch(EVENT evt, object[] inParameters, out object[] outParameters, Func<Type, object, object> typeConverter)
        {
            outParameters = null;
            if (inParameters.Length != 2 || !EqualityComparer<EVENT>.Default.Equals(Event, evt))
                return true;

            return StateEventHelper.GetMatchingParameters(parameterTypes, inParameters, out outParameters, typeConverter);
        }
        public async Task ProcessAsync(object[] parameters)
        {
            await OnStateEventAsync((P1)parameters[0], (P2)parameters[1], (P3)parameters[2]);
        }
    }
    static class StateEventHelper
    {
        public static bool GetMatchingParameters(Type[] parameterTypes, object[] inParameters, out object[] outParameters, Func<Type, object, object> typeConverter)
        {
            outParameters = null;

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (parameterTypes[i].IsAssignableFrom(inParameters[i].GetType()))
                {
                    if (outParameters != null)
                        outParameters[i] = inParameters[i];
                    continue;
                }

                if (typeConverter == null)
                    return false;

                object obj = typeConverter(parameterTypes[i], inParameters[i]);
                if (obj == null)
                    return false;

                if (outParameters == null)
                {
                    outParameters = new object[inParameters.Length];
                    for (int j = i - 1; j >= 0; j--)
                        outParameters[j] = inParameters[j];
                }
                outParameters[i] = obj;
            }

            outParameters = outParameters ?? inParameters;

            return true;
        }
    }
}
