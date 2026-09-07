using System;

namespace Modules.BT.Condition
{
    public static class BTConditionFactory
    {
        public static IBTCondition Create(string id)
        {
            return id switch
            {
                "LOW_HP" => new BTLowHPCondition(),
                _ => throw new Exception($"[BT Condition Factory] Unknown Condition ID : {id}")
            };
        }
    }
}