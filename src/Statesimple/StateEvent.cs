﻿// Copyright (c) Statesimple.com. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Statesimple
{
    interface IStateEvent<EVENT>
    {
        Func<Task> GetMatchingFunc(EVENT evt, object[] inParameters, Func<Type, object, object> typeConverter);
    }
    class StateEvent<EVENT> : IStateEvent<EVENT>
    {
        public EVENT Event { get; set; }
        public Func<Task> OnStateEventAsync { get; set; }
        public Func<Task> GetMatchingFunc(EVENT evt, object[] inParameters, Func<Type, object, object> typeConverter)
        {
            if (inParameters.Length != 0 || !EqualityComparer<EVENT>.Default.Equals(Event, evt))
                return null;

            return () => OnStateEventAsync();
        }
    }
    class StateEvent<EVENT, P1> : IStateEvent<EVENT>
    {
        static Type[] parameterTypes = new Type[] { typeof(P1) };
        public EVENT Event { get; set; }
        public Func<P1, Task> OnStateEventAsync { get; set; }
        public Func<Task> GetMatchingFunc(EVENT evt, object[] inParameters, Func<Type, object, object> typeConverter)
        {
            if (inParameters.Length != 1 || !EqualityComparer<EVENT>.Default.Equals(Event, evt))
                return null;

            object[] parameters = StateEventHelper.GetMatchingParameters(parameterTypes, inParameters, typeConverter);
            if (parameters == null)
                return null;

            return () => OnStateEventAsync((P1)parameters[0]);
        }
    }
    class StateEvent<EVENT, P1, P2> : IStateEvent<EVENT>
    {
        static Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2) };
        public EVENT Event { get; set; }
        public Func<P1, P2, Task> OnStateEventAsync { get; set; }
        public Func<Task> GetMatchingFunc(EVENT evt, object[] inParameters, Func<Type, object, object> typeConverter)
        {
            if (inParameters.Length != 2 || !EqualityComparer<EVENT>.Default.Equals(Event, evt))
                return null;

            object[] parameters = StateEventHelper.GetMatchingParameters(parameterTypes, inParameters, typeConverter);
            if (parameters == null)
                return null;

            return () => OnStateEventAsync((P1)parameters[0], (P2)parameters[1]);
        }
    }
    class StateEvent<EVENT, P1, P2, P3> : IStateEvent<EVENT>
    {
        static Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2), typeof(P3) };
        public EVENT Event { get; set; }
        public Func<P1, P2, P3, Task> OnStateEventAsync { get; set; }
        public Func<Task> GetMatchingFunc(EVENT evt, object[] inParameters, Func<Type, object, object> typeConverter)
        {
            if (inParameters.Length != 3 || !EqualityComparer<EVENT>.Default.Equals(Event, evt))
                return null;

            object[] parameters = StateEventHelper.GetMatchingParameters(parameterTypes, inParameters, typeConverter);
            if (parameters == null)
                return null;

            return () => OnStateEventAsync((P1)parameters[0], (P2)parameters[1], (P3)parameters[2]);
        }
    }
    static class StateEventHelper
    {
        public static object[] GetMatchingParameters(Type[] parameterTypes, object[] inParameters, Func<Type, object, object> typeConverter)
        {
            object[] outParameters = null;

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (parameterTypes[i].IsAssignableFrom(inParameters[i].GetType()))
                {
                    if (outParameters != null)
                        outParameters[i] = inParameters[i];
                    continue;
                }

                if (typeConverter == null)
                    return null;

                object obj = typeConverter(parameterTypes[i], inParameters[i]);
                if (obj == null)
                    return null;

                if (outParameters == null)
                {
                    outParameters = new object[inParameters.Length];
                    for (int j = i - 1; j >= 0; j--)
                        outParameters[j] = inParameters[j];
                }
                outParameters[i] = obj;
            }

            return outParameters ?? inParameters;
        }
    }
}
