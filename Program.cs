using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NodaTime;
using System.Linq;

namespace Timetable_Generator
{
    public class Program
    {

        public class AllSubjects
        {
            public Subject[] subjects;

            public List<Subject.Class> AllClasses()
            {
                List<Subject.Class> classes = new();
                foreach (var item in AllClassesE())
                {
                    classes.Add(item);
                }
                return classes;
            }

            /// <summary>
            /// Does not include sub classes!
            /// </summary>
            /// <returns></returns>
            public IEnumerable<Subject.Class> AllClassesE()
            {
                foreach (Subject subject in subjects)
                {
                    foreach (Subject.Class c in subject.classes)
                    {
                        yield return c;
                    }
                }
            }

            public class Subject
            {

                public string name;
                public Class[] classes;

                public class Class : SubClass
                {
                    public bool HasDependency => dependency.Length != 0;
                    public SubClass[] dependency = Array.Empty<SubClass>();
                }
                public class SubClass
                {
                    public string name;
                    public Time[] time;
                }
                public class Time
                {
                    public Day day;
                    public LocalTime start;
                    public LocalTime end;

                    public Time(Day day, LocalTime start, LocalTime end)
                    {
                        this.day = day;
                        this.start = start;
                        this.end = end;
                    }

                    public override string ToString()
                    {
                        return $"{day}: {TimeStr(start)}-{TimeStr(end)}";
                    }

                    public string TimeStr(LocalTime t)
                    {
                        return $"{string.Format("{0:00}", t.Hour)}:{string.Format("{0:00}" ,t.Minute)}";
                    }
                }
                public enum Day
                {
                    Mon = 1,
                    Tue = 2,
                    Wed = 3,
                    Thu = 4,
                    Fri = 5,
                    Sat = 6,
                    Sun = 7,
                }
            }
        }

        public class ClassChoice
        {
            public AllSubjects.Subject subject;
            public AllSubjects.Subject.SubClass DependencyClass => chosenClass.dependency[dependencyIndex];
            public AllSubjects.Subject.Class chosenClass;
            public int dependencyIndex = -1;

            public IEnumerable<AllSubjects.Subject.SubClass> AllSubClasses()
            {
                yield return chosenClass;
                if (dependencyIndex != -1)
                {
                    yield return DependencyClass;
                }
            }

            public IEnumerable<AllSubjects.Subject.Time> AllTimes()
            {
                for (int i = 0; i < chosenClass.time.Length; i++)
                {
                    yield return chosenClass.time[i];
                }
                if (dependencyIndex != -1)
                {
                    for (int i = 0; i < DependencyClass.time.Length; i++)
                    {
                        yield return DependencyClass.time[i];
                    }
                }
            }

            public override bool Equals(object obj)
            {
                if (obj is ClassChoice classChoice)
                {
                    return chosenClass == classChoice.chosenClass && dependencyIndex == classChoice.dependencyIndex;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override string ToString()
            {
                string text = $"{subject.name} Class: {chosenClass.name}\n";
                for (int i = 0; i < chosenClass.time.Length; i++)
                {
                    text += $"\t{chosenClass.time[i]}\n";
                }
                if (dependencyIndex != -1)
                {
                    text += $"\t\tDependent Class: {DependencyClass.name}\n";
                    for (int i = 0; i < DependencyClass.time.Length; i++)
                    {
                        text += $"\t\t\t{DependencyClass.time[i]}\n";
                    }
                }
                return text;
            }
        }

        public class Timetable
        {
            public List<ClassChoice> allClasses;

            public IEnumerable<AllSubjects.Subject.Time> AllTimes()
            {
                for (int i = 0; i < allClasses.Count; i++)
                {
                    for (int j = 0; j < allClasses[i].chosenClass.time.Length; j++)
                    {
                        yield return allClasses[i].chosenClass.time[j];
                    }
                    if (allClasses[i].dependencyIndex != -1)
                    {
                        for (int j = 0; j < allClasses[i].DependencyClass.time.Length; j++)
                        {
                            yield return allClasses[i].DependencyClass.time[j];
                        }
                    }
                }
            }

            public override bool Equals(object obj)
            {
                if (obj is Timetable timetable)
                {
                    if (allClasses.Count != timetable.allClasses.Count)
                    {
                        return false;
                    }
                    for (int i = 0; i < allClasses.Count; i++)
                    {
                        if (allClasses[i] != timetable.allClasses[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override string ToString()
            {
                string text = "Timetable\n";
                for (int i = 0; i < allClasses.Count; i++)
                {
                    text += allClasses[i];
                }
                return text;
            }
        }

        private static void Print(object o)
        {
            Console.WriteLine(o);
        }

        static void Main(string[] args)
        {
            string path = FilePath(args);
            string lines = File.ReadAllText(path);
            InterpretJSONString(lines);

            Console.WriteLine("Hello World!");
        }

        static void InterpretJSONString(string jsonString)
        {
            AllSubjects subjects = JsonConvert.DeserializeObject<AllSubjects>(jsonString);
            //Print(subjects.subjects[1].classes[0].dependency[1].time[0].start);
            List<Timetable> timetables = GenerateAllPossibilities(subjects);
            FitTimetables(timetables);
            StripTimetable(timetables);
            FilterTimetables(timetables);
            StripTimetable(timetables);
            PrintTimetables(timetables);
        }

        public struct ClassDependency
        {
            public int classes;
            public int[] dependencies;

            public ClassDependency(int classes, int[] dependencies)
            {
                this.classes = classes;
                this.dependencies = dependencies;
            }
        }

        static List<List<ClassChoice>> GetCombinations(List<List<ClassChoice>> arr)
        {
            List<List<ClassChoice>> classChoice = new();

            // Number of arrays
            int n = arr.Count;

            // To keep track of next
            // element in each of
            // the n arrays
            int[] indices = new int[n];

            // Initialize with first
            // element's index
            for (int i = 0; i < n; i++)
                indices[i] = 0;

            int choice = 0;
            while (true)
            {
                // Print current combination
                classChoice.Add(new List<ClassChoice>());
                for (int i = 0; i < n; i++)
                {
                    //Print(i);
                    classChoice[choice].Add(arr[i][indices[i]]);
                }
                    //Console.Write(arr[i][indices[i]] + " ");

                //Console.WriteLine();

                // Find the rightmost array
                // that has more elements
                // left after the current
                // element in that array
                int next = n - 1;
                while (next >= 0 &&
                      (indices[next] + 1 >=
                       arr[next].Count))
                    next--;

                // No such array is found
                // so no more combinations left
                if (next < 0)
                    break;

                // If found move to next
                // element in that array
                indices[next]++;

                // For all arrays to the right
                // of this array current index
                // again points to first element
                for (int i = next + 1; i < n; i++)
                    indices[i] = 0;

                choice++;
            }
            return classChoice;
        }

        private static List<Timetable> GenerateAllPossibilities(AllSubjects subjects)
        {
            List<List<ClassChoice>> classChoicesTable = new();
            foreach (AllSubjects.Subject subject in subjects.subjects)
            {
                List<ClassChoice> row = new();
                classChoicesTable.Add(row);
                foreach (AllSubjects.Subject.Class c in subject.classes)
                {
                    if (c.HasDependency)
                    {
                        for (int depI = 0; depI < c.dependency.Length; depI++)
                        {
                            ClassChoice cc = new()
                            {
                                subject = subject,
                                chosenClass = c,
                                dependencyIndex = depI
                            };
                            row.Add(cc);
                        }
                    }
                    else
                    {
                        ClassChoice cc = new()
                        {
                            subject = subject,
                            chosenClass = c
                        };
                        row.Add(cc);
                    }
                }
            }

            List<List<ClassChoice>> combinations = GetCombinations(classChoicesTable);


            List<Timetable> timetables = new();
            foreach (var item in combinations)
            {
                Timetable timetable = new()
                {
                    allClasses = item
                };
                timetables.Add(timetable);
            }

            //for (int i = 0; i < timetables.Count; i++)
            //{
            //    Print(timetables[i]);
            //}


            return timetables;

        }

        private static void StripTimetable(List<Timetable> timetables)
        {
            _ = timetables.RemoveAll((t) => t == null);
        }

        private static void FitTimetables(List<Timetable> timetables)
        {
            for (int i = 0; i < timetables.Count; i++)
            {
                if (!TimetableAcceptable(timetables[i]))
                {
                    timetables[i] = null;
                }
            }
        }
        //Returns: Acceptable
        private static bool TimetableAcceptable(Timetable timetable)
        {
            foreach (AllSubjects.Subject.Time time1 in timetable.AllTimes())
            {
                foreach (AllSubjects.Subject.Time time2 in timetable.AllTimes())
                {
                    if (time1 == time2)
                    {
                        continue;
                    }
                    if (!CompareTimes(time1, time2))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        //Returns: Acceptable
        private static bool CompareTimes(AllSubjects.Subject.Time time1, AllSubjects.Subject.Time time2)
        {
            if (time1.day != time2.day)
            {
                return true;
            }
            //if (time1.start == time2.end || time1.end == time2.start)
            //{
            //    return true;
            //}
            if (time2.start < time1.end && time1.start < time2.end)
            {
                //Print($"Filtered - {time1}, {time2}");
                return false;
            }
            return true;
        }


        private static void FilterTimetables(List<Timetable> timetables)
        {
            for (int i = 0; i < timetables.Count; i++)
            {
                Timetable timetable = timetables[i];
                for (int j = 0; j < timetable.allClasses.Count; j++)
                {
                    ClassChoice c = timetable.allClasses[j];
                    bool getOut = false;
                    foreach (AllSubjects.Subject.SubClass subclass in c.AllSubClasses())
                    {
                        if (subclass.name.Contains('L'))
                        {
                            timetables[i] = null;
                            getOut = true;
                            break;
                        }
                    }
                    if (getOut)
                    {
                        break;
                    }
                    foreach (AllSubjects.Subject.Time time in c.AllTimes())
                    {
                        if (time.day == AllSubjects.Subject.Day.Fri)
                        {
                            timetables[i] = null;
                            getOut = true;
                            break;
                        }
                    }
                    if (getOut)
                    {
                        break;
                    }
                }
            }
        }

        private static void PrintTimetables(List<Timetable> timetables)
        {
            for (int i = 0; i < timetables.Count; i++)
            {
                Print(timetables[i]);
            }
        }

        static string FilePath(string[] args)
        {
            string text = "File: ";
            string output;
            if (args.Length > 0)
            {
                output = args[0];
            }
            else
            {
                output = "file.txt";
            }
            text += output;
            Print(text);
            return output;
        }

    }
}
