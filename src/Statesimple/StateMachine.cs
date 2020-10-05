// Copyright (c) Statesimple.com. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Statesimple
{
    public class StateMachine<STATE, EVENT>
    {
        readonly Func<Task<STATE>> _loadStateAsync;
        readonly Func<STATE, Task> _saveStateAsync;
        readonly Dictionary<STATE, StateConfiguration<STATE, EVENT>> _states = new Dictionary<STATE, StateConfiguration<STATE, EVENT>>();
        readonly ConcurrentQueue<Func<Task>> _taskList = new ConcurrentQueue<Func<Task>>();
        List<Func<STATE, STATE, EVENT, object[], Task>> _transitionCallbacks;
        Func<Task> _onIntializeAsync = () => Task.CompletedTask;
        Func<EVENT,STATE, Task> _onUnhandledEventAsync;
        Func<Type, object, object> _typeConverter;
        public StateMachine(STATE state) :this(() => state, (newState) => state = newState)
        {
        }
        public StateMachine(Func<Task<STATE>> loadStateAsync, Func<STATE, Task> saveStateAsync)
        {
            _loadStateAsync = loadStateAsync;
            _saveStateAsync = saveStateAsync;
        }
        public StateMachine(Func<STATE> loadState, Action<STATE> saveState) : this(() => { return Task.FromResult(loadState()); }, (state) => { saveState(state); return Task.CompletedTask; }) { }
        public STATE State => _loadStateAsync().Result;
        public bool IsSuperStateOf(STATE superState, STATE subState)
        {
            object state = _states[subState].SuperState;
            while (state != null)
            {
                if (EqualityComparer<STATE>.Default.Equals(superState, (STATE)state))
                    return true;
                state = _states[(STATE)state].SuperState;
            }
            return false;
        }
        public bool IsInState(STATE state)
        {
            if (EqualityComparer<STATE>.Default.Equals(State, state))
                return true;
            return IsSuperStateOf(state, State);
        }
        public IEnumerable<EVENT> PermittedTriggers => _states[State].PermittedEvents;
        public void IgnoreUnhandledEvent()
        {
            _onUnhandledEventAsync = (a, s) => Task.CompletedTask;
        }
        public void OnTypeConversion(Func<Type, object, object> typeConverter)
        {
            _typeConverter = typeConverter;
        }
        public void OnUnhandledEvent(Func<EVENT, STATE, Task> func)
        {
            _onUnhandledEventAsync = func;
        }
        public void OnUnhandledEvent(Action<EVENT, STATE> action)
        {
            OnUnhandledEvent((evt,state) => { action(evt, state); return Task.CompletedTask; });
        }
        public void OnInitialize(Func<Task> func)
        {
            _onIntializeAsync = func;
        }
        public void OnInitialize(Action action)
        {
            OnInitialize(() => { action(); return Task.CompletedTask; });
        }
        public StateConfiguration<STATE, EVENT> Configure(STATE state)
        {
            if (_states.ContainsKey(state))
                return _states[state];

            StateConfiguration<STATE, EVENT> stateConfiguration = new StateConfiguration<STATE, EVENT>(state);
            _states.Add(state, stateConfiguration);
            return stateConfiguration;
        }
        public async Task ProcessEventAsync(EVENT evt, params object[] parameters)
        {
            lock (_taskList)
            {
                _taskList.Enqueue(() => ProcessEventCoreAsync(evt, parameters));
                if (_taskList.Count > 1)
                    return;
            }

            while (_taskList.TryPeek(out Func<Task> func))
            {
                try
                {
                    await func();
                }
                finally
                {
                    _taskList.TryDequeue(out _);
                }
            }
        }
        Func<Task> GetNextStateHandler(STATE currentState, EVENT evt, object[] parameter, out STATE nextState)
        {
            object superStateObject;

            do
            {
                try
                {
                    foreach(STATE state in _states[currentState].GetNextStates(evt))
                    {
                        Func<Task> handlerFunc = _states[state].GetMatchingHandler(evt, parameter);
                        if(handlerFunc == null && parameter.Length > 0 && _typeConverter != null)
                            handlerFunc = _states[state].GetMatchingHandler(evt, parameter, _typeConverter);
                        if (handlerFunc == null)
                            handlerFunc = _states[state].GetDefaultHandler();
                        if (handlerFunc != null)
                        {
                            nextState = state;
                            return handlerFunc;
                        }
                    }
                }
                catch (Exception e) when (e.Message == "guarded")
                {
                    // retry in superstate
                    continue;
                }
                catch (Exception e) when (e.Message == "ignore")
                {
                    throw;
                }
                catch (NotSupportedException)
                {
                    // retry in superstate
                    continue;
                }
            }
            while ((superStateObject = _states[currentState].SuperState) != null && (currentState = (STATE)superStateObject) != null);

            throw new Exception("unhandled");
        }
        async Task ProcessEventCoreAsync(EVENT evt, params object[] parameters)
        {
            STATE currentState = await _loadStateAsync();
            if (!_states.ContainsKey(currentState))
                throw new NotSupportedException($"State {currentState} is not a valid state");

            STATE nextState;
            Func<Task> handlerFunc = null;

            try
            {
                handlerFunc = GetNextStateHandler(currentState, evt, parameters, out nextState);
            }
            catch (Exception e) when (e.Message == "unhandled")
            {
                if (_onUnhandledEventAsync == null)
                    throw;
                await _onUnhandledEventAsync(evt, currentState);
                return;
            }
            catch (Exception e) when (e.Message == "ignore")
            {
                return;
            }

            if (!_states.ContainsKey(nextState))
                throw new NotSupportedException($"State {nextState} is not a valid state");

            // call HandleActivateAsync and HandleDeactivateAsync once
            bool nextStateEqualsCurrentState = EqualityComparer<STATE>.Default.Equals(nextState, currentState);
            
            // do not call HandleActivateAsync, HandleEventAsync and HandleDeactivateAsync for nextState
            bool nextStateIsSuperState = !nextStateEqualsCurrentState && IsSuperStateOf(nextState, currentState);
            
            // do not call HandleActivateAsync, HandleExitAsync and HandleDeactivateAsync for currentState
            bool currentStateIsSuperState = !nextStateEqualsCurrentState && IsSuperStateOf(currentState, nextState);

            if (_states[currentState].HasOnExit && !currentStateIsSuperState)
            {
                await _states[currentState].HandleActivateAsync();
                await _states[currentState].HandleExitAsync();
                if (!nextStateEqualsCurrentState)
                {
                    await _states[currentState].HandleDeactivateAsync();
                    if (!nextStateIsSuperState)
                        await _states[nextState].HandleActivateAsync();
                }
            }
            else if (!nextStateIsSuperState)
            {              
                await _states[nextState].HandleActivateAsync();
            }

            if (!nextStateIsSuperState)
            {
                await handlerFunc();
                await _states[nextState].HandleDeactivateAsync();
            }

            await _saveStateAsync(nextState);

            _transitionCallbacks?.ForEach(func => func(currentState, nextState, evt, parameters));
        }
        public void OnTransitioned(Action<STATE, STATE, EVENT, object[]> onTransitionAction)
        {
            (_transitionCallbacks ??= new List<Func<STATE, STATE, EVENT, object[], Task>>())
                .Add((from, to, evt, parameters) => { onTransitionAction(from, to, evt, parameters); return Task.CompletedTask; });
        }
        public void OnTransitioned(Func<STATE, STATE, EVENT, object[], Task> onTransitionAction)
        {
            (_transitionCallbacks ??= new List<Func<STATE, STATE, EVENT, object[], Task>>())
                .Add(onTransitionAction);
        }
    }
}
