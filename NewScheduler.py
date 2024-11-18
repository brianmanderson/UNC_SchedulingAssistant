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

    def __repr__(self):
        return f"{self.month}/{self.day}/{self.year}"


class AbstractTask:
    name: str
    weight: float
    location: Optional[str]
    compatible_with: List[str]
    requires: List[str]
    people_can_perform: List[str]

    def __init__(self, name: str, weight: float, compatible_with: Optional[List[str]] = None,
                 requires: Optional[List[str]] = None, location: Optional[str] = None):
        self.name = name
        self.weight = weight
        self.compatible_with = compatible_with if compatible_with else []
        self.requires = requires if requires else []
        self.location = location
        self.people_can_perform = []

    def __repr__(self):
        return (f"Task(name={self.name}, weight={self.weight}, "
                f"compatible_with={self.compatible_with}, requires={self.requires}, location={self.location})")


class Task(AbstractTask):
    date: DateTimeClass

    def __init__(self, abstract_task: AbstractTask, date: DateTimeClass):
        super().__init__(
            name=abstract_task.name,
            weight=abstract_task.weight,
            compatible_with=abstract_task.compatible_with,
            requires=abstract_task.requires,
            location=abstract_task.location
        )
        self.date = date


class Preference:
    day: DateTimeClass
    task_or_location: str
    weight: float

    def __init__(self, day: DateTimeClass, task_or_location: str, weight: float):
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
    performable_tasks: List[AbstractTask]

    def __init__(self, name: str, weight_per_day: float, preferences: Optional[List[Preference]] = None,
                 avoid_preferences: Optional[List[Preference]] = None, performable_tasks: Optional[List[AbstractTask]] = None):
        self.name = name
        self.weight_per_day = weight_per_day
        self.preferences = preferences if preferences else []
        self.avoid_preferences = avoid_preferences if avoid_preferences else []
        self.schedule = []
        self.current_weight = 0.0
        self.max_weight = 0.0
        # Set default performable tasks
        if performable_tasks is None:
            performable_tasks = []
        self.performable_tasks = performable_tasks
        self.add_dev_vacation()

    def add_dev_vacation(self):
        for t in [AbstractTask('Dev', 0.0, location="Away"), AbstractTask('HalfDev', 0.0),
                  AbstractTask('Vacation', 0.0, location='Vacation')]:
            if t not in self.performable_tasks:
                self.performable_tasks.append(t)

    def add_day(self):
        self.max_weight += self.weight_per_day

    def can_perform_task(self, task: Task, weight: float) -> bool:
        # Check if this is in their avoidance preferences
        for avoid_pref in self.avoid_preferences:
            if (avoid_pref.day == task.date and
                    (task.name == avoid_pref.task_or_location or
                     task.location == avoid_pref.task_or_location) and
                    avoid_pref.weight > weight):
                return False

        # Check if the person can perform the task
        if task.name not in [i.name for i in self.performable_tasks]:
            return False

        # Check location compatibility with tasks already scheduled on the same day
        day_schedule = [t for d, t in self.schedule if d == task.date]
        if self.name == 'Dance' and len(self.schedule) != 0:
            x = 1
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

    def assign_task(self, task: Task):
        self.schedule.append((task.date.to_string(), task))
        self.current_weight += task.weight

    def __repr__(self):
        return f"Person(name={self.name}, max_weight={self.max_weight}, current_weight={self.current_weight})"


class Physicist(Person):
    def __init__(self, name: str, weight_per_day: float, preferences: Optional[List[Preference]] = None,
                 avoid_preferences: Optional[List[Preference]] = None,
                 performable_tasks: Optional[List[AbstractTask]] = None):
        # Define the performable tasks for a Physicist
        if performable_tasks is None:
            performable_tasks = []
        performable_tasks += [
            AbstractTask('POD', weight=0.0, location='UNC'),
            AbstractTask('POD_Backup', weight=0.0, location='UNC'),
            AbstractTask('SAD', weight=0.0, location=None),
            AbstractTask('SAD_Assist', weight=0.0, location=None),
            AbstractTask('HBO', 0.0, location='HBO'),
            AbstractTask("HDR_AMP", 0.0, location='UNC'),
            AbstractTask('IORTTx', 0.0, location='UNC')
        ]
        super().__init__(name, weight_per_day, preferences, avoid_preferences, performable_tasks)


class Day:
    name: str
    date: DateTimeClass
    tasks: List[Task]

    def __init__(self, name: str, tasks: List[Task], date: DateTimeClass):
        self.name = name
        tasks = sorted(tasks, key=lambda p: p.weight, reverse=True)
        self.tasks = tasks
        self.date = date

    def __repr__(self):
        return f"Day(name={self.name}, tasks={self.tasks}, date={self.date})"

    def to_string(self):
        return self.date.to_string()


class Week:
    name: str
    days: List[Day]

    def __init__(self, name: str, days: List[Day]):
        self.name = name
        self.days = days

    def __repr__(self):
        return f"Week(name={self.name}, days={self.days})"


def sort_schedule_by_person_name(schedule: Dict[str, List[Tuple[Person, Task]]]) \
        -> Dict[str, List[Tuple[Person, Task]]]:
    # Iterate over each key in the dictionary
    for key in schedule:
        # Sort the list of (Person, Task) tuples by Person.name
        schedule[key] = sorted(schedule[key], key=lambda item: item[0].name)

    return schedule


def get_tasks_sorted_by_performability(days: List[Day], people: List[Person]) -> List[Task]:
    # Create a list to keep track of tasks and the number of people who can perform each task
    task_performability = []

    # Iterate over each day and each task within the day
    for day in days:
        for task in day.tasks:
            # Count the number of people who can perform this task
            count = 0
            for person in people:
                if person.can_perform_task(task, task.weight):
                    count += 1
            # Add the task and its count to the list
            task_performability.append((task, count))

    # Sort tasks by the number of people who can perform them in ascending order
    task_performability.sort(key=lambda x: x[1])

    # Extract sorted tasks from the list of tuples
    sorted_tasks = [task for task, _ in task_performability]

    return sorted_tasks


def get_day_by_string(days: List[Day], day_str: str) -> Optional[Day]:
    for day in days:
        if day.to_string() == day_str:
            return day
    return None


class Scheduler:
    people: List[Person]
    schedule: Dict[str, List[Tuple[Person, Task]]]
    days: List[Day]
    assigned_tasks: List[str]
    tasks_by_possibility: List[Task]

    def __init__(self):
        self.people = []
        self.days = []
        self.schedule = {}
        self.tasks_by_possibility = []

    def add_person(self, person: Person):
        self.people.append(person)

    def add_day(self, day: Day):
        self.days.append(day)

    def reset(self):
        self.days = []
        self.schedule = {}

    def assign_task(self, person: Person, task: Task) -> Tuple[Person, Task]:
        person.assign_task(task)
        return person, task

    def get_day_by_string(self, day_str: str) -> Optional[Day]:
        for day in self.days:
            if day.to_string() == day_str:
                return day
        return None

    def assign_required_tasks(self, person: Person, task: Task, day: Day):
        for required_task_name in task.requires:
            required_task = next((t for t in day.tasks if t.name == required_task_name), None)
            if required_task and person.can_perform_task(required_task, task.weight):
                self.schedule[day.to_string()].append(self.assign_task(person, required_task))
                day.tasks.remove(required_task)
                self.tasks_by_possibility.remove(required_task)

    def handle_preference_assignment(self, person: Person, preference: Preference, day: Day, is_preference: bool):
        if is_preference:
            tasks = [t for t in day.tasks if t.name == preference.task_or_location or t.location == preference.task_or_location]
        else:
            tasks = [t for t in day.tasks if t.name != preference.task_or_location and t.location != preference.task_or_location]
        tasks = [t for t in tasks if t.weight > 1.0 and person.can_perform_task(t, preference.weight)]

        if not tasks:
            return

        # Sort tasks based on remaining capacity difference or lower weight if max weight is reached
        if person.current_weight >= person.max_weight:
            tasks = [Task(AbstractTask("Dev", 0.0, location="Away"), day.date)] if person.can_perform_task(Task(AbstractTask("Dev", 0.0, location="Away"), day.date), preference.weight) else []
        else:
            tasks.sort(key=lambda t: abs(person.max_weight - person.current_weight - t.weight))

        if tasks:
            task = tasks[0]
            self.schedule[day.to_string()].append(self.assign_task(person, task))
            if task in day.tasks:
                day.tasks.remove(task)
            if task in self.tasks_by_possibility:
                self.tasks_by_possibility.remove(task)
            self.assign_required_tasks(person, task, day)

    def fulfill_requests(self, minimum_weight=0.0):
        """Attempt to fulfill the requests of each person before general scheduling."""
        # Combine preferences and avoid_preferences into a single list
        all_preferences = [(person, pref, True) for person in self.people for pref in person.preferences if
                           pref.weight >= minimum_weight]
        all_preferences += [(person, avoid_pref, False) for person in self.people for avoid_pref in
                            person.avoid_preferences if avoid_pref.weight >= minimum_weight]

        # Sort the combined list based on preference weight
        all_preferences.sort(key=lambda x: x[1].weight, reverse=True)

        for person, preference, is_preference in all_preferences:
            # Find the corresponding day in the scheduler
            day = self.get_day_by_string(preference.day.to_string())
            if day:
                if day.to_string() not in self.schedule:
                    self.schedule[day.to_string()] = []
                self.handle_preference_assignment(person, preference, day, is_preference)

    def create_schedule(self) -> Dict[str, List[Tuple[Person, Task]]]:
        for person in self.people:
            for _ in self.days:
                person.add_day()

        starting_people = copy.deepcopy(self.people)
        starting_days = copy.deepcopy(self.days)

        request_weight = -1.0
        while request_weight < 8.0:
            request_weight += 1.0
            self.schedule = {}
            self.people = copy.deepcopy(starting_people)
            self.days = copy.deepcopy(starting_days)

            # Fulfill preferences
            self.fulfill_requests(minimum_weight=request_weight)

            # Assign remaining tasks
            random.shuffle(self.days)
            self.tasks_by_possibility = get_tasks_sorted_by_performability(self.days, self.people)
            while self.tasks_by_possibility:
                task = self.tasks_by_possibility.pop(0)
                day = self.get_day_by_string(task.date.to_string())
                if day:
                    if day.to_string() == '8/27/2024':
                        x = 1
                    if day.to_string() not in self.schedule:
                        self.schedule[day.to_string()] = []

                    # Sort people by remaining weight capacity
                    people_sorted = sorted(self.people, key=lambda p: p.max_weight -
                                                                      p.current_weight if p.current_weight != 0 else 999
                                           , reverse=True)
                    candidates = [p for p in people_sorted if p.can_perform_task(task, request_weight)]

                    # If no suitable candidates, break and bump up the request weight
                    if candidates:
                        selected_person = candidates[0]
                    else:
                        break
                    self.schedule[day.to_string()].append(self.assign_task(selected_person, task))
                    day.tasks.remove(task)

                    # Handle required tasks
                    self.assign_required_tasks(selected_person, task, day)

            if self.tasks_by_possibility:
                """
                This means we broke out of the loop above because there were tasks nobody could complete
                """
                continue
            # Assign Dev tasks to those with remaining weight capacity
            # for day in self.days:
            #     for person in self.people:
            #         if day.to_string() not in [d for d, _ in person.schedule]:
            #             dev_task = Task(AbstractTask("Dev", 0.0), day.date)
            #             if person.can_perform_task(dev_task, request_weight):
            #                 self.schedule[day.to_string()].append(self.assign_task(person, dev_task))
            #         else:
            #             weight = sum(task.weight for d, task in person.schedule if d == day.to_string())
            #             half_dev_task = Task(AbstractTask("HalfDev", 0.0), day.date)
            #             if weight <= 3.0 and person.can_perform_task(half_dev_task, request_weight):
            #                 self.schedule[day.to_string()].append(self.assign_task(person, half_dev_task))

            remaining_tasks = [f"{day.to_string()}:{task.name}" for day in self.days for task in day.tasks]
            if len(remaining_tasks) == 0:
                break
            else:
                print(f"Could not assign all tasks, {len(remaining_tasks)} remain")
                for remaining_task in remaining_tasks:
                    print(remaining_task)

        # Sort the schedule based on day.date.to_days()
        sorted_schedule = dict(
            sorted(self.schedule.items(), key=lambda item: next(
                (d.date.to_days() for d in self.days if item[0] == d.to_string()), float('inf'))))
        sorted_schedule = sort_schedule_by_person_name(sorted_schedule)
        self.schedule = sorted_schedule
        return self.schedule


def main():
    # Define abstract tasks
    vacation = AbstractTask("Vacation", weight=0.0, location="Vacation")
    pod = AbstractTask("POD", 4.5, location='UNC', requires=["HDR_AMP", "IORTTx"])
    sad = AbstractTask("SAD", 3.0, location=None)
    sad_assist = AbstractTask("SAD_Assist", 2.0, location=None)
    gamma_tile = AbstractTask("Gamma_Tile", 3.0, location="UNC")
    prostate_brachy = AbstractTask("Prostate_Brachy", 3.0, location='UNC',
                                   compatible_with=['SAD', 'SAD_Assist'])
    hbo = AbstractTask("HBO", 2.0, compatible_with=["SAD_Assist", "SAD"], location='HBO')
    pod_backup = AbstractTask("POD_Backup", 2.0,
                              requires=["SAD_Assist", "HDR_AMP", "IORTTx", "Prostate_Brachy"], location='UNC')
    hdr_amp = AbstractTask("HDR_AMP", 1.0, compatible_with=["POD", "POD_Backup"], location='UNC')
    iort_tx = AbstractTask("IORTTx", 2.0, compatible_with=["POD", "POD_Backup"], location='UNC')
    dev = AbstractTask("Dev", 0.0, location="Away")
    half_dev = AbstractTask("HalfDev", 0.0, location=None)

    # Define date instances for each day
    date_monday = DateTimeClass(2024, 8, 26)
    date_tuesday = DateTimeClass(2024, 8, 27)
    date_wednesday = DateTimeClass(2024, 8, 28)
    date_thursday = DateTimeClass(2024, 8, 29)
    date_friday = DateTimeClass(2024, 8, 30)

    # Define people with max_weight
    people = []

    # Leith is a Physicist
    leith = Physicist(
        "Leith",
        weight_per_day=12 / 5,
        preferences=[Preference(date_monday, "POD", weight=7.0)]
    )
    people.append(leith)

    # Taki is a Physicist
    taki = Physicist(
        "Taki",
        weight_per_day=12 / 5,
        preferences=[
            Preference(date_monday, "Dev", weight=7.0),
            Preference(date_tuesday, "Dev", weight=7.0)
        ],
        performable_tasks=[
            AbstractTask("Gamma_Tile", 0.0, location='UNC')
        ]
    )
    people.append(taki)

    # Dance can perform 'Gamma_Tile'
    dance = Physicist(
        "Dance",
        weight_per_day=18 / 5,
        preferences=[],
        performable_tasks=[
            AbstractTask("Gamma_Tile", 0.0, location='UNC')
        ]
    )
    people.append(dance)

    # Adria is a Physicist
    adria = Physicist(
        "Adria",
        weight_per_day=18 / 5,
        preferences=[Preference(date_monday, "Vacation", weight=9.0)]
    )
    people.append(adria)

    # Cielle can perform 'Prostate_Brachy', 'Dev', 'HalfDev', and 'Vacation'
    cielle = Physicist(
        "Cielle",
        weight_per_day=18 / 5,
        preferences=[],
        performable_tasks=[
            AbstractTask('Prostate_Brachy', weight=0.0, location='UNC')
        ]
    )
    people.append(cielle)

    # Brian is a Physicist
    brian = Physicist(
        "Brian",
        weight_per_day=12 / 5,
        preferences=[
            Preference(date_monday, "POD_Backup", weight=3.0),
            Preference(date_friday, "SAD", weight=7.0),
        ]
    )
    people.append(brian)

    # David is a Physicist with avoid preferences
    david = Physicist(
        "David",
        weight_per_day=12 / 5,
        avoid_preferences=[
            Preference(date_monday, "HBO", weight=9.0),
            Preference(date_monday, "UNC", weight=9.0),
            Preference(date_tuesday, "HBO", weight=9.0),
            Preference(date_tuesday, "UNC", weight=9.0),
            Preference(date_wednesday, "HBO", weight=1.0),
            Preference(date_thursday, "HBO", weight=1.0),
            Preference(date_friday, "HBO", weight=1.0),
            Preference(date_friday, "UNC", weight=1.0)
        ]
    )
    people.append(david)

    # Jun can perform 'Prostate_Brachy'
    jun = Physicist(
        "Jun",
        weight_per_day=12 / 5,
        preferences=[
            Preference(date_friday, "Vacation", weight=9.0)
        ],
        performable_tasks=[
            AbstractTask('Prostate_Brachy', weight=0.0, location='UNC')
        ]
    )
    people.append(jun)

    # Ross is a Physicist
    ross = Physicist(
        "Ross",
        weight_per_day=18 / 5,
        preferences=[
            Preference(date_friday, "Vacation", weight=9.0)
        ]
    )
    people.append(ross)

    # Define tasks for each day
    every_day_tasks = [pod, pod, hbo, pod_backup, sad]

    # Create Task instances for each day
    monday_tasks = [Task(task, date_monday) for task in every_day_tasks] + [
        Task(prostate_brachy, date_monday)
    ]
    tuesday_tasks = [Task(task, date_tuesday) for task in every_day_tasks] + [
        Task(hdr_amp, date_tuesday)]
    wednesday_tasks = [Task(task, date_wednesday) for task in every_day_tasks]
    thursday_tasks = [Task(task, date_thursday) for task in every_day_tasks] + [
        Task(hdr_amp, date_thursday)] + [
        Task(gamma_tile, date_thursday)
    ]
    friday_tasks = [Task(task, date_friday) for task in every_day_tasks] + [
        Task(iort_tx, date_friday),
        Task(hdr_amp, date_friday),
        Task(gamma_tile, date_friday)
    ]

    # Create Day instances
    monday = Day("Monday", monday_tasks, date_monday)
    tuesday = Day("Tuesday", tuesday_tasks, date_tuesday)
    wednesday = Day("Wednesday", wednesday_tasks, date_wednesday)
    thursday = Day("Thursday", thursday_tasks, date_thursday)
    friday = Day("Friday", friday_tasks, date_friday)

    # Define week
    week1 = Week("8/26/2024", [monday, tuesday, wednesday, thursday, friday])

    # Initialize scheduler
    scheduler = Scheduler()

    # Add people and days to scheduler
    for p in people:
        scheduler.add_person(p)

    for day in week1.days:
        scheduler.add_day(day)

    # Create and print the schedule
    schedule = scheduler.create_schedule()
    for day, assignments in schedule.items():
        print(f"---------{day}----------")
        for person, task in assignments:
            print(f"{person.name}: {task.name}")


if __name__ == '__main__':
    main()
