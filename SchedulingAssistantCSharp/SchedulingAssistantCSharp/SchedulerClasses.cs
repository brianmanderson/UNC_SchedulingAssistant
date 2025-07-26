using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SchedulingAssistantCSharp
{
    // Represents a generic task definition.
    public class TaskDefinition
    {
        public string Name { get; set; }
        public double Weight { get; set; }
        public string Location { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public List<string> CompatibleWith { get; set; }
        public List<string> Requires { get; set; }
        public List<string> PeopleCanPerform { get; set; }

        public TaskDefinition(string name, double weight, string location = null,
            List<string> compatibleWith = null, List<string> requires = null,
            TimeSpan? startTime = null, TimeSpan? endTime = null)
        {
            Name = name;
            Weight = weight;
            Location = location;
            StartTime = startTime ?? new TimeSpan(8, 30, 0); // default 8:30 AM
            EndTime = endTime ?? new TimeSpan(16, 30, 0);     // default 4:30 PM
            CompatibleWith = compatibleWith ?? new List<string>();
            Requires = requires ?? new List<string>();
            PeopleCanPerform = new List<string>(); // Initially empty; can be filled later.
        }

        public override string ToString()
        {
            return $"Task(name={Name}, weight={Weight}, location={Location}, time={StartTime}-{EndTime})";
        }
    }

    public class TaskGroup
    {
        public string Name { get; set; }
        public List<TaskDefinition> Tasks { get; set; }

        public TaskGroup(string name)
        {
            Name = name;
            Tasks = new List<TaskDefinition>();
        }
        public void add_task(TaskDefinition task)
        {
            Tasks.Add(task);
        }
        public override string ToString()
        {
            return $"TaskGroup(name={Name}, has tasks {Tasks})";
        }
    }

    // Represents a specific instance of a task scheduled for a particular date.
    public class ScheduledTask
    {
        public TaskDefinition Task { get; }
        public DateTime ScheduledDate { get; set; }

        [Newtonsoft.Json.JsonIgnore]           // prevent JSON loops if you’re saving to JSON
        public Person AssignedPerson { get; internal set; }

        public string AssignedPersonName { get; set; }

        public bool Locked { get; set; } = false;

        public ScheduledTask(TaskDefinition task, DateTime scheduledDate)
        {
            Task = task;
            ScheduledDate = scheduledDate;
        }

        public override string ToString()
        {
            return $"{Task.Name} on {ScheduledDate.ToShortDateString()}";
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

    public class Role
    {
        public string Name { get; set; }
        public List<TaskDefinition> Tasks { get; set; }
        public Role(string name)
        {
            Name = name;
            Tasks = new List<TaskDefinition>();
        }
        public override string ToString() => Name;
    }
    public class Person
    {
        public string Name { get; set; }
        public double WeightPerDay { get; set; }
        public double MaxWeight { get; set; }
        public double CurrentWeight { get; set; }
        public List<Preference> Preferences { get; set; }
        public List<Preference> AvoidPreferences { get; set; }
        public List<ScheduledTask> Schedule { get; set; }
        public List<TaskDefinition> PerformableTasks { get; set; }

        private TaskDefinition _task;

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
        public bool CanPerformTask(ScheduledTask scheduled_task, double weight)
        {
            _task = scheduled_task.Task;
            // Check avoidance preferences.
            foreach (var avoidPref in AvoidPreferences)
            {
                if (avoidPref.Day.ToShortDateString() == scheduled_task.ScheduledDate.ToShortDateString() &&
                    (_task.Name == avoidPref.TaskOrLocation || _task.Location == avoidPref.TaskOrLocation) &&
                    avoidPref.Weight > weight)
                {
                    return false;
                }
            }

            // Verify that the task is in the list of tasks this person can perform.
            if (!PerformableTasks.Exists(t => t.Name == _task.Name))
                return false;

            // Check for scheduling conflicts on the same day.
            foreach (var conflict_scheduledTask in Schedule)
            {
                TaskDefinition task = conflict_scheduledTask.Task;
                if (conflict_scheduledTask.ScheduledDate.ToShortDateString() == scheduled_task.ScheduledDate.ToShortDateString())
                {
                    // Location conflict.
                    if (!string.IsNullOrEmpty(task.Location) &&
                        !string.IsNullOrEmpty(task.Location) &&
                        scheduled_task.Task.Location != task.Location)
                        return false;

                    // Compatibility conflict.
                    if (!_task.CompatibleWith.Contains(task.Name) &&
                        !task.CompatibleWith.Contains(_task.Name))
                        return false;

                    // Avoid duplicate tasks.
                    if (task.Name == _task.Name)
                        return false;
                }
            }
            return true;
        }

        // Assigns a scheduled task to the person.
        public void AssignTask(ScheduledTask scheduledTask)
        {
            if (scheduledTask.AssignedPerson != null)
                throw new InvalidOperationException("Already assigned to "
                                                    + scheduledTask.AssignedPerson.Name);

            scheduledTask.AssignedPerson = this;    // ← set the back-ref
            scheduledTask.AssignedPersonName = Name;
            Schedule.Add(scheduledTask);            // ← add to this person
            CurrentWeight += scheduledTask.Task.Weight;
        }

        public void UnassignTask(ScheduledTask scheduledTask)
        {
            if (scheduledTask.AssignedPerson != this)
                throw new InvalidOperationException("Not assigned to me!");

            scheduledTask.AssignedPerson = null;
            scheduledTask.AssignedPersonName = null;
            Schedule.Remove(scheduledTask);
            CurrentWeight -= scheduledTask.Task.Weight;
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
            tasks.Sort((t1, t2) => t2.Task.Weight.CompareTo(t1.Task.Weight));
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
