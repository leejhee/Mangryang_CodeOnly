using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum eLunarPhase
{
    NewMoon,
    WaxingCrescent,
    FirstQuarter,
    FullMon,
    LastQuarter,
    WaningCrescent
}

public class LunarPhaseModel
{
    public eLunarPhase phase;

    public LunarPhaseModel(eLunarPhase phase)
    {
        this.phase = phase;
    }
    
}
