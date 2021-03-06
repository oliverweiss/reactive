﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Xunit;
using ReactiveTests.Dummies;
using System.Reflection;

namespace ReactiveTests.Tests
{
    public class RefCountTest : ReactiveTest
    {

        [Fact]
        public void RefCount_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.RefCount<int>(null));
        }

        [Fact]
        public void RefCount_ConnectsOnFirst()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnCompleted<int>(250)
            );

            var subject = new MySubject();

            var conn = new ConnectableObservable<int>(xs, subject);

            var res = scheduler.Start(() =>
                conn.RefCount()
            );

            res.Messages.AssertEqual(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnCompleted<int>(250)
            );

            Assert.True(subject.Disposed);
        }

        [Fact]
        public void RefCount_NotConnected()
        {
            var disconnected = false;
            var count = 0;
            var xs = Observable.Defer(() =>
            {
                count++;
                return Observable.Create<int>(obs =>
                {
                    return () => { disconnected = true; };
                });
            });

            var subject = new MySubject();

            var conn = new ConnectableObservable<int>(xs, subject);
            var refd = conn.RefCount();

            var dis1 = refd.Subscribe();
            Assert.Equal(1, count);
            Assert.Equal(1, subject.SubscribeCount);
            Assert.False(disconnected);

            var dis2 = refd.Subscribe();
            Assert.Equal(1, count);
            Assert.Equal(2, subject.SubscribeCount);
            Assert.False(disconnected);

            dis1.Dispose();
            Assert.False(disconnected);
            dis2.Dispose();
            Assert.True(disconnected);
            disconnected = false;

            var dis3 = refd.Subscribe();
            Assert.Equal(2, count);
            Assert.Equal(3, subject.SubscribeCount);
            Assert.False(disconnected);

            dis3.Dispose();
            Assert.True(disconnected);
        }

        [Fact]
        public void RefCount_OnError()
        {
            var ex = new Exception();
            var xs = Observable.Throw<int>(ex, Scheduler.Immediate);

            var res = xs.Publish().RefCount();

            res.Subscribe(_ => { Assert.True(false); }, ex_ => { Assert.Same(ex, ex_); }, () => { Assert.True(false); });
            res.Subscribe(_ => { Assert.True(false); }, ex_ => { Assert.Same(ex, ex_); }, () => { Assert.True(false); });
        }

        [Fact]
        public void RefCount_Publish()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnNext(250, 5),
                OnNext(260, 6),
                OnNext(270, 7),
                OnNext(280, 8),
                OnNext(290, 9),
                OnCompleted<int>(300)
            );

            var res = xs.Publish().RefCount();

            var d1 = default(IDisposable);
            var o1 = scheduler.CreateObserver<int>();
            scheduler.ScheduleAbsolute(215, () => { d1 = res.Subscribe(o1); });
            scheduler.ScheduleAbsolute(235, () => { d1.Dispose(); });

            var d2 = default(IDisposable);
            var o2 = scheduler.CreateObserver<int>();
            scheduler.ScheduleAbsolute(225, () => { d2 = res.Subscribe(o2); });
            scheduler.ScheduleAbsolute(275, () => { d2.Dispose(); });

            var d3 = default(IDisposable);
            var o3 = scheduler.CreateObserver<int>();
            scheduler.ScheduleAbsolute(255, () => { d3 = res.Subscribe(o3); });
            scheduler.ScheduleAbsolute(265, () => { d3.Dispose(); });

            var d4 = default(IDisposable);
            var o4 = scheduler.CreateObserver<int>();
            scheduler.ScheduleAbsolute(285, () => { d4 = res.Subscribe(o4); });
            scheduler.ScheduleAbsolute(320, () => { d4.Dispose(); });

            scheduler.Start();

            o1.Messages.AssertEqual(
                OnNext(220, 2),
                OnNext(230, 3)
            );

            o2.Messages.AssertEqual(
                OnNext(230, 3),
                OnNext(240, 4),
                OnNext(250, 5),
                OnNext(260, 6),
                OnNext(270, 7)
            );

            o3.Messages.AssertEqual(
                OnNext(260, 6)
            );

            o4.Messages.AssertEqual(
                OnNext(290, 9),
                OnCompleted<int>(300)
            );

            xs.Subscriptions.AssertEqual(
                Subscribe(215, 275),
                Subscribe(285, 300)
            );
        }

        [Fact]
        public void LazyRefCount_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.RefCount<int>(null, TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void LazyRefCount_ConnectsOnFirst()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnCompleted<int>(250)
            );

            var subject = new MySubject();

            var conn = new ConnectableObservable<int>(xs, subject);

            var res = scheduler.Start(() =>
                conn.RefCount(TimeSpan.FromSeconds(2))
            );

            res.Messages.AssertEqual(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnCompleted<int>(250)
            );

            Assert.True(subject.Disposed);
        }

        [Fact]
        public void LazyRefCount_NotConnected()
        {
            var scheduler = new TestScheduler();
            var disconnected = false;
            var count = 0;
            var xs = Observable.Defer(() =>
            {
                count++;
                return Observable.Create<int>(obs =>
                {
                    return () => { disconnected = true; };
                });
            });

            var subject = new MySubject();

            var conn = new ConnectableObservable<int>(xs, subject);
            var refd = conn.RefCount(TimeSpan.FromTicks(20), scheduler);

            var dis1 = refd.Subscribe();
            Assert.Equal(1, count);
            Assert.Equal(1, subject.SubscribeCount);
            Assert.False(disconnected);

            var dis2 = refd.Subscribe();
            Assert.Equal(1, count);
            Assert.Equal(2, subject.SubscribeCount);
            Assert.False(disconnected);

            dis1.Dispose();
            Assert.False(disconnected);
            dis2.Dispose();
            Assert.False(disconnected);

            scheduler.AdvanceBy(19);
            Assert.False(disconnected);

            scheduler.AdvanceBy(1);
            Assert.True(disconnected);
            disconnected = false;

            var dis3 = refd.Subscribe();
            Assert.Equal(2, count);
            Assert.Equal(3, subject.SubscribeCount);
            Assert.False(disconnected);

            dis3.Dispose();
            scheduler.AdvanceBy(20);
            Assert.True(disconnected);
        }

        [Fact]
        public void LazyRefCount_OnError()
        {
            var ex = new Exception();
            var xs = Observable.Throw<int>(ex, Scheduler.Immediate);

            var res = xs.Publish().RefCount(TimeSpan.FromSeconds(2));

            res.Subscribe(_ => throw new Exception(), ex_ => { Assert.Same(ex, ex_); }, () => throw new Exception());
            res.Subscribe(_ => throw new Exception(), ex_ => { Assert.Same(ex, ex_); }, () => throw new Exception());
        }

        [Fact]
        public void LazyRefCount_Publish()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnNext(250, 5),
                OnNext(260, 6),
                OnNext(270, 7),
                OnNext(280, 8),
                OnNext(290, 9),
                OnCompleted<int>(300)
            );

            var res = xs.Publish().RefCount(TimeSpan.FromTicks(9), scheduler);

            var d1 = default(IDisposable);
            var o1 = scheduler.CreateObserver<int>();
            scheduler.ScheduleAbsolute(215, () => { d1 = res.Subscribe(o1); });
            scheduler.ScheduleAbsolute(235, () => { d1.Dispose(); });

            var d2 = default(IDisposable);
            var o2 = scheduler.CreateObserver<int>();
            scheduler.ScheduleAbsolute(225, () => { d2 = res.Subscribe(o2); });
            scheduler.ScheduleAbsolute(275, () =>
            {
                d2.Dispose();
            });

            var d3 = default(IDisposable);
            var o3 = scheduler.CreateObserver<int>();
            scheduler.ScheduleAbsolute(255, () => { d3 = res.Subscribe(o3); });
            scheduler.ScheduleAbsolute(265, () => { d3.Dispose(); });

            var d4 = default(IDisposable);
            var o4 = scheduler.CreateObserver<int>();
            scheduler.ScheduleAbsolute(285, () => { d4 = res.Subscribe(o4); });
            scheduler.ScheduleAbsolute(320, () => { d4.Dispose(); });

            scheduler.Start();

            o1.Messages.AssertEqual(
                OnNext(220, 2),
                OnNext(230, 3)
            );

            o2.Messages.AssertEqual(
                OnNext(230, 3),
                OnNext(240, 4),
                OnNext(250, 5),
                OnNext(260, 6),
                OnNext(270, 7)
            );

            o3.Messages.AssertEqual(
                OnNext(260, 6)
            );

            o4.Messages.AssertEqual(
                OnNext(290, 9),
                OnCompleted<int>(300)
            );

            xs.Subscriptions.AssertEqual(
                Subscribe(215, 284),
                Subscribe(285, 300)
            );
        }
    }
}
