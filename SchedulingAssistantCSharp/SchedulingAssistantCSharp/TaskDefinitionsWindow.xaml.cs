using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace SchedulingAssistantCSharp
{
    /// <summary>
    /// Interaction logic for TaskDefinitionsWindow.xaml
    /// </summary>
    public partial class TaskDefinitionsWindow : Window
    {
        private ObservableCollection<TaskDefinition> taskDefinitions;

        public class SelectableTask
        {
            public string Name { get; set; }
            public bool IsSelected { get; set; }
        }

        // Exposed as public so that the ItemsControls can bind to them.
        public List<SelectableTask> CompatibleTasks { get; set; } = new List<SelectableTask>();
        public List<SelectableTask> RequiredTasks { get; set; } = new List<SelectableTask>();

        private TaskDefinition currentTaskDefinition;

        public TaskDefinitionsWindow()
        {
            InitializeComponent();
            // Set DataContext for binding.
            this.DataContext = this;
            taskDefinitions = SerializerDeserializerClass.LoadTaskDefinitions();
            RefreshTaskList();
        }
        /// <summary>
        /// Refreshes the ListBox display.
        /// </summary>
        private void RefreshTaskList()
        {
            listBoxTasks.ItemsSource = null;
            listBoxTasks.ItemsSource = taskDefinitions.Select(t => t.Name).ToList();
        }

        private void listBoxTasks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = listBoxTasks.SelectedIndex;
            if (index >= 0 && index < taskDefinitions.Count)
            {
                currentTaskDefinition = taskDefinitions[index];

                // Populate the text fields.
                textBoxName.Text = currentTaskDefinition.Name;
                textBoxWeight.Text = currentTaskDefinition.Weight.ToString();
                textBoxLocation.Text = currentTaskDefinition.Location;

                // Build the selectable tasks lists for Compatible With and Requires.
                BuildSelectableTasks();
            }
        }

        private void BuildSelectableTasks()
        {
            CompatibleTasks.Clear();
            RequiredTasks.Clear();

            foreach (var td in taskDefinitions)
            {
                // Optionally, skip the current task.
                if (currentTaskDefinition != null && td.Name == currentTaskDefinition.Name)
                    continue;

                CompatibleTasks.Add(new SelectableTask
                {
                    Name = td.Name,
                    IsSelected = currentTaskDefinition.CompatibleWith != null && currentTaskDefinition.CompatibleWith.Contains(td.Name)
                });
                RequiredTasks.Add(new SelectableTask
                {
                    Name = td.Name,
                    IsSelected = currentTaskDefinition.Requires != null && currentTaskDefinition.Requires.Contains(td.Name)
                });
            }

            // Refresh the ItemsControl bindings.
            itemsControlCompatible.ItemsSource = null;
            itemsControlCompatible.ItemsSource = CompatibleTasks;
            itemsControlRequired.ItemsSource = null;
            itemsControlRequired.ItemsSource = RequiredTasks;
        }

        // (Optional) These event handlers can be used if you wish to update on each checkbox change.
        private void CheckBox_ChangedCompatible(object sender, RoutedEventArgs e)
        {
            if (currentTaskDefinition == null) return;

            currentTaskDefinition.CompatibleWith = CompatibleTasks
                .Where(st => st.IsSelected)
                .Select(st => st.Name)
                .ToList();
        }

        private void CheckBox_ChangedRequired(object sender, RoutedEventArgs e)
        {
            if (currentTaskDefinition == null) return;

            currentTaskDefinition.Requires = RequiredTasks
                .Where(st => st.IsSelected)
                .Select(st => st.Name)
                .ToList();
        }

        private void btnSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (currentTaskDefinition != null)
            {
                currentTaskDefinition.Name = textBoxName.Text;
                if (double.TryParse(textBoxWeight.Text, out double weight))
                {
                    currentTaskDefinition.Weight = weight;
                }
                currentTaskDefinition.Location = textBoxLocation.Text;

                // Update the CompatibleWith and Requires lists based on the checkbox selections.
                currentTaskDefinition.CompatibleWith = CompatibleTasks.Where(st => st.IsSelected).Select(st => st.Name).ToList();
                currentTaskDefinition.Requires = RequiredTasks.Where(st => st.IsSelected).Select(st => st.Name).ToList();

                SerializerDeserializerClass.SaveTaskDefinitions(taskDefinitions);
                RefreshTaskList();
            }
        }

        private void btnAddNewTask_Click(object sender, RoutedEventArgs e)
        {
            // Create a new TaskDefinition with generic default values.
            TaskDefinition newTask = new TaskDefinition("New Task", 0.0, "DefaultLocation", new List<string>(), new List<string>());

            taskDefinitions.Add(newTask);
            SerializerDeserializerClass.SaveTaskDefinitions(taskDefinitions);
            RefreshTaskList();
            listBoxTasks.SelectedIndex = taskDefinitions.Count - 1;
        }
        private void btnDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            int index = listBoxTasks.SelectedIndex;
            if (index >= 0 && index < taskDefinitions.Count)
            {
                // Confirm deletion (optional)
                if (MessageBox.Show("Delete the selected task definition?", "Confirm Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    taskDefinitions.RemoveAt(index);
                    SerializerDeserializerClass.SaveTaskDefinitions(taskDefinitions);
                    RefreshTaskList();

                    // Clear editing fields.
                    textBoxName.Clear();
                    textBoxWeight.Clear();
                    textBoxLocation.Clear();
                    currentTaskDefinition = null;
                }
            }
        }

        private void btnSaveAndExit_Click(object sender, RoutedEventArgs e)
        {
            SerializerDeserializerClass.SaveTaskDefinitions(taskDefinitions);
            Close();
        }
    }
}
