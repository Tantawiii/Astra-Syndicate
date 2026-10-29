using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AstraSyndicate.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AstraSyndicate.Tests
{
    public class EventBusTests
    {
        private EventBus _bus;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
        }

        [Test]
        public void Publish_DeliversEventToSubscriber()
        {
            int received = 0;
            _bus.Subscribe<Ping>(e => received = e.Value);

            _bus.Publish(new Ping(42));

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            int calls = 0;
            void Handler(Ping e) => calls++;
            _bus.Subscribe<Ping>(Handler);
            _bus.Unsubscribe<Ping>(Handler);

            _bus.Publish(new Ping(1));

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Subscribe_SameHandlerTwice_IsDeliveredOnce()
        {
            int calls = 0;
            void Handler(Ping e) => calls++;
            _bus.Subscribe<Ping>(Handler);
            _bus.Subscribe<Ping>(Handler);

            _bus.Publish(new Ping(1));

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Publish_OnlyReachesSubscribersOfThatEventType()
        {
            int pings = 0;
            _bus.Subscribe<Ping>(_ => pings++);

            _bus.Publish(new Pong());

            Assert.AreEqual(0, pings);
        }

        [Test]
        public void Unsubscribe_DuringPublish_DoesNotSkipOtherHandlers()
        {
            var calls = new List<string>();
            Action<Ping> first = null;
            first = _ =>
            {
                calls.Add("first");
                _bus.Unsubscribe(first);
            };
            _bus.Subscribe(first);
            _bus.Subscribe<Ping>(_ => calls.Add("second"));

            _bus.Publish(new Ping(1));
            _bus.Publish(new Ping(2));

            CollectionAssert.AreEqual(new[] { "first", "second", "second" }, calls);
        }

        [Test]
        public void Publish_WhenHandlerThrows_OtherHandlersStillRun()
        {
            bool secondRan = false;
            _bus.Subscribe<Ping>(_ => throw new InvalidOperationException("boom"));
            _bus.Subscribe<Ping>(_ => secondRan = true);

            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            _bus.Publish(new Ping(1));

            Assert.IsTrue(secondRan);
        }

        private readonly struct Ping
        {
            public readonly int Value;

            public Ping(int value)
            {
                Value = value;
            }
        }

        private readonly struct Pong
        {
        }
    }
}
