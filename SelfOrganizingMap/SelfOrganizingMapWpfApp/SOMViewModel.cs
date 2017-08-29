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
using System.Windows.Input;

namespace SelfOrganizingMapWpfApp
{
    public class SOMViewModel : INotifyPropertyChanged
    {
        AdachSOM SOM;
        DataTable CsvDataTable = new DataTable();
        List<string> lab = new List<string>();
        List<double[]> dat = new List<double[]>();


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

        #region commands

        public ICommand OpenCommand { get { return new RelayCommand(CanOpen, Open); } }
        private void Open(object obj)
        {
            string Path = "";

            dat = new List<double[]>();
            lab = new List<string>();

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = Environment.CurrentDirectory;
            openFileDialog1.Filter = "All Files (*.csv)|*.csv";
            if (openFileDialog1.ShowDialog() == true)
            {
                Path = openFileDialog1.FileName;
            }
            else
                return;



            string CSVFilePathName = Path;
            string[] Lines = File.ReadAllLines(CSVFilePathName);

            for (int i = 1; i < Lines.Length; i++)
            {
                string[] items = Lines[i].Split(',');

                lab.Add(items[0]);

                double[] temp = new double[items.Length - 1];
                for (int j = 1; j < items.Length; j++)
                    temp[j - 1] = double.Parse(items[j].Replace('.', ','));
                dat.Add(temp);
            }






        }
        public void Normalize(List<double[]> d)
        {
           // double min = 1, max = 1;
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
                    d[j][i] = d[j][i] / max;


            }
            //for (int i = 0; i < d[0].Length; i++)
            //    for (int j = 0; j < d.Count; j++)
            //        d[j][i] = d[j][i] / max;
        }

        private bool CanOpen(object obj)
        {
            return true;
        }

        public ICommand SortCommand { get { return new RelayCommand(CanSort, Sort); } }
        private void Sort(object obj)
        {
            Random r = new Random();
            SOM = new AdachSOM(HowManyGroups, null);
            Normalize(dat);

            for (int i = 0; i < lab.Count; i++)
                SOM.Add(lab[i], dat[i]);

            SOM.StartFitting(10);
            Thread.Sleep(4000);


            CsvDataTable = new DataTable();
            for (int i = 0; i < HowManyGroups; i++)
                CsvDataTable.Columns.Add("group " + i.ToString());


            for (int i = 0; i < lab.Count; i++)
            {
                int group = SOM.GetGroup((lab[i]));
                double dist = SOM.GetDistance(dat[i]);
                string[] row = new string[HowManyGroups] ;
                row[group] = lab[i] + " " +  Math.Round( dist,3).ToString() ;
                CsvDataTable.Rows.Add(row);
            }


            RaisePropertyChangedEvent("CsvDataSet");
        }
        private bool CanSort(object obj)
        {
            return true;
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
