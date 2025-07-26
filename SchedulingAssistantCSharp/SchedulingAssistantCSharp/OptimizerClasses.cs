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
        public List<Person> GetEligiblePeople(ObservableCollection<Person> allPeople, ScheduledTask task)
        {
            return allPeople
                .Where(p => p.CanPerformTask(task, task.Task.Weight))
                .ToList();
        }
        private int GetNumDaysAssigned(Person person)
        {
            return person.Schedule.Select(s => s.ScheduledDate.Date).Distinct().Count();
        }
        private TimeSpan GetPreviousDayEndTime(Person person, DateTime currentDate)
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
            double maxPossibleWeight = person.WeightPerDay * GetNumDaysAssigned(person);
            double weightGap = maxPossibleWeight - person.CurrentWeight;

            // Time penalty score (lower is better)
            TimeSpan previousDayEndTime = GetPreviousDayEndTime(person, task.ScheduledDate);
            TimeSpan taskStart = task.Task.StartTime;

            double timePenalty = 0.0;
            if (previousDayEndTime != TimeSpan.Zero && previousDayEndTime > taskStart)
                timePenalty = (previousDayEndTime - taskStart).Hours;

            return weightGap + timePenalty;
        }
    }
}
