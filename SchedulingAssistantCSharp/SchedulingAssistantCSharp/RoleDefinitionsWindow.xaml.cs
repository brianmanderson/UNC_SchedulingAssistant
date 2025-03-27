using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SchedulingAssistantCSharp
{
    public partial class RoleDefinitionsWindow : Window
    {
        // Role model (local to this window)

        // For binding role tasks.
        private List<SelectableTask> selectableRoleTasks = new List<SelectableTask>();
        // Global list of TaskDefinitions loaded from JSON.
        private List<TaskDefinition> allTaskDefinitions = new List<TaskDefinition>();
        // List of roles defined here.
        private List<Role> roles = new List<Role>();
        private Role currentRole;
        private readonly string taskjsonFilePath = "TaskDefinitions.json";
        private readonly string rolesjsonFilePath = "RoleDefinitions.json";

        private void LoadTaskDefinitions()
        {
            if (File.Exists(taskjsonFilePath))
            {
                string json = File.ReadAllText(taskjsonFilePath);
                allTaskDefinitions = JsonConvert.DeserializeObject<List<TaskDefinition>>(json)
                                  ?? new List<TaskDefinition>();
            }
            else
            {
                allTaskDefinitions = new List<TaskDefinition>();
            }
        }

        private void LoadRoleDefinitions()
        {
            if (File.Exists(rolesjsonFilePath))
            {
                string json = File.ReadAllText(rolesjsonFilePath);
                roles = JsonConvert.DeserializeObject<List<Role>>(json)
                                  ?? new List<Role>();
            }
            else
            {
                roles = new List<Role>();
            }
        }

        private void SaveRolesDefinitions()
        {
            string json = JsonConvert.SerializeObject(roles, Formatting.Indented);
            File.WriteAllText(rolesjsonFilePath, json);
        }

        public RoleDefinitionsWindow()
        {
            InitializeComponent();
            LoadTaskDefinitions();
            LoadRoleDefinitions();
            RefreshRolesList();
        }

        private void btnAddNewRole_Click(object sender, RoutedEventArgs e)
        {
            // Create a new Role and prepopulate its Tasks with a clone of every available task.
            Role newRole = new Role("New Role")
            {
                Tasks = allTaskDefinitions
                    .Select(t => new TaskDefinition(t.Name, t.Weight, t.Location,
                        new List<string>(t.CompatibleWith), new List<string>(t.Requires)))
                    .ToList()
            };
            roles.Add(newRole);
            RefreshRolesList();
            listBoxRoles.SelectedItem = newRole;
        }

        private void listBoxRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBoxRoles.SelectedItem is Role selectedRole)
            {
                currentRole = selectedRole;
                textBoxRoleName.Text = currentRole.Name;
                BuildSelectableRoleTasks();
            }
        }

        private void BuildSelectableRoleTasks()
        {
            selectableRoleTasks.Clear();
            foreach (var task in allTaskDefinitions)
            {
                selectableRoleTasks.Add(new SelectableTask
                {
                    Name = task.Name,
                    IsSelected = currentRole.Tasks.Any(t => t.Name == task.Name)
                });
            }
            itemsControlRoleTasks.ItemsSource = null;
            itemsControlRoleTasks.ItemsSource = selectableRoleTasks;
        }

        private void textBoxRoleName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (currentRole != null)
            {
                currentRole.Name = textBoxRoleName.Text;
                RefreshRolesList();
            }
        }

        // Update the current Role's Tasks based on checkbox selections.

        private void RefreshRolesList()
        {
            listBoxRoles.ItemsSource = null;
            listBoxRoles.ItemsSource = roles;
        }


        private void SaveExit_Click(object sender, RoutedEventArgs e)
        {
            SaveRolesDefinitions();
            Close();
        }
    }

    // Minimal selectable task for checkbox binding.
    public class SelectableTask
    {
        public string Name { get; set; }
        public bool IsSelected { get; set; }
    }
}
