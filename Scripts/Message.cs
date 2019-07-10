using System;
using UnityEngine;

namespace CoordEDBT
{
    public class Message
    {
        protected Agent sender;
        protected Request request;
        protected Condition condition;
        protected int timeout;
        protected float issued;

        public Message(Agent sender, Request request, Condition condition, int timeout)
        {
            this.sender = sender;
            this.request = request;
            this.condition = condition;
            this.timeout = timeout;
            this.issued = Time.time * 1000;
        }

        public bool IsExpired()
        {
            if (timeout == -1)
            {
                return false;
            }
            else
            {
                return (Time.time * 1000 - issued > timeout);
            }
        }

        public Condition GetCondition()
        {
            return condition;
        }

        public Agent GetSender()
        {
            return sender;
        }

        public Request GetRequest()
        {
            return request;
        }
    }
}