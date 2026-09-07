using System;
using UnityEngine;

[Serializable]
public class SpawnData
{
    public long SpawnCharacterIndex;
    public Vector3 SpawnPosition;

    public SpawnData(long SpawnCharacterIndex, Vector3 SpawnPosition)
    {
        this.SpawnCharacterIndex = SpawnCharacterIndex;
        this.SpawnPosition = SpawnPosition;
    }
}