using System;
using System.Collections.Generic;
using System.Linq;

namespace Estimation
{
    public class Room
    {

        // normal
        public string name = "room", newName;
        public double width, length, exitSize, inputFlow, flowDuration;
        public int initialPopulation;
        public int x, y;

        public Dictionary<Room, double> nexts = new Dictionary<Room, double>();

        public List<Room> dependency = new List<Room>();

        // results from estimations
        public bool estimated = false;
        public double simulationTime, averageExitTime, averageSpeed, averageDensity, firstExitTime;
        
        // time variables
        public double cumulativeTime, incomeInitialTime, incomeFinalTime;

        public Room(string name)
        {
            this.name = name;
        }


        public double[,] toMatrix()
        {
            return new double[,] { { width, length, exitSize, inputFlow, flowDuration, initialPopulation } };
        }
        public void NormalizeConnections()
        {
            double sum = 0;
            var keys = nexts.Keys.ToList();
            //foreach (var key in nexts.Keys)

            for (int i = 0; i < keys.Count; i++)
                sum += nexts[keys[i]];

            for (int i = 0; i < keys.Count; i++)
                nexts[keys[i]] = nexts[keys[i]] / sum;
        }
        public void AddConnection(Room r2)
        {
            if (nexts.ContainsKey(r2))
                return;

            double sum = 0;
            var keys = nexts.Keys.ToList();
            //foreach (var key in nexts.Keys)
            
            for (int i = 0; i < keys.Count; i++)
            {
                nexts[keys[i]] = 1.0f / (nexts.Count + 1.0f);
                sum += nexts[keys[i]];
            }

            nexts.Add(r2, 1 - sum);
        }
        public void AddConnection(Room r2, double f)
        {
            if (nexts.ContainsKey(r2))
                return;

            nexts.Add(r2, f);
        }
        public void RemoveConnection(Room room)
        {
            if (nexts.Remove(room))
                NormalizeConnections();
        }
        public bool DependencyFullfilled()
        {
            bool result = true;

            foreach (var item in dependency)
                result &= item.estimated;

            return result;
        }
    }
}
