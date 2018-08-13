using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Statesimple.test
{
    public class TransitionTests
    {
        enum State { State1, State2, State3, State2_Substate1, State2_Substate2 }
        enum Event { Next, Prev, FirstSubstate, NextSubstate, PrevSubstate, Timeout }
        [Fact]
        public async Task TransitionStateChange()
        {
            State state = State.State1;
            var machine = new StateMachine<State, Event>(() => state, (s) => state = s);
            machine.IgnoreUnhandledEvent();

            machine.Configure(State.State1)
                .EventTransitionTo(Event.Next, State.State2)
            ;
            machine.Configure(State.State2)
                .EventTransitionTo(Event.Next, State.State3)
                .EventTransitionTo(Event.Prev, State.State1)
            ;
            machine.Configure(State.State3)
                .EventTransitionTo(Event.Prev, State.State2)
            ;

            await machine.ProcessEventAsync(Event.Next);
            Assert.Equal(State.State2, state);
            Assert.False(machine.IsInState(State.State3));
            Assert.True(machine.IsInState(State.State2));
            Assert.False(machine.IsInState(State.State3));

            await machine.ProcessEventAsync(Event.Next);
            Assert.Equal(State.State3, state);
            Assert.True(machine.IsInState(State.State3));

            await machine.ProcessEventAsync(Event.Prev);
            Assert.Equal(State.State2, state);
            Assert.True(machine.IsInState(State.State2));

            await machine.ProcessEventAsync(Event.Next);
            Assert.Equal(State.State3, state);
            Assert.True(machine.IsInState(State.State3));

            await machine.ProcessEventAsync(Event.Next);
            // ignored
            Assert.Equal(State.State3, state);
            Assert.True(machine.IsInState(State.State3));

            await machine.ProcessEventAsync(Event.Next);
            // ignored
            Assert.Equal(State.State3, state);
            Assert.True(machine.IsInState(State.State3));

            await machine.ProcessEventAsync(Event.Prev);
            Assert.Equal(State.State2, state);
            Assert.True(machine.IsInState(State.State2));

            await machine.ProcessEventAsync(Event.Prev);
            Assert.Equal(State.State1, state);
            Assert.True(machine.IsInState(State.State1));
        }
        [Fact]
        public async Task TransitionStateChangeWithSubstate()
        {
            State state = State.State1;
            var machine = new StateMachine<State, Event>(() => state, (s) => state = s);
            machine.IgnoreUnhandledEvent();

            machine.Configure(State.State1)
                .EventTransitionTo(Event.Next, State.State2)
            ;
            machine.Configure(State.State2)
                .EventTransitionTo(Event.Next, State.State3)
                .EventTransitionTo(Event.Prev, State.State1)
                .EventTransitionTo(Event.FirstSubstate, State.State2_Substate1)
            ;
            machine.Configure(State.State2_Substate1)
                .SubstateOf(State.State2)
                .EventTransitionTo(Event.NextSubstate, State.State2_Substate2)
            ;
            machine.Configure(State.State2_Substate2)
                .SubstateOf(State.State2)
                .EventTransitionTo(Event.PrevSubstate, State.State2_Substate1)
            ;
            machine.Configure(State.State3)
                .EventTransitionTo(Event.Prev, State.State2)
            ;

            await machine.ProcessEventAsync(Event.Next);
            Assert.Equal(State.State2, state);
            Assert.False(machine.IsInState(State.State1));
            Assert.False(machine.IsInState(State.State2_Substate2));
            Assert.False(machine.IsInState(State.State2_Substate1));

            await machine.ProcessEventAsync(Event.FirstSubstate);
            Assert.Equal(State.State2_Substate1, state);
            Assert.True(machine.IsInState(State.State2_Substate1));
            Assert.False(machine.IsInState(State.State2_Substate2));
            Assert.True(machine.IsInState(State.State2));

            await machine.ProcessEventAsync(Event.NextSubstate);
            Assert.Equal(State.State2_Substate2, state);
            Assert.True(machine.IsInState(State.State2_Substate2));
            Assert.False(machine.IsInState(State.State2_Substate1));
            Assert.True(machine.IsInState(State.State2));

            await machine.ProcessEventAsync(Event.Next);
            Assert.Equal(State.State3, state);
            Assert.False(machine.IsInState(State.State2_Substate2));
            Assert.False(machine.IsInState(State.State2_Substate1));
            Assert.True(machine.IsInState(State.State3));
        }
        [Fact]
        public async Task TriggerEventFromState()
        {
            State state = State.State1;
            var machine = new StateMachine<State, Event>(() => state, (s) => state = s);

            machine.Configure(State.State1)
                .OnEnter(Event.Timeout, async () => 
                {
                    await machine.ProcessEventAsync(Event.Next);
                    await machine.ProcessEventAsync(Event.Next);
                })
                .EventTransitionToSelf(Event.Timeout)
                .EventTransitionTo(Event.Next, State.State2)
            ;
            machine.Configure(State.State2)
                .EventTransitionTo(Event.Next, State.State3)
                .EventTransitionTo(Event.Prev, State.State1)
            ;
            machine.Configure(State.State3)
                .EventTransitionTo(Event.Prev, State.State2)
            ;

            await machine.ProcessEventAsync(Event.Timeout);
            Assert.True(machine.IsInState(State.State3));
        }
        [Fact]
        public async Task EventWithParameters()
        {
            State state = State.State1;
            var machine = new StateMachine<State, Event>(() => state, (s) => state = s);

            string p1 = null;

            machine.Configure(State.State1)
                .EventTransitionTo(Event.Next, State.State2)
            ;

            machine.Configure(State.State2)
                .OnEnter<string>(Event.Next, (s) => p1 = s)
                .EventTransitionToSelf(Event.Next)
            ;

            await machine.ProcessEventAsync(Event.Next);
            Assert.NotEqual("hello", p1);
            Assert.True(machine.IsInState(State.State2));

            await machine.ProcessEventAsync(Event.Next, "hello");
            Assert.Equal("hello", p1);
            Assert.True(machine.IsInState(State.State2));
        }
    }
}
