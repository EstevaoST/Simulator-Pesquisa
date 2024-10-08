using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NurseroomObstacleGenerator : ObstacleGenerator
{
    [Header("Generator Settings")]
    public Vector2 bedSize = new Vector2(0.95f, 2.05f); // Neufert 389:4-6
    public float   bedWallMargin = 0.45f; // Neufert 388:6
    public Vector2 bedsMinMargin = new Vector2(0.9f, 1.2f); // Neufert 388:6
    public float   tableSize = 0.45f; // Neufert 388 ~ não achei exatamente mas induzi

    public override void Generate()
    {
        Rect room = new Rect(-roomWidth * 0.5f, -roomLength * 0.5f, roomWidth, roomLength);
        room.x += bedWallMargin;
        room.y += bedWallMargin;
        room.width -= bedWallMargin * 2;
        room.height -= bedWallMargin * 2;

        // beds on back
        if(room.height >= bedSize.y && room.width >= bedSize.x)
        {
            Rect back = new Rect(room);
            back.yMax = back.yMin + bedSize.y;
            back.x += bedSize.x / 2.0f;
            back.width -= bedSize.x;

            float fBeds = Mathf.Lerp(0, (back.width - bedSize.x) / (bedSize.x + bedsMinMargin.x), obstacularity);
            int nBeds = 1 + Mathf.FloorToInt(fBeds);
            for (int i = 0; i < nBeds; i++)
            {
                Transform o = CreateObstacle();

                o.localScale = bedSize.ToVec3XZ(1);
                float xPos = Mathf.Lerp(back.xMin, back.xMax, i / (nBeds - 1.0f));                
                o.localPosition = new Vector3(xPos, 0, back.center.y);
            }

            float margin = back.height + bedsMinMargin.y;
            room.y      += margin;
            room.height -= margin;
        }

        // sides
        Vector2 bedSizeT = new Vector2(bedSize.y, bedSize.x); // bedsize transposed
        if (room.height >= bedSizeT.y && room.width >= bedSize.x * 2 + 0.6f)
        {
            // left
            Rect left = new Rect(room);
            left.width   = bedSizeT.x;
            left.y      += bedSizeT.x / 2.0f;
            left.height -= bedSizeT.x;

            float fBeds = Mathf.Lerp(0, (left.height - bedSizeT.y) / (bedSizeT.y + bedsMinMargin.x), obstacularity);
            int nBeds = 1 + Mathf.FloorToInt(fBeds); // aqui é o x mesmo, estamos trabalhando com camas rotacionadas
            for (int i = 0; i < nBeds; i++)
            {
                Transform o = CreateObstacle();

                o.localScale = bedSize.ToVec3XZ(1);
                o.Rotate(Vector3.up, 90);
                float zPos = Mathf.Lerp(left.yMin, left.yMax, i / (nBeds - 1.0f));
                o.localPosition = new Vector3(left.center.x, 0, zPos);
            }

            // right
            Rect right = new Rect(room);
            right.xMin    = right.xMax - bedSizeT.x;
            right.width   = bedSizeT.x;
            right.y      += bedSizeT.x / 2.0f;
            right.height -= bedSizeT.x;

            for (int i = 0; i < nBeds; i++)
            {
                Transform o = CreateObstacle();

                o.localScale = bedSize.ToVec3XZ(1);
                o.Rotate(Vector3.up, -90);
                float zPos = Mathf.Lerp(right.yMin, right.yMax, i / (nBeds - 1.0f));
                o.localPosition = new Vector3(right.center.x, 0, zPos);
            }
        }

        // tables
        List<Transform> beds = new List<Transform>();
        foreach (Transform bed in transform)
        {
            beds.Add(bed);
        }
        foreach(Transform bed in beds)
        { 
            Transform o = CreateObstacle();

            o.localScale = new Vector3(tableSize, 0.85f, tableSize);
            o.rotation = bed.rotation;
            o.position = bed.position
                         + (-bed.right * bed.localScale.x - bed.forward * bed.localScale.z) * 0.5f
                         + (-bed.right * o.localScale.x   + bed.forward * o.localScale.z  ) * 0.5f;
        }
    }
}
