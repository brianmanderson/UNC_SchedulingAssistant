using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SchedulingAssistantCSharp
{
    public partial class WorkStatsWindow : Window
    {
        private readonly List<Person> people;

        public WorkStatsWindow(List<Person> people)
        {
            InitializeComponent();
            this.people = people;
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-7);
            EndDatePicker.SelectedDate = DateTime.Today;
            RefreshStats();
        }

        private void UpdateStats_Click(object sender, RoutedEventArgs e)
        {
            RefreshStats();
        }

        private void RefreshStats()
        {
            StatsPanel.Children.Clear();
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
                return;

            DateTime start = StartDatePicker.SelectedDate.Value.Date;
            DateTime end = EndDatePicker.SelectedDate.Value.Date;

            foreach (var person in people)
            {
                var groupBox = new GroupBox
                {
                    Header = $"{person.Name} - Current Weight: {person.CurrentWeight:F1} / {person.WeightPerDay * 7:F1}",
                    Margin = new Thickness(5),
                    Padding = new Thickness(5)
                };

                var tasks = person.Schedule
                    .Where(s => s.ScheduledDate >= start && s.ScheduledDate <= end)
                    .GroupBy(t => t.Task.Name)
                    .OrderByDescending(g => g.Count());

                var panel = new StackPanel();
                foreach (var taskGroup in tasks)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"{taskGroup.Key}: {taskGroup.Count()}",
                        Margin = new Thickness(5, 2, 0, 2)
                    });
                }

                groupBox.Content = panel;
                StatsPanel.Children.Add(groupBox);
            }
        }
    }
}
