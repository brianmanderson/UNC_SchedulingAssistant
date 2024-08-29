from typing import List, Optional, Dict


class Task:
    def __init__(self, name: str, weight: float, facility: Optional[str]):
        self.name = name
        self.weight = weight
        self.facility = facility


class PrimaryTask(Task):
    def __init__(self, name: str, weight: float):
        super().__init__(name, weight, 'UNC')


class POD(PrimaryTask):
    def __init__(self):
        super().__init__("Physicist of the Day", 4.5)


class SAD(PrimaryTask):
    def __init__(self):
        super().__init__("Second Checks", 3.0)


class PODBackup(PrimaryTask):
    def __init__(self):
        super().__init__("Physicist Backup", 2.0)


class SAD_Assist(Task):
    def __init__(self):
        super().__init__("Second check assist", 2.0, None)


class IORT_Setup(Task):
    def __init__(self):
        super().__init__("IORT Setup", 2.0, "UNC")


class DevAdmin(Task):
    def __init__(self):
        super().__init__("Dev.Admin", 0.0, None)


class HBO(Task):
    def __init__(self):
        super().__init__("Hillsborough Coverage", 2.0, 'HBO')


class HDR_AMP(Task):
    def __init__(self):
        super().__init__("HDR Amp Second Check", 1.0, 'UNC')


class Preference:
    Preferences: List[str]

    def __init__(self):
        self.Preferences = []


class Physicist:
    Total_Score: float
    Max_Score: float
    Monday: Preference
    Tuesday: Preference
    Wednesday: Preference
    Thursday: Preference
    Friday: Preference

    def __init__(self, name: str, Max_Score=18):
        self.name = name
        self.tasks: List[Task] = []

    @property
    def Total_Score(self) -> float:
        return sum(task.weight for task in self.tasks) if self.tasks else 0.0

    def can_assign_task(self, task: Task) -> bool:
        for t in self.tasks:
            if isinstance(t, PrimaryTask) and isinstance(task, PrimaryTask):
                return False
            if t.facility and task.facility and t.facility != task.facility:
                return False
        return True

    def assign_task(self, task: Task):
        if self.can_assign_task(task):
            self.tasks.append(task)
            return True
        else:
            return False



monday = [POD(), POD(), PODBackup(), SAD(), SAD(), SAD_Assist(), IORT_Setup()]
tuesday = [POD(), POD(), PODBackup(), SAD(), SAD(), HDR_AMP()]
wednesday = [POD(), POD(), PODBackup(), SAD(), SAD(), HDR_AMP()]
thursday = [POD(), POD(), PODBackup(), SAD(), SAD(), HDR_AMP()]
friday = [POD(), POD(), PODBackup(), SAD(), SAD(), HDR_AMP()]

# Adding people
person1 = Person("Alice")
person2 = Person("Bob")
scheduler.add_person(person1)
scheduler.add_person(person2)

# Scheduling tasks
task1 = POD()
task2 = HBO()
task3 = SAD_Assist()

try:
    assigned_person1 = scheduler.schedule_task(task1)
    assigned_person2 = scheduler.schedule_task(task2)
    assigned_person3 = scheduler.schedule_task(task3)
    print(f"Task {task1.name} assigned to {assigned_person1}")
    print(f"Task {task2.name} assigned to {assigned_person2}")
    print(f"Task {task3.name} assigned to {assigned_person3}")
except ValueError as e:
    print(e)
