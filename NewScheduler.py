import copy
import random
from typing import List, Optional, Dict, Tuple
from datetime import datetime


class DateTimeClass(object):
    year: int
    month: int
    day: int

    def __init__(self, year: int, month: int, day: int):
        self.year = year
        self.day = day
        self.month = month

    def __eq__(self, other):
        if isinstance(other, DateTimeClass):
            if self.__dict__ == other.__dict__:
                return True
        return False

    def __sub__(self, other):
        assert isinstance(other, DateTimeClass)
        k = datetime(self.year, self.month, self.day)
        k2 = datetime(other.year, other.month, other.day)
        return k - k2

    def from_rs_datetime(self, k):
        self.year = k.Year
        self.month = k.Month
        self.day = k.Day

    def from_python_datetime(self, k: datetime):
        self.year = k.year
        self.month = k.month
        self.day = k.day

    def from_pandas_timestamp(self, k):
        self.from_python_datetime(k)

    def from_string(self, k):
        year, month, day, hour, minute = k.split('.')
        self.year = int(year)
        self.month = int(month)
        self.day = int(day)

    def to_string(self):
        return f"{self.month}/{self.day}/{self.year}"

    def to_days(self):
        return (self.year * 365) + (self.month * 31) + self.day


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


class Preference:
    day: str
    task_or_location: str
    weight: float

    def __init__(self, day: str, task_or_location: str, weight: float):
        self.day = day
        self.task_or_location = task_or_location
        self.weight = weight


class Person:
    name: str
    weight_per_day: float
    max_weight: float
    preferences: List[Preference]
    avoid_preferences: List[Preference]
    schedule: List[Tuple[str, Task]]
    current_weight: float

    def __init__(self, name: str, weight_per_day: float, preferences: Optional[List[Preference]] = None,
                 avoid_preferences: Optional[List[Preference]] = None):
        self.name = name
        self.weight_per_day = weight_per_day
        self.preferences = preferences if preferences else []
        self.avoid_preferences = avoid_preferences if avoid_preferences else []
        self.schedule = []
        self.current_weight = 0.0
        self.max_weight = 0.0

    def add_day(self):
        self.max_weight += self.weight_per_day

    def can_perform_task(self, task: Task, day: str) -> bool:
        # Check if this is in their avoidance preferences
        for avoid_pref in self.avoid_preferences:
            if avoid_pref.day == day and (task.name == avoid_pref.task_or_location or task.location == avoid_pref.task_or_location):
                return False

        # Check location compatibility with tasks already scheduled on the same day
        day_schedule = [t for d, t in self.schedule if d == day]
        for scheduled_task in day_schedule:
            # Location check
            if (scheduled_task.location is not None and task.location is not None
                    and scheduled_task.location != task.location):
                return False
            # Compatibility check
            if task.name not in scheduled_task.compatible_with and scheduled_task.name not in task.compatible_with:
                return False
            # Redundant task check
            if task.name == scheduled_task.name:
                return False
        return True

    def assign_task(self, task: Task, day: str):
        self.schedule.append((day, task))
        self.current_weight += task.weight

    def __repr__(self):
        return f"Person(name={self.name}, weight_per_day={self.weight_per_day}, current_weight={self.current_weight})"


class Day:
    name: str
    date: DateTimeClass
    tasks: List[Task]

    def __init__(self, name: str, tasks: List[Task], date: DateTimeClass):
        self.name = name
        # tasks = sorted(tasks, key=lambda p: p.weight, reverse=True)
        self.tasks = tasks
        self.date = date

    def __repr__(self):
        return f"Day(name={self.name}, tasks={self.tasks}, date={self.date})"

    def to_string(self):
        return self.name + '_' + self.date.to_string()


class Week:
    name: str
    days: List[Day]

    def __init__(self, name: str, days: List[Day]):
        self.name = name
        self.days = days

    def __repr__(self):
        return f"Week(name={self.name}, days={self.days})"


def sort_schedule_by_person_name(schedule: Dict[str, List[Tuple[Person, Task]]]) -> Dict[
    str, List[Tuple[Person, Task]]]:
    # Iterate over each key in the dictionary
    for key in schedule:
        # Sort the list of (Person, Task) tuples by Person.name
        schedule[key] = sorted(schedule[key], key=lambda item: item[0].name)

    return schedule


class Scheduler:
    people: List[Person]
    schedule: Dict[str, List[Tuple[Person, Task]]]
    days: List[Day]
    assigned_tasks: List[str]

    def __init__(self):
        self.people = []
        self.days = []
        self.schedule = {}
        self.assigned_tasks = []

    def add_person(self, person: Person):
        self.people.append(person)

    def add_day(self, day: Day):
        self.days.append(day)
        for p in self.people:
            p.add_day()

    def reset(self):
        self.days = []
        self.schedule = {}

    def assign_task(self, person: Person, task: Task, day: Day) -> Tuple[Person, Task]:
        person.assign_task(task, day.to_string())
        return person, task

    def fulfill_requests(self, minimum_weight=0.0):
        """Attempt to fulfill the requests of each person before general scheduling."""
        people_preferences = [(person, preference) for person in self.people for preference in person.preferences
                              if preference.weight >= minimum_weight]
        # Sort the list based on preference.weight
        people_preferences.sort(key=lambda x: x[1].weight, reverse=True)
        for person, preference in people_preferences:
            # Find the corresponding day in the scheduler
            day = next((d for d in self.days if d.to_string() == preference.day), None)
            if day:
                if day.to_string() not in self.schedule:
                    self.schedule[day.to_string()] = []
                daily_schedule = self.schedule[day.to_string()]

                # Find the task or location in the day's tasks
                task = next((t for t in day.tasks if
                             t.name == preference.task_or_location or t.location == preference.task_or_location),
                            None)

                if task and person.can_perform_task(task, day.to_string()):
                    daily_schedule.append(self.assign_task(person, task, day))
                    day.tasks.remove(task)

        # Handle avoid preferences: Attempt to assign the next available task
        people_avoid_preferences = [(person, avoid_preference) for person in self.people for avoid_preference in
                                    person.avoid_preferences if avoid_preference.weight >= minimum_weight]
        for person, avoid_pref in people_avoid_preferences:
            # Skip days that are not in avoid preferences
            day = next((d for d in self.days if d.to_string() == avoid_pref.day), None)
            if day:
                if day.to_string() not in self.schedule:
                    self.schedule[day.to_string()] = []
                daily_schedule = self.schedule[day.to_string()]

                # Try to find the next available task that meets the criteria
                for task in day.tasks:
                    if task.name != avoid_pref.task_or_location and task.location != avoid_pref.task_or_location:
                        if person.can_perform_task(task, day.to_string()):
                            daily_schedule.append(self.assign_task(person, task, day))
                            day.tasks.remove(task)
                            for required_task_name in task.requires:
                                required_task = next((t for t in day.tasks if t.name == required_task_name), None)
                                if required_task and person.can_perform_task(task, day.to_string()):
                                    daily_schedule.append(self.assign_task(person, required_task, day))
                                    self.assigned_tasks.append(required_task_name)
                                    day.tasks.remove(required_task)
                            break  # Exit after assigning one task to avoid over-assigning

    def create_schedule(self) -> Dict[str, List[Tuple[Person, Task]]]:

        # First pass: Fulfill individual requests
        starting_people = copy.deepcopy(self.people)
        starting_days = copy.deepcopy(self.days)
        starting_tasks = []
        for d in starting_days:
            starting_tasks += d.tasks
        request_weight = -1.0
        while request_weight < 8.0:
            request_weight += 1.0
            self.assigned_tasks = []
            self.schedule = {}
            self.people = copy.deepcopy(starting_people)
            self.days = copy.deepcopy(starting_days)
            self.fulfill_requests(minimum_weight=request_weight)

            # Second pass: Schedule remaining tasks
            # Do a random shuffle, otherwise it'll heavy schedule on Monday first
            random.Random(None).shuffle(self.days)
            for day in self.days:
                if day.to_string() not in self.schedule:
                    self.schedule[day.to_string()] = []
                daily_schedule = self.schedule[day.to_string()]
                while day.tasks:
                    task = day.tasks.pop(0)
                    people = sorted(self.people, key=lambda p: p.max_weight - p.current_weight, reverse=True)
                    candidates = [p for p in people if p.can_perform_task(task, day.to_string())]

                    # If no suitable candidates, pick the one with the lowest current weight
                    if not candidates:
                        candidates = sorted(self.people, key=lambda p: p.max_weight - p.current_weight, reverse=True)

                    selected_person = candidates[0]
                    daily_schedule.append(self.assign_task(selected_person, task, day))
                    self.assigned_tasks.append(task.name)

                    # Handle required tasks
                    for required_task_name in task.requires:
                        required_task = next((t for t in day.tasks if t.name == required_task_name), None)
                        if required_task and selected_person.can_perform_task(task, day.to_string()):
                            daily_schedule.append(self.assign_task(selected_person, required_task, day))
                            self.assigned_tasks.append(required_task_name)
                            day.tasks.remove(required_task)

                # Assign Dev tasks to those with remaining weight capacity
                for person in self.people:
                    if day.to_string() not in [d for d, _ in person.schedule]:
                        daily_schedule.append(self.assign_task(person, Task("Dev", 0.0), day))
                    else:
                        weight = 0
                        for d, task in person.schedule:
                            if d == day.to_string():
                                weight += task.weight
                        if weight <= 3.0 and person.can_perform_task(Task("HalfDev", 0.0), day.to_string()):
                            daily_schedule.append(self.assign_task(person, Task("HalfDev", 0.0), day))
            remaining_tasks = []
            for day in self.days:
                remaining_tasks += [day.to_string() + ':' + i.name for i in day.tasks]
            if len(remaining_tasks) == 0:
                break
            else:
                print(f"Could not assign all tasks, {len(remaining_tasks)} remain")
                for remaining_task in remaining_tasks:
                    print(remaining_task)
        # Sort the schedule based on day.date.to_days()
        sorted_schedule = dict(
            sorted(self.schedule.items(), key=lambda item: next(
                (d.date.to_days() for d in self.days if
                 item[0] == d.to_string()), float('inf'))))
        sorted_schedule = sort_schedule_by_person_name(sorted_schedule)
        self.schedule = sorted_schedule
        return self.schedule


def main():
    # Define tasks
    vacation = Task("Vacation", weight=0.0, location="Vacation")
    pod = Task("POD", 4.5, location='UNC', requires=["HDR_AMP", "IORTTx"])
    sad = Task("SAD", 3.0, location=None)
    sad_assist = Task("SAD_Assist", 2.0, location=None)
    gamma_tile = Task("Gamma_Tile", 3.0, location="UNC")
    prostate_brachy = Task("Prostate_Brachy", 3.0, location='UNC', compatible_with=['SAD', 'SAD_Assist'])
    hbo = Task("HBO", 2.0, compatible_with=["SAD_Assist", "SAD"], location='HBO')
    pod_backup = Task("POD_Backup", 2.0, requires=["SAD_Assist", "HDR_AMP", "IORTTx", "Prostate_Brachy"], location='UNC')
    hdr_amp = Task("HDR_AMP", 1.0, compatible_with=["POD", "POD_Backup"], location='UNC')
    iort_tx = Task("IORTTx", 2.0, compatible_with=["POD", "POD_Backup"], location='UNC')
    dev = Task("Dev", 0.0, location="Away")

    # Define people with max_weight
    people = []

    # Leith's preferences
    leith = Person(
        "Leith",
        weight_per_day=12/5,
        preferences=[Preference("Monday_8/26/2024", "POD", weight=7.0)]
    )
    people.append(leith)

    # Taki's preferences
    taki = Person(
        "Taki",
        weight_per_day=12/5,
        preferences=[
            Preference("Monday_8/26/2024", "Dev", weight=7.0),
            Preference("Tuesday_8/27/2024", "Dev", weight=7.0)
        ]
    )
    people.append(taki)

    # Dance's preferences
    dance = Person(
        "Dance",
        weight_per_day=18/5,
        preferences=[
            Preference("Thursday_8/29/2024", "Gamma_Tile", weight=9.0),
            Preference("Friday_8/30/2024", "Gamma_Tile", weight=9.0)
        ]
    )
    people.append(dance)

    # Adria's preferences
    adria = Person(
        "Adria",
        weight_per_day=18/5,
        preferences=[Preference("Monday_8/26/2024", "Vacation", weight=9.0)]
    )
    people.append(adria)

    # Cielle's preferences
    cielle = Person(
        "Cielle",
        weight_per_day=18/5,
        preferences=[Preference("Monday_8/26/2024", "Prostate_Brachy", weight=9.0)]
    )
    people.append(cielle)

    # Brian's preferences
    brian = Person(
        "Brian",
        weight_per_day=12/5,
        preferences=[
            Preference("Monday_8/26/2024", "POD_Backup", weight=3.0),
            Preference("Friday_8/30/2024", "SAD", weight=7.0)
        ]
    )
    people.append(brian)

    # David's avoid preferences
    david = Person(
        "David",
        weight_per_day=12/5,
        avoid_preferences=[
            Preference("Monday_8/26/2024", "HBO", weight=9.0),
            Preference("Monday_8/26/2024", "UNC", weight=9.0),
            Preference("Tuesday_8/27/2024", "HBO", weight=9.0),
            Preference("Tuesday_8/27/2024", "UNC", weight=9.0),
            Preference("Wednesday_8/28/2024", "HBO", weight=1.0),
            Preference("Thursday_8/29/2024", "HBO", weight=1.0),
            Preference("Friday_8/30/2024", "HBO", weight=1.0),
            Preference("Friday_8/30/2024", "UNC", weight=1.0)
        ]
    )
    people.append(david)

    # Jun's preferences
    jun = Person(
        "Jun",
        weight_per_day=12/5,
        preferences=[
            Preference("Monday_8/26/2024", "Prostate_Brachy", weight=9.0),
            Preference("Friday_8/30/2024", "Vacation", weight=9.0)
        ]
    )
    people.append(jun)

    ross = Person(
        "Ross",
        weight_per_day=18/5,
        preferences=[
            Preference("Friday_8/30/2024", "Vacation", weight=9.0)
        ]
    )
    people.append(ross)

    # Define days with specific tasks
    every_day_tasks = [pod, pod, hbo, pod_backup, sad_assist, sad]
    monday = Day("Monday", every_day_tasks + [prostate_brachy, prostate_brachy, sad_assist, sad_assist],
                 DateTimeClass(year=2024, month=8, day=26))
    tuesday = Day("Tuesday", every_day_tasks + [hdr_amp, hdr_amp, hdr_amp, hdr_amp, sad_assist],
                  DateTimeClass(year=2024, month=8, day=27))
    wednesday = Day("Wednesday", every_day_tasks + [sad],
                    DateTimeClass(year=2024, month=8, day=28))
    thursday = Day("Thursday", every_day_tasks + [hdr_amp, hdr_amp, hdr_amp, sad, gamma_tile],
                   DateTimeClass(year=2024, month=8, day=29))
    friday = Day("Friday", every_day_tasks + [iort_tx, hdr_amp, sad_assist, iort_tx, hdr_amp, gamma_tile],
                 DateTimeClass(year=2024, month=8, day=30))

    # Define weeks (same as before)
    week1 = Week("8/26/2024", [monday, tuesday, wednesday, thursday, friday])

    monday = Day("Monday", every_day_tasks + [sad, sad, hdr_amp, hdr_amp, hdr_amp],
                 DateTimeClass(year=2024, month=9, day=2))
    tuesday = Day("Tuesday", every_day_tasks + [sad, sad, hdr_amp, hdr_amp, hdr_amp],
                  DateTimeClass(year=2024, month=9, day=3))
    wednesday = Day("Wednesday", every_day_tasks + [sad, sad],
                    DateTimeClass(year=2024, month=9, day=4))
    thursday = Day("Thursday", every_day_tasks + [hdr_amp, sad, hdr_amp, hdr_amp],
                   DateTimeClass(year=2024, month=9, day=5))
    friday = Day("Friday", every_day_tasks + [sad, sad, iort_tx, iort_tx, hdr_amp, hdr_amp],
                 DateTimeClass(year=2024, month=9, day=6))
    week2 = Week("9/2/204", [monday, tuesday, wednesday, thursday, friday])

    # Initialize scheduler
    scheduler = Scheduler()

    # Add people and weeks to scheduler
    for p in people:
        scheduler.add_person(p)

    for day in week1.days:
        scheduler.add_day(day)
    schedule = scheduler.create_schedule()
    for day, assignments in schedule.items():

        print(f"---------{day}----------")
        for person, task in assignments:
            print(f"{person.name}: {task.name}")
    # for day in week2.days:
    #     scheduler.add_day(day)

    # Create and print the schedule
    schedule = scheduler.create_schedule()
    for day, assignments in schedule.items():

        print(f"---------{day}----------")
        for person, task in assignments:
            print(f"{person.name}: {task.name}")


if __name__ == '__main__':
    main()
