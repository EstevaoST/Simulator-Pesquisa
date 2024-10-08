using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ObstacleGenerator : MonoBehaviour
{
    [Header("Simulation Settings")]
    public float roomWidth;
    public float roomLength;
    [Range(0, 1)] public float obstacularity = 0;
    public Transform obstaclePrefab;

    public abstract void Generate();

    protected Transform CreateObstacle()
    {
        Transform t = Instantiate(obstaclePrefab).transform;// new GameObject("Obstacle").transform;
        t.SetParent(this.transform);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;

        t.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>().carving = true;
        return t;
    }

    public static readonly string[] DefaultObstacleDataNames = { "Obst.Level", "Obst.Area" };

    // Data saving features
    public virtual string[] ObstacleDataNames => DefaultObstacleDataNames;
    public virtual float[] GetObstacleData()
    {
        return new float[] {obstacularity, GetObstacleArea()};
    }
    public float GetObstacleArea()
    {
        float area = 0;
        foreach (Transform t in transform)
        {
            area += t.localScale.x * t.localScale.z;
        }
        return area;
    }
}
public enum ObstacleType
{
    Empty,
    Square,
    Classroom,
    RandomNxN,
    Labyrinth,
    Nurseroom,
    Church_Hall
}

public static class Helper
{ 
    public static Type GetGeneratorType(this ObstacleType type)
    {
        switch (type)
        {
            case ObstacleType.Empty: return null;
            case ObstacleType.Square: return typeof(SquareObstacleGenerator);
            case ObstacleType.Classroom: return typeof(ClassroomObstacleGenerator);
            case ObstacleType.RandomNxN: return typeof(RandomNxNObstacleGenerator);
            case ObstacleType.Labyrinth: return typeof(LabyrinthObstacleGenerator);
            case ObstacleType.Nurseroom: return typeof(NurseroomObstacleGenerator);
            case ObstacleType.Church_Hall: return typeof(ChurchHallObstacleGenerator);
        }
        return null;
    }
}
