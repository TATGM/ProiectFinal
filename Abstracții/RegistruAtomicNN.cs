using ProiectFinal.Utilități;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ProiectFinal.Abstracții
{
    class Entitate : IComparable<Entitate>
    {
        public int Timestamp { get; set; }
        public int WriterRank { get; set; }
        public ProtoComm.Value Value { get; set; }

        public static bool operator >(Entitate n1, Entitate n2)
        {
            if ((n1.Timestamp > n2.Timestamp) ||
                ((n1.Timestamp == n2.Timestamp) && (n1.WriterRank > n2.WriterRank)))
            {
                return true;
            }

            return false;
        }

        public static bool operator <(Entitate n1, Entitate n2)
        {
            if ((n1.Timestamp < n2.Timestamp) ||
                ((n1.Timestamp == n2.Timestamp) && (n1.WriterRank < n2.WriterRank)))
            {
                return true;
            }

            return false;
        }

        public int CompareTo([AllowNull] Entitate other)
        {
            if (other == null)
                return 1;

            if (this > other)
            {
                return 1;
            }
            else if (this < other)
            {
                return -1;
            }

            return 0;
        }
    }

    class RegistruAtomicNN : Abstractie
    {
        public static readonly string Nume = "nnar";

        private Entitate _entitate = new Entitate { Timestamp = 0, WriterRank = 0, Value = new ProtoComm.Value { Defined = false } };
        private int _acks = 0;
        private int _readId = 0;
        private ConcurrentDictionary<string, Entitate> _readList = new ConcurrentDictionary<string, Entitate>();
        private bool _isReading = false;

        private ProtoComm.Value _writeValue = new ProtoComm.Value { Defined = false };
        private ProtoComm.Value _readValue = new ProtoComm.Value { Defined = false };


        public RegistruAtomicNN(string abstractionId, Sistem.Sistem system)
            : base(abstractionId, system)
        {
            _sistem.InregistrareAbstractie(new PerfectLink(UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, PerfectLink.NumeAplicatie), _sistem));
            _sistem.InregistrareAbstractie(new BestEffortBroadcast(UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, BestEffortBroadcast.NumeAplicatie), _sistem));
        }

        public override bool Manipulare(ProtoComm.Message mesaj)
        {
            if (mesaj.Type == ProtoComm.Message.Types.Type.NnarRead)
            {
                CitireRegistruAtomicManipulat();
                return true;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.NnarWrite)
            {
                AfisareRegistruAtomicManipulat(mesaj);
                return true;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.BebDeliver)
            {
                if (mesaj.BebDeliver.Message.Type == ProtoComm.Message.Types.Type.NnarInternalRead)
                {
                    CitireLocalaRegistruAtomicManipulat(mesaj);
                    return true;
                }

                if (mesaj.BebDeliver.Message.Type == ProtoComm.Message.Types.Type.NnarInternalWrite)
                {
                    AfisareLocalaRegistruAtomicManipulat(mesaj);
                    return true;
                }

                return false;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.PlDeliver)
            {
                if (mesaj.PlDeliver.Message.Type == ProtoComm.Message.Types.Type.NnarInternalValue)
                {
                    var nnarInternalValueMessage = mesaj.PlDeliver.Message.NnarInternalValue;
                    if (nnarInternalValueMessage.ReadId == _readId)
                    {
                        ValoareLocalaRegistruAtomicManipulat(mesaj);
                        return true;
                    }
                    return false;
                }

                if (mesaj.PlDeliver.Message.Type == ProtoComm.Message.Types.Type.NnarInternalAck)
                {
                    var nnarInternalAckMessage = mesaj.PlDeliver.Message.NnarInternalAck;
                    if (nnarInternalAckMessage.ReadId == _readId)
                    {
                        ConfirmareLocalaRegistruAtomicManipulat(mesaj);
                        return true;
                    }
                    return false;
                }

                return false;
            }

            return false;
        }

        private Entitate GetValoareaMaxima()
        {
            return _readList.Values.Max();
        }

        private void ConfirmareLocalaRegistruAtomicManipulat(ProtoComm.Message mesaj)
        {
            _acks++;
            if (_acks > (_sistem.Procese.Count / 2))
            {
                var outputMessage = new ProtoComm.Message
                {
                    SystemId = _sistem.IDSistem,
                    ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieParinte(_IdAbstractie),
                    FromAbstractionId = _IdAbstractie,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _acks = 0;
                if (_isReading)
                {
                    _isReading = false;

                    outputMessage.Type = ProtoComm.Message.Types.Type.NnarReadReturn;
                    outputMessage.NnarReadReturn = new ProtoComm.NnarReadReturn
                    {
                        Value = _readValue
                    };
                }
                else
                {
                    outputMessage.Type = ProtoComm.Message.Types.Type.NnarWriteReturn;
                    outputMessage.NnarWriteReturn = new ProtoComm.NnarWriteReturn();
                }

                _sistem.DeclansareEveniment(outputMessage);
            }
        }

        private void ValoareLocalaRegistruAtomicManipulat(ProtoComm.Message mesaj)
        {
            var plDeliverMessage = mesaj.PlDeliver;

            var nnarInternalValueMessage = plDeliverMessage.Message.NnarInternalValue;

            var receivedEntitate = new Entitate
            {
                Timestamp = nnarInternalValueMessage.Timestamp,
                WriterRank = nnarInternalValueMessage.WriterRank,
                Value = nnarInternalValueMessage.Value
            };

            _readList[plDeliverMessage.Sender.Owner + '-' + plDeliverMessage.Sender.Index] = receivedEntitate;

            if (_readList.Count > (_sistem.Procese.Count / 2))
            {
                var maxEntitate = GetValoareaMaxima();
                _readValue = maxEntitate.Value;

                _readList.Clear();

                ProtoComm.NnarInternalWrite nnarInternalWriteMessage;
                if (_isReading)
                {
                    nnarInternalWriteMessage = new ProtoComm.NnarInternalWrite
                    {
                        ReadId = nnarInternalValueMessage.ReadId,
                        Timestamp = maxEntitate.Timestamp,
                        WriterRank = maxEntitate.WriterRank,
                        Value = maxEntitate.Value
                    };
                }
                else
                {
                    nnarInternalWriteMessage = new ProtoComm.NnarInternalWrite
                    {
                        ReadId = nnarInternalValueMessage.ReadId,
                        Timestamp = maxEntitate.Timestamp + 1,
                        WriterRank = _sistem.IDProces.Rank,
                        Value = _writeValue
                    };
                }

                var outputMessage = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.BebBroadcast,
                    BebBroadcast = new ProtoComm.BebBroadcast
                    {
                        Message = new ProtoComm.Message
                        {
                            Type = ProtoComm.Message.Types.Type.NnarInternalWrite,
                            NnarInternalWrite = nnarInternalWriteMessage,
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

        private void AfisareLocalaRegistruAtomicManipulat(ProtoComm.Message mesaj)
        {
            var bebDeliverMessage = mesaj.BebDeliver;
            var nnarInternalWriteMessage = bebDeliverMessage.Message.NnarInternalWrite;

            var receivedEntity = new Entitate
            {
                Timestamp = nnarInternalWriteMessage.Timestamp,
                WriterRank = nnarInternalWriteMessage.WriterRank,
                Value = nnarInternalWriteMessage.Value
            };

            if (_entitate > receivedEntity)
            {
                _entitate = receivedEntity;
            }

            var plSendMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NnarInternalAck,
                NnarInternalAck = new ProtoComm.NnarInternalAck
                {
                    ReadId = nnarInternalWriteMessage.ReadId
                },
                SystemId = _sistem.IDSistem,
                ToAbstractionId = _IdAbstractie,
                FromAbstractionId = _IdAbstractie,
                MessageUuid = Guid.NewGuid().ToString()
            };

            var messageOutput = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlSend,
                PlSend = new ProtoComm.PlSend
                {
                    Message = plSendMessage,
                    Destination = bebDeliverMessage.Sender
                },
                SystemId = _sistem.IDSistem,
                ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, PerfectLink.NumeAplicatie),
                FromAbstractionId = _IdAbstractie,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _sistem.DeclansareEveniment(messageOutput);
        }

        private void CitireLocalaRegistruAtomicManipulat(ProtoComm.Message mesaj)
        {
            var bebDeliverMessage = mesaj.BebDeliver;
            var nnarInternalReadMessage = bebDeliverMessage.Message.NnarInternalRead;

            var plSendMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NnarInternalValue,
                NnarInternalValue = new ProtoComm.NnarInternalValue
                {
                    ReadId = nnarInternalReadMessage.ReadId,
                    Timestamp = _entitate.Timestamp,
                    WriterRank = _entitate.WriterRank,
                    Value = _entitate.Value
                },
                SystemId = _sistem.IDSistem,
                ToAbstractionId = _IdAbstractie,
                FromAbstractionId = _IdAbstractie,
                MessageUuid = Guid.NewGuid().ToString()
            };

            var messageOutput = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlSend,
                PlSend = new ProtoComm.PlSend
                {
                    Message = plSendMessage,
                    Destination = bebDeliverMessage.Sender
                },
                SystemId = _sistem.IDSistem,
                ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, PerfectLink.NumeAplicatie),
                FromAbstractionId = _IdAbstractie,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _sistem.DeclansareEveniment(messageOutput);
        }

        private void CitireRegistruAtomicManipulat()
        {
            _readId++;
            _acks = 0;
            _readList.Clear();
            _isReading = true;

            var nnarInternalReadMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NnarInternalRead,
                NnarInternalRead = new ProtoComm.NnarInternalRead
                {
                    ReadId = _readId
                },
                SystemId = _sistem.IDSistem,
                ToAbstractionId = _IdAbstractie,
                FromAbstractionId = _IdAbstractie,
                MessageUuid = Guid.NewGuid().ToString()
            };

            var messageOutput = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.BebBroadcast,
                BebBroadcast = new ProtoComm.BebBroadcast
                {
                    Message = nnarInternalReadMessage
                },
                SystemId = _sistem.IDSistem,
                ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, BestEffortBroadcast.NumeAplicatie),
                FromAbstractionId = _IdAbstractie,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _sistem.DeclansareEveniment(messageOutput);
        }

        private void AfisareRegistruAtomicManipulat(ProtoComm.Message mesaj)
        {
            var nnarWriteMessage = mesaj.NnarWrite;

            _readId++;
            _writeValue = new ProtoComm.Value { Defined = true, V = nnarWriteMessage.Value.V };
            _acks = 0;
            _readList.Clear();

            var nnarInternalReadMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NnarInternalRead,
                NnarInternalRead = new ProtoComm.NnarInternalRead
                {
                    ReadId = _readId
                },
                SystemId = _sistem.IDSistem,
                ToAbstractionId = _IdAbstractie,
                FromAbstractionId = _IdAbstractie,
                MessageUuid = Guid.NewGuid().ToString()
            };

            var messageOutput = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.BebBroadcast,
                BebBroadcast = new ProtoComm.BebBroadcast
                {
                    Message = nnarInternalReadMessage
                },
                SystemId = _sistem.IDSistem,
                ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, BestEffortBroadcast.NumeAplicatie),
                FromAbstractionId = _IdAbstractie,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _sistem.DeclansareEveniment(messageOutput);
        }
    }
}