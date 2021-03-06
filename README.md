# Statesimple
<!-- 
[![Build status](https://ci.appveyor.com/api/projects/status/github/gnxi/statesimple?svg=true)](https://ci.appveyor.com/project/gnxi/statesimple/branch/master) 
-->
[![NuGet Pre Release](https://img.shields.io/nuget/vpre/Statesimple.svg)](https://www.nuget.org/packages/statesimple)
<!-- 
    [![Stack Overflow](https://img.shields.io/badge/stackoverflow-tag-orange.svg)](http://stackoverflow.com/questions/tagged/statesimple)
-->
Use Statesimple to easily build state machines in .Net 
code using the latest language features.

## Release notes v0.6.0

Modernized build and code. Added PhoneCall sample

## Release notes v0.5.0

Performance improvement and bugfix.

## Release notes v0.4.0

Added support for parameter type conversion.

## How to build a simple state machine

First decide on a type for the states and the events, 
we will use `string` for states and `char` for events.

```csharp
string on = "On", off = "Off";
```

Create the state machine:

```csharp
var machine = new StateMachine<string, char>(off);
```

"off" is the initial state. Configure the state machine's states:

```csharp
machine
    .Configure(off)
    .EventTransitionTo(' ', on);
machine
    .Configure("on")
    .EventTransitionTo(' ', off);
machine.OnUnhandledEvent((state, evt) => {});
```

The state machine has two states, "on" and "off", and it will transition between 
states on ' '. And we ignore unhandled events. Lets finish the program.

```csharp
Console.WriteLine("Press <space> to toggle the switch and q to quit.");

for(char c = Console.ReadKey(true).KeyChar; c != 'q'; c = Console.ReadKey(true).KeyChar)
{
    await machine.ProcessEventAsync(c);
    Console.WriteLine("State is " + machine.State);
}
```

We now have a finished state machine.

More samples [OnOff](https://github.com/gnxi/Statesimple/tree/master/samples/OnOff), [PhoneCall](https://github.com/gnxi/Statesimple/tree/master/samples/PhoneCall)

## Features

Summary of features:
- Any .Net type for states and events
- Simple parametrized events
- Task based API
- Reentrant and thread safe
- `OnActivate`, `OnEnter` (with parameters), `Onexit` and `OnDeactivate`
- Conditional state transitions
- Hierarchical states 
- External state storage

## Reference

### Creating the state machine

Statesimple does not store the actual state. It
is the responsibility of the caller to supply functions for loading and storing state.

```csharp
public StateMachine(Func<Task<STATE>> loadStateAsync, Func<STATE, Task> saveStateAsync);
public StateMachine(Func<STATE> loadState, Action<STATE> saveState);
```

Usually Statesimple provides both async and old school methods as above. 

To create a state machine when you don't care about storing the state as in the sample above.

```csharp
public StateMachine(STATE state)
```

To add a one time state machine initialization code use OnInitialize:

```csharp
public void OnInitialize(Func<Task> func);
public void OnInitialize(Action action);
```

### Configuring the state machine

You create a state using

```csharp
public StateConfiguration<STATE, EVENT> Configure(STATE state);
```

A state is configured using a flowing programming style.

`OnActivate` is called after the state is loaded before any processing is done. 
Use it to load resources needed by the state.

```csharp
public StateConfiguration<STATE, EVENT> OnActivate(Func<Task> func);
public StateConfiguration<STATE, EVENT> OnActivate(Action action);
```

`OnDeactivate` is called before the state is stored and after all processing is done.
Use it to clean up resources used by the state.

```csharp
public StateConfiguration<STATE, EVENT> OnDeactivate(Func<Task> func);
public StateConfiguration<STATE, EVENT> OnDeactivate(Action action);
```

`OnEnter` is called when the state machine moves to the state  (after `OnActivate`).

```csharp
public StateConfiguration<STATE, EVENT> OnEnter(Func<Task> func);
public StateConfiguration<STATE, EVENT> OnEnter(Action action);
public StateConfiguration<STATE, EVENT> OnEnter(EVENT evt, Func<Task> func);
public StateConfiguration<STATE, EVENT> OnEnter(EVENT evt, Action action);
public StateConfiguration<STATE, EVENT> OnEnter<P1>(EVENT evt, Func<P1, Task> func);
public StateConfiguration<STATE, EVENT> OnEnter<P1>(EVENT evt, Action<P1> action);
public StateConfiguration<STATE, EVENT> OnEnter<P1, P2>(EVENT evt, Func<P1, P2, Task> func);
public StateConfiguration<STATE, EVENT> OnEnter<P1, P2>(EVENT evt, Action<P1, P2> action);
public StateConfiguration<STATE, EVENT> OnEnter<P1, P2, P3>(EVENT evt, Func<P1, P2, P3, Task> func);
public StateConfiguration<STATE, EVENT> OnEnter<P1, P2, P3>(EVENT evt, Action<P1, P2, P3> action);
```

`OnExit` is called when the state machine moves from the state (before `OnDeactivate`).

```csharp
public StateConfiguration<STATE, EVENT> OnExit(Func<Task> func);
public StateConfiguration<STATE, EVENT> OnExit(Action action);
```

`EventTransitionTo` defines that an event is handled in the state and the action.

```csharp
public StateConfiguration<STATE, EVENT> EventTransitionTo(EVENT evt, STATE nextState, Func<bool> guard = null);
public StateConfiguration<STATE, EVENT> EventTransitionToSelf(EVENT evt, Func<bool> guard = null);
public StateConfiguration<STATE, EVENT> EventIgnore(EVENT evt);
```
 
### Running a state machine

After the state machine is configured, it is ready to process events. Use `ProcessEventAsync`.

```csharp
public Task ProcessEventAsync(EVENT evt, params object[] parameters);
```

`ProcessEventAsync` is thread safe and will queue events. 
Calls to `ProcessEventAsync` will return immediately if queued, 
though there will always be one call
that processes the last queued event.