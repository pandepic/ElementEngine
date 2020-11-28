using System;
using System.Collections.Generic;
using System.Text;

namespace ElementEngine
{
    public class WorldTimeMonth
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public int Days { get; set; }

        public WorldTimeMonth(string name, int days)
        {
            Name = name;
            Days = days;
        }
    }

    public class WorldTime
    {
        public static List<string> DefaultDays { get; set; } = new List<string>() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        public static List<WorldTimeMonth> DefaultMonths { get; set; } = new List<WorldTimeMonth>()
        {
            new WorldTimeMonth("January", 31),
            new WorldTimeMonth("February", 28),
            new WorldTimeMonth("March", 31),
            new WorldTimeMonth("April", 30),
            new WorldTimeMonth("May", 31),
            new WorldTimeMonth("June", 30),
            new WorldTimeMonth("July", 31),
            new WorldTimeMonth("August", 31),
            new WorldTimeMonth("September", 30),
            new WorldTimeMonth("October", 31),
            new WorldTimeMonth("November", 30),
            new WorldTimeMonth("December", 31),
        };

        public ulong TotalTicks { get; set; } = 0;
        public ulong TicksPerSecond { get; set; } = 1;
        public ulong SecondsPerMinute { get; set; } = 60;
        public ulong MinutesPerHour { get; set; } = 60;
        public ulong HoursPerDay { get; set; } = 24;

        public List<string> Days { get; set; }
        public List<WorldTimeMonth> Months { get; set; }

        public ulong TotalDays => TotalTicks / (TicksPerSecond * SecondsPerMinute * MinutesPerHour * HoursPerDay);
        public ulong TotalYears => TotalDays / (ulong)DaysPerYear;

        public int DaysPerYear { get; private set; } = 0;
        public int DaysPerWeek => Days.Count;
        public int CurrentDayIndex => (int)TotalDays % DaysPerWeek;
        public string CurrentDayName => Days[CurrentDayIndex];

        public WorldTime() : this(DefaultDays, DefaultMonths) { }

        public WorldTime(List<string> days, List<WorldTimeMonth> months)
        {
            Days = days;
            Months = months;

            foreach (var month in months)
                DaysPerYear += month.Days;
        }

        public void Tick(ulong amount = 1)
        {
            TotalTicks += amount;
        }
    } // WorldTime
}
