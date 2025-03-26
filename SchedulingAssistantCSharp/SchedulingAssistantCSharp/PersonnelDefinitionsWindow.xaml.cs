using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SchedulingAssistantCSharp
{
    /// <summary>
    /// Interaction logic for PersonnelDefinitionsWindow.xaml
    /// </summary>
    /// 
    public partial class PersonnelDefinitionsWindow : Window
    {
        // Lists for Roles and Persons.
        public class Role
        {
            public string Name { get; set; }
            public List<TaskDefinition> Tasks { get; set; }
            public Role(string name)
            {
                Name = name;
                Tasks = new List<TaskDefinition>();
            }
            public override string ToString()
            {
                return Name;
            }
        }
        private List<Role> roles = new List<Role>();
        private List<Person> persons = new List<Person>();

        // Global list of TaskDefinitions loaded from JSON.
        private List<TaskDefinition> allTaskDefinitions = new List<TaskDefinition>();

        // For binding Role tasks as selectable items.
        private List<SelectableTask> selectableRoleTasks = new List<SelectableTask>();

        // For binding Person performable tasks.
        private List<SelectableTask> selectablePersonTasks = new List<SelectableTask>();

        private Role currentRole;
        private Person currentPerson;
        private readonly string jsonFilePath = "TaskDefinitions.json";

        private void LoadTaskDefinitions()
        {
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                allTaskDefinitions = JsonConvert.DeserializeObject<List<TaskDefinition>>(json)
                                  ?? new List<TaskDefinition>();
            }
            else
            {
                allTaskDefinitions = new List<TaskDefinition>();
            }
        }

        public PersonnelDefinitionsWindow()
        {
            InitializeComponent();
            LoadTaskDefinitions();
            RefreshRolesList();
            RefreshPersonsList();
        }

        #region Role Section

        private void btnAddNewRole_Click(object sender, RoutedEventArgs e)
        {
            // Create a new Role and prepopulate its Tasks with a clone of every available task.
            Role newRole = new Role("New Role");
            newRole.Tasks = allTaskDefinitions
                .Select(t => new TaskDefinition(t.Name, t.Weight, t.Location, new List<string>(t.CompatibleWith), new List<string>(t.Requires)))
                .ToList();
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
        private void UpdateCurrentRoleTasks()
        {
            if (currentRole != null)
            {
                currentRole.Tasks = selectableRoleTasks
                    .Where(st => st.IsSelected)
                    .Select(st =>
                    {
                        var baseTask = allTaskDefinitions.FirstOrDefault(t => t.Name == st.Name);
                        return new TaskDefinition(st.Name, baseTask?.Weight ?? 0.0, baseTask?.Location, baseTask?.CompatibleWith, baseTask?.Requires);
                    })
                    .ToList();
            }
        }

        #endregion

        #region Person Section

        private void btnAddPerson_Click(object sender, RoutedEventArgs e)
        {
            if (currentRole == null)
            {
                MessageBox.Show("Please select a Role first.");
                return;
            }
            // Ensure the Role's tasks are up-to-date.
            UpdateCurrentRoleTasks();
            // Create a new Person using the current Role's tasks.
            Person newPerson = new Person("New Person", 0.0, null, null, currentRole.Tasks);
            persons.Add(newPerson);
            RefreshPersonsList();
            listBoxPersons.SelectedItem = newPerson;
        }

        private void listBoxPersons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBoxPersons.SelectedItem is Person selectedPerson)
            {
                currentPerson = selectedPerson;
                textBoxPersonName.Text = currentPerson.Name;
                textBoxWeightPerDay.Text = currentPerson.WeightPerDay.ToString();
                textBoxMaxWeight.Text = currentPerson.MaxWeight.ToString();
                textBoxCurrentWeight.Text = currentPerson.CurrentWeight.ToString();
                // Display Preferences, AvoidPreferences, and Schedule (shown simply as lists of strings).
                itemsControlPreferences.ItemsSource = currentPerson.Preferences;
                itemsControlAvoidPreferences.ItemsSource = currentPerson.AvoidPreferences;
                BuildSelectablePersonTasks();
            }
        }

        private void BuildSelectablePersonTasks()
        {
            selectablePersonTasks.Clear();
            foreach (var task in allTaskDefinitions)
            {
                selectablePersonTasks.Add(new SelectableTask
                {
                    Name = task.Name,
                    IsSelected = currentPerson.PerformableTasks.Any(t => t.Name == task.Name)
                });
            }
            itemsControlPerformableTasks.ItemsSource = null;
            itemsControlPerformableTasks.ItemsSource = selectablePersonTasks;
        }

        private void textBoxPersonName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (currentPerson != null)
            {
                currentPerson.Name = textBoxPersonName.Text;
                RefreshPersonsList();
            }
        }

        private void textBoxWeightPerDay_LostFocus(object sender, RoutedEventArgs e)
        {
            if (currentPerson != null && double.TryParse(textBoxWeightPerDay.Text, out double weight))
            {
                currentPerson.WeightPerDay = weight;
            }
        }

        // Save changes for the selected Person – update PerformableTasks from checkbox selections.
        private void btnSavePerson_Click(object sender, RoutedEventArgs e)
        {
            if (currentPerson != null)
            {
                currentPerson.PerformableTasks = selectablePersonTasks
                    .Where(st => st.IsSelected)
                    .Select(st =>
                    {
                        var baseTask = allTaskDefinitions.FirstOrDefault(t => t.Name == st.Name);
                        return new TaskDefinition(st.Name, baseTask?.Weight ?? 0.0, baseTask?.Location, baseTask?.CompatibleWith, baseTask?.Requires);
                    })
                    .ToList();
                RefreshPersonsList();
                MessageBox.Show("Person changes saved.");
            }
        }

        #endregion

        private void RefreshRolesList()
        {
            listBoxRoles.ItemsSource = null;
            listBoxRoles.ItemsSource = roles;
        }

        private void RefreshPersonsList()
        {
            listBoxPersons.ItemsSource = null;
            listBoxPersons.ItemsSource = persons;
        }
    }

    // Minimal selectable task for checkbox binding.
    public class SelectableTask
    {
        public string Name { get; set; }
        public bool IsSelected { get; set; }
        public TaskDefinition Task { get; set; }
    }
}
