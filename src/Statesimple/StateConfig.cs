// Copyright (c) Statesimple.com. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Statesimple
{
    public class StateConfiguration<STATE, EVENT>
    {
        class EventConfiguration
        {
            public EVENT Evt { get; set; }
            public STATE State { get; set; }
            public Func<bool> Guard { get; set; }
            public bool Ignore { get; set; }
        }
        static Func<bool> _defaultGuard = () => true;
        readonly STATE _state;
        readonly List<EventConfiguration> _stateTransitions = new List<EventConfiguration>();
        readonly List<IStateEvent<EVENT>> _entries = new List<IStateEvent<EVENT>>();
        Func<Task> _onEnterAsync;
        Func<Task> _onExitAsync;
        Func<Task> _onActivateAsync;
        Func<Task> _onDeactivateAsync;
        public object SuperState { get; private set; }
        public IEnumerable<EVENT> PermittedEvents => _stateTransitions.Select(x=>x.Evt);
        public StateConfiguration(STATE state)
        {
            _state = state;
        }
        public StateConfiguration<STATE, EVENT> SubstateOf(STATE superState)
        {
            SuperState = superState;
            return this;
        }
        public StateConfiguration<STATE,EVENT> OnActivate(Func<Task> func)
        {
            _onActivateAsync = func;
            return this;
        }
        public StateConfiguration<STATE, EVENT> OnActivate(Action action)
        {
            return OnActivate(() => { action(); return Task.CompletedTask; });
        }
        public StateConfiguration<STATE, EVENT> OnDeactivate(Func<Task> func)
        {
            _onDeactivateAsync = func;
            return this;
        }
        public StateConfiguration<STATE, EVENT> OnDeactivate(Action action)
        {
            return OnDeactivate(() => { action(); return Task.CompletedTask; });
        }
        public StateConfiguration<STATE, EVENT> OnEnter(Func<Task> func)
        {
            _onEnterAsync = func;
            return this;
        }
        public StateConfiguration<STATE, EVENT> OnEnter(Action action)
        {
            return OnEnter(() => { action(); return Task.CompletedTask; });
        }
        public StateConfiguration<STATE, EVENT> OnEnter(EVENT evt, Func<Task> func)
        {
            _entries.Add(new StateEvent<EVENT> { Event = evt, OnStateEventAsync = func });
            return this;
        }
        public StateConfiguration<STATE, EVENT> OnEnter(EVENT evt, Action action)
        {
            return OnEnter(evt, () => { action(); return Task.CompletedTask; });
        }
        public StateConfiguration<STATE, EVENT> OnEnter<P1>(EVENT evt, Func<P1, Task> func)
        {
            _entries.Add(new StateEvent<EVENT, P1> { Event = evt, OnStateEventAsync = func });
            return this;
        }
        public StateConfiguration<STATE, EVENT> OnEnter<P1>(EVENT evt, Action<P1> action)
        {
            return OnEnter<P1>(evt, (p1) => { action(p1); return Task.CompletedTask; });
        }
        public StateConfiguration<STATE, EVENT> OnEnter<P1, P2>(EVENT evt, Func<P1, P2, Task> func)
        {
            _entries.Add(new StateEvent<EVENT, P1, P2> { Event = evt, OnStateEventAsync = func });
            return this;
        }
        public StateConfiguration<STATE, EVENT> OnEnter<P1, P2>(EVENT evt, Action<P1, P2> action)
        {
            return OnEnter<P1, P2>(evt, (p1, p2) => { action(p1, p2); return Task.CompletedTask; });
        }
        public StateConfiguration<STATE, EVENT> OnEnter<P1, P2, P3>(EVENT evt, Func<P1, P2, P3, Task> func)
        {
            _entries.Add(new StateEvent<EVENT, P1, P2, P3> { Event = evt, OnStateEventAsync = func });
            return this;
        }
        public StateConfiguration<STATE, EVENT> OnEnter<P1, P2, P3>(EVENT evt, Action<P1, P2, P3> action)
        {
            return OnEnter<P1, P2, P3>(evt, (p1, p2, p3) => { action(p1, p2,p3); return Task.CompletedTask; });
        }
        public StateConfiguration<STATE, EVENT> OnExit(Func<Task> func)
        {
            _onExitAsync = func;
            return this;
        }
        public StateConfiguration<STATE, EVENT> OnExit(Action action)
        {
            return OnExit(() => { action(); return Task.CompletedTask; });
        }
        public StateConfiguration<STATE, EVENT> EventTransitionTo(EVENT evt, STATE nextState, Func<bool> guard = null)
        {
            if (_stateTransitions.Any(x=> EqualityComparer<EVENT>.Default.Equals(x.Evt, evt) && (guard ?? _defaultGuard) != x.Guard))
                throw new NotSupportedException($"Transitions from {_state} using {evt} is already defined");

            _stateTransitions.Add(new EventConfiguration { Evt = evt, State = nextState, Guard = guard ?? _defaultGuard });
            return this;
        }
        public StateConfiguration<STATE, EVENT> EventTransitionToSelf(EVENT evt, Func<bool> guard = null)
        {
            if (_stateTransitions.Any(x => EqualityComparer<EVENT>.Default.Equals(x.Evt, evt) && (guard ?? _defaultGuard) != x.Guard && !x.Ignore))
                throw new NotSupportedException($"Transitions from {_state} using {evt} is already defined");

            _stateTransitions.Add(new EventConfiguration { Evt = evt, State = _state, Guard = guard ?? _defaultGuard });
            return this;
        }
        public StateConfiguration<STATE, EVENT> EventIgnore(EVENT evt)
        {
            if (_stateTransitions.Any(x => EqualityComparer<EVENT>.Default.Equals(x.Evt, evt)))
                throw new NotSupportedException($"Transitions from {_state} using {evt} is already defined");

            _stateTransitions.Add(new EventConfiguration { Evt = evt, Ignore = true });
            return this;
        }
        internal async Task HandleActivateAsync()
        {
            if (_onActivateAsync != null)
                await _onActivateAsync();
        }
        internal async Task HandleDeactivateAsync()
        {
            if (_onDeactivateAsync != null)
                await _onDeactivateAsync();
        }
        internal bool HasOnExit { get { return _onExitAsync != null; } }
        internal async Task HandleExitAsync()
        {
            if (_onExitAsync != null)
                await _onExitAsync();
        }
        internal async Task HandleEventAsync(EVENT evt, object[] parameters)
        {
            IStateEvent<EVENT> stateEvent = _entries.FirstOrDefault(x => x.IsMatch(evt, parameters));
            if (stateEvent != null)
                await stateEvent.ProcessAsync(parameters);
            else if (_onEnterAsync != null)
                await _onEnterAsync();
        }
        internal STATE GetNextState(EVENT evt)
        {
            IEnumerable<EventConfiguration> transitions = _stateTransitions
                .Where(x => EqualityComparer<EVENT>.Default.Equals(x.Evt, evt));

            if (!transitions.Any())
                throw new NotSupportedException($"Transitions from {_state} using {evt} is not defined");

            EventConfiguration transition = transitions.FirstOrDefault(x => x.Guard());
            if(transition == null)
                throw new Exception("guarded");

            if (transition.Ignore)
                throw new Exception("ignore");

            return transition.State;
        }
    }
}
