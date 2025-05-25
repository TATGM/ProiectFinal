using ProiectFinal.Manipulator;
using ProiectFinal.Utilități;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProiectFinal.Abstracții
{
    class DetectorEventualPerfectEsec : Abstractie
    {
        public static readonly string NumeAplicatie = "efpd";
        private static readonly int Delta = 1000; // 0.5 secunde
        private ManipulatorTimer _timer;
        private HashSet<ProtoComm.ProcessId> _rulat;
        private HashSet<ProtoComm.ProcessId> _suspectat = new HashSet<ProtoComm.ProcessId>();
        private int _intarziere = Delta;

        public DetectorEventualPerfectEsec(string IdAbstractie, Sistem.Sistem sistem) : base(IdAbstractie, sistem)
        {
            _sistem.InregistrareAbstractie(new PerfectLink(UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, PerfectLink.NumeAplicatie), _sistem));
            _rulat = new HashSet<ProtoComm.ProcessId>(_sistem.Procese);

            _timer = _sistem.PregatireTaskProgramat((source, e) =>
            {
                var mesaj = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.EpfdTimeout,
                    EpfdTimeout = new ProtoComm.EpfdTimeout(),
                    SystemId = _sistem.IDSistem,
                    ToAbstractionId = _IdAbstractie,
                    FromAbstractionId = _IdAbstractie,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _sistem.DeclansareEveniment(mesaj);
            });

            StartTimer();
        }

        public override bool Manipulare(ProtoComm.Message mesaj)
        {
            if (mesaj.Type == ProtoComm.Message.Types.Type.EpfdTimeout)
            {
                ManipularePauzaEPFD();
                return true;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.PlDeliver)
            {
                if (mesaj.PlDeliver.Message.Type == ProtoComm.Message.Types.Type.EpfdInternalHeartbeatRequest)
                {
                    ManipulareCerereInterna(mesaj);
                    return true;
                }

                if (mesaj.PlDeliver.Message.Type == ProtoComm.Message.Types.Type.EpfdInternalHeartbeatReply)
                {
                    ManipulareRaspunsIntern(mesaj);
                    return true;
                }

                return false;
            }

            return false;
        }

        private void ManipulareRaspunsIntern(ProtoComm.Message mesaj)
        {
            var senderProc = mesaj.PlDeliver.Sender;

            _rulat.Add(senderProc);
        }

        private void ManipulareCerereInterna(ProtoComm.Message mesaj)
        {
            var senderProc = mesaj.PlDeliver.Sender;

            var sendMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlSend,
                PlSend = new ProtoComm.PlSend
                {
                    Destination = senderProc,
                    Message = new ProtoComm.Message
                    {
                        Type = ProtoComm.Message.Types.Type.EpfdInternalHeartbeatReply,
                        EpfdInternalHeartbeatReply = new ProtoComm.EpfdInternalHeartbeatReply(),
                        SystemId = _sistem.IDSistem,
                        ToAbstractionId = _IdAbstractie,
                        FromAbstractionId = _IdAbstractie,
                        MessageUuid = Guid.NewGuid().ToString()
                    }
                },
                SystemId = _sistem.IDSistem,
                ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, PerfectLink.NumeAplicatie),
                FromAbstractionId = _IdAbstractie,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _sistem.DeclansareEveniment(sendMessage);
        }

        private void ManipularePauzaEPFD()
        {
            if (_rulat.Intersect(_suspectat).Count() != 0)
            {
                _intarziere += Delta;
            }

            foreach (var procId in _sistem.Procese)
            {
                var Message = new ProtoComm.Message
                {
                    SystemId = _sistem.IDSistem,
                    ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieParinte(_IdAbstractie),
                    FromAbstractionId = _IdAbstractie,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                if (!_rulat.Contains(procId) && !_suspectat.Contains(procId))
                {
                    _suspectat.Add(procId);

                    Message.Type = ProtoComm.Message.Types.Type.EpfdSuspect;
                    Message.EpfdSuspect = new ProtoComm.EpfdSuspect
                    {
                        Process = procId
                    };

                    _sistem.DeclansareEveniment(Message);
                }
                else if (_rulat.Contains(procId) && _suspectat.Contains(procId))
                {
                    _suspectat.Remove(procId);

                    Message.Type = ProtoComm.Message.Types.Type.EpfdRestore;
                    Message.EpfdRestore = new ProtoComm.EpfdRestore
                    {
                        Process = procId
                    };

                    _sistem.DeclansareEveniment(Message);
                }

                var sendMessage = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.PlSend,
                    PlSend = new ProtoComm.PlSend
                    {
                        Destination = procId,
                        Message = new ProtoComm.Message
                        {
                            Type = ProtoComm.Message.Types.Type.EpfdInternalHeartbeatRequest,
                            EpfdInternalHeartbeatRequest = new ProtoComm.EpfdInternalHeartbeatRequest(),
                            SystemId = _sistem.IDSistem,
                            ToAbstractionId = _IdAbstractie,
                            FromAbstractionId = _IdAbstractie,
                            MessageUuid = Guid.NewGuid().ToString()
                        }
                    },
                    SystemId = _sistem.IDSistem,
                    ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, PerfectLink.NumeAplicatie),
                    FromAbstractionId = _IdAbstractie,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _sistem.DeclansareEveniment(sendMessage);
            }

            _rulat.Clear();
            StartTimer();
        }

        private void StartTimer()
        {
            _timer.ProgramareTask(_intarziere);
        }
    }
}