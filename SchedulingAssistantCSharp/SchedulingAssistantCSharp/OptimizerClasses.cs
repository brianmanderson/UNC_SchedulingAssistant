using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingAssistantCSharp
{
    public class OptimizerClass
    {
        public OptimizerClass()
        {
            // Constructor logic if needed
        }
        public List<Person> GetEligiblePeople(List<Person> allPeople, ScheduledTask task, double preference_weight)
        {
            return allPeople
                .Where(p => p.CanPerformTask(task, preference_weight))
                .ToList();
        }
        public TimeSpan GetPreviousDayEndTime(Person person, DateTime currentDate)
        {
            var previousDay = currentDate.AddDays(-1).Date;
            var prevTasks = person.Schedule
                .Where(s => s.ScheduledDate.Date == previousDay)
                .Select(s => s.Task.EndTime)
                .ToList();

            return prevTasks.Any() ? prevTasks.Max() : TimeSpan.Zero;
        }
        public double CalculatePersonAssignmentScore(Person person, ScheduledTask task)
        {
            // Weight balance score
            double weightGap = person.CurrentWeight - person.MaxWeight;

            // Time penalty score (lower is better)
            TimeSpan previousDayEndTime = GetPreviousDayEndTime(person, task.ScheduledDate);
            TimeSpan taskStart = task.Task.StartTime;

            // We can argue that working within 12 hours of the previous day incurs a penalty
            double timePenalty = 0.0;
            if (previousDayEndTime != TimeSpan.Zero && (taskStart - previousDayEndTime).Hours < 12)
                timePenalty = 4.0;

            return weightGap + timePenalty;
        }
    }

    public class SimulatedAnnealingScheduler
    {
        private Random rng = new Random();
        private OptimizerClass optimizer = new OptimizerClass();

        public class ScheduleState
        {
            public Dictionary<ScheduledTask, Person> Assignment;
            public Dictionary<string, double> VirtualWeights;
            public double Cost;

            public ScheduleState Clone()
            {
                // Step 1: Deep copy all ScheduledTasks
                Dictionary<ScheduledTask, ScheduledTask> taskMap = new Dictionary<ScheduledTask, ScheduledTask>();
                foreach (var task in Assignment.Keys)
                {
                    var clonedTask = new ScheduledTask(
                        new TaskDefinition(
                            name: task.Task.Name,
                            weight: task.Task.Weight,
                            location: task.Task.Location,
                            compatibleWith: new List<string>(task.Task.CompatibleWith),
                            requires: new List<string>(task.Task.Requires),
                            startTime: task.Task.StartTime,
                            endTime: task.Task.EndTime
                        ),
                        task.ScheduledDate
                    )
                    {
                        Locked = task.Locked
                    };
                    taskMap[task] = clonedTask;
                }

                // Step 2: Deep copy all Persons
                Dictionary<string, Person> personMap = Assignment.Values
                    .Distinct()
                    .ToDictionary(
                        p => p.Name,
                        p => new Person(
                            p.Name,
                            p.WeightPerDay,
                            p.Preferences.Select(pref => new Preference(pref.Day, pref.TaskOrLocation, pref.Weight)).ToList(),
                            p.AvoidPreferences.Select(pref => new Preference(pref.Day, pref.TaskOrLocation, pref.Weight)).ToList(),
                            p.PerformableTasks, // Assume task definitions are safe to share across
                            p.MaxWeight
                        )
                    );

                // Step 3: Reconstruct Assignment with cloned ScheduledTasks and Persons
                var newAssignment = new Dictionary<ScheduledTask, Person>();
                foreach (var kvp in Assignment)
                {
                    var originalTask = kvp.Key;
                    var originalPerson = kvp.Value;

                    var clonedTask = taskMap[originalTask];
                    var clonedPerson = personMap[originalPerson.Name];

                    clonedTask.AssignedPerson = clonedPerson;
                    clonedTask.AssignedPersonName = clonedPerson.Name;

                    clonedPerson.Schedule.Add(clonedTask);
                    newAssignment[clonedTask] = clonedPerson;
                }

                // Step 4: Clone Virtual Weights
                var newWeights = VirtualWeights.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                foreach (var p in personMap.Values)
                {
                    if (newWeights.TryGetValue(p.Name, out var w))
                        p.CurrentWeight = w;
                }

                return new ScheduleState
                {
                    Assignment = newAssignment,
                    VirtualWeights = newWeights,
                    Cost = Cost
                };
            }
        }
        public ScheduleState Run(List<Person> people, List<ScheduledTask> tasks)
        {
            //ResetPeople(people);
            ScheduleState current = GenerateInitialSchedule(people, tasks);
            ScheduleState best = current.Clone();
            double temperature = 200.0;
            const double minTemp = 0.1;
            const double coolingRate = 0.98;

            while (temperature > minTemp)
            {
                for (int i = 0; i < 1000; i++)
                {
                    ScheduleState neighbor = GenerateNeighbor(current);
                    double delta = neighbor.Cost - current.Cost;
                    if (delta < 0 || rng.NextDouble() < Math.Exp(-delta / temperature))
                    {
                        current = neighbor;
                        if (current.Cost <= best.Cost)
                            best = current.Clone();
                    }
                }
                temperature *= coolingRate;
            }
            return best;
        }
        public ScheduleState RunWithAdaptiveCooling(
            List<Person> people,
            List<ScheduledTask> tasks,
            Action<double, double> progressCallback = null)
        {
            ScheduleState current = GenerateInitialSchedule(people, tasks);
            ScheduleState best = current.Clone();

            double temperature = 5000.0;
            int stop_without_improvement = 250;
            const double minTemp = 10;
            double coolingRate = 0.98;

            int iterationsPerTemp = 500;
            int parallelNeighbors = 20;
            double step = 0;
            progressCallback?.Invoke(step, best.Cost);
            int steps_since_improvement = 0;
            while (temperature > minTemp)
            {
                bool improvedThisTemp = false;
                for (int i = 0; i < iterationsPerTemp; i++)
                {
                    steps_since_improvement++;
                    step++;
                    if (steps_since_improvement > stop_without_improvement)
                    {
                        temperature = 0;
                        break;
                    }
                    var neighbors = new ConcurrentBag<ScheduleState>();

                    Parallel.For(0, parallelNeighbors, _ =>
                    {
                        neighbors.Add(GenerateNeighbor(current));
                    });

                    var bestNeighbor = neighbors.OrderBy(n => n.Cost).First();
                    double delta = bestNeighbor.Cost - current.Cost;

                    if (delta <= 0 || rng.NextDouble() < Math.Exp(-delta / temperature))
                    {
                        current = bestNeighbor;

                        if (current.Cost < best.Cost)
                        {
                            best = current.Clone();
                            improvedThisTemp = true;
                            steps_since_improvement = 0;
                        }
                    }
                    progressCallback?.Invoke(step, best.Cost);
                }

                // Adaptive cooling
                coolingRate = improvedThisTemp
                    ? Math.Min(0.995, coolingRate * 1.002)
                    : Math.Max(0.95, coolingRate * 0.995);

                temperature *= coolingRate;

                // Update UI via callback
            }

            return best;
        }

        private ScheduleState GenerateInitialSchedule(List<Person> people, List<ScheduledTask> tasks)
        {
            var assignment = new Dictionary<ScheduledTask, Person>();
            var virtualWeights = people.ToDictionary(p => p.Name, p => 0.0);
            List<Person> temp_people = new List<Person>(people);
            foreach (var task in tasks)
            {
                if (task.AssignedPerson is null)
                {
                    List<Person> eligible = optimizer.GetEligiblePeople(temp_people, task, 9.0);
                    if (eligible.Count == 0) continue;
                    //Person best = eligible.OrderBy(p => optimizer.CalculatePersonAssignmentScore(p, task)).First();
                    Person best = eligible[rng.Next(eligible.Count)];
                    best.AssignTask(task);
                    assignment[task] = best;
                    virtualWeights[best.Name] += task.Task.Weight;
                }
                else
                {
                    Person best = people.Where(p => p.Name == task.AssignedPersonName).First();
                    assignment[task] = best;
                    virtualWeights[best.Name] += task.Task.Weight;
                }
            }

            return new ScheduleState
            {
                Assignment = assignment,
                VirtualWeights = virtualWeights,
                Cost = EvaluateSchedule(assignment, virtualWeights)
            };
        }

        private ScheduleState GenerateNeighbor(ScheduleState current)
        {
            var neighbor = current.Clone();

            List<ScheduledTask> unlockedTasks = neighbor.Assignment.Keys.Where(t => !t.Locked).ToList();
            ScheduledTask taskToReassign = unlockedTasks[rng.Next(unlockedTasks.Count)];
            Person currentPerson = neighbor.Assignment[taskToReassign];
            List<Person> other_people = neighbor.Assignment.Values.Distinct().Where(p => p != currentPerson && p.IsAbleToPerformTask(taskToReassign)).ToList();
            Person newPerson = other_people[rng.Next(other_people.Count)];

            bool canPerform = newPerson.CanPerformTask(taskToReassign, 999, false);
            // If they are capable of performing the task, but they cannot take it right now because of other scheduling tasks
            // We should find out what tasks are conflicting and try to swap them around
            if (canPerform)
            {
                currentPerson.UnassignTask(taskToReassign);
                neighbor.VirtualWeights[currentPerson.Name] -= taskToReassign.Task.Weight;
                newPerson.AssignTask(taskToReassign);
                neighbor.Assignment[taskToReassign] = newPerson;
                neighbor.VirtualWeights[newPerson.Name] += taskToReassign.Task.Weight;
            }
            else if (!canPerform)
            {
                List<ScheduledTask> conflictingTasks = newPerson.Schedule
                    .Where(s => s.ScheduledDate.Date == taskToReassign.ScheduledDate.Date && !s.Locked)
                    .ToList();
                bool canSwap = true;
                foreach (ScheduledTask conflictingTask in conflictingTasks)
                {
                    if (!currentPerson.IsAbleToPerformTask(conflictingTask))
                    {
                        canSwap = false;
                        break;
                    }
                }
                if (!canSwap)
                {
                    return neighbor; // If we can't swap tasks, return the current state
                }
                // Perform the swap
                currentPerson.UnassignTask(taskToReassign);
                neighbor.VirtualWeights[currentPerson.Name] -= taskToReassign.Task.Weight;
                foreach (ScheduledTask conflictingTask in conflictingTasks)
                {
                    newPerson.UnassignTask(conflictingTask);
                    neighbor.VirtualWeights[newPerson.Name] -= conflictingTask.Task.Weight;

                    currentPerson.AssignTask(conflictingTask);
                    neighbor.Assignment[conflictingTask] = currentPerson;
                    neighbor.VirtualWeights[currentPerson.Name] += conflictingTask.Task.Weight;
                }
                newPerson.AssignTask(taskToReassign);
                neighbor.Assignment[taskToReassign] = newPerson;
                neighbor.VirtualWeights[newPerson.Name] += taskToReassign.Task.Weight;
            }
            neighbor.Cost = EvaluateSchedule(neighbor.Assignment, neighbor.VirtualWeights);
            return neighbor;
        }
        private double EvaluateSchedule(Dictionary<ScheduledTask, Person> assignment, Dictionary<string, double> virtualWeights)
        {
            var personAssignments = assignment.GroupBy(kvp => kvp.Value);
            double total = 0.0;

            foreach (var group in personAssignments)
            {
                Person person = group.Key;

                // Original Weight Error
                double weightDifference = virtualWeights[person.Name] - person.MaxWeight;
                double weightError = weightDifference > 0
                    ? weightDifference * 1.2
                    : Math.Abs(weightDifference);

                double shiftPenalty = 0.0;
                double preferencePenalty = 0.0;

                foreach (ScheduledTask scheduledTask in person.Schedule.OrderBy(t => t.ScheduledDate).ThenBy(t => t.Task.EndTime))
                {
                    // Preference penalty
                    var matchingPref = person.AvoidPreferences
                        .Where(pref => pref.Day.Date == scheduledTask.ScheduledDate.Date)
                        .Where(pref => pref.TaskOrLocation == scheduledTask.Task.Name || pref.TaskOrLocation == scheduledTask.Task.Location)
                        .Select(pref => pref.Weight);

                    preferencePenalty += matchingPref.Any() ? matchingPref.Max() : 0.0;
                }

                var orderedTasks = person.Schedule
                    .OrderBy(t => t.ScheduledDate)
                    .ThenBy(t => t.Task.EndTime)
                    .ToList();

                for (int i = 0; i < orderedTasks.Count; i++)
                {
                    var scheduledTask = orderedTasks[i];

                    if (i == 0)
                        continue; // No previous task to compare

                    var prevTask = orderedTasks[i - 1];

                    // Check if the previous task is exactly one day before the current task
                    if ((scheduledTask.ScheduledDate.Date - prevTask.ScheduledDate.Date).TotalDays == 1)
                    {
                        TimeSpan prevEnd = prevTask.Task.EndTime;
                        TimeSpan thisStart = scheduledTask.Task.StartTime;

                        // Calculate the gap between previous day's end and current day's start
                        double gapHours = (24 - prevEnd.TotalHours) + thisStart.TotalHours;
                        if (gapHours < 12)
                            shiftPenalty += 8;
                    }
                }

                // Weekly Distribution Penalty (new)
                double weeklyDistributionPenalty = 0.0;

                var weeklyGroups = person.Schedule
                    .GroupBy(t => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                        t.ScheduledDate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday));

                foreach (var weekGroup in weeklyGroups)
                {
                    double weeklyWeight = weekGroup.Sum(t => t.Task.Weight);
                    double idealWeeklyWeight = person.WeightPerDay * 5;
                    double weeklyDifference = weeklyWeight - idealWeeklyWeight;
                    if (weeklyWeight > idealWeeklyWeight)
                    {
                        weeklyDistributionPenalty += weeklyDifference * 1.2;
                    }
                    else
                    {
                        weeklyDistributionPenalty += Math.Abs(weeklyDifference);
                    }
                }

                // Add the weekly distribution penalty with a weighting factor (adjustable)
                total += weightError + shiftPenalty + preferencePenalty + (weeklyDistributionPenalty * 0.5);
            }

            // Existing uneven task distribution penalty
            var tasksByWeek = assignment.Keys
                .GroupBy(t => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                    t.ScheduledDate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday));

            double totalDistributionPenalty = 0.0;
            List<Person> people = assignment.Values.Distinct().ToList();
            foreach (var weeklyTasks in tasksByWeek)
            {
                var tasksByType = weeklyTasks.GroupBy(t => t.Task.Name);

                foreach (var taskGroup in tasksByType)
                {
                    // Identify eligible people for this task type
                    var eligiblePeople = people
                        .Where(p => p.PerformableTasks.Any(pt => pt.Name == taskGroup.Key))
                        .ToList();

                    var countsPerPerson = eligiblePeople
                        .Select(person => taskGroup.Count(t => assignment[t] == person))
                        .ToList();

                    if (countsPerPerson.Count == 0)
                        continue; // Avoid division by zero

                    double mean = countsPerPerson.Average();
                    double variance = countsPerPerson
                        .Select(count => Math.Pow(count - mean, 2))
                        .Average();

                    double unevennessPenalty = variance * 1.0; // Adjust factor as needed
                    totalDistributionPenalty += unevennessPenalty;
                }
            }

            total += totalDistributionPenalty;
            return total;
        }

        private void ResetPeople(List<Person> people)
        {
            foreach (Person person in people)
            {
                List<ScheduledTask> tasksToRemove = person.Schedule.Where(t => !t.Locked).ToList();
                foreach (ScheduledTask scheduledTask in tasksToRemove)
                {
                    person.UnassignTask(scheduledTask);
                }
            }
        }
        public void ApplySchedule(ScheduleState state, List<Person> people, List<ScheduledTask> scheduledTasks)
        {
            ResetPeople(people);
            foreach (var kvp in state.Assignment)
            {
                string personName = kvp.Key.AssignedPersonName;
                Person person = people.Where(p => p.Name == kvp.Key.AssignedPersonName).First();
                ScheduledTask actualTask = scheduledTasks.Where(p => !p.Locked && p.ScheduledDate == kvp.Key.ScheduledDate && p.Task.Name == kvp.Key.Task.Name).First();
                person.AssignTask(actualTask);
            }

        }
    }

}
