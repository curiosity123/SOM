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
            double[][] Inputs = new double[6][];

            Inputs[0] = new double[3];
            Inputs[1] = new double[3];
            Inputs[2] = new double[3];
            Inputs[3] = new double[3];
            Inputs[4] = new double[3];
            Inputs[5] = new double[3];

            Inputs[0][0] = 0;
            Inputs[0][1] = 0;
            Inputs[0][2] = 0.1;

            Inputs[1][0] = 0.58;
            Inputs[1][1] = 0.5;
            Inputs[1][2] = 0.48;

            Inputs[2][0] = 1;
            Inputs[2][1] = 1;
            Inputs[2][2] = 0.9;

            Inputs[3][0] = 0.92;
            Inputs[3][1] = 0.75;
            Inputs[3][2] = 0.86;

            Inputs[4][0] = 0.87;
            Inputs[4][1] = 0.95;
            Inputs[4][2] = 0.71;

            Inputs[5][0] = 0.5;
            Inputs[5][1] = 0.4;
            Inputs[5][2] = 0.55;


            AdachSOM som = new AdachSOM(Inputs, 7, null);
            som.StartFitting();
            Thread.Sleep(7800);
            som.StopFitting();
            Thread.Sleep(100);
            Console.WriteLine("\n");
            foreach (double[] input in Inputs)
            {
                Console.ForegroundColor = (ConsoleColor)1 + som.GetGroup(input);

                foreach (double d in input)
                    Console.Write(" |" + d + "| ");
                Console.WriteLine();
            }
            Console.ReadKey();
        }
    }

    public class AdachSOM
    {
        bool isStarted = false;
        public delegate void DiagnosticDatas(string diagString);
        private event DiagnosticDatas DiagnosticInfo;
        double[][] Datas;
        double[][] Neurons;
        double[] DistanceMatrix;

        public AdachSOM(double[][] _datas, int _neuronsCount, DiagnosticDatas _diagnosticDatas = null)
        {
            if (_diagnosticDatas != null)
                DiagnosticInfo = _diagnosticDatas;
            else
                DiagnosticInfo = Console.Write;

            Random r = new Random();
            Datas = _datas;
            Neurons = new double[_neuronsCount][];
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
                        DiagnosticInfo("Total distance: " + Math.Round(FitNetworkBest(Datas, Neurons, DistanceMatrix), 4) + "      | ");
                        // DiagnosticInfo("Total distance: " + Math.Round(FitNetwork(Datas, Neurons, DistanceMatrix), 4) + "      | ");

                        Print(Neurons);
                    }
                }).Start();
            }
        }

        public void StopFitting()
        {
            isStarted = false;
        }

        private double FitNetworkBest(double[][] Inputs, double[][] Neurons, double[] DistanceMatrix)
        {
            double totalDistance = 0;
            for (int i = 0; i < Inputs.Length; i++)
            {
                for (int j = 0; j < DistanceMatrix.Length; j++)
                    DistanceMatrix[j] = CalculateDistance(Inputs[i], Neurons[j]);
                int winner = FindSmallestDistance(DistanceMatrix);
                totalDistance += DistanceMatrix[winner];
                FitNeuron(Inputs[i], Neurons[winner]);
            }
            totalDistance /= Inputs.Length;
            return totalDistance;
        }
        private double FitNetwork(double[][] Inputs, double[][] Neurons, double[] DistanceMatrix)
        {
            double totalDistance = 0;
            for (int i = 0; i < Inputs.Length; i++)
            {
                for (int j = 0; j < DistanceMatrix.Length; j++)
                {
                    DistanceMatrix[j] = CalculateDistance(Inputs[i], Neurons[j]);
                    int winner = FindSmallestDistance(DistanceMatrix);
                    totalDistance += DistanceMatrix[winner];
                    FitNeuron(Inputs[i], Neurons[j], CalculateDistance(Inputs[i], Neurons[j]), winner);
                }
            }
            totalDistance /= Inputs.Length;
            return totalDistance;
        }


        private int FindSmallestDistance(double[] distanceMatrix)
        {
            int winner = 0;
            double distance = distanceMatrix[0];

            for (int i = 0; i < distanceMatrix.Length; i++)
                if (distanceMatrix[i] < distance)
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
        private void FitNeuron(double[] Inputs, double[] Neurons, double distance, double winner)
        {
            if (distance < winner * 3)
                for (int i = 0; i < Inputs.Length; i++)
                    Neurons[i] -= (distance * 0.1) * (Neurons[i] - Inputs[i]);
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
