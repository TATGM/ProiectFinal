using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProiectFinal.Nucleu
{
    internal class Nucleu
    {
        private List<Task> _procese = new List<Task>();
        private List<Sistem.Sistem> _sisteme = new List<Sistem.Sistem>();

        public Nucleu()
        {
        }

        public void Rulare(ParametriiNucleului coreParameters)
        {
            var IDProcesHub = new ProtoComm.ProcessId
            {
                Host = coreParameters.HostHub,
                Port = coreParameters.PortHub,
                Owner = "hub"
            };

            int indiceProces = 1;
            foreach (var port in coreParameters.PorturiProcese)
            {
                var IDProces = new ProtoComm.ProcessId
                {
                    Host = coreParameters.HostProcese,
                    Port = port,
                    Owner = coreParameters.Proprietar,
                    Index = indiceProces
                };
                indiceProces++;

                var system = new Sistem.Sistem(IDProces, IDProcesHub);
                var process = Task.Run(() =>
                {
                    system.Incepe();
                });

                _sisteme.Add(system);
                _procese.Add(process);
            }
        }

        public void Oprire()
        {
            foreach (var system in _sisteme)
            {
                system.Oprire();
            }

            foreach (var process in _procese)
            {
                process.Wait();
            }
        }
    }
}
