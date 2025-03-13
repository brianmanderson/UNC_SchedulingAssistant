using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingAssistantCSharp
{
    // Represents a generic task definition.
    public class TaskDefinition
    {
        public string Name { get; set; }
        public double Weight { get; set; }
        public string Location { get; set; }
        public List<string> CompatibleWith { get; set; }
        public List<string> Requires { get; set; }
        public List<string> PeopleCanPerform { get; set; }

        public TaskDefinition(string name, double weight, string location = null,
            List<string> compatibleWith = null, List<string> requires = null)
        {
            Name = name;
            Weight = weight;
            Location = location;
            CompatibleWith = compatibleWith ?? new List<string>();
            Requires = requires ?? new List<string>();
            PeopleCanPerform = new List<string>(); // Initially empty; can be filled later.
        }

        public override string ToString()
        {
            return $"Task(name={Name}, weight={Weight}, location={Location})";
        }
    }

    // Represents a specific instance of a task scheduled for a particular date.
    public class ScheduledTask : TaskDefinition
    {
        public DateTime Date { get; set; }

        public ScheduledTask(TaskDefinition taskDefinition, DateTime date)
            : base(taskDefinition.Name, taskDefinition.Weight, taskDefinition.Location,
                   new List<string>(taskDefinition.CompatibleWith), new List<string>(taskDefinition.Requires))
        {
            Date = date;
        }

        public override string ToString()
        {
            return $"{base.ToString()} on {Date.ToShortDateString()}";
        }
    }

    // Represents a preference for either a task or a location on a given day.
    public class Preference
    {
        public DateTime Day { get; set; }
        public string TaskOrLocation { get; set; }
        public double Weight { get; set; }

        public Preference(DateTime day, string taskOrLocation, double weight)
        {
            Day = day;
            TaskOrLocation = taskOrLocation;
            Weight = weight;
        }

        public override string ToString()
        {
            return $"{TaskOrLocation} on {Day.ToShortDateString()} (weight: {Weight})";
        }
    }

    // Represents a person who can be assigned tasks.
    public class Person
    {
        public string Name { get; set; }
        public double WeightPerDay { get; set; }
        public double MaxWeight { get; private set; }
        public double CurrentWeight { get; private set; }
        public List<Preference> Preferences { get; set; }
        public List<Preference> AvoidPreferences { get; set; }
        public List<ScheduledTask> Schedule { get; set; }
        public List<TaskDefinition> PerformableTasks { get; set; }

        public Person(string name, double weightPerDay,
            List<Preference> preferences = null,
            List<Preference> avoidPreferences = null,
            List<TaskDefinition> performableTasks = null)
        {
            Name = name;
            WeightPerDay = weightPerDay;
            Preferences = preferences ?? new List<Preference>();
            AvoidPreferences = avoidPreferences ?? new List<Preference>();
            Schedule = new List<ScheduledTask>();
            CurrentWeight = 0.0;
            MaxWeight = 0.0;
            PerformableTasks = performableTasks ?? new List<TaskDefinition>();

            AddDevVacation(); // Adds default tasks like "Dev" and "Vacation"
        }

        // Adds default performable tasks if they are not already in the list.
        private void AddDevVacation()
        {
            var devTask = new TaskDefinition("Dev", 0.0, "Away");
            var halfDevTask = new TaskDefinition("HalfDev", 0.0);
            var vacationTask = new TaskDefinition("Vacation", 0.0, "Vacation");

            if (!PerformableTasks.Exists(t => t.Name == devTask.Name))
                PerformableTasks.Add(devTask);
            if (!PerformableTasks.Exists(t => t.Name == halfDevTask.Name))
                PerformableTasks.Add(halfDevTask);
            if (!PerformableTasks.Exists(t => t.Name == vacationTask.Name))
                PerformableTasks.Add(vacationTask);
        }

        // Adds daily capacity.
        public void AddDay()
        {
            MaxWeight += WeightPerDay;
        }

        // Checks whether the person can perform a given scheduled task.
        public bool CanPerformTask(ScheduledTask task, double weight)
        {
            // Check avoidance preferences.
            foreach (var avoidPref in AvoidPreferences)
            {
                if (avoidPref.Day.ToShortDateString() == task.Date.ToShortDateString() &&
                    (task.Name == avoidPref.TaskOrLocation || task.Location == avoidPref.TaskOrLocation) &&
                    avoidPref.Weight > weight)
                {
                    return false;
                }
            }

            // Verify that the task is in the list of tasks this person can perform.
            if (!PerformableTasks.Exists(t => t.Name == task.Name))
                return false;

            // Check for scheduling conflicts on the same day.
            foreach (var scheduledTask in Schedule)
            {
                if (scheduledTask.Date.ToShortDateString() == task.Date.ToShortDateString())
                {
                    // Location conflict.
                    if (!string.IsNullOrEmpty(scheduledTask.Location) &&
                        !string.IsNullOrEmpty(task.Location) &&
                        scheduledTask.Location != task.Location)
                        return false;

                    // Compatibility conflict.
                    if (!scheduledTask.CompatibleWith.Contains(task.Name) &&
                        !task.CompatibleWith.Contains(scheduledTask.Name))
                        return false;

                    // Avoid duplicate tasks.
                    if (task.Name == scheduledTask.Name)
                        return false;
                }
            }
            return true;
        }

        // Assigns a scheduled task to the person.
        public void AssignTask(ScheduledTask task)
        {
            Schedule.Add(task);
            CurrentWeight += task.Weight;
        }

        public override string ToString()
        {
            return $"Person(name={Name}, max_weight={MaxWeight}, current_weight={CurrentWeight})";
        }
    }

    // A specialized Person with default performable tasks for a Physicist.
    public class Physicist : Person
    {
        public Physicist(string name, double weightPerDay,
            List<Preference> preferences = null,
            List<Preference> avoidPreferences = null,
            List<TaskDefinition> performableTasks = null)
            : base(name, weightPerDay, preferences, avoidPreferences, performableTasks)
        {
            // Add tasks specific to a Physicist.
            var physicistTasks = new List<TaskDefinition>
            {
                new TaskDefinition("POD", 0.0, "UNC"),
                new TaskDefinition("POD_Backup", 0.0, "UNC"),
                new TaskDefinition("SAD", 0.0),
                new TaskDefinition("SAD_Assist", 0.0),
                new TaskDefinition("HBO", 0.0, "HBO"),
                new TaskDefinition("HDR_AMP", 0.0, "UNC"),
                new TaskDefinition("IORTTx", 0.0, "UNC")
            };

            foreach (var task in physicistTasks)
            {
                if (!PerformableTasks.Exists(t => t.Name == task.Name))
                    PerformableTasks.Add(task);
            }
        }
    }

    // Represents a day containing a set of scheduled tasks.
    public class Day
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public List<ScheduledTask> Tasks { get; set; }

        public Day(string name, DateTime date, List<ScheduledTask> tasks)
        {
            Name = name;
            Date = date;
            // Sort tasks by weight descending.
            tasks.Sort((t1, t2) => t2.Weight.CompareTo(t1.Weight));
            Tasks = tasks;
        }

        public override string ToString()
        {
            return $"{Name} ({Date.ToShortDateString()})";
        }
    }

    // Represents a week, which is a collection of days.
    public class Week
    {
        public string Name { get; set; }
        public List<Day> Days { get; set; }

        public Week(string name, List<Day> days)
        {
            Name = name;
            Days = days;
        }

        public override string ToString()
        {
            return $"Week: {Name}";
        }
    }
}
