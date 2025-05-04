using Google.Protobuf;
using ProiectFinal.Abstracții;
using ProiectFinal.Manipulator;
using ProiectFinal.Rețelistică;
using ProiectFinal.Utilități;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ProiectFinal.Sistem
{
    class Sistem
    {
        private static readonly NLog.Logger registrator_date = NLog.LogManager.GetCurrentClassLogger();

        public ProtoComm.ProcessId IDProces { get; private set; }
        public ProtoComm.ProcessId IDProcesHub { get; private set; }

        public HashSet<ProtoComm.ProcessId> Procese { get; private set; }

        public string IDSistem { get; private set; }

        private ConcurrentDictionary<string, Abstractie> _abstractii;

        private Task _receptorMesaj;
        private ManipulatorRetea _manipulatorRetea;

        private BlockingCollection<ProtoComm.Message> _coadaEvenimente;

        private List<ManipulatorTimer> _manipulatoriTimer;

        private bool _oprit = false;

        public Sistem(ProtoComm.ProcessId processId, ProtoComm.ProcessId hubProcesId)
        {
            IDProces = processId;
            IDProcesHub = hubProcesId;

            Procese = new HashSet<ProtoComm.ProcessId>();

            _abstractii = new ConcurrentDictionary<string, Abstractie>();

            _manipulatorRetea = new ManipulatorRetea(processId.Host, IDProces.Port);
            _coadaEvenimente = new BlockingCollection<ProtoComm.Message>(new ConcurrentQueue<ProtoComm.Message>());
            _manipulatoriTimer = new List<ManipulatorTimer>();
        }

        public void Incepe()
        {
            AbonareReceptorMesaje();
            _receptorMesaj = new Task(() =>
            {
                _manipulatorRetea.ReceptarePentruConexiuni();
            });
            var RetragereReceptorMesajeTask = _receptorMesaj.ContinueWith(OprireReceptorMesaje);
            _receptorMesaj.Start();

            InregistrareHub();

            CicluEvenimente();

            RetragereReceptorMesajeTask.Wait();
        }

        public void Oprire()
        {
            if (_oprit)
                return;

            OprireTimeri();
            DezabonareReceptorMesaje();
            _manipulatorRetea.OprireReceptor();

            try
            {
                _receptorMesaj.Wait();
            }
            catch (AggregateException) { }

            _oprit = true;
        }

        public ProtoComm.ProcessId GasireProcesHostPort(string host, int port)
        {
            foreach (var proc in Procese)
            {
                if ((proc.Host == host) && (proc.Port == port))
                    return proc;
            }

            return null;
        }

        public void InregistrareAbstractie(Abstractie abstraction)
        {
            if (_abstractii.TryAdd(abstraction.GetId(), abstraction))
            {
                registrator_date.Trace($"[{IDProces.Port}]: Nouă abstracție înregistrată: {abstraction.GetId()}");
            }
        }

        public void DeclansareEveniment(ProtoComm.Message e)
        {
            try
            {
                if (!_coadaEvenimente.IsCompleted)
                    _coadaEvenimente.Add(e);
            }
            catch (Exception ex)
            {
                registrator_date.Error($"[{IDProces.Port}]: {ex.Message}. Excepție în timpul declanșării evenimentului - [{e}]");
            }
        }

        private void GolireCoadaEvenimente()
        {
            if (_coadaEvenimente == null)
            {
                return;
            }

            while (_coadaEvenimente.Count > 0)
            {
                _coadaEvenimente.TryTake(out var _);
            }
        }

        public bool TrimitereMesajPrinRetea(ProtoComm.Message mesaj, string remoteHost, int remotePort)
        {
            if (mesaj.Type != ProtoComm.Message.Types.Type.NetworkMessage)
            {
                registrator_date.Error($"[{IDProces.Port}]: Mesaj invalid trimis - {mesaj}");
                return false;
            }

            var innerMessage = mesaj.NetworkMessage.Message;
            registrator_date.Info($"[{IDProces.Port}]: ===> Trimitere mesaj: [{innerMessage.Type}] -> [{innerMessage.ToAbstractionId}] ({remoteHost}:{remotePort})");

            byte[] serializedMessage = mesaj.ToByteArray();

            try
            {
                _manipulatorRetea.TrimiteMesaj(serializedMessage, remoteHost, remotePort);
                return true;
            }
            catch (ExceptieRetea exceptie)
            {
                registrator_date.Error($"[{IDProces.Port}]: {exceptie.Message}");
                return false;
            }
        }

        private void OprireReceptorMesaje(Task antecedent)
        {
            if (antecedent.Status == TaskStatus.RanToCompletion)
            {
                registrator_date.Debug($"[{IDProces.Port}]: Receptor mesaje oprit");
                return;
            }
            else if (antecedent.Status == TaskStatus.Faulted)
            {
                var ex = antecedent.Exception?.GetBaseException();
                registrator_date.Fatal($"[{IDProces.Port}]: {ex.Message}");
                Oprire();
            }
        }

        private void InregistrareHub()
        {
            if (_oprit)
                return;

            var processRegistration = new ProtoComm.ProcRegistration
            {
                Owner = IDProces.Owner,
                Index = IDProces.Index
            };

            var wrapperMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.ProcRegistration,
                ProcRegistration = processRegistration,
                ToAbstractionId = IDProcesHub.Owner,
                MessageUuid = Guid.NewGuid().ToString()
            };

            var networkMessage = new ProtoComm.NetworkMessage
            {
                Message = wrapperMessage,
                SenderHost = IDProces.Host,
                SenderListeningPort = IDProces.Port
            };

            var outputMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NetworkMessage,
                NetworkMessage = networkMessage,
                SystemId = wrapperMessage.SystemId,
                ToAbstractionId = wrapperMessage.ToAbstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            if (TrimitereMesajPrinRetea(outputMessage, IDProces.Host, IDProcesHub.Port))
            {
                registrator_date.Info($"[{IDProces.Port}]: Proces înregistrat - [{IDProces.Owner}-{IDProces.Index}]");
            }
            else
            {
                registrator_date.Fatal($"[{IDProces.Port}]: Nu s-a putut înregistra în Hub");
                Oprire();
            }
        }

        private void AbonareReceptorMesaje()
        {
            _manipulatorRetea.LaPublicare += LaPrimireMesaj;
        }

        private void DezabonareReceptorMesaje()
        {
            _manipulatorRetea.LaPublicare -= LaPrimireMesaj;
            _coadaEvenimente.CompleteAdding();
        }

        protected virtual void LaPrimireMesaj(ManipulatorRetea primire, byte[] serializedMessage)
        {
            ProtoComm.Message mesaj;
            try
            {
                mesaj = ProtoComm.Message.Parser.ParseFrom(serializedMessage);
            }
            catch (InvalidProtocolBufferException)
            {
                registrator_date.Error($"[{IDProces.Port}]: Protobuf nu a putut analiza următorul mesaj serializat. Mesaj ignorat");
                return;
            }

            if (mesaj.Type != ProtoComm.Message.Types.Type.NetworkMessage)
            {
                registrator_date.Error($"[{IDProces.Port}]: Mesaj invalid primit - [{mesaj.Type}]. Mesaj ignorat");
                return;
            }

            var innerMessage = mesaj.NetworkMessage.Message;
            registrator_date.Info($"[{IDProces.Port}]: <=== Mesaj primit: [{innerMessage.Type}] -> [{innerMessage.ToAbstractionId}]");

            if (innerMessage.Type == ProtoComm.Message.Types.Type.ProcessInitializeSystem)
            {
                InitializareProcesManipulat(innerMessage);
            }
            else if (innerMessage.Type == ProtoComm.Message.Types.Type.ProcDestroySystem)
            {
                DistrugereProcesManipulat(innerMessage);
            }
            else
            {
                DeclansareEveniment(mesaj);
            }
        }

        private void DistrugereProcesManipulat(ProtoComm.Message innerMessage)
        {
            OprireTimeri();
            Procese.Clear();
            _abstractii.Clear();
        }

        private void InitializareProcesManipulat(ProtoComm.Message mesaj)
        {
            try
            {
                GolireCoadaEvenimente();

                InregistrareAbstractie(new Aplicatie(Aplicatie.NumeAplicatie, this));

                var processInitializationSystemMessage = mesaj.ProcessInitializeSystem;
                foreach (var proc in processInitializationSystemMessage.Processes)
                {
                    Procese.Add(proc);
                }

                var foundProcessId = GasireProcesHostPort(IDProces.Host, IDProces.Port);   // Hub updates info of ProcessId, so it should be replaced
                if (foundProcessId != null)
                {
                    IDProces = foundProcessId;
                }
                else
                {
                    registrator_date.Fatal($"[{IDProces.Port}]: Nu s-a găsit ID-ul procesului din ProcessInitializeSystem");
                    Oprire();
                }

                IDSistem = mesaj.SystemId;
            }
            catch (Exception exceptie)
            {
                registrator_date.Fatal($"[{IDProces.Port}]: Excepție în timpul manipulării mesajului ProcessInitializeSystem - {exceptie.Message}");
                Oprire();
            }
        }

        private void CicluEvenimente()
        {
            if (_oprit)
                return;
            try
            {
                foreach (var mesaj in _coadaEvenimente.GetConsumingEnumerable())
                {
                    try
                    {
                        ManipulareEveniment(mesaj);
                    }
                    catch (Exception exceptie)
                    {
                        registrator_date.Error($"[{IDProces.Port}]: {exceptie.Message}. Excepție în timpul manipulării mesajului: [{mesaj}]");
                        continue;
                    }
                }
            }
            catch (Exception exceptie)
            {
                registrator_date.Fatal($"[{IDProces.Port}]: {exceptie.Message}. Excepție în CicluEvenimente");
                Oprire();
            }
            finally
            {
                registrator_date.Debug($"[{IDProces.Port}]: CicluEvenimente oprit");
            }
        }

        private void ManipulareEveniment(ProtoComm.Message mesaj)
        {
            if (!_abstractii.ContainsKey(mesaj.ToAbstractionId))
            {
                ManipulareIDAbstractieNecunoscuta(mesaj);
            }

            if (_abstractii.ContainsKey(mesaj.ToAbstractionId) && !_abstractii[mesaj.ToAbstractionId].Manipulare(mesaj))
            {
                this.DeclansareEveniment(mesaj);
            }
            else return;
        }

        private void ManipulareIDAbstractieNecunoscuta(ProtoComm.Message mesaj)
        {
            var nnarRegisterName = UtilitatiIDAbstractie.GetNumeRegistruAtomicNN(mesaj.ToAbstractionId);
            if (nnarRegisterName != "")
            {
                var nnarAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieRegistruAtomicNN(Aplicatie.NumeAplicatie, nnarRegisterName);
                InregistrareAbstractie(new RegistruAtomicNN(nnarAbstractionId, this));
                return;
            }
            this.DeclansareEveniment(mesaj);
        }

        public ManipulatorTimer PregatireTaskProgramat(Action<object, object> task)
        {
            var newTimerHandler = new ManipulatorTimer(task);
            _manipulatoriTimer.Add(newTimerHandler);
            return newTimerHandler;
        }

        private void OprireTimeri()
        {
            foreach (var timerHandler in _manipulatoriTimer)
            {
                timerHandler.Oprire();
            }
            _manipulatoriTimer.Clear();
        }
    }
}
