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

            AdachSOM som = new AdachSOM(2, null);

            List<string> lab = new List<string>();
            List<double[]> dat = new List<double[]>();

            for (int i = 0; i < 25; i++)
            {
                lab.Add("test" + i.ToString());
                dat.Add(new double[] { r.NextDouble(),r.NextDouble() });
                som.Add(lab.Last(), dat.Last());
            }

            som.StartFitting();
            Thread.Sleep(2000);
            som.StopFitting();
            Thread.Sleep(100);
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

    public class AdachSOM
    {
        bool isStarted = false;
        public delegate void DiagnosticDatas(string diagString);
        private event DiagnosticDatas DiagnosticInfo;
        List<double[]> Datas;
        List<string> Labels;
        List<double[]> Neurons;
        double[] DistanceMatrix;
        int howManyGroups = 0;

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
                isStarted = true;
                Init();
                new Task(() =>
                {
                    while (isStarted)
                    {
                        DiagnosticInfo("Total distance: " + Math.Round(FitNetworkWinner(Datas, Neurons, DistanceMatrix), 4) + "      | ");
                        // DiagnosticInfo("Total distance: " + Math.Round(FitNetworkGauss(Datas, Neurons, DistanceMatrix), 4) + "      | ");

                        // Print(Neurons);
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
            List<int> AliveNeurons = new List<int>();
            for (int i = 0; i < Inputs.Count; i++)
            {
                for (int j = 0; j < Neurons.Count; j++)
                    DistanceMatrix[j] = CalculateDistance(Inputs[i], Neurons[j]);

                int winner = FindShortestDistance(DistanceMatrix);
                AliveNeurons.Add(winner);
                totalDistance += DistanceMatrix[winner];
                FitNeuron(Inputs[i], Neurons[winner]);
            }

            for (int i = 0; i < Neurons.Count; i++)
                if (!AliveNeurons.Contains(i))
                    for (int j = 0; j < Neurons[i].Length; j++)
                        Neurons[i][j] = 0;

            totalDistance /= Inputs.Count;
            return totalDistance;
        }
        private double FitNetworkGauss(double[][] Inputs, double[][] Neurons, double[] DistanceMatrix)
        {
            double totalDistance = 0;
            for (int i = 0; i < Inputs.Length; i++)
            {
                for (int j = 0; j < DistanceMatrix.Length; j++)
                {
                    DistanceMatrix[j] = CalculateDistance(Inputs[i], Neurons[j]);
                    int winner = FindShortestDistance(DistanceMatrix);
                    totalDistance += DistanceMatrix[winner];
                    FitNeuron(Inputs[i], Neurons[j], CalculateDistance(Inputs[i], Neurons[j]), winner);
                }
            }
            totalDistance /= Inputs.Length;
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
                Neurons[i] -= 0.1 * (Neurons[i] - Inputs[i]);
        }
        private void FitNeuron(double[] Inputs, double[] Neurons, double distance, double winner)
        {
            //   if (distance <0.5)
            for (int i = 0; i < Inputs.Length; i++)
                Neurons[i] -= distance * (0.1 * (Neurons[i] - Inputs[i]));
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

        private void Print(double[][] Neurons)
        {
            for (int i = 0; i < Neurons.Length; i++)
                for (int j = 0; j < Neurons[i].Length; j++)
                    DiagnosticInfo(Math.Round(Neurons[i][j], 4) + " | ");

            DiagnosticInfo("\n");
        }
    }

    public class AdachSOMList
    {
        bool isStarted = false;
        public delegate void DiagnosticDatas(string diagString);
        private event DiagnosticDatas DiagnosticInfo;
        double[][] Datas;
        double[][] Neurons;
        double[] DistanceMatrix;


        public AdachSOMList(double[][] _datas, int _howManyGroups, DiagnosticDatas _diagnosticDatas = null)
        {
            if (_diagnosticDatas != null)
                DiagnosticInfo = _diagnosticDatas;
            else
                DiagnosticInfo = Console.Write;

            Random r = new Random();
            Datas = _datas;
            Neurons = new double[_howManyGroups][];
            DistanceMatrix = new double[Neurons.Length];


            for (int i = 0; i < Neurons.Length; i++)
            {
                Neurons[i] = new double[_datas[0].Length];
                for (int j = 0; j < Datas[0].Length; j++)
                    Neurons[i][j] = r.NextDouble();
            }

        }

        public int GetGroup(double[] data)
        {
            int group = 0;
            double distance = CalculateDistance(data, Neurons[0]);

            for (int i = 0; i < Neurons.Length; i++)
                if (CalculateDistance(data, Neurons[i]) < distance)
                {
                    distance = CalculateDistance(data, Neurons[i]);
                    group = i;
                }

            return group;
        }
        public double GetDistance(double[] data)
        {
            int group = 0;
            double distance = CalculateDistance(data, Neurons[0]);

            for (int i = 0; i < Neurons.Length; i++)
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
                isStarted = true;
                new Task(() =>
                {
                    while (isStarted)
                    {
                        DiagnosticInfo("Total distance: " + Math.Round(FitNetworkWinner(Datas, Neurons, DistanceMatrix), 4) + "      | ");
                        // DiagnosticInfo("Total distance: " + Math.Round(FitNetworkGauss(Datas, Neurons, DistanceMatrix), 4) + "      | ");

                        Print(Neurons);
                    }
                }).Start();
            }
        }
        public void StopFitting()
        {
            isStarted = false;
        }

        private double FitNetworkWinner(double[][] Inputs, double[][] Neurons, double[] DistanceMatrix)
        {
            double totalDistance = 0;
            List<int> AliveNeurons = new List<int>();
            for (int i = 0; i < Inputs.Length; i++)
            {
                for (int j = 0; j < Neurons.Length; j++)
                    DistanceMatrix[j] = CalculateDistance(Inputs[i], Neurons[j]);

                int winner = FindShortestDistance(DistanceMatrix);
                AliveNeurons.Add(winner);
                totalDistance += DistanceMatrix[winner];
                FitNeuron(Inputs[i], Neurons[winner]);
            }

            for (int i = 0; i < Neurons.Length; i++)
                if (!AliveNeurons.Contains(i))
                    for (int j = 0; j < Neurons[i].Length; j++)
                        Neurons[i][j] = 0;

            totalDistance /= Inputs.Length;
            return totalDistance;
        }
        private double FitNetworkGauss(double[][] Inputs, double[][] Neurons, double[] DistanceMatrix)
        {
            double totalDistance = 0;
            for (int i = 0; i < Inputs.Length; i++)
            {
                for (int j = 0; j < DistanceMatrix.Length; j++)
                {
                    DistanceMatrix[j] = CalculateDistance(Inputs[i], Neurons[j]);
                    int winner = FindShortestDistance(DistanceMatrix);
                    totalDistance += DistanceMatrix[winner];
                    FitNeuron(Inputs[i], Neurons[j], CalculateDistance(Inputs[i], Neurons[j]), winner);
                }
            }
            totalDistance /= Inputs.Length;
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
                Neurons[i] -= 0.1 * (Neurons[i] - Inputs[i]);
        }
        private void FitNeuron(double[] Inputs, double[] Neurons, double distance, double winner)
        {
            //   if (distance <0.5)
            for (int i = 0; i < Inputs.Length; i++)
                Neurons[i] -= distance * (0.1 * (Neurons[i] - Inputs[i]));
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

        private void Print(double[][] Neurons)
        {
            for (int i = 0; i < Neurons.Length; i++)
                for (int j = 0; j < Neurons[i].Length; j++)
                    DiagnosticInfo(Math.Round(Neurons[i][j], 4) + " | ");

            DiagnosticInfo("\n");
        }
    }


}
