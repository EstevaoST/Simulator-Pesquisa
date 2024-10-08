using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquareObstacleGenerator : ObstacleGenerator
{
    [Header("Generator Settings")]
    public bool useWalkingMargin = true;
    public float minMargin = 0.8f;


    public override void Generate()
    {
        SetupSquareObstacle();
    }

    private void SetupSquareObstacle()
    {
        Vector3 maxSize = new Vector3(roomWidth, 1, roomLength);
        if (useWalkingMargin) {
            maxSize.x -= minMargin * 2;
            maxSize.z -= minMargin * 2;
        }

        if (obstacularity > 0)
        {
            Transform obst = CreateObstacle();
            obst.localScale = Vector3.Lerp(Vector3.up, maxSize, obstacularity);
        }
    }
}
