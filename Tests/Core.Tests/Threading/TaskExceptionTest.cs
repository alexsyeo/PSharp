﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests
{
    public class TaskExceptionTest : BaseTest
    {
        public TaskExceptionTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SharedEntry
        {
            public volatile int Value = 0;
        }

        private static async MachineTask WriteAsync(SharedEntry entry, int value)
        {
            await MachineTask.CompletedTask;
            entry.Value = value;
        }

        private static async MachineTask WriteWithDelayAsync(SharedEntry entry, int value)
        {
            await MachineTask.Delay(1);
            entry.Value = value;
        }

        [Fact(Timeout = 5000)]
        public async Task TestNoSynchronousTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            var task = WriteAsync(entry, 5);
            await task;

            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestNoAsynchronousTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            var task = WriteWithDelayAsync(entry, 5);
            await task;

            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestNoParallelSynchronousTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            var task = MachineTask.Run(() =>
            {
                entry.Value = 5;
            });

            await task;

            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestNoParallelAsynchronousTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            var task = MachineTask.Run(async () =>
            {
                entry.Value = 5;
                await MachineTask.Delay(1);
            });
            await task;

            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestNoParallelFuncTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            async MachineTask func()
            {
                entry.Value = 5;
                await MachineTask.Delay(1);
            }

            var task = MachineTask.Run(func);
#pragma warning disable IDE0067 // Dispose objects before losing scope
            var innerTask = await task;
#pragma warning restore IDE0067 // Dispose objects before losing scope
            await innerTask;

            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            Assert.Equal(TaskStatus.RanToCompletion, innerTask.Status);
            Assert.Equal(5, entry.Value);
        }

        private static async MachineTask WriteWithExceptionAsync(SharedEntry entry, int value)
        {
            await MachineTask.CompletedTask;
            entry.Value = value;
            throw new InvalidOperationException();
        }

        private static async MachineTask WriteWithDelayedExceptionAsync(SharedEntry entry, int value)
        {
            await MachineTask.Delay(1);
            entry.Value = value;
            throw new InvalidOperationException();
        }

        [Fact(Timeout = 5000)]
        public async Task TestSynchronousTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            var task = WriteWithExceptionAsync(entry, 5);

            Exception exception = null;
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal(TaskStatus.Faulted, task.Status);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAsynchronousTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            var task = WriteWithDelayedExceptionAsync(entry, 5);

            Exception exception = null;
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal(TaskStatus.Faulted, task.Status);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestParallelSynchronousTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            var task = MachineTask.Run(() =>
            {
                entry.Value = 5;
                throw new InvalidOperationException();
            });

            Exception exception = null;
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal(TaskStatus.Faulted, task.Status);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestParallelAsynchronousTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            var task = MachineTask.Run(async () =>
            {
                entry.Value = 5;
                await MachineTask.Delay(1);
                throw new InvalidOperationException();
            });

            Exception exception = null;
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal(TaskStatus.Faulted, task.Status);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestParallelFuncTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            async MachineTask func()
            {
                entry.Value = 5;
                await MachineTask.Delay(1);
                throw new InvalidOperationException();
            }

            var task = MachineTask.Run(func);

            MachineTask innerTask = null;
            Exception exception = null;
            try
            {
#pragma warning disable IDE0067 // Dispose objects before losing scope
                innerTask = await task;
#pragma warning restore IDE0067 // Dispose objects before losing scope
                await innerTask;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal(TaskStatus.Faulted, task.Status);
            Assert.Equal(5, entry.Value);
        }
    }
}
