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
        public async Task EventWithoutParameters()
        {
            State state = State.State1;
            var machine = new StateMachine<State, Event>(() => state, (s) => state = s);
            machine.IgnoreUnhandledEvent();

            string s1 = null;

            machine.Configure(State.State1)
                .OnEnter(() => s1 = "default")
                .EventTransitionTo(Event.Prev, State.State1)
                .EventTransitionTo(Event.Next, State.State2)
            ;

            machine.Configure(State.State2)
                .OnEnter(Event.Next, () => s1 = "noparams")
                .OnEnter<string>(Event.Next, (s) => s1 = s)
                .EventTransitionToSelf(Event.Next)
            ;

            await machine.ProcessEventAsync(Event.Prev);
            Assert.Equal("default", s1);
            Assert.True(machine.IsInState(State.State1));

            await machine.ProcessEventAsync(Event.Next);
            Assert.Equal("noparams", s1);
            Assert.True(machine.IsInState(State.State2));
        }
        [Fact]
        public async Task EventWithParameters()
        {
            State state = State.State1;
            var machine = new StateMachine<State, Event>(() => state, (s) => state = s);
            machine.IgnoreUnhandledEvent();

            string s1 = null;

            machine.Configure(State.State1)
                .EventTransitionTo(Event.Next, State.State2)
            ;

            machine.Configure(State.State2)
                .OnEnter(Event.Next, () => s1 = "noparams")
                .OnEnter<string>(Event.Next, (s) => s1 = s)
                .EventTransitionToSelf(Event.Next)
            ;

            await machine.ProcessEventAsync(Event.Next);
            Assert.Equal("noparams", s1);
            Assert.True(machine.IsInState(State.State2));

            await machine.ProcessEventAsync(Event.Next, "hello");
            Assert.Equal("hello", s1);
            Assert.True(machine.IsInState(State.State2));
        }
        [Fact]
        public async Task EventWithTypeConverter()
        {
            State state = State.State1;
            var machine = new StateMachine<State, Event>(() => state, (s) => state = s);

            machine.IgnoreUnhandledEvent();
            machine.OnTypeConversion((type, obj) =>
            {
                if (type == typeof(string))
                    return obj.ToString();
                else if (type == typeof(int) && obj is string && int.TryParse((string)obj, out int value))
                    return value;
                return null;
            });

            string s1 = null;
            int i1 = 0;

            machine.Configure(State.State1)
                .EventTransitionTo(Event.Next, State.State2)
            ;

            machine.Configure(State.State2)
                .OnEnter<string>(Event.Next, (s) => s1 = s)
                .OnEnter<int>(Event.Next, (i) => i1 = i)
                .OnEnter<string,int>(Event.Next, (s, i) => { s1 = s; i1 = i; })
                .EventTransitionToSelf(Event.Next)
            ;

            await machine.ProcessEventAsync(Event.Next);
            Assert.NotEqual("hello", s1);
            Assert.False(machine.IsInState(State.State2));

            await machine.ProcessEventAsync(Event.Next, 10);
            Assert.Equal(10, i1);
            Assert.True(machine.IsInState(State.State2));

            await machine.ProcessEventAsync(Event.Next, "world", 100);
            Assert.Equal("world", s1);
            Assert.Equal(100, i1);
            Assert.True(machine.IsInState(State.State2));

            await machine.ProcessEventAsync(Event.Next, 1000, "10000");
            Assert.Equal("1000", s1);
            Assert.Equal(10000, i1);
            Assert.True(machine.IsInState(State.State2));

        }
    }
}
