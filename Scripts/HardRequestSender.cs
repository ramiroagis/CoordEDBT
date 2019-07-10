using NPBehave;
using System.Collections.Generic;

namespace CoordEDBT
{
    public class HardRequestSender : Decorator
    {
        private string type;
        private Parameter[] parameters;
        private string receivers;
        private Condition condition;
        private int timeout;
        private int quorumCount;
        private string quorum;
        private List<Agent> quorumList;

        public HardRequestSender(string type, Parameter[] parameters, string receivers, Condition condition, int timeout, int quorumCount, Node decoratee) : base("HardRequestSender", decoratee)
        {
            this.type = type;
            this.receivers = receivers;
            this.condition = condition;
            this.quorumCount = quorumCount;
            this.quorum = null;
            this.timeout = timeout;
            this.parameters = parameters;
            quorumList = null;
        }

        public HardRequestSender(string type, Parameter[] parameters, string receivers, Condition condition, int timeout, string quorum, Node decoratee) : base("HardRequestSender", decoratee)
        {
            this.type = type;
            this.receivers = receivers;
            this.condition = condition;
            this.quorumCount = -1;
            this.quorum = quorum;
            this.timeout = timeout;
            this.parameters = parameters;
            quorumList = null;
        }

        protected override void DoStart()
        {
            RootNode.Blackboard.Get<Agent>("self").DisableCheckMailbox();
            RootNode.Blackboard[type + "-confirmed"] = new List<Agent>();
            List<Agent> receiversList = RootNode.Blackboard.Get<List<Agent>>(receivers);
            if (quorumCount == -1)
            {
                // Check quorum list
                quorumList = RootNode.Blackboard.Get<List<Agent>>(quorum);
            }
            HardRequest request = new HardRequest(type);
            foreach (Parameter p in parameters)
            {
                request.Add(p.GetReceiverKey(), RootNode.Blackboard.Get<object>(p.GetSenderKey()));
            }
            Message message = new Message(RootNode.Blackboard.Get<Agent>("self"), request, condition, timeout);
            foreach (Agent agent in receiversList)
            {
                agent.ReceiveMessage(message);
            }
            Clock.AddTimer(0.0f, 0.0f, -1, CheckQuorum);
            Clock.AddTimer(timeout / 1000.0f, -1, QuorumNotMet);
        }

        private void CheckQuorum()
        {
            List<Agent> confirmedList = RootNode.Blackboard.Get<List<Agent>>(type + "-confirmed");
            if (quorumCount == -1)
            {
                // Check quorum list
                foreach (Agent agent in quorumList)
                {
                    if (!confirmedList.Contains(agent))
                    {
                        return;
                    }
                }
                foreach (Agent agent in confirmedList)
                {
                    agent.ReceiveReconfirmation();
                }
            }
            else
            {
                // Check quorum count
                if (confirmedList.Count < quorumCount)
                {
                    return;
                }
                int remaining = quorumCount;
                foreach (Agent agent in confirmedList)
                {

                    agent.ReceiveReconfirmation();
                    remaining--;
                    if (remaining == 0)
                    {
                        break;
                    }
                }
            }
            Clock.RemoveTimer(CheckQuorum);
            Clock.RemoveTimer(QuorumNotMet);
            Decoratee.Start();

        }

        private void QuorumNotMet()
        {
            Clock.RemoveTimer(CheckQuorum);
            Clock.RemoveTimer(QuorumNotMet);
            RootNode.Blackboard.Get<Agent>("self").EnableCheckMailbox();
            Stopped(false);
        }

        protected override void DoStop()
        {
            Clock.RemoveTimer(CheckQuorum);
            Clock.RemoveTimer(QuorumNotMet);
            RootNode.Blackboard.Get<Agent>("self").EnableCheckMailbox();
            if (Decoratee.IsActive)
            {
                Decoratee.Stop();
            }
            else
            {
                Stopped(false);
            }
        }

        protected override void DoChildStopped(Node child, bool result)
        {
            RootNode.Blackboard.Get<Agent>("self").EnableCheckMailbox();
            Stopped(result);
        }
    }
}