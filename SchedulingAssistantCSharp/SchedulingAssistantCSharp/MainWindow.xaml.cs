using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace SchedulingAssistantCSharp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string json_people_path = @"people.json";
        ObservableCollection<Person> people = new ObservableCollection<Person>();
        public MainWindow()
        {
            InitializeComponent();
            load_people();

        }
        public void load_people()
        {
            if (File.Exists(json_people_path))
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    Formatting = Formatting.Indented
                };
                string json = File.ReadAllText(json_people_path);
                people = JsonConvert.DeserializeObject<ObservableCollection<Person>>(json, settings);
            }
            else
            {
                create_people();
            }
        }
        public void create_people()
        {
            DateTime dateMonday = new DateTime(2024, 8, 26);
            DateTime dateTuesday = new DateTime(2024, 8, 27);
            DateTime dateWednesday = new DateTime(2024, 8, 28);
            DateTime dateThursday = new DateTime(2024, 8, 29);
            DateTime dateFriday = new DateTime(2024, 8, 30);
            // Leith is a Physicist.
            var leith = new Physicist(
                "Leith",
                12.0 / 5,
                new List<Preference>
                {
                    new Preference(dateMonday, "POD", 7.0)
                }
            );
            people.Add(leith);

            // Taki is a Physicist.
            var taki = new Physicist(
                "Taki",
                12.0 / 5,
                new List<Preference>
                {
                    new Preference(dateMonday, "Dev", 7.0),
                    new Preference(dateTuesday, "Dev", 7.0)
                },
                null, // No avoid preferences.
                new List<TaskDefinition>
                {
                    new TaskDefinition("Gamma_Tile", 0.0, "UNC")
                }
            );
            people.Add(taki);

            // Dance can perform 'Gamma_Tile'.
            var dance = new Physicist(
                "Dance",
                18.0 / 5,
                new List<Preference>(), // No preferences.
                null,
                new List<TaskDefinition>
                {
                    new TaskDefinition("Gamma_Tile", 0.0, "UNC")
                }
            );
            people.Add(dance);

            // Adria is a Physicist.
            var adria = new Physicist(
                "Adria",
                18.0 / 5,
                new List<Preference>
                {
                    new Preference(dateMonday, "Vacation", 9.0)
                }
            );
            people.Add(adria);

            // Cielle can perform 'Prostate_Brachy', 'Dev', 'HalfDev', and 'Vacation'.
            var cielle = new Physicist(
                "Cielle",
                18.0 / 5,
                new List<Preference>(), // No specific preferences.
                null,
                new List<TaskDefinition>
                {
                    new TaskDefinition("Prostate_Brachy", 0.0, "UNC")
                }
            );
            people.Add(cielle);

            // Brian is a Physicist.
            var brian = new Physicist(
                "Brian",
                12.0 / 5,
                new List<Preference>
                {
                    new Preference(dateMonday, "POD_Backup", 3.0),
                    new Preference(dateFriday, "SAD", 7.0)
                }
            );
            people.Add(brian);

            // David is a Physicist with avoid preferences.
            var david = new Physicist(
                "David",
                12.0 / 5,
                null, // No normal preferences.
                new List<Preference>
                {
                    new Preference(dateMonday, "HBO", 9.0),
                    new Preference(dateMonday, "UNC", 9.0),
                    new Preference(dateTuesday, "HBO", 9.0),
                    new Preference(dateTuesday, "UNC", 9.0),
                    new Preference(dateWednesday, "HBO", 1.0),
                    new Preference(dateThursday, "HBO", 1.0),
                    new Preference(dateFriday, "HBO", 1.0),
                    new Preference(dateFriday, "UNC", 1.0)
                }
            );
            people.Add(david);

            // Jun can perform 'Prostate_Brachy'.
            var jun = new Physicist(
                "Jun",
                12.0 / 5,
                new List<Preference>
                {
                    new Preference(dateFriday, "Vacation", 9.0)
                },
                null,
                new List<TaskDefinition>
                {
                    new TaskDefinition("Prostate_Brachy", 0.0, "UNC")
                }
            );
            people.Add(jun);

            // Ross is a Physicist.
            var ross = new Physicist(
                "Ross",
                18.0 / 5,
                new List<Preference>
                {
                    new Preference(dateFriday, "Vacation", 9.0)
                }
            );
            people.Add(ross);
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Formatting = Formatting.Indented
            };
            string jsonString = JsonConvert.SerializeObject(people, settings);
            File.WriteAllText(json_people_path, jsonString);
            // Optionally, iterate over people and print their details.
            foreach (var person in people)
            {
                Console.WriteLine(person);
            }
        }
    }
}
