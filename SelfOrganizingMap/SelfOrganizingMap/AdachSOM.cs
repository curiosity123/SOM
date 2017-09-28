using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfOrganizingMap
{

    public class AdachSOM
    {
        bool _isStarted = false;
        public delegate void DiagnosticDatas(string diagString);
        event DiagnosticDatas _diagnosticInfo;
        List<double[]> _datas;
        List<string> _labels;
        List<double[]> _neurons;
        double[] D_distanceMatrix;
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
            _labels.Add(_label);
            _datas.Add(_data);
        }

        private void Init()
        {
            Random r = new Random();

            for (int i = 0; i < howManyGroups; i++)
            {
                _neurons.Add(new double[_datas[0].Length]);
                for (int j = 0; j < _datas[0].Length; j++)
                    _neurons[i][j] = r.NextDouble();
            }
        }

        public AdachSOM(int _howManyGroups, DiagnosticDatas _diagnosticDatas = null)
        {
            if (_diagnosticDatas != null)
                _diagnosticInfo = _diagnosticDatas;
            else
                _diagnosticInfo = Console.Write;

            _labels = new List<string>();
            _datas = _datas = new List<double[]>();
            _neurons = _neurons = new List<double[]>();

            howManyGroups = _howManyGroups;

            D_distanceMatrix = new double[_howManyGroups];
        }

        public int GetGroup(double[] _data)
        {
            int group = 0;
            double distance = CalculateDistance(_data, _neurons[0]);

            for (int i = 0; i < _neurons.Count; i++)
                if (CalculateDistance(_data, _neurons[i]) < distance)
                {
                    distance = CalculateDistance(_data, _neurons[i]);
                    group = i;
                }

            return group;
        }

        public int GetGroup(string label)
        {
            int index = _labels.IndexOf(label);
            int group = 0;
            double distance = CalculateDistance(_datas[index], _neurons[0]);

            for (int i = 0; i < _neurons.Count; i++)
                if (CalculateDistance(_datas[index], _neurons[i]) < distance)
                {
                    distance = CalculateDistance(_datas[index], _neurons[i]);
                    group = i;
                }

            return group;
        }

        public double GetDistance(double[] data)
        {
            int group = 0;
            double distance = CalculateDistance(data, _neurons[0]);

            for (int i = 0; i < _neurons.Count; i++)
                if (CalculateDistance(data, _neurons[i]) < distance)
                {
                    distance = CalculateDistance(data, _neurons[i]);
                    group = i;
                }

            return distance;
        }

        public void StartFitting()
        {
            if (_isStarted == false)
            {
                double result = 0;
                _isStarted = true;
                Init();
                new Task(() =>
                {
                    while (_isStarted)
                    {
                        double tempResult = FitNetworkWinner(_datas, _neurons, D_distanceMatrix);
                        result = tempResult;
                        _diagnosticInfo("Total distance: " + Math.Round(result, 10) + "\n");
                    }
                }).Start();
            }
        }

        public void StopFitting()
        {
            _isStarted = false;
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
