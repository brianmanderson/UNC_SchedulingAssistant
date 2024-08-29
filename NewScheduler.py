import os
from typing import *


class Task:
    def __init__(self, name: str, weight: float, compatible_with=None, requires=None):
        self.name = name
        self.weight = weight
        self.compatible_with = compatible_with if compatible_with else []
        self.requires = requires if requires else []

    def __repr__(self):
        return (f"Task(name={self.name}, weight={self.weight}, "
                f"compatible_with={self.compatible_with}, requires={self.requires})")


class Person:
    def __init__(self, name: str, max_weight: float, preferences: dict = None):
        self.name = name
        self.max_weight = max_weight
        self.preferences = preferences if preferences else {}
        self.schedule = []
        self.current_weight = 0.0

    def can_perform_task(self, task: Task, day: str):
        if day in self.preferences and task.name in self.preferences[day]:
            return False
        if self.current_weight + task.weight > self.max_weight:
            return False
        return True

    def assign_task(self, task: Task, day: str):
        self.schedule.append((day, task))
        self.current_weight += task.weight

    def __repr__(self):
        return f"Person(name={self.name}, max_weight={self.max_weight}, current_weight={self.current_weight})"


class Day:
    Tasks: List[Task]

    def __init__(self, name: str, tasks: list):
        self.name = name
        self.Tasks = tasks

    def __repr__(self):
        return f"Day(name={self.name}, tasks={self.Tasks})"


class Scheduler:
    def __init__(self):
        self.people = []
        self.days = []

    def add_person(self, person: Person):
        self.people.append(person)

    def add_day(self, day: Day):
        self.days.append(day)

    def assign_task(self, person: Person, task: Task, day: Day):
        person.assign_task(task, day.name)
        return (person, task)

    def create_schedule(self):
        schedule = {}

        for day in self.days:
            daily_schedule = []
            assigned_tasks = []

            for task in day.Tasks:
                candidates = [p for p in self.people if p.can_perform_task(task, day.name)]

                # If no suitable candidates, pick the one with the lowest current weight
                if not candidates:
                    candidates = sorted(self.people, key=lambda p: p.current_weight)

                selected_person = candidates[0]
                daily_schedule.append(self.assign_task(selected_person, task, day))
                assigned_tasks.append(task.name)

                for required_task_name in task.requires:
                    for t in day.Tasks:
                        if t.name == required_task_name:
                            daily_schedule.append(self.assign_task(selected_person, t, day))
                            assigned_tasks.append(required_task_name)
                            day.Tasks.remove(t)

                for compatible_task_name in task.compatible_with:
                    for t in day.Tasks:
                        if t.name == compatible_task_name:
                            daily_schedule.append(self.assign_task(selected_person, t, day))
                            assigned_tasks.append(compatible_task_name)
                            day.Tasks.remove(t)

            schedule[day.name] = daily_schedule

            # Assign Dev tasks to those with remaining weight capacity
            for person in self.people:
                if person.current_weight < person.max_weight:
                    if day.name not in [d for d, _ in person.schedule]:
                        daily_schedule.append(self.assign_task(person, Task("Dev", 0.0), day))

        return schedule


# Define tasks
pod = Task("POD", 4.5)
sad = Task("SAD", 3.0)
sad_assist = Task("SAD_Assist", 2.0)
hbo = Task("HBO", 2.0, compatible_with=["SAD_Assist"])
pod_backup = Task("POD_Backup", 2.0, requires=["SAD_Assist"])
hdr_amp = Task("HDR_AMP", 1.0, compatible_with=["POD", "POD_Backup"])
dev = Task("Dev", 0.0)

# Define people with max_weight
alice = Person("Alice", max_weight=12, preferences={"Monday": ["POD"]})
bob = Person("Bob", max_weight=18, preferences={"Tuesday": ["SAD", "Dev"]})
charlie = Person("Charlie", max_weight=18)

# Define days with specific tasks
monday = Day("Monday", [pod, pod_backup, sad, sad_assist, sad_assist])
tuesday = Day("Tuesday", [pod, pod_backup, sad_assist, hdr_amp])
wednesday = Day("Wednesday", [pod, pod_backup, sad_assist, hbo, sad])
thursday = Day("Thursday", [pod, pod_backup, sad_assist, sad])
friday = Day("Friday", [pod, pod_backup, sad_assist, sad, hbo])

# Initialize scheduler
scheduler = Scheduler()

# Add people and days to scheduler
scheduler.add_person(alice)
scheduler.add_person(bob)
scheduler.add_person(charlie)

scheduler.add_day(monday)
scheduler.add_day(tuesday)
scheduler.add_day(wednesday)
scheduler.add_day(thursday)
scheduler.add_day(friday)

# Create and print the schedule
schedule = scheduler.create_schedule()

for day, assignments in schedule.items():
    print(f"{day}: {assignments}")
