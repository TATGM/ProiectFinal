using ProiectFinal.Utilități;
using System;

namespace ProiectFinal.Abstracții
{
    class ConsensusUniform : Abstractie
    {
        public static readonly string NumeAplicatie = "uc";
        ProtoComm.Value _valoare = new ProtoComm.Value { Defined = false };
        bool _propus = false;
        bool _decis = false;
        ProtoComm.ProcessId _lider;
        int _epocaMarcaTemporala = 0;
        ProtoComm.ProcessId _nouLider;
        int _nouaEpocaMarcaTemporala = 0;

        public ConsensusUniform(string IdAbstractie, Sistem.Sistem sistem) : base(IdAbstractie, sistem)
        {
            _sistem.InregistrareAbstractie(new SchimbareEpoca(UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, SchimbareEpoca.NumeAplicatie), _sistem));

            _lider = UtilitatiIDProcese.GasireRangMaxim(_sistem.Procese);

            StareEpocaConsensus StareaZeroEpoca = new StareEpocaConsensus
            {
                Valoare = new ProtoComm.Value { Defined = false },
                ValoareTimestamp = 0
            };

            _sistem.InregistrareAbstractie(new EpocaConsensus(UtilitatiIDAbstractie.GetIDAbstractieEpoca(_IdAbstractie, _epocaMarcaTemporala), _sistem, _lider, StareaZeroEpoca, _epocaMarcaTemporala));
        }

        public override bool Manipulare(ProtoComm.Message mesaj)
        {
            if (mesaj.Type == ProtoComm.Message.Types.Type.UcPropose)
            {
                ManipularePropunereUC(mesaj);
                return true;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.EcStartEpoch)
            {
                ManipulareRulareEpocaEC(mesaj);
                return true;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.EpAborted)
            {
                var epAbortedMessage = mesaj.EpAborted;
                if (epAbortedMessage.EpochTimestamp == _epocaMarcaTemporala)
                {
                    ManipulareAbandonareEpoca(mesaj);
                    return true;
                }
                return false;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.EpDecide)
            {
                var epDecideMessage = mesaj.EpDecide;
                if (epDecideMessage.Ets == _epocaMarcaTemporala)
                {
                    ManipulareDecideEpoca(mesaj);
                    return true;
                }
                return false;
            }

            return false;
        }

        private void ManipulareDecideEpoca(ProtoComm.Message mesaj)
        {
            var epDecideMessage = mesaj.EpDecide;

            if (!_decis)
            {
                _decis = true;

                var outputMessage = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.UcDecide,
                    UcDecide = new ProtoComm.UcDecide
                    {
                        Value = epDecideMessage.Value
                    },
                    SystemId = _sistem.IDSistem,
                    ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieParinte(_IdAbstractie),
                    FromAbstractionId = _IdAbstractie,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _sistem.DeclansareEveniment(outputMessage);
            }
        }

        private void ManipulareVerificareInterna()
        {
            if (_sistem.IDProces.Equals(_lider) && _valoare.Defined && !_propus)
            {
                _propus = true;

                var outputMessage = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.EpPropose,
                    EpPropose = new ProtoComm.EpPropose
                    {
                        Value = _valoare
                    },
                    SystemId = _sistem.IDSistem,
                    ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieEpoca(_IdAbstractie, _epocaMarcaTemporala),
                    FromAbstractionId = _IdAbstractie,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _sistem.DeclansareEveniment(outputMessage);
            }
        }

        private void ManipulareAbandonareEpoca(ProtoComm.Message mesaj)
        {
            var epAbortedMessage = mesaj.EpAborted;

            _epocaMarcaTemporala = _nouaEpocaMarcaTemporala;
            _lider = _nouLider;
            _propus = false;

            StareEpocaConsensus stareEpoca = new StareEpocaConsensus
            {
                Valoare = epAbortedMessage.Value,
                ValoareTimestamp = epAbortedMessage.ValueTimestamp
            };

            _sistem.InregistrareAbstractie(new EpocaConsensus(UtilitatiIDAbstractie.GetIDAbstractieEpoca(_IdAbstractie, _epocaMarcaTemporala), _sistem, _lider, stareEpoca, _epocaMarcaTemporala));

            ManipulareVerificareInterna();
        }

        private void ManipulareRulareEpocaEC(ProtoComm.Message mesaj)
        {
            var ecStartEpochMessage = mesaj.EcStartEpoch;

            _nouaEpocaMarcaTemporala = ecStartEpochMessage.NewTimestamp;
            _nouLider = ecStartEpochMessage.NewLeader;

            var outputMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.EpAbort,
                EpAbort = new ProtoComm.EpAbort(),
                SystemId = _sistem.IDSistem,
                ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieEpoca(_IdAbstractie, _epocaMarcaTemporala),
                FromAbstractionId = _IdAbstractie,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _sistem.DeclansareEveniment(outputMessage);
        }

        private void ManipularePropunereUC(ProtoComm.Message mesaj)
        {
            _valoare = mesaj.UcPropose.Value;

            ManipulareVerificareInterna();
        }
    }
}