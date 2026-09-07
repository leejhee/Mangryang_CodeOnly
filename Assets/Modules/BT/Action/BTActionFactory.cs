using System;

namespace Modules.BT.Action
{
    public static class BTActionFactory
    {
        public static IBTAction Create(string id)
        {
            return id switch
            {
                "FLEE" => new BTFleeAction(),
                _ => throw new Exception($"[BT Condition Factory] Unknown Condition ID : {id}")
            };
        }
    }
}