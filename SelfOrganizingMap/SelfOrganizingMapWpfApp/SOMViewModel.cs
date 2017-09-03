using Microsoft.Win32;
using SelfOrganizingMap;
using System;
using System.Collections.Generic;
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

namespace SelfOrganizingMapWpfApp
{
    public class SOMViewModel : INotifyPropertyChanged
    {
        AdachSOM SOM;
        DataTable CsvDataTable = new DataTable();
        List<string> CurrentLabelList = new List<string>();
        List<double[]> CurrentDataList = new List<double[]>();


        public SOMViewModel()
        {
        }

        #region Properties

        private int howManyGroups = 4;
        public int HowManyGroups
        {
            get { return howManyGroups; }
            set
            {
                if (value > 0 && value < 100)
                    howManyGroups = value;
                RaisePropertyChangedEvent("HowManyGroups");
            }
        }

        public DataTable CsvDataSet
        {
            get
            {
                return CsvDataTable;
            }
            set
            {
                CsvDataTable = value;
                RaisePropertyChangedEvent("CsvDataSet");
            }
        }

        #endregion

        #region Commands

        public ICommand OpenCommand { get { return new RelayCommand(CanOpen, Open); } }
        private void Open(object obj)
        {
            string Path = "";

            CurrentDataList = new List<double[]>();
            CurrentLabelList = new List<string>();
            LoadCSV(ref Path);
        }
        private bool CanOpen(object obj)
        {
            return true;
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

                    CurrentLabelList.Add(items[0]);

                    double[] temp = new double[items.Length - 1];
                    for (int j = 1; j < items.Length; j++)
                        temp[j - 1] = double.Parse(items[j].Replace('.', ','));
                    CurrentDataList.Add(temp);
                }
            }
            catch
            {
                MessageBox.Show("CSV file is not correct, data should have this format: label, value, value,value and so on. Values should be grather than zero.");
            }
        }
        private void BuildDataTable()
        {
            CsvDataTable = new DataTable();
            CsvDataTable.Columns.Add("Label");
            CsvDataTable.Columns.Add("Distance");
            CsvDataTable.Columns.Add("Group");

            for (int i = 0; i < CurrentLabelList.Count; i++)
            {
                int group = SOM.GetGroup((CurrentLabelList[i]));
                double dist = SOM.GetDistance(CurrentDataList[i]);

                object[] row = new object[3] { CurrentLabelList[i] + " " + Math.Round(dist, 3).ToString(), dist, group.ToString() };
                CsvDataTable.Rows.Add(row);
            }
            CsvDataTable.DefaultView.Sort="Group";
        }

        public ICommand SortCommand { get { return new RelayCommand(CanSort, Sort); } }
        private void Sort(object obj)
        {
            Random r = new Random();
            SOM = new AdachSOM(HowManyGroups, null);


            AdachSOM.NormalizeData(CurrentDataList);

            for (int i = 0; i < CurrentLabelList.Count; i++)
                SOM.Add(CurrentLabelList[i], CurrentDataList[i]);

            SOM.StartFitting(10);
            Thread.Sleep(4000);

            BuildDataTable();

            RaisePropertyChangedEvent("CsvDataSet");
        }
        private bool CanSort(object obj)
        {
            return CurrentDataList.Count > 0;
        }
        #endregion


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
        public double Opacity { get; set; }
        public Record(string label, double opacity)
        {
            Label = label;
            Opacity = opacity;
        }

        public override string ToString()
        {
            return Label.ToString();
        }

    }
    public class ValueToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value as TextBlock).Opacity;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
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
