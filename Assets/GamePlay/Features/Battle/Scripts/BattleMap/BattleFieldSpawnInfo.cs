using static Core.Scripts.Foundation.Define.SystemEnum;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class BattleFieldSpawnInfo
{
    [SerializeField]
    public List<FieldSpawnInfo> fieldSpawnInfos = new();

    [SerializeField]
    public List<FieldObjectInfo> fieldObjectInfos = new();

    public Dictionary<eCharType, List<SpawnData>> Convert2Dict()
    {
        Dictionary<eCharType, List<SpawnData>> dict = new();
        foreach (FieldSpawnInfo info in fieldSpawnInfos)
        {
            if(!dict.ContainsKey(info.SpawnType))
                dict.Add(info.SpawnType, new List<SpawnData>());
            foreach(SpawnData data in info.UnitSpawnList)
                dict[info.SpawnType].Add(data);
        }
        return dict;
    }
}

/// <summary> 유닛 스폰 정보 </summary>
[Serializable]
public class FieldSpawnInfo
{
    [SerializeField] private eCharType spawnType;
    [FormerlySerializedAs("spawnPositions")] [SerializeField] private List<SpawnData> unitSpawnList = new();

    public eCharType SpawnType => spawnType;
    public List<SpawnData> UnitSpawnList => unitSpawnList;
    public int SpawnerCount => unitSpawnList.Count;



    public FieldSpawnInfo(eCharType spawnType, List<SpawnData> unitSpawnList)
    {
        this.spawnType = spawnType;
        this.unitSpawnList = unitSpawnList;
    }
}

[Serializable]
public class FieldObjectInfo
{
    [SerializeField] private string prefabName;
    [SerializeField] private Vector3 position;

    public string PrefabName => prefabName;
    public Vector3 Position => position;

    public FieldObjectInfo(string prefabName, Vector3 position)
    {
        this.prefabName = prefabName;
        this.position = position;
    }
}