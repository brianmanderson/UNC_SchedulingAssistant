from typing import List, Optional, Dict, Tuple
import os


class Task:
    name: str
    weight: float
    location: Optional[str]
    compatible_with: List[str]
    requires: List[str]

    def __init__(self, name: str, weight: float, compatible_with: Optional[List[str]] = None, requires: Optional[List[str]] = None, location: Optional[str] = None):
        self.name = name
        self.weight = weight
        self.compatible_with = compatible_with if compatible_with else []
        self.requires = requires if requires else []
        self.location = location

    def __repr__(self):
        return (f"Task(name={self.name}, weight={self.weight}, "
                f"compatible_with={self.compatible_with}, requires={self.requires}, location={self.location})")


class Person:
    name: str
    max_weight: float
    preferences: Dict[str, List[str]]
    schedule: List[Tuple[str, Task]]
    current_weight: float

    def __init__(self, name: str, max_weight: float, preferences: Optional[Dict[str, List[str]]] = None):
        self.name = name
        self.max_weight = max_weight
        self.preferences = preferences if preferences else {}
        self.schedule = []
        self.current_weight = 0.0

    def can_perform_task(self, task: Task, day: str) -> bool:
        # Check if the person has exceeded their weight limit
        if self.current_weight + task.weight > self.max_weight:
            return False

        # Check location compatibility with tasks already scheduled on the same day
        day_schedule = [t for d, t in self.schedule if d == day]
        for scheduled_task in day_schedule:
            # Location check
            if scheduled_task.location is not None and task.location is not None and scheduled_task.location != task.location:
                return False
            # Compatibility check
            if task.name not in scheduled_task.compatible_with and scheduled_task.name not in task.compatible_with:
                return False
        return True

    def assign_task(self, task: Task, day: str):
        self.schedule.append((day, task))
        self.current_weight += task.weight

    def __repr__(self):
        return f"Person(name={self.name}, max_weight={self.max_weight}, current_weight={self.current_weight})"


class Day:
    name: str
    tasks: List[Task]

    def __init__(self, name: str, tasks: List[Task]):
        self.name = name
        self.tasks = tasks

    def __repr__(self):
        return f"Day(name={self.name}, tasks={self.tasks})"


class Scheduler:
    people: List[Person]
    days: List[Day]

    def __init__(self):
        self.people = []
        self.days = []

    def add_person(self, person: Person):
        self.people.append(person)

    def add_day(self, day: Day):
        self.days.append(day)

    def assign_task(self, person: Person, task: Task, day: Day) -> Tuple[Person, Task]:
        person.assign_task(task, day.name)
        return person, task

    def fulfill_requests(self):
        """Attempt to fulfill the requests of each person before general scheduling."""
        for person in self.people:
            for day_name, preferred_tasks in person.preferences.items():
                day = next((d for d in self.days if d.name == day_name), None)
                if day:
                    for task_name in preferred_tasks:
                        task = next((t for t in day.tasks if t.name == task_name), None)
                        if task and person.can_perform_task(task, day.name):
                            self.assign_task(person, task, day)
                            day.tasks.remove(task)

    def create_schedule(self) -> Dict[str, List[Tuple[Person, Task]]]:
        schedule = {}

        # First pass: Fulfill individual requests
        self.fulfill_requests()

        # Second pass: Schedule remaining tasks
        for day in self.days:
            daily_schedule = []
            assigned_tasks = []

            for task in day.tasks:
                people = sorted(self.people, key=lambda p: p.current_weight)
                candidates = [p for p in people if p.can_perform_task(task, day.name)]

                # If no suitable candidates, pick the one with the lowest current weight
                if not candidates:
                    candidates = sorted(self.people, key=lambda p: p.current_weight)

                selected_person = candidates[0]
                daily_schedule.append(self.assign_task(selected_person, task, day))
                assigned_tasks.append(task.name)

                # Handle required tasks
                for required_task_name in task.requires:
                    required_task = next((t for t in day.tasks if t.name == required_task_name), None)
                    if required_task:
                        daily_schedule.append(self.assign_task(selected_person, required_task, day))
                        assigned_tasks.append(required_task_name)
                        day.tasks.remove(required_task)

                # Handle compatible tasks
                for compatible_task_name in task.compatible_with:
                    compatible_task = next((t for t in day.tasks if t.name == compatible_task_name), None)
                    if compatible_task:
                        daily_schedule.append(self.assign_task(selected_person, compatible_task, day))
                        assigned_tasks.append(compatible_task_name)
                        day.tasks.remove(compatible_task)

            schedule[day.name] = daily_schedule

            # Assign Dev tasks to those with remaining weight capacity
            for person in self.people:
                if person.current_weight < person.max_weight:
                    if day.name not in [d for d, _ in person.schedule]:
                        daily_schedule.append(self.assign_task(person, Task("Dev", 0.0), day))

        return schedule


def main():
    # Define tasks
    pod = Task("POD", 4.5)
    sad = Task("SAD", 3.0)
    sad_assist = Task("SAD_Assist", 2.0)
    hbo = Task("HBO", 2.0, compatible_with=["SAD_Assist", "SAD"])
    pod_backup = Task("POD_Backup", 2.0, requires=["SAD_Assist"])
    hdr_amp = Task("HDR_AMP", 1.0, compatible_with=["POD", "POD_Backup"])
    dev = Task("Dev", 0.0)

    # Define people with max_weight
    alice = Person("Alice", max_weight=12, preferences={"Monday": ["POD"]})
    bob = Person("Bob", max_weight=18, preferences={"Tuesday": ["Dev"], 'Wednesday': ["POD"]})
    david = Person("David", max_weight=12, preferences={"Tuesday": ["SAD"], 'Wednesday': ["POD"]})
    brian = Person("Brian", max_weight=12, preferences={"Monday": ["Dev"], 'Friday': ["POD"]})
    charlie = Person("Charlie", max_weight=18)
    dance = Person("Dance", max_weight=18)
    adria = Person("Adria", max_weight=18)
    jun = Person("Jun", max_weight=12)

    # Define days with specific tasks
    monday = Day("Monday", [pod, pod, pod_backup, sad, sad_assist, sad_assist])
    tuesday = Day("Tuesday", [pod, pod, pod_backup, sad_assist, hdr_amp, sad, sad])
    wednesday = Day("Wednesday", [pod, pod, pod_backup, sad_assist, hbo, sad, sad])
    thursday = Day("Thursday", [pod, pod, pod_backup, sad_assist, sad, sad])
    friday = Day("Friday", [pod, pod, pod_backup, sad_assist, sad, hbo, sad])

    # Initialize scheduler
    scheduler = Scheduler()

    # Add people and days to scheduler
    scheduler.add_person(alice)
    scheduler.add_person(bob)
    scheduler.add_person(charlie)
    scheduler.add_person(david)
    scheduler.add_person(brian)
    scheduler.add_person(dance)
    scheduler.add_person(adria)
    scheduler.add_person(jun)

    scheduler.add_day(monday)
    scheduler.add_day(tuesday)
    scheduler.add_day(wednesday)
    scheduler.add_day(thursday)
    scheduler.add_day(friday)

    # Create and print the schedule
    schedule = scheduler.create_schedule()

    for day, assignments in schedule.items():
        print(f"{day}: {assignments}")


if __name__ == '__main__':
    main()
