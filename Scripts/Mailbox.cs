using System.Collections.Generic;
using System.Reflection;

namespace CoordEDBT
{
    public class Mailbox
    {
        private List<Message> mailbox;
        private Agent agent;

        public Mailbox(Agent agent)
        {
            this.mailbox = new List<Message>();
            this.agent = agent;
        }

        public void Add(Message m)
        {
            mailbox.Add(m);
        }

        private void FilterExpired()
        {
            List<Message> toDelete = new List<Message>();
            foreach (Message m in mailbox)
            {
                if (m.IsExpired())
                {
                    toDelete.Add(m);
                }
            }
            foreach (Message m in toDelete)
            {
                mailbox.Remove(m);
            }
        }

        public Message SelectMessage()
        {
            Message selected = null;
            FilterExpired();
            foreach (Message message in mailbox)
            {
                Condition condition = message.GetCondition();
                MethodInfo conditionCheck = agent.GetType().GetMethod(condition.GetMethodName());
                bool result = (bool)conditionCheck.Invoke(agent, condition.GetParameters());
                if (result)
                {
                    mailbox.Remove(message);
                    selected = message;
                    break;
                }
            }
            return selected;
        }
    }
}