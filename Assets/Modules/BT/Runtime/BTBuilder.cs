using AngelBeat;
using Modules.BT.Action;
using Modules.BT.Condition;
using Modules.BT.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Modules.BT.Runtime
{
    public static class BTBuilder
    {
        public static BTNode BuildTree(BTGraphData data, BTContext context)
        {
            var nodeDict = new Dictionary<string, BTNode>();

            foreach (var nodeData in data.nodes)
            {
                BTNode node = nodeData.nodeType switch
                {
                    "Selector" => new BTSelector(),
                    "Sequence" => new BTSequence(),
                    "Condition" => new BTCondition(BTConditionFactory.Create(nodeData.parameter)),
                    "Action" => new BTAction(BTActionFactory.Create(nodeData.parameter)),
                    _ => throw new Exception($"Node Type을 확인하세요 : {nodeData.nodeType}")
                };
                nodeDict[nodeData.guid] = node;
            }

            foreach (var link in data.links)
            {
                BTNode parent = nodeDict[link.outputNodeGuid];
                BTNode child = nodeDict[link.inputNodeGuid];
                switch (parent)
                {
                    case BTCompositeNode composite:
                        composite.AddChild(child);
                        break;
                    case BTDecorator decorator:
                        decorator.SetChild(child);
                        break;
                    default:
                        throw new Exception("잘못된 노드 유형이 부모 노드가 되려 합니다. 구성을 확인하세요.");
                }
            }

            var outputSet = data.links.Select(l => l.outputNodeGuid).ToHashSet();
            var inputSet = data.links.Select(l => l.inputNodeGuid).ToHashSet();
            var rootGuid = outputSet.Except(inputSet).FirstOrDefault();
            
            return rootGuid != null ? nodeDict[rootGuid] : null;
        }
    }
}