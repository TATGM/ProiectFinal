using System;
using ProiectFinal.Utilități;

namespace ProiectFinal.Abstracții
{
    class SchimbareEpoca : Abstractie
    {
        public static readonly string NumeAplicatie = "ec";

        private ProtoComm.ProcessId _deincredere;
        private int _ultimaMarcaTemporala = 0;
        private int _marcaTemporala;

        public SchimbareEpoca(string idAbstractie, Sistem.Sistem sistem) : base(idAbstractie, sistem)
        {
            _sistem.InregistrareAbstractie(new PerfectLink(UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, PerfectLink.NumeAplicatie), _sistem));
            _sistem.InregistrareAbstractie(new BestEffortBroadcast(UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, BestEffortBroadcast.NumeAplicatie), _sistem));
            _sistem.InregistrareAbstractie(new DetectorLiderEventual(UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, DetectorLiderEventual.NumeAplicatie), _sistem));

            _deincredere = UtilitatiIDProcese.GasireRangMaxim(_sistem.Procese);
            _marcaTemporala = _sistem.IDProces.Rank;
        }

        public override bool Manipulare(ProtoComm.Message mesaj)
        {
            if (mesaj.Type == ProtoComm.Message.Types.Type.EldTrust)
            {
                ManipulareDispozitivIncredere(mesaj);
                return true;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.BebDeliver)
            {
                if (mesaj.BebDeliver.Message.Type == ProtoComm.Message.Types.Type.EcInternalNewEpoch)
                {
                    ManipulareEpocaNoua(mesaj); return true;
                }
                return false;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.PlDeliver)
            {
                if (mesaj.Type == ProtoComm.Message.Types.Type.EcInternalNack)
                {
                    ManipulareNRecunoastere(); return true;
                }
                return false;
            }

            return false;
        }

        private void ManipulareNRecunoastere()
        {
            if (_deincredere.Equals(_sistem.IDProces))
            {
                _marcaTemporala += _sistem.Procese.Count;
                var outputMessage = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.BebBroadcast,
                    BebBroadcast = new ProtoComm.BebBroadcast
                    {
                        Message = new ProtoComm.Message
                        {
                            Type = ProtoComm.Message.Types.Type.EcInternalNewEpoch,
                            EcInternalNewEpoch = new ProtoComm.EcInternalNewEpoch
                            {
                                Timestamp = _marcaTemporala
                            },
                            SystemId = _sistem.IDSistem,
                            ToAbstractionId = _IdAbstractie,
                            FromAbstractionId = _IdAbstractie,
                            MessageUuid = Guid.NewGuid().ToString()
                        }
                    },
                    SystemId = _sistem.IDSistem,
                    ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, BestEffortBroadcast.NumeAplicatie),
                    FromAbstractionId = _IdAbstractie,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _sistem.DeclansareEveniment(outputMessage);
            }
        }

        private void ManipulareEpocaNoua(ProtoComm.Message mesaj)
        {
            var sender = mesaj.BebDeliver.Sender;
            var newEpochMessage = mesaj.BebDeliver.Message.EcInternalNewEpoch;
            var newTimestamp = newEpochMessage.Timestamp;

            if (sender.Equals(_deincredere) && newTimestamp > _ultimaMarcaTemporala)
            {
                _ultimaMarcaTemporala = newTimestamp;
                var outputMessage = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.EcStartEpoch,
                    EcStartEpoch = new ProtoComm.EcStartEpoch
                    {
                        NewLeader = sender,
                        NewTimestamp = newTimestamp
                    },
                    SystemId = _sistem.IDSistem,
                    ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieParinte(_IdAbstractie),
                    FromAbstractionId = _IdAbstractie,
                    MessageUuid = Guid.NewGuid().ToString()
                };
                _sistem.DeclansareEveniment(outputMessage);
            }
            else
            {
                var outputMessage = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.PlSend,
                    PlSend = new ProtoComm.PlSend
                    {
                        Destination = sender,
                        Message = new ProtoComm.Message
                        {
                            Type = ProtoComm.Message.Types.Type.EcInternalNack,
                            EcInternalNack = new ProtoComm.EcInternalNack(),
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

                _sistem.DeclansareEveniment(outputMessage);
            }
        }

        private void ManipulareDispozitivIncredere(ProtoComm.Message mesaj)
        {
            _deincredere = mesaj.EldTrust.Process;
            if (_deincredere.Equals(_sistem.IDProces))
            {
                _marcaTemporala += _sistem.Procese.Count;
                var outputMessage = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.BebBroadcast,
                    BebBroadcast = new ProtoComm.BebBroadcast
                    {
                        Message = new ProtoComm.Message
                        {
                            Type = ProtoComm.Message.Types.Type.EcInternalNewEpoch,
                            EcInternalNewEpoch = new ProtoComm.EcInternalNewEpoch
                            {
                                Timestamp = _marcaTemporala
                            },
                            SystemId = _sistem.IDSistem,
                            ToAbstractionId = _IdAbstractie,
                            FromAbstractionId = _IdAbstractie,
                            MessageUuid = Guid.NewGuid().ToString()
                        }
                    },
                    SystemId = _sistem.IDSistem,
                    ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, BestEffortBroadcast.NumeAplicatie),
                    FromAbstractionId = _IdAbstractie,
                    MessageUuid = Guid.NewGuid().ToString()
                };
                _sistem.DeclansareEveniment(outputMessage);
            }
        }
    }
}