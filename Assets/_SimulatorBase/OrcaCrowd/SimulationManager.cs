using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using UnityEngine.SceneManagement;
using System;
using System.Threading;
using System.Globalization;

namespace OrcaSimulator.Core
{
    [DefaultExecutionOrder(-1)]
    public class SimulationManager : MonoBehaviour
    {
        // singleton
        public static SimulationManager manager;

        public static List<LineSegment> navmeshBorders;
        [HideInInspector] public List<Agent> agentList = new List<Agent>();
        [NonSerialized] public List<Agent> activeAgentList = new List<Agent>();
        [HideInInspector] private List<Agent> toUpdateStateAgents = new List<Agent>();
        [HideInInspector] public List<Context> contexts = new List<Context>();
        [HideInInspector] public List<DecisionContext> decisionContexts = new List<DecisionContext>();

        public Agent defaultAgentPrefab;
        public bool limitTime = false;
        public int timeLimit = 50000;

        [Header("Runtime")]
        public int scenecount, agentcount;
        public float stepTime = -1;
        private bool firstFrame = false;
        public bool finished = false;
        public float simulatedTime = 0;
        public int simulatedFrame = 0;
        public bool simulationInterrupted = false;
        protected Coroutine simulationRoutine;

        [Header("ORCA Settings")]
        public float ORCATime = 1.0f;
        [Range(0.0f, 1.0f)]
        public float ORCAOtherPercent = 1.0f;



        //[Header("Space Division")]
        //public float spaceDivisionCellSize = 1;
        //Dictionary<Vector2, List<Agent>> spaceDivision = new Dictionary<Vector2, List<Agent>>();

        // Simulation Events
        public Action OnSimulationFinished;
        public Action OnStepFinished;

        // unity events
        private void Awake()
        {
            if (manager != null)
                Destroy(this);
            else
                manager = this;
        }
        protected virtual void Start()
        {
            if (stepTime <= 0)
                stepTime = Time.deltaTime;
            if (simulationRoutine == null)
                simulationRoutine = StartCoroutine(SimulationGo());
            navmeshBorders = ShowNavmeshEdges.FindNavMeshBorders(UnityEngine.AI.NavMesh.CalculateTriangulation());
#if UNITY_EDITOR
        if(UnityEditor.Selection.activeObject == null)
            UnityEditor.Selection.SetActiveObjectWithContext(gameObject, this);
#endif
        }
        private void OnDestroy()
        {
            if (manager == this)
                manager = null;
        }

        // simulation events
        protected virtual IEnumerator SimulationGo()
        {
            yield return new WaitForSeconds(1);
            int timer = 0;
            simulatedTime = 0;
            simulatedFrame = 0;
            finished = false;
            simulationInterrupted = false;
            AgentORCA.inverseORCAtime = 1.0f / ORCATime;
            firstFrame = true;
            var wait = new WaitForFixedUpdate();

            DateTime start, finish;


            foreach (var c in decisionContexts)
            {
                c.onAgentExit += SaveAgentExit;
            }


            while (!finished)
            {
                start = DateTime.Now;

                //SimulationMultithreadStep(stepTime);
                SimulationParallelStep(stepTime);
                //SimulationLinearStep(stepTime);

                SimulationUpdateAgentsStateStep();

                simulatedFrame++;
                simulatedTime += stepTime;


                if (--timer <= 0)
                {
                    yield return wait;
                    timer = SimGlobalConfig.framesToUpdate.value;
                }

                if (OnStepFinished != null)
                    OnStepFinished.Invoke();

                if (IsSimulationFinished() || (limitTime && simulatedTime > timeLimit))
                {
                    if (simulatedTime > timeLimit)
                        InterruptSimulation();
                    finished = true;
                }

                firstFrame = false;
                finish = DateTime.Now;
            }

            SimulationFinished();
        }

        protected void SimulationParallelStep(float time)
        {
            foreach (var item in activeAgentList)
                item.IntentionStep(time);

            foreach (var item in activeAgentList)
                if (item is AgentORCA)
                    (item as AgentORCA).AvoidanceStep(time);

            foreach (var item in activeAgentList)
                item.Step(time);

            foreach (var item in contexts)
                item.Step(time);

        }
        protected void SimulationMultithreadStep(float time)
        {
            int nthreads = 4;
            int batch = activeAgentList.Count / nthreads;
            Thread[] threads = new Thread[nthreads];

            //foreach (var item in agentList)
            //    item.IntentionStep(time);
            for (int i = 0; i < nthreads; i++)
            {
                int init = batch * i;
                int end = batch * (i + 1);
                if (i == nthreads - 1)
                    end = activeAgentList.Count;

                threads[i] = new Thread(() =>
                {
                    for (int j = init; j < end; j++)
                        activeAgentList[j].IntentionStep(time);
                });
                threads[i].Start();
            }

            foreach (var item in threads)
                item.Join(2000);

            for (int i = 0; i < nthreads; i++)
            {
                int init = batch * i;
                int end = batch * (i + 1);
                if (i == nthreads - 1)
                    end = agentList.Count;

                threads[i] = new Thread(() =>
                {
                    for (int j = init; j < end; j++)
                        if (agentList[j] is AgentORCA)
                            (agentList[j] as AgentORCA).AvoidanceStep(time);
                });
                threads[i].Start();
            }
            foreach (var item in threads)
                item.Join(2000);

            foreach (var item in activeAgentList)
                item.Step(time); // can't multithread here, since this moves the transform
            foreach (var item in contexts)
                item.Step(time);

        }
        protected void SimulationLinearStep(float time)
        {
            foreach (var item in activeAgentList)
            {
                item.IntentionStep(time);
                if (item is AgentORCA)
                    (item as AgentORCA).AvoidanceStep(time);

                item.Step(time);
            }

            foreach (var item in contexts)
                item.Step(time);
        }

        protected void SimulationUpdateAgentsStateStep()
        {
            foreach (var item in toUpdateStateAgents)
            {
                activeAgentList.Remove(item);
            }
            toUpdateStateAgents.Clear();
        }

        public void InterruptSimulation()
        {
            simulationInterrupted = true;
        }
        public virtual void SimulationFinished()
        {
            SaveSummaryResult();

            if (OnSimulationFinished != null)
                OnSimulationFinished.Invoke();
            if (exitsFile != null)
                exitsFile.Close();
        }

        // utils
        public bool IsSimulationFinished()
        {
            foreach (var item in activeAgentList)
            {
                if (item.target != null)
                    return false;
            }
            foreach (var item in contexts)
            {
                if (!item.IsFinished)
                    return false;
            }
            return true;
        }
        public bool IsFirstFrame()
        {
            return firstFrame;
        }

        // Agents/Contexts CRUD Methods
        public void RegisterAgent(Agent agent)
        {
            agentList.Add(agent);
            activeAgentList.Add(agent);
        }
        public void DeactivateAgent(Agent ag)
        {
            ag.gameObject.SetActive(false);
        }
        public void UpdateAgentState(Agent agent)
        {
            if (agent.isActiveAndEnabled)
                return;
            if (toUpdateStateAgents.Contains(agent))
                return;

            toUpdateStateAgents.Add(agent);
        }
        public void RegisterContext(Context context)
        {
            contexts.Add(context);
            if (context is DecisionContext)
                decisionContexts.Add(context as DecisionContext);
        }

        // space division methods [not currently used]
        //static readonly Vector2[] spaceDirections = new Vector2[] { Vector2.zero, Vector2.up, Vector2.up + Vector2.right, Vector2.right, Vector2.right + Vector2.down, Vector2.down, Vector2.down + Vector2.left, Vector2.left, Vector2.left + Vector2.up };
        //public Vector2 WorldToDivisionSpace(Vector3 pos)
        //{
        //    Vector2 p = new Vector2(pos.x, pos.z);
        //    p /= spaceDivisionCellSize;
        //    p.x = Mathf.Round(p.x);
        //    p.y = Mathf.Round(p.y);

        //    return p;
        //}
        //public List<Agent> GetDividedSpace(Vector2 pos)
        //{
        //    List<Agent> a;
        //    if (spaceDivision.TryGetValue(pos, out a))
        //        return a;
        //    else
        //        return null;
        //}
        //public bool RemoveAgentFromSpace(Agent agt, Vector2 space)
        //{
        //    var divided = GetDividedSpace(space);
        //    if (divided != null)
        //        return divided.Remove(agt);
        //    return false;
        //}
        //public bool AddAgentToSpace(Agent agt)
        //{
        //    return AddAgentToSpace(agt, WorldToDivisionSpace(agt.transform.position));
        //}
        //public bool AddAgentToSpace(Agent agt, Vector2 space)
        //{
        //    var divided = GetDividedSpace(space);
        //    if (divided != null)
        //        divided.Add(agt);
        //    else
        //    {
        //        var list = new List<Agent>();
        //        list.Add(agt);
        //        spaceDivision.Add(space, list);
        //    }


        //    return true;
        //}
        //public void MoveAgentDivisionSpace(Agent agt, Vector3 oldPos, Vector3 newPos)
        //{
        //    var oldspace = WorldToDivisionSpace(oldPos);
        //    var newspace = WorldToDivisionSpace(newPos);

        //    if (oldspace != newspace) {
        //        RemoveAgentFromSpace(agt, oldPos);
        //        AddAgentToSpace(agt, newPos);
        //    }
        //}
        //public IEnumerable<Agent> GetNeighboursEnumerator(Vector3 pos) {
        //    var mySpace = WorldToDivisionSpace(pos);
        //    for (int i = 0; i < spaceDirections.Length; i++)
        //    {
        //        var compareSpace = mySpace + spaceDirections[i];
        //        var space = GetDividedSpace(compareSpace);
        //        if (space == null)
        //            continue;


        //        for (int j = 0; j < space.Count; j++)
        //            yield return space[j];

        //    }
        //}

        // Save files methods
        protected virtual void SaveSummaryResult()
        {

        }
        private StreamWriter exitsFile = null;
        protected virtual void SaveAgentExit(Agent ag)
        {
            if (exitsFile == null)
            {
                string filename = SceneManager.GetActiveScene().name + "_exits";
                exitsFile = new StreamWriter(filename + ".csv", true);
                exitsFile.WriteLine("Agent;X;Y;Z;time");
            }

            var p = ag.transform.position;
            exitsFile.WriteLine(string.Format("{0};{1};{2};{3};{4}",
                ag.name,
                p.x.ToString(SimGlobalConfig.ExportFormat),
                p.y.ToString(SimGlobalConfig.ExportFormat),
                p.z.ToString(SimGlobalConfig.ExportFormat),
                simulatedTime.ToString(SimGlobalConfig.ExportFormat)
            ));
        }

        // automatization
        #region Automatization
        public void ReadScenePaulo(string agentfile = "agents.dat", string scenefile = "Obstacles.csv")
        {
            scenefile = "Assets/Files/" + scenefile;
            agentfile = "Assets/Files/" + agentfile;
            StreamReader sr;
            string line;

            // cleanup
            var aux = GameObject.Find("Obstacles");
            var obstacles = new GameObject("Obstacles");
            if (aux != null)
            {
                obstacles.transform.position = aux.transform.position;
                obstacles.transform.rotation = aux.transform.rotation;
                obstacles.transform.localScale = aux.transform.localScale;
                DestroyImmediate(aux);
            }
            aux = GameObject.Find("Agents");
            var agents = new GameObject("Agents");
            if (aux != null)
            {
                agents.transform.position = aux.transform.position;
                agents.transform.rotation = aux.transform.rotation;
                agents.transform.localScale = aux.transform.localScale;
                DestroyImmediate(aux);
            }
            aux = GameObject.Find("Goals");
            var goals = new GameObject("Goals");
            if (aux != null)
            {
                goals.transform.position = aux.transform.position;
                goals.transform.rotation = aux.transform.rotation;
                goals.transform.localScale = aux.transform.localScale;
                DestroyImmediate(aux);
            }

            // read obstacles
            sr = new StreamReader(scenefile);
            while (!sr.EndOfStream)
            {
                line = sr.ReadLine();
                if (line == "Obstacle")
                    ReadAnObstacle(sr);
            }
            sr.Close();

            // read agents
            sr = new StreamReader(agentfile);
            sr.ReadLine();
            while (!sr.EndOfStream)
            {
                ReadAnAgent(sr);
                //line = sr.ReadLine();
            }
            sr.Close();

            //UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
            //UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        }
        private void ReadAnObstacle(StreamReader sr)
        {
            string line = "";
            string[] split;
            var mesh = new Mesh();
            UnityEngine.AI.NavMeshHit nmh = new UnityEngine.AI.NavMeshHit();
            List<Vector3> vecs = new List<Vector3>();
            List<int> tris = new List<int>();
            do
            {
                line = sr.ReadLine();
                if (line.Contains("qntVertices"))
                {
                    line = sr.ReadLine();
                    while (line.Contains(";"))
                    {
                        split = line.Split(';');
                        var pos = new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
                        if (UnityEngine.AI.NavMesh.SamplePosition(pos, out nmh, 1, UnityEngine.AI.NavMesh.AllAreas))
                            pos = nmh.position;
                        vecs.Add(pos);
                        line = sr.ReadLine();
                    }
                }
                if (line.Contains("qntTriangles"))
                {
                    line = sr.ReadLine();

                    while (!string.IsNullOrEmpty(line))
                    {
                        tris.Add(int.Parse(line));
                        line = sr.ReadLine();
                    }
                }
            }
            while (!string.IsNullOrEmpty(line));

            var obstacles = GameObject.Find("Obstacles");
            var go = new GameObject("Obstacle");
            go.transform.SetParent(obstacles.transform, false);

            go.AddComponent<MeshFilter>().mesh = mesh;
            go.AddComponent<MeshRenderer>();
            mesh.vertices = vecs.ToArray();
            mesh.triangles = tris.ToArray();

#if UNITY_EDITOR
        UnityEditor.GameObjectUtility.SetStaticEditorFlags(go, UnityEditor.StaticEditorFlags.NavigationStatic);
        UnityEditor.GameObjectUtility.SetNavMeshArea(go, 1);
#endif
            Debug.Log("Obstacle created");

        }
        private void ReadAnAgent(StreamReader sr)
        {
            string line = sr.ReadLine();
            if (sr.EndOfStream)
                return;
            var agents = GameObject.Find("Agents");
            var agent_prefab = defaultAgentPrefab;
            var goals = GameObject.Find("Goals");
            List<Context> lgoals = new List<Context>();
            while (!string.IsNullOrEmpty(line))
            {
                var nums = line.Split(',');

                Vector3 pos = Vector3.zero;
                pos.x = float.Parse(nums[0]);
                pos.y = float.Parse(nums[1]);
                pos.z = float.Parse(nums[2]);

                UnityEngine.AI.NavMeshHit nmh = new UnityEngine.AI.NavMeshHit();
                if (UnityEngine.AI.NavMesh.SamplePosition(pos, out nmh, 1f, UnityEngine.AI.NavMesh.AllAreas))
                    pos = nmh.position;


                var goal = new GameObject("Goal");
                goal.transform.SetParent(goals.transform);
                goal.transform.localPosition = pos;
                lgoals.Add(goal.AddComponent<DecisionContext>());
                Debug.Log("Goal created");
                line = sr.ReadLine();
            }
            while (string.IsNullOrEmpty(line))
                line = sr.ReadLine();
            line = sr.ReadLine();
            while (!string.IsNullOrEmpty(line))
            {
                var a = line.Split(':');
                var nums = a[0].Split(',');

                Vector3 pos = Vector3.zero;
                pos.x = float.Parse(nums[0]);
                pos.y = float.Parse(nums[1]);
                pos.z = float.Parse(nums[2]);

                UnityEngine.AI.NavMeshHit nmh = new UnityEngine.AI.NavMeshHit();
                if (UnityEngine.AI.NavMesh.SamplePosition(pos, out nmh, 1f, UnityEngine.AI.NavMesh.AllAreas))
                    pos = nmh.position;


                var agent = Instantiate(agent_prefab.gameObject).GetComponent<Agent>();
                agent.name = "Agent_" + this.name;
                agent.transform.SetParent(agents.transform);
                agent.transform.localPosition = pos;

                if (a.Length > 1)
                {
                    var b = int.Parse(a[1]);
                    var c = lgoals[b - 1];
                    agent.target = c;
                }
                Debug.Log("Agent created");
                if (sr.EndOfStream)
                    break;
                line = sr.ReadLine();
            }
        }

        public void ReadSceneFile(string filename = "scene.txt")
        {
            filename = "Assets/Files/" + filename;
            StreamReader sr = new StreamReader(filename);
            string line;
            string[] splitline;

            var aux = GameObject.Find("Obstacles");
            var obstacles = new GameObject("Obstacles");
            if (obstacles != null)
            {
                obstacles.transform.position = aux.transform.position;
                obstacles.transform.rotation = aux.transform.rotation;
                obstacles.transform.localScale = aux.transform.localScale;
                DestroyImmediate(aux);
            }


            aux = GameObject.Find("Agents");
            var agents = new GameObject("Agents");
            if (obstacles != null)
            {
                agents.transform.position = aux.transform.position;
                agents.transform.rotation = aux.transform.rotation;
                agents.transform.localScale = aux.transform.localScale;
                DestroyImmediate(aux);
            }

            while (!sr.EndOfStream)
            {
                line = sr.ReadLine();
                splitline = line.Split(':');
                if (splitline.Length != 2)
                    Debug.LogError("Inconsistent line at: " + filename);
                else
                {
                    if (splitline[0] == "obstacle")
                        AddObstacle(splitline[1]);
                    if (splitline[0] == "agent")
                        AddAgent(splitline[1]);
                }
            }
            sr.Close();

            //UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
            //UnityEditor.AI.NavMeshBuilder.BuildNavMesh();        
        }
        private void AddAgent(string a)
        {
            var agents = GameObject.Find("Agents");

            var agent_prefab = defaultAgentPrefab;

            var nums = a.Split(',');
            if (nums.Length < 2)
            {
                Debug.LogError("Could not read position of agent");
                return;
            }
            Vector3 pos = Vector3.zero;
            pos.x = float.Parse(nums[0]);
            pos.y = 0;
            pos.z = float.Parse(nums[1]);

            UnityEngine.AI.NavMeshHit nmh = new UnityEngine.AI.NavMeshHit();
            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out nmh, 0.1f, UnityEngine.AI.NavMesh.AllAreas))
                pos = nmh.position;


            var agent = Instantiate(agent_prefab.gameObject).GetComponent<Agent>();
            agent.name = "Agent_" + this.name;
            agent.transform.SetParent(agents.transform);
            agent.transform.localPosition = pos;
            Debug.Log("Agent created");
        }
        private void AddObstacle(string o)
        {
            var obstacles = GameObject.Find("Obstacles");

            var aux = o.Split(' ');
            if (aux.Length == 0)
            {
                Debug.LogError("Could not read obstacle");
                return;
            }
            Vector3[] vertices = new Vector3[aux.Length];
            UnityEngine.AI.NavMeshHit nmh = new UnityEngine.AI.NavMeshHit();
            for (int i = 0; i < vertices.Length; i++)
            {
                var nums = aux[i].Split(',');
                if (nums.Length < 2)
                {
                    Debug.LogError("Could not read vertex of obstacle");
                    return;
                }
                vertices[i].x = float.Parse(nums[0]);
                vertices[i].y = 0;
                vertices[i].z = float.Parse(nums[1]);

                if (UnityEngine.AI.NavMesh.SamplePosition(vertices[i], out nmh, 0.1f, UnityEngine.AI.NavMesh.AllAreas))
                    vertices[i] = nmh.position;
            }

            int[] triangles = new int[3 * (vertices.Length - 2)];
            for (int i = 0; i < vertices.Length - 2; i++)
            {
                triangles[i * 3 + 0] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            var go = new GameObject("Obstacle");
            go.transform.SetParent(obstacles.transform, false);

            var mesh = new Mesh();
            go.AddComponent<MeshFilter>().mesh = mesh;
            go.AddComponent<MeshRenderer>();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = null;


#if UNITY_EDITOR
        UnityEditor.GameObjectUtility.SetStaticEditorFlags(go, UnityEditor.StaticEditorFlags.NavigationStatic);
        UnityEditor.GameObjectUtility.SetNavMeshArea(go, 1);
#endif
            Debug.Log("Obstacle created");
        }
        #endregion
    }
}
