using System;
using System.Timers;

namespace ProiectFinal.Manipulator
{
    class ManipulatorTimer
    {
        private System.Timers.Timer _timer = new System.Timers.Timer();

        public ManipulatorTimer(Action<object, object> task)
        {
            _timer.AutoReset = false;
            _timer.Elapsed += new ElapsedEventHandler(task);
        }

        public void ProgramareTask(int delay)
        {
            _timer.Interval = delay;
            _timer.Start();
        }

        public void Oprire()
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
            }
        }
    }
}