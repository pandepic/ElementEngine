using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine.Timer
{
    public enum TimerStatus
    {
        NotStarted,
        Running,
        Paused,
        Finished,
    }

    public interface ITimer
    {
        public TimerStatus TimerStatus { get; set; }

        public void Update(GameTimer gameTimer);
    }

    public static class TimerManager
    {
        public static List<ITimer> Timers = new List<ITimer>();

        public static T AddTimer<T>(T timer) where T : ITimer
        {
            Timers.Add(timer);
            return timer;
        }

        public static void Update(GameTimer gameTimer)
        {
            for (var i = Timers.Count - 1; i >= 0; i--)
            {
                var timer = Timers[i];

                if (timer.TimerStatus == TimerStatus.Running)
                    timer.Update(gameTimer);

                if (timer.TimerStatus == TimerStatus.Finished)
                    Timers.Remove(timer);
            }
        }

    } // TimerManager
}
