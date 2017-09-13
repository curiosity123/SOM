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
            //Thread.Sleep(500);
            //   som.StopFitting();
            Thread.Sleep(1000);
            Console.WriteLine("\n");
            for (int i = 0; i < lab.Count; i++)
            {
                Console.ForegroundColor = (ConsoleColor)1 + som.GetGroup(dat[i]);
                Console.Write("Group: " + som.GetGroup(dat[i]) + "");
                foreach (double d in dat[i])
                    Console.Write(" |" + d + "| ");
                Console.WriteLine("\n");
            }



            //for (double i = 0; i < 100; i++)
            //{
            //    double d = i / 100;
            //    Console.ForegroundColor = (ConsoleColor)1 + som.GetGroup(new double[] { d});
            //    Console.Write("Group: " + som.GetGroup(new double[] { d }) + "   value: " + d.ToString());
            //   // Console.Write(" |" +  ((double)(i/100)).ToString() );
            //    Console.WriteLine("\n");
            //}


            Console.ReadKey();
        }
    }

    public class AdachSOM
    {
        bool isStarted = false;
        public delegate void DiagnosticDatas(string diagString);
        event DiagnosticDatas DiagnosticInfo;
        List<double[]> Datas;
        List<string> Labels;
        List<double[]> Neurons;
        double[] DistanceMatrix;
        int howManyGroups = 0;

        public static void NormalizeData(List<double[]> d)
        {
            for (int i = 0; i < d[0].Length; i++)
            {
                double min = 1, max = 1;
                for (int j = 0; j < d.Count; j++)
                {
                    if (d[j][i] > max)
                        max = d[j][i];

                    if (d[j][i] < min)
                        min = d[j][i];
                }

                for (int j = 0; j < d.Count; j++)
                    d[j][i] = (d[j][i] - min) / (max - min);   //alternative algorithm d[j][i] / max;
            }
        }

        public void Add(string _label, double[] _data)
        {
            Labels.Add(_label);
            Datas.Add(_data);
        }

        private void Init()
        {

            Random r = new Random();


            for (int i = 0; i < howManyGroups; i++)
            {
                Neurons.Add(new double[Datas[0].Length]);
                for (int j = 0; j < Datas[0].Length; j++)
                    Neurons[i][j] = r.NextDouble();
            }
        }

        public AdachSOM(int _howManyGroups, DiagnosticDatas _diagnosticDatas = null)
        {
            if (_diagnosticDatas != null)
                DiagnosticInfo = _diagnosticDatas;
            else
                DiagnosticInfo = Console.Write;

            Labels = new List<string>();
            Datas = Datas = new List<double[]>();
            Neurons = Neurons = new List<double[]>();

            howManyGroups = _howManyGroups;

            DistanceMatrix = new double[_howManyGroups];
        }

        public int GetGroup(double[] _data)
        {
            int group = 0;
            double distance = CalculateDistance(_data, Neurons[0]);

            for (int i = 0; i < Neurons.Count; i++)
                if (CalculateDistance(_data, Neurons[i]) < distance)
                {
                    distance = CalculateDistance(_data, Neurons[i]);
                    group = i;
                }

            return group;
        }

        public int GetGroup(string label)
        {
            int index = Labels.IndexOf(label);
            int group = 0;
            double distance = CalculateDistance(Datas[index], Neurons[0]);

            for (int i = 0; i < Neurons.Count; i++)
                if (CalculateDistance(Datas[index], Neurons[i]) < distance)
                {
                    distance = CalculateDistance(Datas[index], Neurons[i]);
                    group = i;
                }

            return group;
        }

        public double GetDistance(double[] data)
        {
            int group = 0;
            double distance = CalculateDistance(data, Neurons[0]);

            for (int i = 0; i < Neurons.Count; i++)
                if (CalculateDistance(data, Neurons[i]) < distance)
                {
                    distance = CalculateDistance(data, Neurons[i]);
                    group = i;
                }

            return distance;
        }

        public void StartFitting()
        {
            if (isStarted == false)
            {
                double result = 0;
                isStarted = true;
                Init();
                new Task(() =>
                {
                    while (isStarted)
                    {
                        double tempResult = FitNetworkWinner(Datas, Neurons, DistanceMatrix);
                        result = tempResult;
                        DiagnosticInfo("Total distance: " + Math.Round(result, 10) + "\n");
                    }
                }).Start();
            }
        }

        public void StopFitting()
        {
            isStarted = false;
        }

        private double FitNetworkWinner(List<double[]> Inputs, List<double[]> Neurons, double[] DistanceMatrix)
        {
            double totalDistance = 0;

            for (int i = 0; i < Inputs.Count; i++)
            {
                for (int j = 0; j < Neurons.Count; j++)
                    DistanceMatrix[j] = CalculateDistance(Inputs[i], Neurons[j]);

                int winner = FindShortestDistance(DistanceMatrix);

                totalDistance += DistanceMatrix[winner];
                for (int j = 0; j < Neurons.Count; j++)
                    if (j == winner)
                        FitNeuron(Inputs[i], Neurons[j]);
                    
            }



            totalDistance /= Inputs.Count;
            return totalDistance;
        }

        private int FindShortestDistance(double[] distanceMatrix)
        {
            int winner = 0;
            double distance = 100;

            for (int i = 0; i < distanceMatrix.Length; i++)
                if (distanceMatrix[i] <= distance)
                {
                    distance = distanceMatrix[i];
                    winner = i;
                }
            return winner;
        }

        private void FitNeuron(double[] Inputs, double[] Neurons)
        {
            for (int i = 0; i < Inputs.Length; i++)
                Neurons[i] -= 0.01 * (Neurons[i] - Inputs[i]);
        }

        private void FitNeuron(double[] Inputs, double[] Neurons, double distance)
        {
            for (int i = 0; i < Inputs.Length; i++)
                Neurons[i] -= distance * (0.01 * (Neurons[i] - Inputs[i]));
        }

        private double CalculateDistance(double[] Input, double[] Neuron)
        {
            if (Input.Length == Neuron.Length)
            {
                double DifferencBetweenDimensions = 0;
                for (int i = 0; i < Input.Length; i++)
                    DifferencBetweenDimensions += Math.Pow(Input[i] - Neuron[i], 2);

                return Math.Sqrt(DifferencBetweenDimensions);
            }
            else
                throw new Exception("Calculate distance error");
        }
    }

}
