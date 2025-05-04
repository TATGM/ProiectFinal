using System;

namespace ProiectFinal.Rețelistică
{
    class ExceptieRetea : Exception
    {
        public ExceptieRetea()
        {
        }

        public ExceptieRetea(string message)
            : base(message)
        {
        }

        public ExceptieRetea(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
