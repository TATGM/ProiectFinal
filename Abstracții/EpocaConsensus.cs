using ProiectFinal.Utilități;
using System;
using System.Collections.Generic;

namespace ProiectFinal.Abstracții
{
    class StareEpocaConsensus
    {
        public int ValoareTimestamp;
        public ProtoComm.Value Valoare;
    }
    class EpocaConsensus : Abstractie
    {
        public static readonly string NumeAplicatie = "ep";
        private bool _oprit = false;

        private ProtoComm.ProcessId _lider;
        private int _epocaMarcaTemporala;
        private StareEpocaConsensus _stare;
        private ProtoComm.Value _tmpValoare = new ProtoComm.Value { Defined = false };
        private Dictionary<ProtoComm.ProcessId, StareEpocaConsensus> _stari = new Dictionary<ProtoComm.ProcessId, StareEpocaConsensus>();
        private int _acceptat = 0;

        public EpocaConsensus(string IDAbstractie, Sistem.Sistem sistem, ProtoComm.ProcessId leader, StareEpocaConsensus state, int epochTimestamp) : base(IDAbstractie, sistem)
        {
            _sistem.InregistrareAbstractie(new PerfectLink(UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, PerfectLink.NumeAplicatie), _sistem));
            _sistem.InregistrareAbstractie(new BestEffortBroadcast(UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, BestEffortBroadcast.NumeAplicatie), _sistem));

            _lider = leader;
            _epocaMarcaTemporala = epochTimestamp;
            _stare = state;
        }

        public override bool Manipulare(ProtoComm.Message mesaj)
        {
            if (_oprit)
                throw new EpocaConsensusExceptieOprire("EpocaConsensus a fost abandonată");

            if (mesaj.Type == ProtoComm.Message.Types.Type.EpPropose)
            {
                ManipularePropunereEpoca(mesaj);
                return true;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.BebDeliver)
            {
                if (mesaj.BebDeliver.Message.Type == ProtoComm.Message.Types.Type.EpInternalRead)
                {
                    ManipulareCitireEpoca(mesaj);
                    return true;
                }

                if (mesaj.BebDeliver.Message.Type == ProtoComm.Message.Types.Type.EpInternalWrite)
                {
                    ManipulareAfisareEpoca(mesaj);
                    return true;
                }

                if (mesaj.BebDeliver.Message.Type == ProtoComm.Message.Types.Type.EpInternalDecided)
                {
                    ManipulareDecizieEpoca(mesaj);
                    return true;
                }

                return false;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.PlDeliver)
            {
                if (mesaj.BebDeliver.Message.Type == ProtoComm.Message.Types.Type.EpInternalState)
                {
                    ManipulareStareEpoca(mesaj);
                    return true;
                }

                if (mesaj.BebDeliver.Message.Type == ProtoComm.Message.Types.Type.EpInternalAccept)
                {
                    ManipulareAcceptareEpoca();
                    return true;
                }

                return false;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.EpAbort)
            {
                ManipulareAbandonareEpoca();
                return true;
            }

            return false;
        }

        private void ManipulareAbandonareEpoca()
        {
            var outputMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.EpAborted,
                EpAborted = new ProtoComm.EpAborted
                {
                    EpochTimestamp = _epocaMarcaTemporala,
                    Value = _stare.Valoare,
                    ValueTimestamp = _stare.ValoareTimestamp
                },
                SystemId = _sistem.IDSistem,
                ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieParinte(_IdAbstractie),
                FromAbstractionId = _IdAbstractie,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _sistem.DeclansareEveniment(outputMessage);

            _oprit = true;
        }

        private void ManipulareDecizieEpoca(ProtoComm.Message mesaj)
        {
            var decidedMessage = mesaj.BebDeliver.Message.EpInternalDecided;

            var outputMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.EpDecide,
                EpDecide = new ProtoComm.EpDecide
                {
                    Ets = _epocaMarcaTemporala,
                    Value = decidedMessage.Value
                },
                SystemId = _sistem.IDSistem,
                ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieParinte(_IdAbstractie),
                FromAbstractionId = _IdAbstractie,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _sistem.DeclansareEveniment(outputMessage);
        }

        private void ManipulareAcceptareEpoca()
        {
            _acceptat += 1;

            ManipulareVerificareAcceptata();
        }

        private void ManipulareVerificareAcceptata()
        {
            if (_acceptat > (_sistem.Procese.Count / 2))
            {
                _acceptat = 0;

                var outputMessage = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.BebBroadcast,
                    BebBroadcast = new ProtoComm.BebBroadcast
                    {
                        Message = new ProtoComm.Message
                        {
                            Type = ProtoComm.Message.Types.Type.EpInternalDecided,
                            EpInternalDecided = new ProtoComm.EpInternalDecided
                            {
                                Value = _tmpValoare
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

        private void ManipulareAfisareEpoca(ProtoComm.Message mesaj)
        {
            var sender = mesaj.BebDeliver.Sender;
            var messageWrite = mesaj.BebDeliver.Message.EpInternalWrite;

            _stare = new StareEpocaConsensus
            {
                ValoareTimestamp = _epocaMarcaTemporala,
                Valoare = messageWrite.Value
            };

            var outputMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlSend,
                PlSend = new ProtoComm.PlSend
                {
                    Destination = sender,
                    Message = new ProtoComm.Message
                    {
                        Type = ProtoComm.Message.Types.Type.EpInternalAccept,
                        EpInternalAccept = new ProtoComm.EpInternalAccept(),
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

        private void ManipulareStareEpoca(ProtoComm.Message mesaj)
        {
            var sender = mesaj.PlDeliver.Sender;
            var stateMessage = mesaj.PlDeliver.Message.EpInternalState;

            _stari[sender] = new StareEpocaConsensus
            {
                Valoare = stateMessage.Value,
                ValoareTimestamp = stateMessage.ValueTimestamp
            };

            ManipulareVerificareStari();
        }

        private void ManipulareVerificareStari()
        {
            if (_stari.Count > (_sistem.Procese.Count / 2))
            {
                var highest = UtilitatiStareEpocaConsensus.GasireCelMaiMare(_stari.Values);
                if (highest == null)
                    return;

                if (highest.Valoare.Defined)
                {
                    _tmpValoare = highest.Valoare;
                }

                _stari.Clear();

                var outputMessage = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.BebBroadcast,
                    BebBroadcast = new ProtoComm.BebBroadcast
                    {
                        Message = new ProtoComm.Message
                        {
                            Type = ProtoComm.Message.Types.Type.EpInternalWrite,
                            EpInternalWrite = new ProtoComm.EpInternalWrite
                            {
                                Value = _tmpValoare
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

        private void ManipulareCitireEpoca(ProtoComm.Message mesaj)
        {
            var sender = mesaj.BebDeliver.Sender;

            var outputMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlSend,
                PlSend = new ProtoComm.PlSend
                {
                    Destination = sender,
                    Message = new ProtoComm.Message
                    {
                        Type = ProtoComm.Message.Types.Type.EpInternalState,
                        EpInternalState = new ProtoComm.EpInternalState
                        {
                            Value = _stare.Valoare,
                            ValueTimestamp = _stare.ValoareTimestamp
                        },
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

        private void ManipularePropunereEpoca(ProtoComm.Message mesaj)
        {
            _tmpValoare = mesaj.EpPropose.Value;

            var outputMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.BebBroadcast,
                BebBroadcast = new ProtoComm.BebBroadcast
                {
                    Message = new ProtoComm.Message
                    {
                        Type = ProtoComm.Message.Types.Type.EpInternalRead,
                        EpInternalRead = new ProtoComm.EpInternalRead(),
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

    class EpocaConsensusExceptieOprire : Exception
    {
        public EpocaConsensusExceptieOprire()
        {
        }

        public EpocaConsensusExceptieOprire(string message)
            : base(message)
        {
        }

        public EpocaConsensusExceptieOprire(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}