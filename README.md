# CoordEDBT - An extension for Event-Driven Behavior Trees to facilitate NPC coordination in Unity

### May 4, 2020: The research paper about CoordEDBT has been finally published in the scientific journal Expert Systems and Applications. [[Read PDF]](https://drive.google.com/file/d/12Umh5G5fqP6TuHwnxY3ohDtM3Kd4JHfc/view)

## What is CoordEDBT?

CoordEDBT is extension to Event-Driven Behavior Trees (EDBTs) developed in Unity, consisting of three new types of nodes that facilitate the implementation of Non-Player Characters (NPCs) that coordinate with each other. With these nodes, developers can create coordinated behaviors in a visually intuitive way without manually programming the act of coordination.

## How does it work?

CoordEDBT is an extension for [NPBehave](https://github.com/meniku/NPBehave), a powerful and flexible code-based approach to create Event-Driven Behavior Trees in Unity. In particular, CoordEDBT extends NPBehave with three new types of nodes which allows NPCs to coordinate with each other through a request protocol. Briefly, this extension allows NPCs to send and receive messages that encapsulate requests to execute subtrees. By taking advantage of the event-drivenness of EDBTs, NPCs can abort their running nodes to execute incoming requests.

If you are unfamiliar with the difference between EDBTs and classical Behavior Trees, you can read the _Advanced Behavior Tree
Implementations_ chapter from [The Behavior Tree Starter Kit](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter06_The_Behavior_Tree_Starter_Kit.pdf).

## Installation

Copy the `Scripts` folder from this repository and the `NPBehave` folder from the [NPBehave's repository](https://github.com/meniku/NPBehave) into your Unity project.

## How to use

Before proceeding, please read [NPBehave's documentation](https://github.com/meniku/NPBehave).

The following step-by-step tutorial will refer to the example inside `Firefighters Example - Unity Project.zip`, which can be imported from Unity if necessary. This example consists in a squad of firefighter NPCs that have to coordinate to extinguish the fires started by the player. A fire can be extinguished only if at least three firefighters simultaneously douse it for a few seconds. Therefore, the firefighters must coordinate to avoid focusing on different fires. Whenever one finds a fire, it sends a request two other firefighters to move to that location to help extinguish it. This example can be played by running the executable file inside `Firefighters Example - Executable.zip`.

![CoordEDBT - Firefighters Example.exe](https://i.imgur.com/mZ9JLzM.png)

### 1. Setting up a NPC

In order to be able to send and receive requests, the NPC’s class that contains the behavior tree must inherit from the class `Agent`. This class provides the NPC (from now on, _agent_) with a mailbox (a message priority queue), a private blackboard, and the request protocol.

`FirefighterAI.cs`
```c#
public class FirefighterAI : Agent {
  ...
}
```

The `FirefighterAI` class can be found inside the `Assets\FirefighterAI Scripts` folder.
A class that inherits from `Agent` must implement the inherited abstract method `CreateBehaviorTree`.

`FirefighterAI.cs`
```c#
protected override Root CreateBehaviorTree() {
  return new Root(blackboard,
    ...
  );
}
```

The method `Start` in the NPC’s script can do whatever is necessary as long as it calls the method `Start` from its parent class.

`FirefighterAI.cs`
```c#
new void Start() {
  ...
  base.Start();
}
```
The method `Start` in the class `Agent` registers the GameObject associated to the NPC's script in the list of agents that follow the request protocol. Then, it ticks the root of the agent's behavior tree.

### 2. Using coordination nodes

CoordEDBT extends NPBehave with three new types of node called _coordination nodes_. Coordination nodes allow agents to send and receive messages that encapsulate requests. A request from a sender S to a receiver R means that S wants R to execute certain subtree in R's behavior tree. A request is composed of:
* A string `type` that determines the type of the request and the subtree that the receiver will execute.
* An array of `Parameter` objects (values) that personalize the request. Each `Parameter` is composed of two blackboard keys; the first one corresponds to the sender’s key where the value is stored (origin) when the request is sent, and the second one corresponds to the receiver’s key where the value will be copied (destination) if the request is going to be executed.

In the example, whenever a firefighter finds a nearby fire (which is stored in the blackboard key `BK_NEARBY_FIRE`) it sends the following request to two other agents:
```
type: "extinguish-fire", 
parameter: new Parameter[] { new Parameter(BK_NEARBY_FIRE, BK_TARGET_FIRE) }
```
As will be explained below, this request is packed inside a message which is stored in each receiver's mailbox. Eventually, any receiver that is going to execute the request will have the value from the sender’s blackboard key `BK_NEARBY_FIRE` copied to their blackboard key `BK_TARGET_FIRE`.

A message is composed of:
* A reference to the sender
* A request
* A condition that the receiver must satisfy in order to execute the request.
* A number of milliseconds after which the message times out and must be discarded by the receiver.

Messages can be sent from _Soft Request Sender_ (**SRS**) task nodes or _Hard Request Sender_ (**HRS**) decorator nodes.

`SoftRequestSender.cs`
```c#
public class SoftRequestSender : Task {
  public SoftRequestSender(string type, Parameter[] parameters, List<Agent> receivers, Condition condition, int timeout) : base("SoftRequestSender") {
    ...
  }
}
```

Whenever a **SRS** task node is ticked, the sender, the request `type`, the request `parameters`, the `condition` and the `timeout` are packed into a message that is sent to `receivers`; then, the node simply returns `success` to its parent. That is, after the messages are sent, the sender proceeds with its individual behavior regardless of what the receivers do. Some receivers may not be able to select the message from their mailbox before it times out if they are executing some uninterruptible behavior.

`HardRequestSender.cs`
```c#
public class HardRequestSender : Decorator {
  public HardRequestSender(string type, Parameter[] parameters, string receivers, Condition condition, int timeout, int quorumCount, Node decoratee) : base("HardRequestSender", decoratee) {
    ...
  }

  public HardRequestSender(string type, Parameter[] parameters, string receivers, Condition condition, int timeout, string quorum, Node decoratee) : base("HardRequestSender", decoratee) {
    ...
  }
}
```

**HRS** decorator nodes have a child node `decoratee`. The purpose of a **HRS** node is to coordinate the execution of its subtree with the execution of the subtree specified in the request. Whenever a **HRS** node is ticked, the sender, the request `type`, the request `parameters`, the `condition` and the `timeout` are packed into a message that is sent to `receivers`. Then, the sender waits for enough receivers to confirm the request until a _quorum_ is met. If the quorum is met before `timeout` elapses, the sender sends a reconfirmation to the receivers that previously confirmed, and the execution of the **HRS** node's subtree and the receivers' subtrees begins. Otherwise, if the quorum is not met before `timeout` elapses, the **HRS** node returns `failure` to its parent.

Note that the class `HardRequestSender` has two constructors. The reason is that the quorum can be specified either as a number of agents `quorum`, or as a list of agents stored in a blackboard key `quorumList`.

`FirefightersAI.cs`
```c#
new HardRequestSender("extinguish-fire", new Parameter[] { new Parameter(BK_NEARBY_FIRE, BK_TARGET_FIRE) }, BK_RECEIVERS, new CoordEDBT.Condition("True"), 150, 2,
  new Sequence(
    new Action(Douse),
    new WaitUntilDestroyed(BK_NEARBY_FIRE),
    new Action(StopDousing)
  )
)
```

Note that the **HRS** node from the example sends a message containing the aforementioned request, composed of the type `"extinguish-fire"` and a single parameter `new Parameter(BK_NEARBY_FIRE, BK_TARGET_FIRE)`. The message is sent to the list of receivers stored in the blackboard key `BK_RECEIVERS`. In particular, the method `GetReceivers` from the class `Agent` returns a list with all the agents that follow the request protocol (that is, all the NPCs that inherit from `Agent`). 

`FirefightersAI.cs`
```c#
new void Start() {
  SetReceivers();
  ...
  base.Start();
}

void SetReceivers() {
  blackboard[BK_RECEIVERS] = GetReceivers();
}
```

In the example, the list of receivers never changes since it is set in the method `Start`. However, if dynamically changing the list of receivers during the execution of the behavior tree is necessary, the list returned by `GetReceivers` can be filtered and stored in a blackboard key in a method called by an `Action` node.

Recall that the fourth parameter in a **HRS** node is a condition that receivers must satisfy in order to execute the request. This condition is represented by an object containing:
* A string corresponding to a method that returns `bool` in the receiver's class that inherits from `Agent`.
* A sequence of parameters for that method.

`Condition.cs`
```c#
public Condition(string methodName, params object[] parameters) {
  ...
}
```

In the example, the message's condition is `new CoordEDBT.Condition("True")`, which corresponds to the method `True` provided by the class `Agent`.

`Agent.cs`
```c#
public bool True() {
    return true;
}
```

This method simply returns `true` since it can be used when the sender wants the receivers to execute the request without satisfying any particular condition.
Note that, if necessary, the receivers could be different types of NPCs implemented with different classes, and each class could have a different implementation of the method stated in the condition. If a receiver does not have a corresponding method to evaluate the condition, it will not be able to execute the corresponding request.

Messages are handled by the method `CheckMailbox` in the class `Agent`. Whenever this method is called, the agent discards all the messages in its mailbox that have already timed out, and then _selects_ one whose condition returns `true` (if any). The selection is carried out by the method `SelectMessage` in the class `Mailbox`. By defualt, it selects messages in a FIFO manner, but can be modified if necessary.

The method `CheckMailbox` must be repeatedly called from a service node below the root. Otherwise, the agent will not select any of the messages it receives and thus will not execute any incoming requests.

`FirefighterAI.cs`
```c#
protected override Root CreateBehaviorTree() {
  return new Root(blackboard,
    new Service(0.1f, CheckMailbox,
      ...
}
```

Whenever an agent selects a message from its mailbox, the request may be handled by one of the Request Handler (**RH**) observer decorator nodes in its behavior tree. A **RH** node, implemented by the class `RequestHandler`, works as a `BlackboardCondition` node (see [NPBehave](https://github.com/meniku/NPBehave)) with the difference that:
* the string corresponding to the blackboard key is semantically a request type.
* the operator is fixed as `Operator.IS_SET`.
* the stop rule is fixed as `Stops.LOWER_PRIORITY_IMMEDIATE_RESTART`.

`FirefighterAI.cs`
```c#
new RequestHandler("extinguish-fire",
    new Sequence(
    new NavMoveTo(GetComponent<UnityEngine.AI.NavMeshAgent>(), BK_TARGET_FIRE, 6f, true),
    new Action(Douse),
    new WaitUntilDestroyed(BK_TARGET_FIRE),
    new Action(StopDousing)
  )
),

```

By default, agents are committed to the coordinated behaviors they are part of. In order to avoid breaking their commitment due to an interruption caused by other incoming requests, they will automatically disable the method `CheckMailbox` while:
* executing a **RH** node's subtree,
* waiting for a **HRS** node's quorum to be met and executing the subtree below, and
* waiting for the reconfirmation of a hard request.

However, this default behavior can be bypassed by using a task node that calls the method `EnableCheckMailbox` or `DisableCheckMailbox` provided by the class `Agent`.
