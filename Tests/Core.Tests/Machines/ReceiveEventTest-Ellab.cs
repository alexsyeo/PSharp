﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests
{
    public class ReceiveTestEllaB : BaseTest
    {
        public ReceiveTestEllaB(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public SetupEvent(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        internal class SetupEventInt : Event
        {
            public TaskCompletionSource<int> Tcs;

            public SetupEventInt(TaskCompletionSource<int> tcs)
            {
                this.Tcs = tcs;
            }
        }

        internal class SetupEventBool : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public SetupEventBool(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class E : Event
        {
        }

        private class H : Event
        {
        }

        private class G : Event
        {
            public int I;
            public TaskCompletionSource<bool> Tcs;

            public G(int i, TaskCompletionSource<bool> tcs)
            {
                this.I = i;
                this.Tcs = tcs;
            }
        }

        private class J : Event
        {
            public int I;
            public TaskCompletionSource<bool> Tcs;

            public J(int i, TaskCompletionSource<bool> tcs)
            {
                this.I = i;
                this.Tcs = tcs;
            }
        }

        private class F : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public F(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class Config : Event
        {
            public MachineId Id;
            public TaskCompletionSource<bool> Tcs;

            public Config(MachineId id, TaskCompletionSource<bool> tcs)
            {
                this.Id = id;
                this.Tcs = tcs;
            }
        }

        private class Unit : Event
        {
        }

        private class UnitTcsInt : Event
        {
            public TaskCompletionSource<int> Tcs;

            public UnitTcsInt(TaskCompletionSource<int> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class UnitTcsBool : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public UnitTcsBool(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class UnitInt : Event
        {
            public TaskCompletionSource<int> Tcs;

            public UnitInt(TaskCompletionSource<int> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class AcqReq : Event
        {
            public MachineId Machine;

            public AcqReq(MachineId machine)
            {
                this.Machine = machine;
            }
        }

        private class AcqResponse : Event
        {
            public int Data;

            public AcqResponse(int data)
            {
                this.Data = data;
            }
        }

        private class Release : Event
        {
            public int Token;

            public Release(int token)
            {
                this.Token = token;
            }
        }

        private class ClientConfig : Event
        {
            public MachineId Lock;
            public int Iter;
            public TaskCompletionSource<bool> Tcs;

            public ClientConfig(MachineId lck, int iter, TaskCompletionSource<bool> tcs)
            {
                this.Lock = lck;
                this.Iter = iter;
                this.Tcs = tcs;
            }
        }

        private class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                var bid = this.CreateMachine(typeof(B1), new SetupEvent(tcs));
                this.Send(bid, new F(tcs));
            }
        }

        private class B1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(UnitTcsBool), typeof(X))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryX))]
            [OnEventGotoState(typeof(F), typeof(Y))]
            private class X : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryY))]
            private class Y : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Raise(new UnitTcsBool(tcs));
            }

            private async Task InitOnEntryX()
            {
                var tcs = (this.ReceivedEvent as UnitTcsBool).Tcs;
                tcs.SetResult(true);
                await this.Receive(typeof(E));
            }

            private void InitOnEntryY()
            {
                // Since Receive in state X is blocking, event F
                // will never get dequeued, and InitOnEntryY handler is unreachable:
                var tcs = (this.ReceivedEvent as F).Tcs;
                tcs.SetResult(false);
            }
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                var bid = this.CreateMachine(typeof(B2), new SetupEvent(tcs));
                this.Send(bid, new F(tcs));
            }
        }

        private class B2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventPushState(typeof(UnitTcsBool), typeof(X))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryX))]
            [OnEventGotoState(typeof(F), typeof(Y))]
            private class X : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryY))]
            private class Y : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Raise(new UnitTcsBool(tcs));
            }

            private async Task InitOnEntryX()
            {
                var e = await this.Receive(typeof(F));
                this.Pop();
                (e as F).Tcs.SetResult(true);
            }

            private async Task InitOnEntryY()
            {
                // Since receive statement ignores the event handlers of the enclosing context,
                // InitOnEntryY handler is unreachable:
                var e = await this.Receive(typeof(F));
                (e as F).Tcs.SetResult(false);
            }
        }

        private class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                var bid = this.CreateMachine(typeof(B3), new SetupEvent(tcs));
                this.Send(bid, new F(tcs));
            }
        }

        private class B3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventPushState(typeof(UnitTcsBool), typeof(X))]
            [OnEventGotoState(typeof(F), typeof(Error))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryX))]
            [OnEventGotoState(typeof(F), typeof(Error))]
            private class X : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryError))]
            private class Error : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Raise(new UnitTcsBool(tcs));
            }

            private async Task InitOnEntryX()
            {
                var e = await this.Receive(typeof(F));
                this.Pop();
                (e as F).Tcs.SetResult(true);
            }

            private async Task InitOnEntryError()
            {
                // Since receive statement ignores the event handlers of the enclosing context,
                // InitOnEntryError handler is unreachable:
                var e = await this.Receive(typeof(F));
                (e as F).Tcs.SetResult(false);
            }
        }

        private class M4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(T))]
            [OnEventDoAction(typeof(UnitTcsBool), nameof(OnUnitAction))]
            [OnExit(nameof(ExitInit))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryT))]
            private class T : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEventBool).Tcs;
                var bid = this.CreateMachine(typeof(B4), new Config(this.Id, tcs));
                this.Raise(new UnitTcsBool(tcs));
            }

            private void InitOnEntryT()
            {
            }

            private async Task ExitInit()
            {
                // This exit action is executed after the OnUnitAction action, upon
                // transitioning from the state Init to the state T
                var e = await this.Receive(typeof(G));
                var payload = (e as G).I;
                this.Assert(payload == 2);
                (e as G).Tcs.SetResult(true);
            }

            private async Task OnUnitAction()
            {
                var e = await this.Receive(typeof(G));
                var payload = (e as G).I;
                this.Assert(payload == 1);
                this.Raise(new Unit());
            }
        }

        private class B4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                MachineId mid = (this.ReceivedEvent as Config).Id;
                var tcs = (this.ReceivedEvent as Config).Tcs;
                this.Send(mid, new G(1, tcs));
                this.Send(mid, new G(2, tcs));
            }
        }

        private class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEventBool).Tcs;
                var bid = this.CreateMachine(typeof(B5), new Config(this.Id, tcs));
                this.Send(bid, new G(1, tcs));
                this.Send(bid, new G(2, tcs));
            }
        }

        private class B5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitInit))]
            [OnEventGotoState(typeof(Unit), typeof(T))]
            [OnEventDoAction(typeof(UnitTcsBool), nameof(OnUnitAction))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(InitOnEntryT))]
            private class T : MachineState
            {
            }

            private void InitOnEntry()
            {
                MachineId mid = (this.ReceivedEvent as Config).Id;
                var tcs = (this.ReceivedEvent as Config).Tcs;
                this.Raise(new UnitTcsBool(tcs));
            }

            private async Task ExitInit()
            {
                // This exit action is executed after the OnUnitAction action, upon
                // transitioning from the state Init to the state T
                var e = await this.Receive(typeof(G));
                var payload = (e as G).I;
                this.Assert(payload == 2);
                (e as G).Tcs.SetResult(true);
            }

            private void InitOnEntryT()
            {
            }

            private async Task OnUnitAction()
            {
                var e = await this.Receive(typeof(G));
                var payload = (e as G).I;
                this.Assert(payload == 1);
                this.Raise(new Unit());
            }
        }

        private class M6 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(UnitTcsBool), nameof(OnUnitAction))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEventBool).Tcs;
                var bid = this.CreateMachine(typeof(B6), new Config(this.Id, tcs));
                this.Raise(new UnitTcsBool(tcs));
            }

            private async Task OnUnitAction()
            {
                Event receivedEvent = await this.Receive(Tuple.Create<Type, Func<Event, bool>>(typeof(G), e => (e as G).I == 1),
                                                         Tuple.Create<Type, Func<Event, bool>>(typeof(J), e => (e as J).I == 2));

                if (receivedEvent is G eventG)
                {
                    eventG.Tcs.SetResult(true);
                }
                else if (receivedEvent is J eventJ)
                {
                    eventJ.Tcs.SetResult(false);
                }
            }
        }

        private class B6 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                MachineId mid = (this.ReceivedEvent as Config).Id;
                var tcs = (this.ReceivedEvent as Config).Tcs;
                this.Send(mid, new G(1, tcs));
                this.Send(mid, new J(2, tcs));
            }
        }

        private class M7 : Machine
        {
            // private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(UnitTcsBool), nameof(OnUnitAction))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEventBool).Tcs;
                this.Raise(new UnitTcsBool(tcs));
                // this.Raise(new UnitTcsBool(tcs));
                // this.Send(this.Id, new E());
            }

            private async Task OnUnitAction()
            {
                Event receivedEvent = await this.Receive(typeof(UnitTcsBool));
                if (receivedEvent is UnitTcsBool unitEvent)
                {
                    unitEvent.Tcs.SetResult(true);
                }

                // (receivedEvent as UnitTcsBool).Tcs.SetResult(false);
            }
        }

        private class MainMach : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                // this.Logger.WriteLine("MainMach: InitOnEntry start");
                var tcs = (this.ReceivedEvent as SetupEventBool).Tcs;
                var data = 0;
                var lockMach = this.CreateMachine(typeof(Lock), new J(data, tcs));
                var client1 = this.CreateMachine(typeof(ClientMach), new ClientConfig(lockMach, 2, tcs));
                var client2 = this.CreateMachine(typeof(ClientMach), new ClientConfig(lockMach, 3, tcs));
                // this.Logger.WriteLine("MainMach: InitOnEntry finish");
            }
        }

        private class ClientMach : Machine
        {
            private MachineId Lock;
            private int Iter;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                // this.Logger.WriteLine("ClientMach with id: {0}: InitOnEntry start", this.Id);
                var tcs = (this.ReceivedEvent as ClientConfig).Tcs;
                this.Lock = (this.ReceivedEvent as ClientConfig).Lock;
                this.Iter = (this.ReceivedEvent as ClientConfig).Iter;

                int i = 0;
                while (i < this.Iter)
                {
                    // this.Logger.WriteLine("ClientMach with id: {0}: iteration {1}, total iterations {2}",
                        // this.Id, i, this.Iter);
                    this.Send(this.Lock, new AcqReq(this.Id));
                    Event e = await this.Receive(typeof(AcqResponse));
                    if (e is AcqResponse AcqResp)
                    {
                        // this.Logger.WriteLine("ClientMach with id: {0} after Receive: iteration {1}, total iterations {2}",
                        // this.Id, i, this.Iter, AcqResp.Data);
                        var v = AcqResp.Data;
                        this.Send(this.Lock, new Release(v));
                    }

                    i++;
                }

                // this.Logger.WriteLine("ClientMach with id: {0}: after while loop");
            }
        }

        private class Lock : Machine
        {
            private int Data;
            private TaskCompletionSource<bool> Tcs;
            private int NumReleases;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(UnheldEntry))]
            [OnEventGotoState(typeof(AcqReq), typeof(Held))]
            [DeferEvents(typeof(Release))]
            private class Unheld : MachineState
            {
            }

            [OnEntry(nameof(HeldEntry))]
            [OnEventGotoState(typeof(Release), typeof(RelState))]
            [DeferEvents(typeof(AcqReq))]
            private class Held : MachineState
            {
            }

            [OnEntry(nameof(RelStateEntry))]
            [DeferEvents(typeof(AcqReq), typeof(Release))]
            private class RelState : MachineState
            {
            }

            private void InitOnEntry()
            {
                // this.Logger.WriteLine("Lock: InitOnEntry start");
                this.Tcs = (this.ReceivedEvent as J).Tcs;
                this.Data = (this.ReceivedEvent as J).I;
                this.Goto<Unheld>();
            }

            private void UnheldEntry()
            {
            }

            private void HeldEntry()
            {
                // this.Logger.WriteLine("Lock: HeldEntry start");
                var client = (this.ReceivedEvent as AcqReq).Machine;
                this.Send(client, new AcqResponse(this.Data));
            }

            private void RelStateEntry()
            {
                this.NumReleases++;
                var client = (this.ReceivedEvent as Release).Token;
                // this.Logger.WriteLine("Lock: RelStateEntry start, token is {0}, numReleases is {1}",
                    // client, this.NumReleases);
                if (this.NumReleases == 5)
                {
                    this.Tcs.SetResult(true);
                }
                else
                {
                    this.Goto<Unheld>();
                }
            }
        }

        [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Feature2Stmts\Correct\receive6\receive6.p
        public void TestReceiveEventBlocking1()
        {
            var configuration = GetConfiguration();
            // TODO: timeout doesn't help in case of a deadlock: why?
            // configuration.Timeout = 10;
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M1), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Feature2Stmts\Correct\receive12\receive12.p
        public void TestReceiveEventBlocking2()
        {
            var configuration = GetConfiguration();
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M2), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Feature2Stmts\Correct\receive13\receive13.p
        public void TestReceiveEventBlocking3()
        {
            var configuration = GetConfiguration();
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M3), new SetupEvent(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Feature2Stmts\Correct\receive14\receiveInExit1.p
        public void TestReceiveEventInExit1()
        {
            var configuration = GetConfiguration();
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M4), new SetupEventBool(tcs));
                // TODO: timeout reached when assert in a machine fails: why?
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Feature2Stmts\Correct\receive15\receiveInExit2.p
        public void TestReceiveEventInExit2()
        {
            var configuration = GetConfiguration();
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M5), new SetupEventBool(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        public void TestMultipleReceiveEventsPreds()
        {
            var configuration = GetConfiguration();
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M6), new SetupEventBool(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Feature2Stmts\Correct\lock\lock.p
        public void LockFromP()
        {
            var configuration = GetConfiguration();
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(MainMach), new SetupEventBool(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }

        // TODO: this test dosn't work: why?
        // [Fact(Timeout = 5000)]
        // Similar to \P\Tst\RegressionTests\Feature1SMLevelDecls\Correct\bug1\bug1.p
        public void Bug1FromP()
        {
            var configuration = GetConfiguration();
            configuration.IsVerbose = true;
            var test = new Action<IMachineRuntime>(async (r) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M7), new SetupEventBool(tcs));
                var result = await Task.WhenAny(tcs.Task, Task.Delay(0));
                Assert.True(tcs.Task.Result);
            });

            this.Run(configuration, test);
        }
    }
}
