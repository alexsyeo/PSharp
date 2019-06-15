﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp.TestingServices.Tracing.Error
{
    /// <summary>
    /// Class implementing a P# bug trace. A trace is a
    /// series of transitions from some initial state to
    /// some end state.
    /// </summary>
    [DataContract]
    internal sealed class BugTrace : IEnumerable, IEnumerable<BugTraceStep>
    {
        /// <summary>
        /// The steps of the bug trace.
        /// </summary>
        [DataMember]
        private readonly List<BugTraceStep> Steps;

        /// <summary>
        /// The number of steps in the bug trace.
        /// </summary>
        internal int Count
        {
            get { return this.Steps.Count; }
        }

        /// <summary>
        /// Index for the bug trace.
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>BugTraceStep</returns>
        internal BugTraceStep this[int index]
        {
            get { return this.Steps[index]; }
            set { this.Steps[index] = value; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        internal BugTrace()
        {
            this.Steps = new List<BugTraceStep>();
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="targetMachine">Target machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal void AddCreateMachineStep(Machine machine, MachineId targetMachine, EventInfo eventInfo)
        {
            MachineId mid = null;
            string machineState = null;
            if (machine != null)
            {
                mid = machine.Id;
                machineState = machine.CurrentStateName;
            }

            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.CreateMachine,
                mid, machineState, eventInfo, null, targetMachine, null, null, null, null, eventInfo.Event.GetEventRepresentation(), null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        /// <param name="monitor">MachineId</param>
        internal void AddCreateMonitorStep(MachineId monitor)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.CreateMonitor,
                null, null, null, null, monitor, null, null, null, null, null, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="machineState">MachineState</param>
        /// <param name="eventInfo">EventInfo</param>
        /// <param name="targetMachine">Target machine</param>
        internal void AddSendEventStep(BaseMachine machine, string machineState,
            EventInfo eventInfo, Machine targetMachine)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.SendEvent,
                machine.Id, machineState, eventInfo, null, targetMachine.Id, null, null,
                (machine as Machine)?.GetStateInfo(), targetMachine.GetStateInfo(), eventInfo.Event.GetEventRepresentation(), null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="machineState">MachineState</param>
        /// <param name="eventInfo">EventInfo</param>
        internal void AddDequeueEventStep(Machine machine, string machineState, EventInfo eventInfo)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.DequeueEvent,
                machine.Id, machineState, eventInfo, null, null, null, null, machine.GetStateInfo(), null, eventInfo.Event.GetEventRepresentation(), null);
            this.Push(scheduleStep);
        }

        internal void ReplaceDequedEventMachineStateInfo(Machine machine, string machineState)
        {
            bool foundEvent = false;
            for (int i = this.Steps.Count - 1; i >= 0; i--)
            {
                var step = this.Steps[i];
                if (step.Machine.Equals(machine.Id) && step.Type == BugTraceStepType.DequeueEvent)
                {
                    step.MachineStateInfo = machineState;
                    foundEvent = true;
                    break;
                }
            }

            if (!foundEvent)
            {
                throw new System.Exception("Assert: foundEvent == true");
            }
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="machineState">MachineState</param>
        /// <param name="eventInfo">EventInfo</param>
        internal void AddRaiseEventStep(MachineId machine, string machineState, EventInfo eventInfo)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.RaiseEvent,
                machine, machineState, eventInfo, null, null, null, null, null, null, eventInfo.Event.GetEventRepresentation(), null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="machineState">MachineState</param>
        internal void AddGotoStateStep(MachineId machine, string machineState)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.GotoState,
                machine, machineState, null, null, null, null, null, null, null, null, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="machineState">MachineState</param>
        /// <param name="action">MethodInfo</param>
        internal void AddInvokeActionStep(MachineId machine, string machineState, MethodInfo action)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.InvokeAction,
                machine, machineState, null, action, null, null, null, null, null, null, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="machineState">MachineState</param>
        /// <param name="eventNames">Event names</param>
        internal void AddWaitToReceiveStep(MachineId machine, string machineState, string eventNames)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.WaitToReceive,
                machine, machineState, null, null, null, null, null, null, null, null, eventNames);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="machineState">MachineState</param>
        /// <param name="eventInfo">EventInfo</param>
        internal void AddReceivedEventStep(MachineId machine, string machineState, EventInfo eventInfo)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.ReceiveEvent,
                machine, machineState, eventInfo, null, null, null, null, null, null, eventInfo.Event.GetEventRepresentation(), null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="machineState">MachineState</param>
        /// <param name="choice">Choice</param>
        internal void AddRandomChoiceStep(MachineId machine, string machineState, bool choice)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.RandomChoice,
                machine, machineState, null, null, null, choice, null, null, null, null, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="machineState">MachineState</param>
        /// <param name="choice">Choice</param>
        internal void AddRandomChoiceStep(MachineId machine, string machineState, int choice)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.RandomChoice,
                machine, machineState, null, null, null, null, choice, null, null,  null, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="machineState">MachineState</param>
        internal void AddHaltStep(MachineId machine, string machineState)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.Halt,
                machine, machineState, null, null, null, null, null, null, null, null, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Returns the latest bug trace step and
        /// removes it from the trace.
        /// </summary>
        /// <returns>BugTraceStep</returns>
        internal BugTraceStep Pop()
        {
            if (this.Count > 0)
            {
                this.Steps[this.Count - 1].Next = null;
            }

            var step = this.Steps[this.Count - 1];
            this.Steps.RemoveAt(this.Count - 1);

            return step;
        }

        /// <summary>
        /// Returns the latest bug trace step
        /// without removing it.
        /// </summary>
        /// <returns>BugTraceStep</returns>
        internal BugTraceStep Peek()
        {
            BugTraceStep step = null;

            if (this.Steps.Count > 0)
            {
                step = this.Steps[this.Count - 1];
            }
            
            return step;
        }

        /// <summary>
        /// Returns a json format of the trace that can be used for visualization.
        /// </summary>
        /// <returns>The json trace.</returns>
        internal string FormatInJsonForVisualization()
        {
            if (this.Steps.Count == 0)
            {
                return "";
            }

            // hack for now
            int idxLastSend = this.Steps.FindLastIndex(s => s.Type == BugTraceStepType.DequeueEvent);

            StringBuilder trace = new StringBuilder();
            trace.AppendLine("[");
            for (int i = 0; i < this.Steps.Count; i++)
            {
                BugTraceStep step = this.Steps[i];
                if (step.Type == BugTraceStepType.DequeueEvent)
                {
                    trace.AppendLine("  {");
                    trace.AppendLine($"    \"From\": \"{step.EventInfo.OriginInfo.SenderMachineId}\",");
                    trace.AppendLine($"    \"To\": \"{step.Machine}\",");

                    bool toStateInfoExists = step.MachineStateInfo != null && step.MachineStateInfo.Length > 0;
                    trace.AppendLine($"    \"Message\": \"{step.EventRepresentation}\"{(toStateInfoExists ? "," : "")}");
                    
                    if (toStateInfoExists)
                    {
                        trace.AppendLine($"    \"State\":");
                        trace.AppendLine("    {");
                        trace.AppendLine($"    \"{step.Machine}\": \"{step.MachineStateInfo}\"");
                        trace.AppendLine("    }");
                    }

                    if (i == idxLastSend/*this.Steps.Count - 1*/)
                    {
                        trace.AppendLine("  }");
                    }
                    else
                    {
                        trace.AppendLine("  },");
                    }
                }
            }

            trace.AppendLine("]");
            return trace.ToString();
        }

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        /// <returns>IEnumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Steps.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        /// <returns>IEnumerator</returns>
        IEnumerator<BugTraceStep> IEnumerable<BugTraceStep>.GetEnumerator()
        {
            return this.Steps.GetEnumerator();
        }

        /// <summary>
        /// Pushes a new step to the trace.
        /// </summary>
        /// <param name="step">BugTraceStep</param>
        private void Push(BugTraceStep step)
        {
            if (this.Count > 0)
            {
                this.Steps[this.Count - 1].Next = step;
                step.Previous = this.Steps[this.Count - 1];
            }

            this.Steps.Add(step);
        }
    }
}
