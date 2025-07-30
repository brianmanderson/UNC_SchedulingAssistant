using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private List<Person> InternalPeople;

        public class ScheduleState
        {
            public Dictionary<ScheduledTask, Person> Assignment;
            public Dictionary<string, double> VirtualWeights;
            public double Cost;

            public ScheduleState Clone()
            {
                return new ScheduleState
                {
                    Assignment = new Dictionary<ScheduledTask, Person>(Assignment),
                    VirtualWeights = new Dictionary<string, double>(VirtualWeights),
                    Cost = this.Cost
                };
            }
        }
        public ScheduleState Run(List<Person> people, List<ScheduledTask> tasks)
        {
            ScheduleState current = GenerateInitialSchedule(people, tasks);
            ScheduleState best = current.Clone();
            double temperature = 100.0;
            const double minTemp = 0.1;
            const double coolingRate = 0.95;

            while (temperature > minTemp)
            {
                for (int i = 0; i < 100; i++)
                {
                    ScheduleState neighbor = GenerateNeighbor(current, people);
                    double delta = neighbor.Cost - current.Cost;
                    if (delta < 0 || rng.NextDouble() < Math.Exp(-delta / temperature))
                    {
                        current = neighbor;
                        if (current.Cost < best.Cost)
                            best = current.Clone();
                    }
                }
                temperature *= coolingRate;
            }
            return best;
        }


        private ScheduleState GenerateInitialSchedule(List<Person> people, List<ScheduledTask> tasks)
        {
            var assignment = new Dictionary<ScheduledTask, Person>();
            var virtualWeights = people.ToDictionary(p => p.Name, p => 0.0);
            List<Person> temp_people = new List<Person>(people);
            foreach (var task in tasks.Where(t => !t.Locked))
            {
                List<Person> eligible = optimizer.GetEligiblePeople(temp_people, task, 9.0);
                if (eligible.Count == 0) continue;
                Person best = eligible.OrderBy(p => optimizer.CalculatePersonAssignmentScore(p, task)).First();
                best.AssignTask(task);
                assignment[task] = best;
                virtualWeights[best.Name] += task.Task.Weight;
            }

            return new ScheduleState
            {
                Assignment = assignment,
                VirtualWeights = virtualWeights,
                Cost = EvaluateSchedule(assignment, virtualWeights, people)
            };
        }

        private ScheduleState GenerateNeighbor(ScheduleState current, List<Person> base_people)
        {
            var neighbor = current.Clone();
            List<Person> people = new List<Person>(base_people);
            List<ScheduledTask> unlockedTasks = neighbor.Assignment.Keys.ToList();
            var taskToReassign = unlockedTasks[rng.Next(unlockedTasks.Count)];
            Person currentPerson = neighbor.Assignment[taskToReassign];
            List<Person> other_people = people.Where(p => p != currentPerson).ToList();
            Person newPerson = other_people[rng.Next(other_people.Count)];
            bool isAbleToPerform = newPerson.IsAbleToPerformTask(taskToReassign);
            if (!isAbleToPerform)
            {
                return neighbor; // If the new person can't perform the task, return the current state
            }
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
                    .Where(s => s.ScheduledDate.Date == taskToReassign.ScheduledDate.Date)
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
            neighbor.Cost = EvaluateSchedule(neighbor.Assignment, neighbor.VirtualWeights, people);
            return neighbor;
        }

        private double EvaluateSchedule(Dictionary<ScheduledTask, Person> assignment, Dictionary<string, double> virtualWeights, List<Person> people)
        {
            var personAssignments = assignment.GroupBy(kvp => kvp.Value);
            double total = 0.0;

            foreach (var group in personAssignments)
            {
                Person person = group.Key;
                double weightError = Math.Abs(virtualWeights[person.Name] - person.MaxWeight);

                double shiftPenalty = 0.0;
                double preferencePenalty = 0.0;

                foreach (var task in group)
                {
                    ScheduledTask scheduledTask = task.Key;

                    // Shift penalty
                    TimeSpan prevEnd = optimizer.GetPreviousDayEndTime(person, scheduledTask.ScheduledDate);
                    TimeSpan thisStart = scheduledTask.Task.StartTime;
                    if (prevEnd != TimeSpan.Zero && (thisStart - prevEnd).TotalHours < 12)
                        shiftPenalty += 8;

                    // Preference penalty
                    var matchingPref = person.AvoidPreferences
                        .Where(pref => pref.Day.Date == scheduledTask.ScheduledDate.Date)
                        .Where(pref => pref.TaskOrLocation == scheduledTask.Task.Name || pref.TaskOrLocation == scheduledTask.Task.Location)
                        .Select(pref => pref.Weight);

                    preferencePenalty += matchingPref.Any() ? matchingPref.Max() : 0.0;
                }

                total += weightError + shiftPenalty + preferencePenalty;
            }
            return total;
        }

        public void ApplySchedule(ScheduleState state)
        {
            foreach (var person in state.Assignment.Values.Distinct())
                person.Schedule.Clear();

            foreach (var kvp in state.Assignment)
                kvp.Value.AssignTask(kvp.Key);
        }
    }

}
