using System.Collections.Generic;
using System.Windows;

namespace SchedulingAssistantCSharp
{
    public partial class AssignPersonWindow : Window
    {
        public Person SelectedPerson => listBoxPersons.SelectedItem as Person;

        public AssignPersonWindow(IEnumerable<Person> people)
        {
            InitializeComponent();
            listBoxPersons.ItemsSource = people;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPerson != null)
                DialogResult = true;
            else
                MessageBox.Show("Please select a person first.", "No selection",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
