using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace SchedulingAssistantCSharp
{
    public static class DataPaths
    {
        public static readonly string TaskJsonFilePath = "TaskDefinitions.json";
        public static readonly string RolesJsonFilePath = "RolesDefinitions.json";
        // Add more file paths as needed.
    }
    public static class SerializerDeserializerClass
    {
        public static List<TaskDefinition> LoadTaskDefinitions()
        {
            List<TaskDefinition> allTaskDefinitions = new List<TaskDefinition>();
            if (File.Exists(DataPaths.TaskJsonFilePath))
            {
                string json = File.ReadAllText(DataPaths.TaskJsonFilePath);
                allTaskDefinitions = JsonConvert.DeserializeObject<List<TaskDefinition>>(json)
                                  ?? new List<TaskDefinition>();
            }
            return allTaskDefinitions;
        }
        public static void SaveTaskDefinitions(List<TaskDefinition> taskDefinitions)
        {
            string json = JsonConvert.SerializeObject(taskDefinitions, Formatting.Indented);
            File.WriteAllText(DataPaths.TaskJsonFilePath, json);
        }
        public static List<Role> LoadRoleDefinitions()
        {
            List<Role> roles = new List<Role>();
            if (File.Exists(DataPaths.RolesJsonFilePath))
            {
                string json = File.ReadAllText(DataPaths.RolesJsonFilePath);
                roles = JsonConvert.DeserializeObject<List<Role>>(json)
                                  ?? new List<Role>();
            }
            return roles;
        }

        public static void SaveRolesDefinitions(List<Role> roles)
        {
            string json = JsonConvert.SerializeObject(roles, Formatting.Indented);
            File.WriteAllText(DataPaths.RolesJsonFilePath, json);
        }
    }
}
