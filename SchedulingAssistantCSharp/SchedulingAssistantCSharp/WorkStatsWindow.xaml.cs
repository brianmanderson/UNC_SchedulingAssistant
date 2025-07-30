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

        private DateTime _startDate = DateTime.Today.AddDays(-90);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged(nameof(StartDate));
            }
        }

        private DateTime _endDate = DateTime.Today.AddDays(90);
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
            // Identify unique Task Names
            var taskNames = _allScheduledTasks
                .Select(t => t.Task.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            // Set Labels as person names
            LabelsXAxis = _allPeople.Select(p => p.Name).ToArray();

            // Create SeriesCollection with a ColumnSeries per task type
            SeriesCollection = new SeriesCollection();

            foreach (var taskName in taskNames)
            {
                var taskCountsPerPerson = new ChartValues<int>();

                foreach (var person in _allPeople)
                {
                    int count = _allScheduledTasks.Count(t =>
                        t.AssignedPerson != null &&
                        t.AssignedPerson.Name == person.Name &&
                        t.Task.Name == taskName &&
                        t.ScheduledDate.Date >= StartDate.Date &&
                        t.ScheduledDate.Date <= EndDate.Date);

                    taskCountsPerPerson.Add(count);
                }

                SeriesCollection.Add(new ColumnSeries
                {
                    Title = taskName,
                    Values = taskCountsPerPerson
                });
            }

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
