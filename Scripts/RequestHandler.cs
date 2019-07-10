using NPBehave;

namespace CoordEDBT
{
    public class RequestHandler : ObservingDecorator
    {
        private string type;

        public RequestHandler(string type, Node decoratee) : base("RequestHandler", Stops.LOWER_PRIORITY_IMMEDIATE_RESTART, decoratee)
        {
            this.type = type;
        }

        override protected void StartObserving()
        {
            this.RootNode.Blackboard.AddObserver(type, onValueChanged);
        }

        override protected void StopObserving()
        {
            this.RootNode.Blackboard.RemoveObserver(type, onValueChanged);
        }

        private void onValueChanged(Blackboard.Type type, object newValue)
        {
            Evaluate();
        }

        override protected bool IsConditionMet()
        {
            bool isMet = RootNode.Blackboard.Get(type) != null;
            if (isMet)
            {
                RootNode.Blackboard.Get<Agent>("self").DisableCheckMailbox();
            }
            return isMet;
        }

        protected override void DoChildStopped(Node child, bool result)
        {
            this.RootNode.Blackboard.Unset(type);
            base.DoChildStopped(child, result);
            RootNode.Blackboard.Get<Agent>("self").EnableCheckMailbox();
        }
    }
}