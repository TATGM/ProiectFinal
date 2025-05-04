using ProiectFinal.Nucleu;
using System;
using System.Collections.Generic;
using System.Threading;
using NLog;

namespace ProiectFinal
{
    class Program
    {
        private static readonly Logger registrator_date = LogManager.GetCurrentClassLogger();

        private static ManualResetEvent terminat = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            Console.Clear();
            Console.CancelKeyPress += new ConsoleCancelEventHandler(BreakHandler);

            var parametri = ValidateInput(args);
            if (parametri == null)
                Environment.Exit(1);

            var nucleu = new Nucleu.Nucleu();
            nucleu.Rulare(parametri);

            terminat.WaitOne();

            nucleu.Oprire();
        }

        private static void BreakHandler(object sender, ConsoleCancelEventArgs e)
        {
            Program.terminat.Set();
            e.Cancel = true;
        }

        static ParametriiNucleului ValidateInput(string[] argumente)
        {
            if (argumente.Length != 7)
            {
                registrator_date.Fatal($"[main]: Intrare invalidă, număr invalid de argumente");
                return null;
            }

            var parametri = new ParametriiNucleului();
            parametri.HostHub = argumente[0];

            int parsedInt;
            if (!Int32.TryParse(argumente[1], out parsedInt))
            {
                registrator_date.Fatal($"[main]: Intrare invalidă, nu s-a găsit portul hubului");
                return null;
            }

            parametri.PortHub = parsedInt;
            parametri.HostProcese = argumente[2];
            parametri.PorturiProcese = new List<int>();

            if (!Int32.TryParse(argumente[3], out parsedInt))
            {
                registrator_date.Fatal($"[main]: Intrare invalidă, nu s-a găsit portul procesului");
                return null;
            }
            parametri.PorturiProcese.Add(parsedInt);

            if (!Int32.TryParse(argumente[4], out parsedInt))
            {
                registrator_date.Fatal($"[main]: Intrare invalidă, nu s-a găsit portul procesului");
                return null;
            }
            parametri.PorturiProcese.Add(parsedInt);

            if (!Int32.TryParse(argumente[5], out parsedInt))
            {
                registrator_date.Fatal($"[main]: Intrare invalidă, nu s-a găsit portul procesului");
                return null;
            }
            parametri.PorturiProcese.Add(parsedInt);

            parametri.Proprietar = argumente[6];

            return parametri;
        }
    }
}