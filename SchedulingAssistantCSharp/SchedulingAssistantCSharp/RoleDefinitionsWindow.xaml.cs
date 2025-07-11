using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private ObservableCollection<TaskDefinition> allTaskDefinitions = new ObservableCollection<TaskDefinition>();
        // List of roles defined here.
        private ObservableCollection<Role> roles = new ObservableCollection<Role>();
        private Role currentRole;

        public RoleDefinitionsWindow()
        {
            InitializeComponent();
            allTaskDefinitions = SerializerDeserializerClass.LoadTaskDefinitions();
            roles = SerializerDeserializerClass.LoadRoleDefinitions();
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
                var selectableTask = new SelectableTask
                {
                    Name = task.Name,
                    IsSelected = currentRole.Tasks.Any(t => t.Name == task.Name)
                };

                // Subscribe to property change
                selectableTask.PropertyChanged += SelectableTask_PropertyChanged;
                selectableRoleTasks.Add(selectableTask);
            }

            itemsControlRoleTasks.ItemsSource = null;
            itemsControlRoleTasks.ItemsSource = selectableRoleTasks;
        }
        private void SelectableTask_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected" && sender is SelectableTask selectableTask)
            {
                var taskInRole = currentRole.Tasks.FirstOrDefault(t => t.Name == selectableTask.Name);
                var originalTaskDefinition = allTaskDefinitions.FirstOrDefault(t => t.Name == selectableTask.Name);

                if (selectableTask.IsSelected)
                {
                    // Add task to the role if not already present
                    if (taskInRole == null && originalTaskDefinition != null)
                        currentRole.Tasks.Add(new TaskDefinition(
                            originalTaskDefinition.Name,
                            originalTaskDefinition.Weight,
                            originalTaskDefinition.Location,
                            originalTaskDefinition.CompatibleWith.ToList(),
                            originalTaskDefinition.Requires.ToList()));
                }
                else
                {
                    // Remove task from the role
                    if (taskInRole != null)
                        currentRole.Tasks.Remove(taskInRole);
                }
            }
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
            SerializerDeserializerClass.SaveRolesDefinitions(roles);
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SerializerDeserializerClass.SaveRolesDefinitions(roles);
        }
        private void DeleteRole_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Role roleToDelete)
            {
                roles.Remove(roleToDelete);
                BuildSelectableRoleTasks();
            }
        }
    }

    // Minimal selectable task for checkbox binding.
    public class SelectableTask : INotifyPropertyChanged
    {
        private bool _isSelected;
        public string Name { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
