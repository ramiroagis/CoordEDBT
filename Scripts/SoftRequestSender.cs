using NPBehave;
using System.Collections.Generic;

namespace CoordEDBT
{
    public class SoftRequestSender : Task
    {
        private string type;
        private Parameter[] parameters;
        private List<Agent> receivers;
        private Condition condition;
        private int timeout;

        public SoftRequestSender(string type, Parameter[] parameters, List<Agent> receivers, Condition condition, int timeout) : base("SoftRequestSender")
        {
            this.type = type;
            this.receivers = receivers;
            this.condition = condition;
            this.timeout = timeout;
            this.parameters = parameters;
        }
        
        protected override void DoStart()
        {
            SoftRequest request = new SoftRequest(type);
            foreach (Parameter p in parameters)
            {
                request.Add(p.GetReceiverKey(), RootNode.Blackboard.Get<object>(p.GetSenderKey()));
            }
            Message message = new Message(RootNode.Blackboard.Get<Agent>("self"), request, condition, timeout);
            foreach (Agent agent in receivers)
            {
                agent.ReceiveMessage(message);
            }
            Stopped(true);
        }

        protected override void DoStop()
        {
            Stopped(false);
        }
    }
}