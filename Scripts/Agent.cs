using NPBehave;
using System.Collections.Generic;
using UnityEngine;

namespace CoordEDBT
{
    public abstract class Agent : MonoBehaviour
    {
        protected Blackboard blackboard;
        protected Blackboard sharedBlackboard;
        protected Root behaviorTree;
        protected Mailbox mailbox;
        protected bool canCheckMailbox;
        protected bool awaitingReconfirmation;
        protected Message unreconfirmedMessage;

        protected void Awake()
        {
            sharedBlackboard = UnityContext.GetSharedBlackboard("sharedbb");
            blackboard = new Blackboard(sharedBlackboard, UnityContext.GetClock());
            RegisterToSharedBlackboard();
            mailbox = new Mailbox(this);
            canCheckMailbox = true;
            awaitingReconfirmation = false;
        }

        protected void Start()
        {
            behaviorTree = CreateBehaviorTree();
            behaviorTree.Start();
        }

        protected List<Agent> GetReceivers()
        {
            List<Agent> receivers = new List<Agent>();
            foreach (Agent other in sharedBlackboard.Get<List<Agent>>("agents"))
            {
                if (!other.Equals(this))
                {
                    receivers.Add(other);
                }
            }
            return receivers;
        }

        public void ReceiveMessage(Message message)
        {
            mailbox.Add(message);
        }

        public void ReceiveConfirmation(string type, Agent receiver)
        {
            blackboard.Get<List<Agent>>(type + "-confirmed").Add(receiver);
        }

        public void ReceiveReconfirmation()
        {
            foreach (KeyValuePair<string, object> pair in unreconfirmedMessage.GetRequest().GetParameters())
            {
                blackboard[pair.Key] = pair.Value;
            }
            behaviorTree.Clock.RemoveTimer(WaitForReconfirmation);
            Request request = unreconfirmedMessage.GetRequest();
            blackboard[request.GetRequestType()] = request;
            unreconfirmedMessage = null;
            awaitingReconfirmation = false;
        }

        protected void CheckMailbox()
        {
            if (canCheckMailbox)
            {
                Message selected = mailbox.SelectMessage();
                if (selected != null)
                {
                    Request request = selected.GetRequest();
                    string type = request.GetRequestType();
                    if (request is SoftRequest)
                    {
                        foreach (KeyValuePair<string, object> pair in request.GetParameters())
                        {
                            blackboard[pair.Key] = pair.Value;
                        }
                        blackboard[type] = request;
                    }
                    else if (request is HardRequest)
                    {
                        DisableCheckMailbox();
                        unreconfirmedMessage = selected;
                        awaitingReconfirmation = true;
                        selected.GetSender().ReceiveConfirmation(type, this);
                        behaviorTree.Clock.AddTimer(0.0f, 0.0f, -1, WaitForReconfirmation);
                    }
                }
            }
        }

        protected void WaitForReconfirmation()
        {
            if (unreconfirmedMessage.IsExpired())
            {
                behaviorTree.Clock.RemoveTimer(WaitForReconfirmation);
                awaitingReconfirmation = false;
                EnableCheckMailbox();
            }
        }

        public void DisableCheckMailbox()
        {
            canCheckMailbox = false;
        }

        public void EnableCheckMailbox()
        {
            canCheckMailbox = true;
        }

        protected void RegisterToSharedBlackboard()
        {
            blackboard["self"] = this;
            if (sharedBlackboard.Get<List<Agent>>("agents") == null)
            {
                sharedBlackboard["agents"] = new List<Agent>();
            }
            sharedBlackboard.Get<List<Agent>>("agents").Add(this);
        }

        public bool True()
        {
            return true;
        }

        protected abstract Root CreateBehaviorTree();
    }
}