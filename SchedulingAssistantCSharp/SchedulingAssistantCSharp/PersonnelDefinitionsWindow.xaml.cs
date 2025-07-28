using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SchedulingAssistantCSharp
{
    public partial class PersonnelDefinitionsWindow : Window
    {
        // Person-related list.
        private ObservableCollection<Person> persons = new ObservableCollection<Person>();
        // Global list of TaskDefinitions loaded from JSON.
        private ObservableCollection<TaskDefinition> allTaskDefinitions = new ObservableCollection<TaskDefinition>();
        // For binding Person performable tasks.
        private List<SelectableTask> selectablePersonTasks = new List<SelectableTask>();

        private Person currentPerson;
        private bool isPopulatingFields = false;

        // List of available roles provided externally.
        private ObservableCollection<Role> availableRoles = new ObservableCollection<Role>();

        // Loads tasks from JSON.
        public PersonnelDefinitionsWindow()
        {
            InitializeComponent();
            allTaskDefinitions = SerializerDeserializerClass.LoadTaskDefinitions();
            availableRoles = SerializerDeserializerClass.LoadRoleDefinitions();
            persons = SerializerDeserializerClass.LoadPeopleDefinitions();
            // (Assume availableRoles is set from an external source.)
            // For demo purposes, if no roles are set, create a default one.
            comboBoxRoles.ItemsSource = availableRoles;
            RefreshPersonsList();
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
                isPopulatingFields = true; // Begin suppressing TextChanged
                currentPerson = selectedPerson;
                textBoxPersonName.Text = currentPerson.Name;
                textBoxWeightPerDay.Text = currentPerson.WeightPerDay.ToString();
                textBoxMaxWeight.Text = currentPerson.MaxWeight.ToString();
                BuildSelectablePersonTasks();
                isPopulatingFields = false; // Re-enable TextChanged
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

        // Event handler that fires whenever a performable task checkbox is checked or unchecked.
        private void PerformableTaskCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (currentPerson != null)
            {
                currentPerson.PerformableTasks = selectablePersonTasks
                    .Where(st => st.IsSelected)
                    .Select(st =>
                    {
                        var baseTask = allTaskDefinitions.FirstOrDefault(t => t.Name == st.Name);
                        return new TaskDefinition(st.Name, baseTask?.Weight ?? 0.0, baseTask?.Location,
                            baseTask?.CompatibleWith, baseTask?.Requires);
                    })
                    .ToList();
            }
        }

        // Optionally still allow a manual Save button.
        private void btnSavePerson_Click(object sender, RoutedEventArgs e)
        {
            // In this implementation, the Person's PerformableTasks are updated immediately as checkboxes change.
            // You can still use this button to confirm and refresh the UI.
            SerializerDeserializerClass.SavePeopleDefinitions(persons);
            RefreshPersonsList();
            MessageBox.Show("Person changes saved.");
        }
        private void DeletePerson_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Person personToDelete)
            {
                if (MessageBox.Show($"Are you sure you want to delete {personToDelete.Name}?", "Confirm Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    persons.Remove(personToDelete);
                    SerializerDeserializerClass.SavePeopleDefinitions(persons);
                    RefreshPersonsList();
                }
            }
        }
        private void RefreshPersonsList()
        {
            listBoxPersons.ItemsSource = null;
            listBoxPersons.ItemsSource = persons;
        }

        private void comboBoxRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnAddRoles_Click(object sender, RoutedEventArgs e)
        {
            RoleDefinitionsWindow roleDefinitionsWindow = new RoleDefinitionsWindow();
            roleDefinitionsWindow.ShowDialog();
            availableRoles = SerializerDeserializerClass.LoadRoleDefinitions();
        }

        private void textBoxChanged(object sender, TextChangedEventArgs e)
        {
            if (isPopulatingFields || currentPerson == null)
                return;
            if (currentPerson != null && double.TryParse(textBoxWeightPerDay.Text, out double weight))
            {
                currentPerson.WeightPerDay = weight;
            }
            if (currentPerson != null && double.TryParse(textBoxMaxWeight.Text, out double maxweight))
            {
                currentPerson.MaxWeight = maxweight;
            }
            if (currentPerson != null && !string.IsNullOrEmpty(textBoxPersonName.Text))
            {
                currentPerson.Name = textBoxPersonName.Text;
            }
        }
    }

    // Minimal selectable task for checkbox binding.
}
