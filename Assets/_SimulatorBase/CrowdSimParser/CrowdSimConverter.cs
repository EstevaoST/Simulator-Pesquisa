using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Estimation;
using OrcaSimulator.Core;

public class CrowdSimConverter : MonoBehaviour {

    public TextAsset crowdSimFile;
    public Transform roomPrefab;
    public DecisionContext contextPrefab;
    public SpawnerContext spawnerPrefab;
    public Material stairsMaterial;
    public Material doorMaterial;
    
    //[Range(0.5f,10)]
    //public float doorMaxArea = 3;
    //public float doorProportion = 3;

    //public float stairsVariation = 10;



    public float floorSize = 4;
    public float sizeY = 0.2f;
    [Range(0f,1)]
    public float inflation = 0.1f;
    public float contextInflation = -0.3f;


    // reading inter elements
    XElement x;
    Vector3[] vertices;
    Vector2[] arestas;
    
    struct Tuple<A, B>
    {
        public A value1;
        public B value2;


        public Tuple(A a, B b)
        {
            value1 = a;
            value2 = b;
        }
    }

    // converting to estimation variables
    [HideInInspector]
    public Transform scenarioRoot, contextsRoot;
    [HideInInspector]
    public string filename = "convertedFile";
    public Vector2 estimationScale;
    public float estimationFloorSize = 300, estimationPadding = 100;
    public int maxXRooms = 10;
    public bool renameScenarioTransforms = false;


    public void ReadNGenerateModel()
    {
        x = XElement.Parse(crowdSimFile.text);


        //cleanup
        foreach (Transform item in transform)
        #if(UNITY_EDITOR)
            DestroyImmediate(item.gameObject);
        #else
            Destroy(item.gameObject);
        #endif
        // read parts
        ReadVertices();
        ReadArestas();


        // read the room
        ReadNGenerateRooms();
    }
    private void ReadNGenerateRooms()
    {
        var contexts = x.Descendants("contextos").Descendants("contexto").ToList();

        int i = 0;
        foreach (var c in contexts)
        {
            String name = c.Attribute("nomeContexto").Value;
            var pontos = (from r in
                              (from r in c.Descendants("refAresta")
                               select int.Parse(r.Value))
                          select vertices[(int)arestas[r].x]).ToList();

            var borders = new float[] { pontos.Max(x => x.x) + inflation, pontos.Max(x => x.z) + inflation, pontos.Min(x => x.x) - inflation, pontos.Min(x => x.z) - inflation };

            var currentRoom = Instantiate(roomPrefab);
            currentRoom.parent = GetFloorFromHeight(pontos.Average(x => x.y));

            //Vector3 normal = SurfaceNormal(pontos[0], pontos[pontos.Count / 2], pontos[pontos.Count - 1]);
            //float angle = Vector3.Angle(normal, Vector3.up);
            //if (Mathf.Abs(angle - 45) <= stairsVariation)
            //{
            //    currentRoom.GetComponent<Renderer>().material = stairsMaterial;
            //}

            //Vector2 size = new Vector2(Mathf.Abs(borders[0] - borders[2]), Mathf.Abs(borders[1] - borders[3]));

            //if ((size.x * size.y < doorMaxArea)) 
            //    //&& (Mathf.Max(size.x / size.y, size.y / size.x)  > doorProportion))
            //{
            //    Debug.Log("polygon too small and rectangular, must be a door: " + name);
            //    currentRoom.GetComponent<Renderer>().material = doorMaterial;
            //}


            currentRoom.position = new Vector3(
                (borders[0] + borders[2]) / 2,
                pontos.Average(x => x.y),
                (borders[1] + borders[3]) / 2
                );

            currentRoom.localScale = new Vector3(
                Mathf.Abs(borders[0] - borders[2]),
                sizeY,
                Mathf.Abs(borders[1] - borders[3])
                );
            currentRoom.name = name;


            i++;
        }

        //Debug.Log("Arestas read: " + arestas.Length);
    }
    public void ReadNGenerateSpawners()
    {
        x = XElement.Parse(crowdSimFile.text);


        //cleanup
        foreach (Transform item in transform)
#if (UNITY_EDITOR)
            DestroyImmediate(item.gameObject);
#else
            Destroy(item.gameObject);
#endif
        // read parts
        ReadVertices();
        ReadArestas();


        // read the room
        ReadNGenerateAgents();
    }
    public void ReadNGenerateAgents()
    {
        var contexts = x.Descendants("contextos").Descendants("contexto").ToList();

        foreach (var c in contexts)
        {
            String name = c.Attribute("nomeContexto").Value;
            int tipo = int.Parse(c.Attribute("tipo_regiao").Value);
            DecisionContext currentRoom = null;

            if (tipo != 0)
            {
                var pontos = (from r in
                                  (from r in c.Descendants("refAresta")
                                   select int.Parse(r.Value))
                              select vertices[(int)arestas[r].x]).ToList();

                var borders = new float[] { pontos.Max(x => x.x) + contextInflation, pontos.Max(x => x.z) + contextInflation, pontos.Min(x => x.x) - contextInflation, pontos.Min(x => x.z) - contextInflation };

                currentRoom = Instantiate((tipo == 2 ? spawnerPrefab : contextPrefab).gameObject).GetComponent<DecisionContext>();
                currentRoom.transform.parent = transform;

                Vector2 size = new Vector2(Mathf.Abs(borders[0] - borders[2]), Mathf.Abs(borders[1] - borders[3]));
                currentRoom.transform.position = new Vector3(
                    (borders[0] + borders[2]) / 2,
                    pontos.Average(x => x.y) + sizeY/2.0f,
                    (borders[1] + borders[3]) / 2
                    );

                currentRoom.transform.localScale = new Vector3(
                    Mathf.Abs(borders[0] - borders[2]),
                    0.01f,
                    Mathf.Abs(borders[1] - borders[3])
                    );
                currentRoom.name = name;
                currentRoom.radius = (size.x + size.y) / 2;

            }
        }



        foreach (var c in contexts)
        {
            String name = c.Attribute("nomeContexto").Value;
            int tipo = int.Parse(c.Attribute("tipo_regiao").Value);
            DecisionContext currentRoom;

            if (tipo >= 1)
            {
                currentRoom = transform.Find(name).GetComponent<DecisionContext>();
                var targets = (from t in c.Descendants("destino")
                               select new Tuple<float, string>(float.Parse(t.Attribute("value").Value), t.Attribute("nome").Value)).ToList();


                if (targets.Count > 0)
                {
                    currentRoom.target = 1;
                    currentRoom.stop = 0;
                    currentRoom.remove = 0;
                    foreach (var t in targets)
                        currentRoom.AddTargetEntry(transform.Find(t.value2).GetComponent<Context>(), t.value1);
                }
                else
                {
                    currentRoom.target = 0;
                    currentRoom.stop = 0;
                    currentRoom.remove = 1;
                }


                if (tipo == 2)
                {
                    var spawnerRoom = currentRoom as SpawnerContext;
                    spawnerRoom.spawnTotal = int.Parse(c.Attribute("totHumanoides").Value);
                }
            }
        }
    }

    private void ReadArestas()
    {
        var arestasNode = x.Descendants("arestas").Descendants("aresta").ToList();

        arestas = new Vector2[arestasNode.Count];
        int i = 0;
        foreach (var v in arestasNode)
        {
            arestas[i] = new Vector3(
                float.Parse(v.Element("refVertice1").Value),
                float.Parse(v.Element("refVertice2").Value)
                );


            i++;
        }

        Debug.Log("Arestas read: " + arestas.Length);
    }
    private void ReadVertices()
    {
        var vertsNode = x.Descendants("vertices").Descendants("vertice").ToList();

        vertices = new Vector3[vertsNode.Count];
        int i = 0;
        foreach (var v in vertsNode)
        {
            vertices[i] = new Vector3(
                float.Parse(v.Element("posx").Value),
                float.Parse(v.Element("posy").Value),
                float.Parse(v.Element("posz").Value)
                );


            i++;
        }

        Debug.Log("Vertices read: " + vertices.Length);
    }



    public void ConvertToEstimatorFile()
    {
        List<Room> rooms = new List<Room>();
        List<Transform> floorRoots = new List<Transform>();
        List<Transform> roomsTrans = new List<Transform>();
        List<Transform> doorForLater = new List<Transform>();
        List<Transform> stairsForLater = new List<Transform>();

        Vector2 min = Vector2.zero, max = Vector2.zero;

        foreach (Collider c in scenarioRoot.GetComponentsInChildren<Collider>())
        {
            var p1 = c.bounds.max;
            var p2 = c.bounds.min;

            max.x = Mathf.Max(max.x, p1.x, p2.x);
            max.y = Mathf.Max(max.y, p1.z, p2.z);
            min.x = Mathf.Min(min.x, p1.x, p2.x);
            min.y = Mathf.Min(min.y, p1.z, p2.z);
        }

        foreach (Transform f in scenarioRoot)
        {
            if (f.name.ToLower().Contains("floor"))
            {
                foreach (Transform t in f)
                {
                    string name = "Floor";
                    if (t.name.ToLower().Contains("door"))
                    {
                        doorForLater.Add(t);
                        continue;
                    }
                    if (t.name.ToLower().Contains("stair"))
                    {
                        stairsForLater.Add(t);
                        name = "Stair";
                    }


                    name += " " + t.parent.GetSiblingIndex() + " - " + t.GetSiblingIndex();
                    if (renameScenarioTransforms)
                        t.name = name;

                    Room r = new Room(t.name);

                    r.width = t.localScale.x;
                    r.length = t.localScale.z;
                    r.newName = name;


                    Vector2 pos = new Vector2(roomsTrans.Count, 0);
                    pos.y = (int)(pos.x / maxXRooms);
                    pos.x -= pos.y * maxXRooms;
                    pos.x *= estimationScale.x;
                    pos.y *= estimationScale.y;
                    r.x = (int)pos.x; r.y = (int)pos.y;

                    roomsTrans.Add(t);
                    rooms.Add(r);
                }
            }
        }
        Room fromR, toR;
        int connectionCount = 0;
        foreach (Transform f in doorForLater)
        {
            var ordered = roomsTrans.FindAll(x => CheckCollisionPoorly(x.GetComponent<Collider>(), f.GetComponent<Collider>()));
            ordered =  ordered.OrderBy(x => Vector3.Dot(f.forward, (x.GetComponent<Collider>().ClosestPoint(f.position) - f.transform.position).normalized)).ToList();

            if (ordered.Count >= 2)
            {

                var from = ordered[0];
                var to = ordered[ordered.Count - 1];


                fromR = rooms.Find(x => x.name == from.name);
                toR = rooms.Find(x => x.name == to.name);

                if (fromR == null || toR == null)
                    fromR = toR = null;
                else
                {
                    fromR.AddConnection(toR);
                    fromR.exitSize += f.localScale.x;
                    connectionCount++;
                }
            }
            else
                fromR = toR = null;
        }

        foreach (Transform s in stairsForLater)
        {
            Room sr = rooms.Find(x => x.name == s.name);
            var ordered = roomsTrans.FindAll(x => x.name != sr.name && CheckCollisionPoorly(x.GetComponent<Collider>(), s.GetComponent<Collider>()));
            ordered = ordered.OrderBy(x => Vector3.Dot(s.forward, (x.GetComponent<Collider>().ClosestPoint(s.position) - s.transform.position).normalized)).ToList();

            if (ordered.Count >= 2)
            {
                var from = ordered[0];
                var to = ordered[ordered.Count - 1];
                

                fromR = rooms.Find(x => x.name == from.name);
                toR = rooms.Find(x => x.name == to.name);

                if (fromR == null || toR == null)
                    fromR = toR = null;
                else
                {
                    fromR.AddConnection(sr);
                    sr.AddConnection(toR);

                    sr.exitSize = s.localScale.x;
                    fromR.exitSize += sr.exitSize;

                    connectionCount++;
                }
            }
            else
                fromR = toR = null;
        }



        foreach (SpawnerContext c in contextsRoot.GetComponentsInChildren<SpawnerContext>())
        {
            var ordered = roomsTrans.OrderBy(x => Vector3.Distance(x.transform.position, c.GetComponent<Collider>().ClosestPointOnBounds(x.transform.position)));
            var nearest = ordered.FirstOrDefault();

            Room r = rooms.Find(x => x.name == nearest.name);
            r.initialPopulation += c.spawnTotal;
        }

        Estimation.EstimatorSaver.Save(filename + ".txt", rooms);
        Debug.Log("Saved " + rooms.Count + " rooms!");
        Debug.Log("Saved " + connectionCount + " connections!");

    }
    public bool CheckCollisionPoorly(Collider a, Collider b)
    {
        var bb = b.bounds;
        var b8points = new Vector3[] { bb.min, new Vector3(bb.min.x, bb.min.y, bb.max.z), new Vector3(bb.min.x, bb.max.y, bb.min.z), new Vector3(bb.min.x, bb.max.y, bb.max.z)
                        , new Vector3(bb.max.x, bb.min.y, bb.min.z), new Vector3(bb.max.x, bb.min.y, bb.max.z), new Vector3(bb.max.x, bb.max.y, bb.min.z), bb.max};

        foreach (var p in b8points)
        {
            if (a.bounds.Contains(p))
                return true;
        }

        return false;
    }





    Vector3 SurfaceNormal(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        Vector3 a, b;
        a = v1 - v2;
        b = v1 - v3;
        return Vector3.Cross(a, b);
    }
    Transform GetFloorFromHeight(float height)
    {
        int floor = (int)(height / floorSize);

        var f = transform.Find("floor " + floor);
        if (f)
            return f;
        else
        {
            f = new GameObject("floor " + floor).transform;
            f.parent = transform;
            return f;
        }
    }
}
