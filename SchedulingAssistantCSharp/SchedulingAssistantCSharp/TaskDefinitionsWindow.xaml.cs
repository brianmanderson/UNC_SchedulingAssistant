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
                var selectedTask = taskDefinitions[index];
                textBoxName.Text = selectedTask.Name;
                textBoxWeight.Text = selectedTask.Weight.ToString();
                textBoxLocation.Text = selectedTask.Location;
                textBoxCompatibleWith.Text = selectedTask.CompatibleWith != null
                    ? string.Join(",", selectedTask.CompatibleWith)
                    : string.Empty;
                textBoxRequires.Text = selectedTask.Requires != null
                    ? string.Join(",", selectedTask.Requires)
                    : string.Empty;
            }
        }

        private void btnSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            int index = listBoxTasks.SelectedIndex;
            if (index >= 0 && index < taskDefinitions.Count)
            {
                var task = taskDefinitions[index];
                task.Name = textBoxName.Text;
                if (double.TryParse(textBoxWeight.Text, out double weight))
                {
                    task.Weight = weight;
                }
                task.Location = textBoxLocation.Text;
                task.CompatibleWith = textBoxCompatibleWith.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(s => s.Trim()).ToList();
                task.Requires = textBoxRequires.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(s => s.Trim()).ToList();

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
    }
}
