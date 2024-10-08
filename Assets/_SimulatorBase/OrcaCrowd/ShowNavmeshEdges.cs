// Copyright (c) 2014-2015 StagPoint Consulting

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

/*
* This component will render all NavMesh border edges in yellow in the SceneView.
* It does this in order to demonstrate a fast and efficient way to determine all
* outside border edges of a navmesh. This information may be useful for custom
* navigation or terrain analysis, etc.
*/
public class LineSegment
{
    public Vector3 start, end;

    public LineSegment(Vector3 x, Vector3 y)
    {
        this.start = x;
        this.end = y;
    }
}


[ExecuteInEditMode]
public class ShowNavmeshEdges : MonoBehaviour
{

    private List<LineSegment> borderEdges = null;

    //protected void OnEnable()
    //{
    //    this.borderEdges = FindNavMeshBorders(NavMesh.CalculateTriangulation());
    //}

    protected void OnDrawGizmosSelected()
    {

        if (!this.enabled)
            return;

        if(borderEdges == null)
            this.borderEdges = FindNavMeshBorders(NavMesh.CalculateTriangulation());

        Gizmos.color = Color.red;

        for (int i = 0; i < this.borderEdges.Count; i++)
        {
            var edge = this.borderEdges[i];
            Gizmos.DrawLine(edge.start, edge.end);
        }

    }

    public static List<LineSegment> FindNavMeshBorders(NavMeshTriangulation mesh)
    {

        Vector3[] verts = null;
        int[] triangles = null;

        weldVertices(mesh, 0.01f, 2f, out verts, out triangles);

        var map = new Dictionary<uint, int>();

        Action<ushort, ushort> processEdge = (a, b) =>
        {

            if (a > b)
            {
                var temp = b;
                b = a;
                a = temp;
            }

            uint key = ((uint)a << 16) | (uint)b;

            if (!map.ContainsKey(key))
                map[key] = 1;
            else
                map[key] += 1;

        };

        for (int i = 0; i < triangles.Length; i += 3)
        {

            var a = (ushort)triangles[i + 0];
            var b = (ushort)triangles[i + 1];
            var c = (ushort)triangles[i + 2];

            processEdge(a, b);
            processEdge(b, c);
            processEdge(c, a);

        }

        var borderEdges = new List<LineSegment>();

        foreach (var key in map.Keys)
        {

            var count = map[key];
            if (count != 1)
                continue;

            var a = (key >> 16);
            var b = (key & 0xFFFF);
            var line = new LineSegment(verts[a], verts[b]);

            borderEdges.Add(line);

        }

        return borderEdges;

    }

    private static void weldVertices(NavMeshTriangulation mesh, float threshold, float bucketStep, out Vector3[] vertices, out int[] indices)
    {

        // This code was adapted from http://answers.unity3d.com/questions/228841/dynamically-combine-verticies-that-share-the-same.html

        Vector3[] oldVertices = mesh.vertices;
        Vector3[] newVertices = new Vector3[oldVertices.Length];
        int[] old2new = new int[oldVertices.Length];
        int newSize = 0;

        // Find AABB
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        for (int i = 0; i < oldVertices.Length; i++)
        {
            if (oldVertices[i].x < min.x)
                min.x = oldVertices[i].x;
            if (oldVertices[i].y < min.y)
                min.y = oldVertices[i].y;
            if (oldVertices[i].z < min.z)
                min.z = oldVertices[i].z;
            if (oldVertices[i].x > max.x)
                max.x = oldVertices[i].x;
            if (oldVertices[i].y > max.y)
                max.y = oldVertices[i].y;
            if (oldVertices[i].z > max.z)
                max.z = oldVertices[i].z;
        }

        // Make cubic buckets, each with dimensions "bucketStep"
        int bucketSizeX = Mathf.FloorToInt((max.x - min.x) / bucketStep) + 1;
        int bucketSizeY = Mathf.FloorToInt((max.y - min.y) / bucketStep) + 1;
        int bucketSizeZ = Mathf.FloorToInt((max.z - min.z) / bucketStep) + 1;
        List<int>[,,] buckets = new List<int>[bucketSizeX, bucketSizeY, bucketSizeZ];

        // Make new vertices
        for (int i = 0; i < oldVertices.Length; i++)
        {

            // Determine which bucket it belongs to
            int x = Mathf.FloorToInt((oldVertices[i].x - min.x) / bucketStep);
            int y = Mathf.FloorToInt((oldVertices[i].y - min.y) / bucketStep);
            int z = Mathf.FloorToInt((oldVertices[i].z - min.z) / bucketStep);

            // Check to see if it's already been added
            if (buckets[x, y, z] == null)
                buckets[x, y, z] = new List<int>(); // Make buckets lazily

            for (int j = 0; j < buckets[x, y, z].Count; j++)
            {
                Vector3 to = newVertices[buckets[x, y, z][j]] - oldVertices[i];
                if (Vector3.SqrMagnitude(to) < threshold)
                {
                    old2new[i] = buckets[x, y, z][j];
                    goto skip; // Skip to next old vertex if this one is already there
                }
            }

            // Add new vertex
            newVertices[newSize] = oldVertices[i];
            buckets[x, y, z].Add(newSize);
            old2new[i] = newSize;
            newSize++;

            skip:
            ;

        }

        // Make new triangles
        int[] oldTris = mesh.indices;
        indices = new int[oldTris.Length];
        for (int i = 0; i < oldTris.Length; i++)
        {
            indices[i] = old2new[oldTris[i]];
        }

        vertices = new Vector3[newSize];
        for (int i = 0; i < newSize; i++)
        {
            vertices[i] = newVertices[i];
        }

    }

}
