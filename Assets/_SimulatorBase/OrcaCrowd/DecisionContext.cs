using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OrcaSimulator.Core
{
    public class DecisionContext : Context
    {
        static Color GIZMO_COLOR = new Color(1, 0, 0, 0.4f);
        [Serializable]
        public class TargetEntry
        {
            [Range(0.0f, 1.0f)]
            public float chance;
            public Context target;
            public int sent;
        }
        [Range(0.0f, 1.0f)]
        public float stop = 0, remove = 1, target = 0;
        public List<TargetEntry> targets = new List<TargetEntry>();
        public float firstExitTime = -1, lastExitTime = -1;
        public int totalSent = 0;

        public int agentCountForLocalStatistics = -1;
        public Action<Agent> onAgentExit;

        // Statistics
        List<StatisticEntry> statistics = new List<StatisticEntry>();
        class StatisticEntry
        {
            public int simulatedFrames;
            public float globalExitTime, globalStartTime;
            public float meanSpeed, meanDensity;
        }
        public float local_startTime, local_SimTm, local_AvgFrames, local_AvgSp, local_AvgDn, local_FET;

        // Agent CRUD Methods
        public override void EnterContext(Agent ag)
        {
            base.EnterContext(ag);
            if (ag.target != this)
                return; // if agent is not coming here, do nothing


            float f = UnityEngine.Random.Range(0.0f, stop + remove + target);

            if (f < stop) // agent should stop;
                StopAgent(ag);
            else if (f < stop + remove) // agent should be removed;
                RemoveAgent(ag);
            else if (f < stop + remove + target) // agent should change target;
                ChangeAgentTarget(ag);



            if (agentCountForLocalStatistics > statistics.Count)
            {
                CollectStatisticData(ag);

                if (agentCountForLocalStatistics == statistics.Count)
                {
                    CalculateLocalStatistics();
                }
            }

        }
        public virtual void ChangeAgentTarget(Agent ag)
        {
            if (targets.Count > 1)
                name = name;

            foreach (var ta in targets)
            {
                if (ta.sent / (float)totalSent <= ta.chance)
                {
                    ag.SetTarget(ta.target);
                    ta.sent++;
                    totalSent++;
                    return;
                }
            }

            //int r = UnityEngine.Random.Range(0, targets.Count);
            int r = 0;
            ag.SetTarget(targets[r].target);
            targets[r].sent++;
            totalSent++;
        }
        private void RemoveAgent(Agent ag)
        {
            if (firstExitTime < 0)
                firstExitTime = SimulationManager.manager.simulatedTime;
            lastExitTime = SimulationManager.manager.simulatedTime;

            if (onAgentExit != null)
                onAgentExit.Invoke(ag);

            SimulationManager.manager.DeactivateAgent(ag);
        }
        private void StopAgent(Agent ag)
        {
            ag.SetTarget(null);
        }

        public void AddTargetEntry(Context target, float chance)
        {
            TargetEntry te = new TargetEntry();
            te.target = target;
            te.chance = Mathf.Clamp01(chance);
            te.sent = 0;

            targets.Add(te);
        }
        public void RebalanceChances()
        {
            float sum = 0;
            foreach (var item in targets)
                sum += item.chance;

            foreach (var item in targets)
                item.chance = item.chance / sum;
        }

        // Statistic methods
        void CollectStatisticData(Agent ag)
        {
            statistics.Add(new StatisticEntry()
            {
                simulatedFrames = ag.simulatedFrames_temp,
                meanSpeed = ag.meanSpeed_temp,
                meanDensity = ag.meanDensity_temp,
                globalExitTime = SimulationManager.manager.simulatedTime,
                globalStartTime = ag.startTime_temp
            });

            ag.ResetTempData();
        }
        void CalculateLocalStatistics()
        {
            local_SimTm = local_AvgFrames = local_AvgSp = local_AvgDn = local_FET = 0;

            if (statistics.Count > 0)
            {
                foreach (var agent in statistics)
                {
                    local_AvgDn += agent.meanDensity;
                    local_AvgFrames += agent.simulatedFrames;
                    local_AvgSp += agent.meanSpeed;
                }
                local_AvgDn /= statistics.Count;
                local_AvgSp /= statistics.Count;
                local_AvgFrames /= statistics.Count;
            }

            local_startTime = statistics.Min(x => x.globalStartTime);
            local_SimTm = statistics.Max(x => x.globalExitTime) - local_startTime;
            local_FET = statistics.Min(x => x.globalExitTime) - local_startTime;
        }


        protected override void OnDrawGizmosSelected()
        {
            Gizmos.color = GIZMO_COLOR;
            //DebugExtension.DebugCylinder(transform.position, transform.position + transform.up * height, Gizmos.color, radius);
            //Gizmos.DrawSphere(transform.position, radius);
            foreach (var item in targets)
            {
                if (item.target != null)
                    Gizmos.DrawLine(transform.position, item.target.transform.position);
            }
        }
        public override void Reset()
        {
            base.Reset();

            firstExitTime = -1;
        }
    }
}
