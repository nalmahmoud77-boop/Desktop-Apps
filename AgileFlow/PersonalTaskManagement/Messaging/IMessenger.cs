using System;

namespace PersonalTaskManagement.Messaging
{
    public interface IMessenger
    {
        void Subscribe<TMessage>(object recipient, Action<TMessage> handler);
        void Unsubscribe<TMessage>(object recipient);
        void UnsubscribeAll(object recipient);
        void Send<TMessage>(TMessage message);
    }
}
