using System;
using System.Collections.Generic;
using UnityEngine;

namespace AstraSyndicate.Core
{
    /// <summary>
    /// Default <see cref="IEventBus"/>. Handler lists are copy-on-write, so handlers may subscribe or
    /// unsubscribe while an event is being published, and one failing handler never stops the others.
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, object> _channels = new Dictionary<Type, object>();

        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            GetChannel<T>(create: true).Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            GetChannel<T>(create: false)?.Remove(handler);
        }

        public void Publish<T>(T evt) where T : struct
        {
            GetChannel<T>(create: false)?.Publish(evt);
        }

        private Channel<T> GetChannel<T>(bool create) where T : struct
        {
            if (_channels.TryGetValue(typeof(T), out object channel)) return (Channel<T>)channel;
            if (!create) return null;

            var created = new Channel<T>();
            _channels.Add(typeof(T), created);
            return created;
        }

        private sealed class Channel<T> where T : struct
        {
            private Action<T>[] _handlers = Array.Empty<Action<T>>();

            public void Add(Action<T> handler)
            {
                if (Array.IndexOf(_handlers, handler) >= 0) return;

                var next = new Action<T>[_handlers.Length + 1];
                Array.Copy(_handlers, next, _handlers.Length);
                next[_handlers.Length] = handler;
                _handlers = next;
            }

            public void Remove(Action<T> handler)
            {
                int index = Array.IndexOf(_handlers, handler);
                if (index < 0) return;

                var next = new Action<T>[_handlers.Length - 1];
                Array.Copy(_handlers, 0, next, 0, index);
                Array.Copy(_handlers, index + 1, next, index, _handlers.Length - index - 1);
                _handlers = next;
            }

            public void Publish(T evt)
            {
                Action<T>[] snapshot = _handlers;
                for (int i = 0; i < snapshot.Length; i++)
                {
                    try
                    {
                        snapshot[i](evt);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }
    }
}
