using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SelfOrganizingMap
{
    class Program
    {
        static void Main(string[] args)
        {
            Random r = new Random();

            AdachSOM som = new AdachSOM(4, null);

            List<string> lab = new List<string>();
            List<double[]> dat = new List<double[]>();

            for (int i = 0; i < 225; i++)
            {
                lab.Add("test" + i.ToString());
                dat.Add(new double[] { r.NextDouble() });
                som.Add(lab.Last(), dat.Last());
            }

            som.StartFitting();
            Thread.Sleep(1000);
            som.StopFitting();
            Console.WriteLine("\n");
            for (int i = 0; i < lab.Count; i++)
            {
                Console.ForegroundColor = (ConsoleColor)1 + som.GetGroup(dat[i]);
                Console.Write("Group: " + som.GetGroup(dat[i]) + "");
                foreach (double d in dat[i])
                    Console.Write(" |" + d + "| ");
                Console.WriteLine("\n");
            }
            Console.ReadKey();
        }
    }

}
