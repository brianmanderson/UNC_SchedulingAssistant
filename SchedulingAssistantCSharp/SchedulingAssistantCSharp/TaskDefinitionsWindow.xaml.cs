using System;
using System.Collections.Generic;
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
        private List<TaskDefinition> taskDefinitions;
        private readonly string jsonFilePath = "TaskDefinitions.json";
        public class SelectableTask
        {
            public string Name { get; set; }
            public bool IsSelected { get; set; }
        }
        private List<SelectableTask> selectableTasksCompatible = new List<SelectableTask>();
        private List<SelectableTask> selectableTasksRequired = new List<SelectableTask>();
        private TaskDefinition currentTaskDefinition;
        public TaskDefinitionsWindow()
        {
            InitializeComponent();
            LoadTaskDefinitions();
            RefreshTaskList();
        }

        /// <summary>
        /// Loads task definitions from the JSON file.
        /// </summary>
        private void LoadTaskDefinitions()
        {
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                taskDefinitions = JsonConvert.DeserializeObject<List<TaskDefinition>>(json)
                                  ?? new List<TaskDefinition>();
            }
            else
            {
                taskDefinitions = new List<TaskDefinition>();
            }
        }

        /// <summary>
        /// Saves the current task definitions to the JSON file.
        /// </summary>
        private void SaveTaskDefinitions()
        {
            string json = JsonConvert.SerializeObject(taskDefinitions, Formatting.Indented);
            File.WriteAllText(jsonFilePath, json);
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

                // Rebuild the selectable tasks list for "Compatible With".
                BuildSelectableTasks();
            }
        }
        private void BuildSelectableTasks()
        {
            selectableTasksCompatible.Clear();
            selectableTasksRequired.Clear();
            foreach (var td in taskDefinitions)
            {
                // Optionally, skip the current task's own name.
                if (currentTaskDefinition != null && td.Name == currentTaskDefinition.Name)
                    continue;

                selectableTasksCompatible.Add(new SelectableTask
                {
                    Name = td.Name,
                    IsSelected = currentTaskDefinition.CompatibleWith != null && currentTaskDefinition.CompatibleWith.Contains(td.Name)
                });
                selectableTasksRequired.Add(new SelectableTask
                {
                    Name = td.Name,
                    IsSelected = currentTaskDefinition.Requires != null && currentTaskDefinition.Requires.Contains(td.Name)
                });
            }
        }
        private void CheckBox_ChangedCompatible(object sender, RoutedEventArgs e)
        {
            if (currentTaskDefinition == null) return;

            // Update the CompatibleWith list from selectableTasks.
            currentTaskDefinition.CompatibleWith = selectableTasksCompatible
                .Where(st => st.IsSelected)
                .Select(st => st.Name)
                .ToList();
        }
        private void CheckBox_ChangedRequired(object sender, RoutedEventArgs e)
        {
            if (currentTaskDefinition == null) return;

            // Update the CompatibleWith list from selectableTasks.
            currentTaskDefinition.Requires = selectableTasksRequired.Where(st => st.IsSelected).Select(st => st.Name).ToList();
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
                // CompatibleWith is updated via the checkboxes.

                SaveTaskDefinitions();
                RefreshTaskList();
            }
        }

        private void btnAddNewTask_Click(object sender, RoutedEventArgs e)
        {
            // Create a new TaskDefinition with generic default values.
            TaskDefinition newTask = new TaskDefinition("New Task", 0.0, "DefaultLocation", new List<string>(), new List<string>());

            taskDefinitions.Add(newTask);
            SaveTaskDefinitions();
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
                    SaveTaskDefinitions();
                    RefreshTaskList();

                    // Clear editing fields.
                    textBoxName.Clear();
                    textBoxWeight.Clear();
                    textBoxLocation.Clear();
                    currentTaskDefinition = null;
                }
            }
        }
    }
}
