using System;
using System.Collections.Generic;
using System.Linq;

namespace PersonalTaskManagement.Messaging
{
    public sealed class Messenger : IMessenger
    {
        public static Messenger Default { get; } = new Messenger();

        private readonly object _lock = new();

        private readonly Dictionary<Type, List<Subscription>> _subs = new();

        private sealed record Subscription(WeakReference Recipient, Delegate Handler);

        public void Subscribe<TMessage>(object recipient, Action<TMessage> handler)
        {
            if (recipient == null) throw new ArgumentNullException(nameof(recipient));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            lock (_lock)
            {
                if (!_subs.TryGetValue(typeof(TMessage), out var list))
                {
                    list = new List<Subscription>();
                    _subs[typeof(TMessage)] = list;
                }
                list.Add(new Subscription(new WeakReference(recipient), handler));
            }
        }

        public void Unsubscribe<TMessage>(object recipient)
        {
            lock (_lock)
            {
                if (!_subs.TryGetValue(typeof(TMessage), out var list)) return;
                list.RemoveAll(s => !s.Recipient.IsAlive || ReferenceEquals(s.Recipient.Target, recipient));
            }
        }

        public void UnsubscribeAll(object recipient)
        {
            lock (_lock)
            {
                foreach (var list in _subs.Values)
                {
                    list.RemoveAll(s => !s.Recipient.IsAlive || ReferenceEquals(s.Recipient.Target, recipient));
                }
            }
        }

        public void Send<TMessage>(TMessage message)
        {
            List<Subscription> snapshot;
            lock (_lock)
            {
                if (!_subs.TryGetValue(typeof(TMessage), out var list)) return;
                list.RemoveAll(s => !s.Recipient.IsAlive);
                snapshot = list.ToList();
            }

            foreach (var sub in snapshot)
            {
                if (!sub.Recipient.IsAlive) continue;
                if (sub.Handler is Action<TMessage> typed)
                {
                    typed(message);
                }
            }
        }
    }
}
