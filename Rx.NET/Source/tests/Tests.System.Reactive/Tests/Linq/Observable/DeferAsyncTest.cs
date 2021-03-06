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
using System.Threading;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace ReactiveTests.Tests
{
    public class DeferAsyncTest : ReactiveTest
    {

        [Fact]
        public void DeferAsync_ArgumentChecking()
        {
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.Defer(default(Func<Task<IObservable<int>>>)));
            ReactiveAssert.Throws<ArgumentNullException>(() => Observable.DeferAsync(default(Func<CancellationToken, Task<IObservable<int>>>)));
        }

        [Fact]
        public void DeferAsync_Simple()
        {
            var xs = Observable.Defer<int>(() => Task.Factory.StartNew(() => Observable.Return(42)));

            var res = xs.ToEnumerable().ToList();

            Assert.True(new[] { 42 }.SequenceEqual(res));
        }

        [Fact]
        public void DeferAsync_WithCancel_Simple()
        {
            var xs = Observable.DeferAsync<int>(ct => Task.Factory.StartNew(() => Observable.Return(42)));

            var res = xs.ToEnumerable().ToList();

            Assert.True(new[] { 42 }.SequenceEqual(res));
        }

        [Fact]
        public void DeferAsync_WithCancel_Cancel()
        {
            var N = 10;// 0000;
            for (int i = 0; i < N; i++)
            {
                var e = new ManualResetEvent(false);
                var called = false;

                var xs = Observable.DeferAsync<int>(ct => Task.Factory.StartNew(() =>
                {
                    e.Set();

                    while (!ct.IsCancellationRequested)
                        ;

                    return Observable.Defer(() => { called = true; return Observable.Return(42); });
                }));

                var d = xs.Subscribe(_ => { });

                e.WaitOne();
                d.Dispose();

                Assert.False(called);
            }
        }

    }
}
