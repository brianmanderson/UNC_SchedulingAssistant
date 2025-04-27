// TaskGroupsWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SchedulingAssistantCSharp
{
    public partial class TaskGroupsWindow : Window
    {
        // In-memory collections
        private ObservableCollection<TaskGroup> taskGroups;
        private ObservableCollection<TaskDefinition> allTaskDefinitions;
        private List<SelectableDefinition> selectableDefinitions = new List<SelectableDefinition>();
        private TaskGroup currentGroup;

        // Helper for checkbox binding
        public class SelectableDefinition
        {
            public string Name { get; set; }
            public bool IsSelected { get; set; }
        }

        public TaskGroupsWindow()
        {
            InitializeComponent();

            // Load everything
            allTaskDefinitions = SerializerDeserializerClass.LoadTaskDefinitions();      // :contentReference[oaicite:0]{index=0}
            taskGroups = SerializerDeserializerClass.LoadTaskGroups();                  // :contentReference[oaicite:1]{index=1}

            RefreshTaskGroupsList();
        }

        private void RefreshTaskGroupsList()
        {
            listBoxTaskGroups.ItemsSource = null;
            listBoxTaskGroups.ItemsSource = taskGroups;
        }

        private void listBoxTaskGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBoxTaskGroups.SelectedItem is TaskGroup grp)
            {
                currentGroup = grp;
                textBoxGroupName.Text = currentGroup.Name;
                BuildSelectableDefinitions();
            }
        }

        private void BuildSelectableDefinitions()
        {
            selectableDefinitions.Clear();
            foreach (var def in allTaskDefinitions)                                     // :contentReference[oaicite:2]{index=2}
            {
                selectableDefinitions.Add(new SelectableDefinition
                {
                    Name = def.Name,
                    IsSelected = currentGroup.Tasks.Any(t => t.Name == def.Name)
                });
            }
            itemsControlDefinitions.ItemsSource = null;
            itemsControlDefinitions.ItemsSource = selectableDefinitions;
        }

        private void btnAddGroup_Click(object sender, RoutedEventArgs e)
        {
            var name = textBoxNewGroup.Text.Trim();
            if (string.IsNullOrEmpty(name)) return;

            var newGroup = new TaskGroup(name);
            taskGroups.Add(newGroup);
            SerializerDeserializerClass.SaveTaskGroups(taskGroups);                     // :contentReference[oaicite:3]{index=3}
            RefreshTaskGroupsList();
            listBoxTaskGroups.SelectedItem = newGroup;
        }

        private void textBoxGroupName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (currentGroup != null)
            {
                currentGroup.Name = textBoxGroupName.Text;
                RefreshTaskGroupsList();
            }
        }

        private void btnSaveAndExit_Click(object sender, RoutedEventArgs e)
        {
            if (currentGroup != null)
            {
                // Update group membership
                currentGroup.Tasks = selectableDefinitions
                    .Where(sd => sd.IsSelected)
                    .Select(sd => allTaskDefinitions.First(td => td.Name == sd.Name))
                    .ToList();
            }

            SerializerDeserializerClass.SaveTaskGroups(taskGroups);                     // :contentReference[oaicite:4]{index=4}
            Close();
        }
    }
}
