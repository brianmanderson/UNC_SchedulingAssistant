using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Globalization;

namespace SchedulingAssistantCSharp
{
    public class DateHasUnlockedTasksConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 ||
                !(values[0] is DateTime date) ||
                !(values[1] is IEnumerable<ScheduledTask> tasks))
                return false;

            // any task on this date that is not locked?
            return tasks.Any(t => t.ScheduledDate.Date == date.Date && !t.Locked);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
    ///
}
    namespace SchedulingAssistantCSharp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        ObservableCollection<Person> people = new ObservableCollection<Person>();
        public ObservableCollection<ScheduledTask> allScheduledTasks = new ObservableCollection<ScheduledTask>();
        private ObservableCollection<TaskGroup> allTaskGroups = new ObservableCollection<TaskGroup>();
        // Collection of available task definitions.
        private ObservableCollection<TaskDefinition> availableTaskDefinitions = new ObservableCollection<TaskDefinition>();

        public MainWindow()
        {
            InitializeComponent();
            //RoleDefinitionsWindow roleDefinitionsWindow = new RoleDefinitionsWindow();
            //roleDefinitionsWindow.ShowDialog();
            availableTaskDefinitions = SerializerDeserializerClass.LoadTaskDefinitions();
            people = SerializerDeserializerClass.LoadPeopleDefinitions();
            allScheduledTasks = SerializerDeserializerClass.LoadSchedule();
            allTaskGroups = SerializerDeserializerClass.LoadTaskGroups();
            if (people.Count == 0)
            {
                create_people();
            }
            DateTime day = DateTime.Now;
            bool is_scheduled = false;
            for (int i = 0; i < 10; i++) 
            {
                day.AddDays(i);
                foreach (ScheduledTask task in allScheduledTasks)
                {
                    if (task.ScheduledDate == day)
                    {
                        is_scheduled = true;
                    }
                }
                if (!is_scheduled)
                {

                }
            }

            // Optionally, set the calendar to today's date.
            calendarControl.SelectedDate = DateTime.Today;
            comboBoxTaskDefinitions.ItemsSource = availableTaskDefinitions;
            comboBoxTaskGroups.ItemsSource = allTaskGroups;
            UpdateScheduledTasksForSelectedDate();
        }
        private void CalendarControl_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateScheduledTasksForSelectedDate();
        }
        private void UpdateScheduledTasksForSelectedDate()
        {
            if (calendarControl.SelectedDate.HasValue)
            {
                DateTime selectedDate = calendarControl.SelectedDate.Value.Date;
                var tasksForDay = allScheduledTasks.Where(t => t.ScheduledDate.Date == selectedDate).ToList();
                listBoxScheduledTasks.ItemsSource = tasksForDay;
            }
            else
            {
                listBoxScheduledTasks.ItemsSource = null;
            }
        }
        private void btnAddTask_Click(object sender, RoutedEventArgs e)
        {
            if (calendarControl.SelectedDates.Count == 0)
            {
                MessageBox.Show("Please select a date first.");
                return;
            }

            // Ensure a TaskDefinition is selected.
            if (comboBoxTaskDefinitions.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a Task Definition from the list.");
                return;
            }
            TaskDefinition selectedTaskDef = (TaskDefinition)comboBoxTaskDefinitions.SelectedItem;
            SelectedDatesCollection selectedDates = calendarControl.SelectedDates;

            // Create a new ScheduledTask using a decorator pattern.
            foreach (var selectedDate in selectedDates)
            {
                ScheduledTask newScheduledTask = new ScheduledTask(selectedTaskDef, selectedDate);
                allScheduledTasks.Add(newScheduledTask);
            }
            // Refresh the ListBox.
            UpdateScheduledTasksForSelectedDate();
        }
        private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button deleteButton && deleteButton.Tag is ScheduledTask scheduledTask)
            {
                allScheduledTasks.Remove(scheduledTask);
                UpdateScheduledTasksForSelectedDate();
            }
        }
        public void create_people()
        {
            DateTime dateMonday = new DateTime(2024, 8, 26);
            DateTime dateTuesday = new DateTime(2024, 8, 27);
            DateTime dateWednesday = new DateTime(2024, 8, 28);
            DateTime dateThursday = new DateTime(2024, 8, 29);
            DateTime dateFriday = new DateTime(2024, 8, 30);
            // Leith is a Physicist.
            var leith = new Physicist(
                "Leith",
                12.0 / 5,
                new List<Preference>
                {
                    new Preference(dateMonday, "POD", 7.0)
                }
            );
            people.Add(leith);

            // Taki is a Physicist.
            var taki = new Physicist(
                "Taki",
                12.0 / 5,
                new List<Preference>
                {
                    new Preference(dateMonday, "Dev", 7.0),
                    new Preference(dateTuesday, "Dev", 7.0)
                },
                null, // No avoid preferences.
                new List<TaskDefinition>
                {
                    new TaskDefinition("Gamma_Tile", 0.0, "UNC")
                }
            );
            people.Add(taki);

            // Dance can perform 'Gamma_Tile'.
            var dance = new Physicist(
                "Dance",
                18.0 / 5,
                new List<Preference>(), // No preferences.
                null,
                new List<TaskDefinition>
                {
                    new TaskDefinition("Gamma_Tile", 0.0, "UNC")
                }
            );
            people.Add(dance);

            // Adria is a Physicist.
            var adria = new Physicist(
                "Adria",
                18.0 / 5,
                new List<Preference>
                {
                    new Preference(dateMonday, "Vacation", 9.0)
                }
            );
            people.Add(adria);

            // Cielle can perform 'Prostate_Brachy', 'Dev', 'HalfDev', and 'Vacation'.
            var cielle = new Physicist(
                "Cielle",
                18.0 / 5,
                new List<Preference>(), // No specific preferences.
                null,
                new List<TaskDefinition>
                {
                    new TaskDefinition("Prostate_Brachy", 0.0, "UNC")
                }
            );
            people.Add(cielle);

            // Brian is a Physicist.
            var brian = new Physicist(
                "Brian",
                12.0 / 5,
                new List<Preference>
                {
                    new Preference(dateMonday, "POD_Backup", 3.0),
                    new Preference(dateFriday, "SAD", 7.0)
                }
            );
            people.Add(brian);

            // David is a Physicist with avoid preferences.
            var david = new Physicist(
                "David",
                12.0 / 5,
                null, // No normal preferences.
                new List<Preference>
                {
                    new Preference(dateMonday, "HBO", 9.0),
                    new Preference(dateMonday, "UNC", 9.0),
                    new Preference(dateTuesday, "HBO", 9.0),
                    new Preference(dateTuesday, "UNC", 9.0),
                    new Preference(dateWednesday, "HBO", 1.0),
                    new Preference(dateThursday, "HBO", 1.0),
                    new Preference(dateFriday, "HBO", 1.0),
                    new Preference(dateFriday, "UNC", 1.0)
                }
            );
            people.Add(david);

            // Jun can perform 'Prostate_Brachy'.
            var jun = new Physicist(
                "Jun",
                12.0 / 5,
                new List<Preference>
                {
                    new Preference(dateFriday, "Vacation", 9.0)
                },
                null,
                new List<TaskDefinition>
                {
                    new TaskDefinition("Prostate_Brachy", 0.0, "UNC")
                }
            );
            people.Add(jun);

            // Ross is a Physicist.
            var ross = new Physicist(
                "Ross",
                18.0 / 5,
                new List<Preference>
                {
                    new Preference(dateFriday, "Vacation", 9.0)
                }
            );
            people.Add(ross);
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Formatting = Formatting.Indented
            };
            string jsonString = JsonConvert.SerializeObject(people, settings);
            SerializerDeserializerClass.SavePeopleDefinitions(people);
            // Optionally, iterate over people and print their details.
            foreach (var person in people)
            {
                Console.WriteLine(person);
            }
        }

        private void btnCreateGeneralTasks_Click(object sender, RoutedEventArgs e)
        {
            // Open the TaskDefinitionsWindow as a modal dialog.
            TaskDefinitionsWindow taskDefinitionsWindow = new TaskDefinitionsWindow();
            taskDefinitionsWindow.Owner = this;
            taskDefinitionsWindow.ShowDialog();
        }

        private void btnCreatePersonnel_Click(object sender, RoutedEventArgs e)
        {
            PersonnelDefinitionsWindow personnelDefinitionsWindow = new PersonnelDefinitionsWindow();
            personnelDefinitionsWindow.Owner = this;
            personnelDefinitionsWindow.ShowDialog();
        }
        private void AssignTaskButton_Click(object sender, RoutedEventArgs e)
        {
            // Grab the ScheduledTask from the button’s Tag
            var btn = (Button)sender;
            var stask = btn.Tag as ScheduledTask;
            if (stask == null) return;

            // Show the assigner dialog (owner so it centers over MainWindow)
            var dlg = new AssignPersonWindow(people)
            {
                Owner = this
            };

            if (dlg.ShowDialog() == true)
            {
                var person = dlg.SelectedPerson;
                person.AssignTask(stask);        // ← sets both sides
                listBoxScheduledTasks.Items.Refresh();
                // Optionally also refresh any weight/balances display
            }
        }

        private void btnSaveSchedule_Click(object sender, RoutedEventArgs e)
        {
            SerializerDeserializerClass.SaveSchedule(allScheduledTasks);
        }

        private void btnAddTaskGroup_Click(object sender, RoutedEventArgs e)
        {
            if (calendarControl.SelectedDates.Count == 0)
            {
                MessageBox.Show("Please select a date first.");
                return;
            }

            // Ensure a TaskDefinition is selected.
            if (comboBoxTaskGroups.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a Task Group from the list.");
                return;
            }
            TaskGroup selectedTaskGroup = (TaskGroup)comboBoxTaskGroups.SelectedItem;
            // Create a new ScheduledTask using a decorator pattern.
            SelectedDatesCollection selectedDates = calendarControl.SelectedDates;

            // Create a new ScheduledTask using a decorator pattern.
            foreach (DateTime selectedDate in selectedDates)
            {
                foreach (TaskDefinition taskDefinition in selectedTaskGroup.Tasks)
                {
                    ScheduledTask newScheduledTask = new ScheduledTask(taskDefinition, selectedDate);
                    allScheduledTasks.Add(newScheduledTask);
                }
            }
            // Refresh the ListBox. 
            UpdateScheduledTasksForSelectedDate();
        }

        private void btnCreateTaskGroup_Click(object sender, RoutedEventArgs e)
        {
            TaskGroupsWindow taskGroupsWindow = new TaskGroupsWindow();
            taskGroupsWindow.ShowDialog();
            allTaskGroups = SerializerDeserializerClass.LoadTaskGroups();
            comboBoxTaskGroups.ItemsSource = allTaskGroups;
        }
    }
}
