using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;
using SchedulingAssistantCSharp;

namespace SchedulingAssistantCSharp
{
    public partial class WorkStatsWindow : Window, INotifyPropertyChanged
    {
        public SeriesCollection SeriesCollection { get; set; }
        public string[] LabelsXAxis { get; set; }
        public Func<double, string> FormatterYAxis { get; set; }

        private DateTime _startDate = DateTime.Today.AddDays(-7);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged(nameof(StartDate));
            }
        }

        private DateTime _endDate = DateTime.Today;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged(nameof(EndDate));
            }
        }

        private readonly List<Person> _allPeople;
        private readonly List<ScheduledTask> _allScheduledTasks;

        public WorkStatsWindow(List<Person> people, List<ScheduledTask> tasks)
        {
            InitializeComponent();
            _allPeople = people;
            _allScheduledTasks = tasks;

            DataContext = this;
            LoadStats();
        }

        private void LoadStats()
        {
            var labels = new List<string>();
            var values = new ChartValues<int>();

            foreach (var person in _allPeople)
            {
                int taskCount = _allScheduledTasks.Count(t =>
                    t.AssignedPerson != null &&
                    t.AssignedPerson.Name == person.Name &&
                    t.ScheduledDate >= StartDate &&
                    t.ScheduledDate <= EndDate);

                labels.Add(person.Name);
                values.Add(taskCount);
            }

            SeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Tasks",
                    Values = values
                }
            };

            LabelsXAxis = labels.ToArray();
            FormatterYAxis = value => value.ToString("N0");

            OnPropertyChanged(nameof(SeriesCollection));
            OnPropertyChanged(nameof(LabelsXAxis));
            OnPropertyChanged(nameof(FormatterYAxis));
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            LoadStats();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
