using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SchedulingAssistantCSharp
{
    public partial class PersonnelDefinitionsWindow : Window
    {
        // Person-related models and lists.
        private List<Person> persons = new List<Person>();
        // Global list of TaskDefinitions loaded from JSON.
        private List<TaskDefinition> allTaskDefinitions = new List<TaskDefinition>();
        // For binding Person performable tasks.
        private List<SelectableTask> selectablePersonTasks = new List<SelectableTask>();

        private Person currentPerson;
        private readonly string taskjsonFilePath = "TaskDefinitions.json";
        private readonly string peoplejsonFilePath = "PeopleDefinitions.json";
        private readonly string rolesjsonFilePath = "RolesDefinitions.json";

        // List of available roles provided externally.
        private List<Role> availableRoles = new List<Role>();

        // Loads tasks from JSON.
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
                availableRoles = JsonConvert.DeserializeObject<List<Role>>(json)
                                  ?? new List<Role>();
            }
            else
            {
                availableRoles = new List<Role>();
            }
            SetAvailableRoles(availableRoles);
        }

        private void LoadPeopleDefinitions()
        {
            if (File.Exists(peoplejsonFilePath))
            {
                string json = File.ReadAllText(peoplejsonFilePath);
                persons = JsonConvert.DeserializeObject<List<Person>>(json)
                                  ?? new List<Person>();
            }
            else
            {
                persons = new List<Person>();
            }
        }

        private void SavePeopleDefinitions()
        {
            string json = JsonConvert.SerializeObject(persons, Formatting.Indented);
            File.WriteAllText(peoplejsonFilePath, json);
        }
        public PersonnelDefinitionsWindow()
        {
            InitializeComponent();
            LoadTaskDefinitions();
            LoadRoleDefinitions();
            LoadPeopleDefinitions();
            // (Assume availableRoles is set from an external source.)
            // For demo purposes, if no roles are set, create a default one.
            comboBoxRoles.ItemsSource = availableRoles;
            RefreshPersonsList();
        }

        // Option for external code to provide the list of roles.
        public void SetAvailableRoles(List<Role> roles)
        {
            availableRoles = roles;
            comboBoxRoles.ItemsSource = availableRoles;
        }

        private void btnAddPerson_Click(object sender, RoutedEventArgs e)
        {
            var selectedRole = comboBoxRoles.SelectedItem as Role;
            if (selectedRole == null)
            {
                MessageBox.Show("Please select a Role from the drop-down.");
                return;
            }
            // Create a new Person using the selected Role's tasks.
            Person newPerson = new Person("New Person", 0.0, null, null, selectedRole.Tasks);
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
            SavePeopleDefinitions();
        }

        private void RefreshPersonsList()
        {
            listBoxPersons.ItemsSource = null;
            listBoxPersons.ItemsSource = persons;
        }

        private void btnSaveAndExit_Click(object sender, RoutedEventArgs e)
        {
            SavePeopleDefinitions();
            Close();
        }

        private void btnAddRoles_Click(object sender, RoutedEventArgs e)
        {
            RoleDefinitionsWindow roleDefinitionsWindow = new RoleDefinitionsWindow();
            roleDefinitionsWindow.ShowDialog();
            LoadRoleDefinitions();
        }

        private void comboBoxRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }

    // Minimal selectable task for checkbox binding.
}
