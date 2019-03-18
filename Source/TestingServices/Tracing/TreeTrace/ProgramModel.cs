﻿using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Tracing.TreeTrace
{
    internal class ProgramModel
    {

        internal EventTree constructTree;

        // Program model
        private EventTreeNode currentHandler;

        private ulong highestKnownId;
        private Dictionary<ulong, EventTreeNode> sendIndexToReceiveEvent;
        private Dictionary<ulong, EventTreeNode> machineIdToStartEvent;
        private Dictionary<ulong, EventTreeNode> machineIdToRunningEvent;

        private bool isFirstStep;
        private HashSet<EventTreeNode> deletedNodes;

        public ProgramModel()
        {
            sendIndexToReceiveEvent = new Dictionary<ulong, EventTreeNode>();
            machineIdToStartEvent = new Dictionary<ulong, EventTreeNode>();
            machineIdToRunningEvent = new Dictionary<ulong, EventTreeNode>();
            deletedNodes = new HashSet<EventTreeNode>();
            constructTree = new EventTree();
            
            ResetProgramModel();
        }


        public void ResetProgramModel()
        {
            sendIndexToReceiveEvent.Clear();
            machineIdToStartEvent.Clear();
            machineIdToRunningEvent.Clear();
            currentHandler = null;
            constructTree = new EventTree();

            isFirstStep = true;
        }

        #region updates
        public void initializeWithTestHarnessMachine(ulong testHarnessMachineId)
        {
            // TODO: I guess TestHarnessMachine is always 0 ?
            EventTreeNode root = EventTree.CreateStartNode(0, testHarnessMachineId);
            constructTree.initializeWithRoot(root);
            currentHandler = root;
            highestKnownId = testHarnessMachineId;
            constructTree.startScheduleChoice(root);
        }

        public void recordSchedulingChoiceStart(ISchedulable choice, ulong stepIndex)
        {
            if( currentHandler!= null)
            {
                throw new ArgumentException("There is an ongoing handler");
            }
            if (!getTreeNodeFromISchedulable(choice, out currentHandler))
            {
                throw new ArgumentException("Cannot map choice back to TreeNode");
            }
            if (currentHandler.opType == OperationType.Send)
            {
                currentHandler.otherId = stepIndex;
            }

            constructTree.startScheduleChoice(currentHandler);

        }

        public void recordSchedulingChoiceResult(ISchedulable current, Dictionary<ulong, ISchedulable> machineToChoices, ulong endStepIndex, bool wasWithHeld)
        {
            if (isFirstStep)
            {   // Needs some help here.
                initializeWithTestHarnessMachine(current.Id);
                isFirstStep = false;
            }
            else
            {
                //if (!currentHandler.checkEquality(current)// Too strong. Current != next of last time
                if (currentHandler.srcMachineId != current.Id )
                {
                    throw new ArgumentException("Current did not match CurrentHandler");
                }
            }

            EventTreeNode createdNode = null;

            if (currentHandler.opType == OperationType.Create)
            {
                // A start operation has to be added to the chains
                //ulong expectedMachineId = machinesCreatedSoFar++;
                ulong expectedMachineId = machineToChoices.Where(x => x.Key > highestKnownId && x.Value.IsEnabled).Select(x => x.Key).Max();
                createdNode = EventTree.CreateStartNode(currentHandler.srcMachineId, expectedMachineId);

                highestKnownId = expectedMachineId;
                machineIdToStartEvent.Add(expectedMachineId, createdNode);

            }
            else if (currentHandler.opType == OperationType.Send && !wasWithHeld)
            {
                createdNode = EventTree.CreateReceiveNode(currentHandler.srcMachineId, currentHandler.targetMachineId, currentHandler.otherId);
                // The send index should mean this is never scheduled
                sendIndexToReceiveEvent.Add(createdNode.otherId, createdNode);
            }

            ISchedulable nextStepOfCurrentSchedulable = null;
            if (machineToChoices.TryGetValue(currentHandler.srcMachineId, out nextStepOfCurrentSchedulable))
            {
                if (!IsContinuation(currentHandler, nextStepOfCurrentSchedulable))
                {
                    nextStepOfCurrentSchedulable = null;
                }
            }

            // Mark EventHandler as complete if the next step is not a continuation.
            EventTreeNode nextNode = null;
            if (nextStepOfCurrentSchedulable != null)
            {
                nextNode = EventTree.CreateNodeFromISchedulable(nextStepOfCurrentSchedulable);
            }
            if (wasWithHeld)
            {
                // Track deleted nodes, don't add them to tree or scheduler
                deletedNodes.Add(createdNode);
                //createdNode = null;
            }

            constructTree.completeScheduleChoice(currentHandler, nextNode, createdNode);
            machineIdToRunningEvent[currentHandler.srcMachineId] = currentHandler;
            currentHandler = null;

        }

        internal void RecordIntegerChoice(int choice)
        {
            currentHandler.addIntegerChoice(choice);
        }

        internal void RecordBooleanChoice(bool choice)
        {
            currentHandler.addBooleanChoice(choice);
        }


        internal void recordEventWithHeld()
        {
            constructTree.withHeldSendIndices.Add(currentHandler.totalOrderingIndex);
        }

        #endregion

        #region ischedulable matching
        internal bool IsContinuation(EventTreeNode treeNode, ISchedulable sch)
        {
            if (treeNode.srcMachineId != sch.Id)
            {
                return false;
            }
            else
            {
                switch (sch.NextOperationType)
                {
                    case OperationType.Create:
                    case OperationType.Send:
                    case OperationType.Stop:
                        return true;
                    default:
                        return false;

                }
            }
        }

        internal bool getTreeNodeFromISchedulable(ISchedulable sch, out EventTreeNode treeNode)
        {
            bool matched = false;
            treeNode = null;
            switch (sch.NextOperationType)
            {
                case OperationType.Receive:
                    matched = sendIndexToReceiveEvent.TryGetValue(sch.NextOperationMatchingSendIndex, out treeNode);
                    break;

                case OperationType.Start:
                    matched = machineIdToStartEvent.TryGetValue(sch.Id, out treeNode);
                    break;

                case OperationType.Create:
                case OperationType.Stop:
                case OperationType.Send:
                    {
                        EventTreeNode tempNode = null;
                        matched = machineIdToRunningEvent.TryGetValue(sch.Id, out tempNode);

                        if (matched && tempNode != null)
                        {
                            treeNode = tempNode.getChildEvent();
                            matched = (treeNode != null);
                        }
                    }
                    break;
                default:
                    throw new ArgumentException("Program model does not yet support OperationType " + sch.NextOperationType );
            }
            
            return matched;
        }

        internal EventTree getTree()
        {
            return constructTree;
        }


        #endregion

    }
}
