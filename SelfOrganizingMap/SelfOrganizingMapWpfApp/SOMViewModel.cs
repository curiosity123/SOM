using Microsoft.Win32;
using SelfOrganizingMap;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SelfOrganizingMapWpfApp
{
    public class SOMViewModel : INotifyPropertyChanged
    {
        AdachSOM SOM;
        DataTable _csvDataTable = new DataTable();
        List<string> _currentLabelList = new List<string>();
        List<double[]> _currentDataList = new List<double[]>();
        bool _start = false;

        public SOMViewModel()
        {
        }

        #region Properties

        private int _howManyGroups = 4;
        public int HowManyGroups
        {
            get { return _howManyGroups; }
            set
            {
                if (value > 0 && value < 100000)
                    _howManyGroups = value;
                RaisePropertyChangedEvent("HowManyGroups");
            }
        }

        private ObservableCollection<Record> _recordList;
        public ObservableCollection<Record> RecordList
        {
            get { return _recordList; }
            set
            {
                _recordList = value;
                RaisePropertyChangedEvent("RecordList");
            }
        }

        private string _results;
        public string Results
        {
            get { return _results; }
            set
            {
                _results = value;
                RaisePropertyChangedEvent("Results");
            }
        }

        private string _calculateButtonLabel = "Extract";
        public string CalculateButtonLabel
        {
            get { return _calculateButtonLabel; }
            set
            {
                _calculateButtonLabel = value;
                RaisePropertyChangedEvent("CalculateButtonLabel");
            }
        }

        #endregion

        #region Commands

        public ICommand OpenCommand { get { return new RelayCommand(CanOpen, Open); } }
        private void Open(object obj)
        {
            string Path = "";

            _currentDataList = new List<double[]>();
            _currentLabelList = new List<string>();
            LoadCSV(ref Path);
        }
        private bool CanOpen(object obj)
        {
            return true;
        }

        public ICommand CalculateCommand { get { return new RelayCommand(CanCalculate, Calculate); } }
        private void Calculate(object obj)
        {
            if (_start == false)
            {
                _start = true;
                CalculateButtonLabel = "Extracting...(press to stop)";
                RaisePropertyChangedEvent("CalculateButtonLabel");

                SOM = new AdachSOM(HowManyGroups, ResultUpdater);
                AdachSOM.NormalizeData(_currentDataList);
                for (int i = 0; i < _currentLabelList.Count; i++)
                    SOM.Add(_currentLabelList[i], _currentDataList[i]);
                SOM.StartFitting();

            }
            else
            {
                CalculateButtonLabel = "Extract";
                RaisePropertyChangedEvent("CalculateButtonLabel");

                _start = false;
                SOM.StopFitting();

                BuildDataTable();
                RaisePropertyChangedEvent("CsvDataSet");
            }

        }
        private bool CanCalculate(object obj)
        {
            return _currentDataList.Count > 0;
        }

        #endregion

        private void ResultUpdater(string s)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Results = s;
                });
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private void LoadCSV(ref string Path)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = Environment.CurrentDirectory;
            openFileDialog1.Filter = "All Files (*.csv)|*.csv";
            if (openFileDialog1.ShowDialog() == true)
                Path = openFileDialog1.FileName;
            else
                return;

            try
            {
                string CSVFilePathName = Path;
                string[] Lines = File.ReadAllLines(CSVFilePathName);

                for (int i = 1; i < Lines.Length; i++)
                {
                    string[] items = Lines[i].Split(',');

                    _currentLabelList.Add(items[0]);

                    double[] temp = new double[items.Length - 1];
                    for (int j = 1; j < items.Length; j++)
                        temp[j - 1] = double.Parse(items[j].Replace('.', ','));
                    _currentDataList.Add(temp);
                }
            }
            catch
            {
                MessageBox.Show("CSV file is not correct, data should have this format: label, value, value,value and so on. Values should be grather than zero.");
            }
        }
        private void BuildDataTable()
        {
            List<int> uniqueGroups = new List<int>();
            RecordList = new ObservableCollection<SelfOrganizingMapWpfApp.Record>();
            _csvDataTable = new DataTable();
            _csvDataTable.Columns.Add("Label");
            _csvDataTable.Columns.Add("Distance");
            _csvDataTable.Columns.Add("Group");

            for (int i = 0; i < _currentLabelList.Count; i++)
            {
                int group = SOM.GetGroup((_currentLabelList[i]));
                double dist = SOM.GetDistance(_currentDataList[i]);

                object[] row = new object[3] { _currentLabelList[i] + " " + Math.Round(dist, 3).ToString(), dist, group.ToString() };
                _csvDataTable.Rows.Add(row);
                if (!uniqueGroups.Contains(group))
                    uniqueGroups.Add(group);
                RecordList.Add(new Record(_currentLabelList[i], dist, group));
            }
            var sortedUniqueGroups = from item in uniqueGroups orderby item select item;
            uniqueGroups = new List<int>(sortedUniqueGroups);
            foreach (Record r in RecordList)
                r.SetColors(uniqueGroups);

            var sortedByGroup = from item in RecordList orderby item.Group select item;
            RecordList = new ObservableCollection<Record>(sortedByGroup.ToList());

            _csvDataTable.DefaultView.Sort = "Group";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChangedEvent(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class Record
    {
        public string Label { get; set; }
        public double Distance { get; set; }
        public int Group { get; set; }
        public double LabelOpacity { get; set; }
        public Brush Color { get; set; }
        public Record(string label, double distance, int group)
        {
            Label = label;
            Distance = distance;
            Group = group;
            LabelOpacity = 1 - Math.Sqrt(distance / 1.42);



        }
        public void SetColors(List<int> uniqueGroups)
        {
            Color = (SolidColorBrush)(new BrushConverter().ConvertFrom(ColourValues[uniqueGroups.IndexOf(Group)]));
        }

        public override string ToString()
        {
            return Label.ToString();
        }

        static string[] ColourValues = new string[] {
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e",
              "#ec7063", "#a569bd","#5499c7","#48c9b0","#f4d03f","#85929e"
    };
    }
    public class RelayCommand : ICommand
    {
        private Predicate<object> _canExecute;
        private Action<object> _execute;

        public RelayCommand(Predicate<object> canExecute, Action<object> execute)
        {
            this._canExecute = canExecute;
            this._execute = execute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
