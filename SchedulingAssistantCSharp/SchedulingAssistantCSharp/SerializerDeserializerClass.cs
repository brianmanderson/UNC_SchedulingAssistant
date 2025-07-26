using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SchedulingAssistantCSharp
{
    public static class DataPaths
    {
        public static readonly string TaskJsonFilePath = "TaskDefinitions.json";
        public static readonly string TaskGroupJsonFilePath = "TaskGrouoDefinitions.json";
        public static readonly string RolesJsonFilePath = "RolesDefinitions.json";
        public static readonly string PeopleJsonFilePath = @"PeopleDefinitions.json";
        public static readonly string ScheduleJsonFilePath = @"ScheduledTasks.json";
        // Add more file paths as needed.
    }
    public static class SerializerDeserializerClass
    {
        public static ObservableCollection<TaskGroup> LoadTaskGroups()
        {
            ObservableCollection<TaskGroup> task_groups = new ObservableCollection<TaskGroup>();
            if (File.Exists(DataPaths.TaskGroupJsonFilePath))
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    Formatting = Formatting.Indented
                };
                string json = File.ReadAllText(DataPaths.TaskGroupJsonFilePath);
                task_groups = JsonConvert.DeserializeObject<ObservableCollection<TaskGroup>>(json, settings);
            }
            return task_groups;
        }
        public static void SaveTaskGroups(ObservableCollection<TaskGroup> task_groups)
        {
            string json = JsonConvert.SerializeObject(task_groups, Formatting.Indented);
            File.WriteAllText(DataPaths.TaskGroupJsonFilePath, json);
        }
        public static ObservableCollection<ScheduledTask> LoadSchedule(ObservableCollection<Person> people)
        {
            ObservableCollection<ScheduledTask> new_schedule = new ObservableCollection<ScheduledTask>();
            //List<Person> all_people = people.ToList();
            if (File.Exists(DataPaths.ScheduleJsonFilePath))
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    Formatting = Formatting.Indented
                };
                string json = File.ReadAllText(DataPaths.ScheduleJsonFilePath);
                new_schedule = JsonConvert.DeserializeObject<ObservableCollection<ScheduledTask>>(json, settings);
            }
            foreach (ScheduledTask st in new_schedule)
            {
                if (!string.IsNullOrEmpty(st.AssignedPersonName))
                {
                    Person person = people.FirstOrDefault(p => p.Name == st.AssignedPersonName);
                    if (person != null)
                        person.AssignTask(st);   // sets both sides: Schedule + AssignedPerson
                }
            }
            return new_schedule;
        }
        public static void SaveSchedule(ObservableCollection<ScheduledTask> schedule)
        {
            string json = JsonConvert.SerializeObject(schedule, Formatting.Indented);
            File.WriteAllText(DataPaths.ScheduleJsonFilePath, json);
        }
        public static ObservableCollection<Person> LoadPeopleDefinitions()
        {
            ObservableCollection<Person> new_people = new ObservableCollection<Person>();
            if (File.Exists(DataPaths.PeopleJsonFilePath))
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    Formatting = Formatting.Indented
                };
                string json = File.ReadAllText(DataPaths.PeopleJsonFilePath);
                new_people = JsonConvert.DeserializeObject<ObservableCollection<Person>>(json, settings);
            }
            return new_people;
        }
        public static void SavePeopleDefinitions(ObservableCollection<Person> people)
        {
            string json = JsonConvert.SerializeObject(people, Formatting.Indented);
            File.WriteAllText(DataPaths.PeopleJsonFilePath, json);
        }
        public static ObservableCollection<TaskDefinition> LoadTaskDefinitions()
        {
            ObservableCollection<TaskDefinition> allTaskDefinitions = new ObservableCollection<TaskDefinition>();
            if (File.Exists(DataPaths.TaskJsonFilePath))
            {
                string json = File.ReadAllText(DataPaths.TaskJsonFilePath);
                allTaskDefinitions = JsonConvert.DeserializeObject<ObservableCollection<TaskDefinition>>(json)
                                  ?? new ObservableCollection<TaskDefinition>();
            }
            return allTaskDefinitions;
        }
        public static void SaveTaskDefinitions(ObservableCollection<TaskDefinition> taskDefinitions)
        {
            string json = JsonConvert.SerializeObject(taskDefinitions, Formatting.Indented);
            File.WriteAllText(DataPaths.TaskJsonFilePath, json);
        }
        public static ObservableCollection<Role> LoadRoleDefinitions()
        {
            ObservableCollection<Role> roles = new ObservableCollection<Role>();
            if (File.Exists(DataPaths.RolesJsonFilePath))
            {
                string json = File.ReadAllText(DataPaths.RolesJsonFilePath);
                roles = JsonConvert.DeserializeObject<ObservableCollection<Role>>(json)
                                  ?? new ObservableCollection<Role>();
            }
            return roles;
        }

        public static void SaveRolesDefinitions(ObservableCollection<Role> roles)
        {
            string json = JsonConvert.SerializeObject(roles, Formatting.Indented);
            File.WriteAllText(DataPaths.RolesJsonFilePath, json);
        }
    }
}
