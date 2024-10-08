using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Estimation
{
    public abstract class EstimatorSaver
    {

        public static void Save(string filename, List<Room> rooms)
        {
            StreamWriter stream = new StreamWriter(filename);
            // write rooms
            stream.WriteLine("<ROOMS>");

            foreach (Room r in rooms)
            {
                stream.WriteLine("\"" + r.newName + "\"");
                stream.WriteLine(("Shape: " + r.width + "x" + r.length).Replace(".",","));
                stream.WriteLine(("Population: " + r.initialPopulation).Replace(".", ","));
                stream.WriteLine(("Exit size: " + r.exitSize).Replace(".", ","));
            }
            stream.WriteLine("</ROOMS>");

            // write connections
            stream.WriteLine("<FLOWS>");
            foreach (Room r in rooms)
            {
                foreach (var c in r.nexts)
                {
                    stream.WriteLine(("\"" + r.newName + "\"" + ">" + "\"" + c.Key.newName + "\"" + ": " + c.Value).Replace(".",","));
                }
            }
            stream.WriteLine("</FLOWS>");

            // write layout
            stream.WriteLine("<LAYOUT>");
            foreach (var r in rooms)
            {
                stream.WriteLine("\"" + r.newName + "\"" + "@" + r.x + "," + r.y);
            }
            stream.WriteLine("</LAYOUT>");

            stream.Close();

        }

    }
}
