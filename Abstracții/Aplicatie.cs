using ProiectFinal.Utilități;
using System;

namespace ProiectFinal.Abstracții
{
    class Aplicatie : Abstractie
    {
        public static readonly string NumeAplicatie = "app";

        public Aplicatie(string abstractionId, Sistem.Sistem system)
            : base(abstractionId, system)
        {
            _sistem.InregistrareAbstractie(new PerfectLink(UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, PerfectLink.NumeAplicatie), _sistem));
            _sistem.InregistrareAbstractie(new BestEffortBroadcast(UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, BestEffortBroadcast.NumeAplicatie), _sistem));
        }

        public override bool Manipulare(ProtoComm.Message mesaj)
        {
            if (mesaj.Type == ProtoComm.Message.Types.Type.PlDeliver)
            {
                var innerMessage = mesaj.PlDeliver.Message;
                if (innerMessage.Type == ProtoComm.Message.Types.Type.AppBroadcast)
                {
                    ManipulareAplicatieBroadcast(innerMessage);
                    return true;
                }

                if (innerMessage.Type == ProtoComm.Message.Types.Type.AppRead)
                {
                    ManipulareAplicatieCitire(innerMessage);
                    return true;
                }

                if (innerMessage.Type == ProtoComm.Message.Types.Type.AppWrite)
                {
                    ManipulareAplicatieAfisare(innerMessage);
                    return true;
                }

                return false;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.BebDeliver)
            {
                ManipulareBroadcastTrimitere(mesaj);
                return true;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.NnarReadReturn)
            {
                ManipulareRegistruAtomicCitireReturnat(mesaj);
                return true;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.NnarWriteReturn)
            {
                ManipulareRegistruAtomicAfisareReturnat(mesaj);
                return true;
            }

            return false;
        }

        private void ManipulareAplicatieCitire(ProtoComm.Message mesaj)
        {
            var appReadMessage = mesaj.AppRead;

            string nnarAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieRegistruAtomicNN(_IdAbstractie, appReadMessage.Register);
            _sistem.InregistrareAbstractie(new RegistruAtomicNN(nnarAbstractionId, _sistem));

            var nnarReadMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NnarRead,
                NnarRead = new ProtoComm.NnarRead(),
                SystemId = _sistem.IDSistem,
                FromAbstractionId = _IdAbstractie,
                ToAbstractionId = nnarAbstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _sistem.DeclansareEveniment(nnarReadMessage);
        }

        private void ManipulareRegistruAtomicAfisareReturnat(ProtoComm.Message mesaj)
        {
            var registerName = UtilitatiIDAbstractie.GetNumeRegistruAtomicNN(mesaj.FromAbstractionId);

            var PerfectLinkSendMessage = new ProtoComm.PlSend
            {
                Destination = _sistem.IDProcesHub,
                Message = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.AppWriteReturn,
                    AppWriteReturn = new ProtoComm.AppWriteReturn
                    {
                        Register = registerName
                    },
                    SystemId = _sistem.IDSistem,
                    FromAbstractionId = _IdAbstractie,
                    MessageUuid = Guid.NewGuid().ToString()
                }
            };

            var outputMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlSend,
                PlSend = PerfectLinkSendMessage,
                SystemId = _sistem.IDSistem,
                FromAbstractionId = _IdAbstractie,
                ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, PerfectLink.NumeAplicatie),
                MessageUuid = Guid.NewGuid().ToString()
            };

            _sistem.DeclansareEveniment(outputMessage);
        }

        private void ManipulareRegistruAtomicCitireReturnat(ProtoComm.Message mesaj)
        {
            var nnarReadReturnMessage = mesaj.NnarReadReturn;

            var registerName = UtilitatiIDAbstractie.GetNumeRegistruAtomicNN(mesaj.FromAbstractionId);

            var PerfectLinkSendMessage = new ProtoComm.PlSend
            {
                Destination = _sistem.IDProcesHub,
                Message = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.AppReadReturn,
                    AppReadReturn = new ProtoComm.AppReadReturn
                    {
                        Register = registerName,
                        Value = nnarReadReturnMessage.Value
                    },
                    SystemId = _sistem.IDSistem,
                    FromAbstractionId = _IdAbstractie,
                    MessageUuid = Guid.NewGuid().ToString()
                }
            };

            var outputMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlSend,
                PlSend = PerfectLinkSendMessage,
                SystemId = _sistem.IDSistem,
                FromAbstractionId = _IdAbstractie,
                ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, PerfectLink.NumeAplicatie),
                MessageUuid = Guid.NewGuid().ToString()
            };

            _sistem.DeclansareEveniment(outputMessage);
        }

        private void ManipulareAplicatieAfisare(ProtoComm.Message mesaj)
        {
            var appWriteMessage = mesaj.AppWrite;

            string nnarAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieRegistruAtomicNN(_IdAbstractie, appWriteMessage.Register);
            _sistem.InregistrareAbstractie(new RegistruAtomicNN(nnarAbstractionId, _sistem));

            var nnarWriteMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NnarWrite,
                NnarWrite = new ProtoComm.NnarWrite
                {
                    Value = appWriteMessage.Value
                },
                SystemId = _sistem.IDSistem,
                FromAbstractionId = _IdAbstractie,
                ToAbstractionId = nnarAbstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _sistem.DeclansareEveniment(nnarWriteMessage);
        }

        private void ManipulareAplicatieBroadcast(ProtoComm.Message mesaj)
        {
            var appBroadcastMessage = mesaj.AppBroadcast;

            var appValueMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.AppValue,
                AppValue = new ProtoComm.AppValue { Value = appBroadcastMessage.Value },
                SystemId = _sistem.IDSistem,
                FromAbstractionId = _IdAbstractie,
                ToAbstractionId = _IdAbstractie,
                MessageUuid = Guid.NewGuid().ToString()
            };

            var bebBroadcastMessage = new ProtoComm.BebBroadcast
            {
                Message = appValueMessage
            };

            var outputMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.BebBroadcast,
                BebBroadcast = bebBroadcastMessage,
                SystemId = _sistem.IDSistem,
                FromAbstractionId = _IdAbstractie,
                ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, BestEffortBroadcast.NumeAplicatie),
                MessageUuid = Guid.NewGuid().ToString()
            };

            _sistem.DeclansareEveniment(outputMessage);
        }

        private void ManipulareBroadcastTrimitere(ProtoComm.Message mesaj)
        {
            var bebDeliverMessage = mesaj.BebDeliver;
            var innerMessage = bebDeliverMessage.Message;

            var PerfectLinkSendMessage = new ProtoComm.PlSend
            {
                Destination = _sistem.IDProcesHub,
                Message = innerMessage
            };

            var outputMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlSend,
                PlSend = PerfectLinkSendMessage,
                SystemId = _sistem.IDSistem,
                FromAbstractionId = _IdAbstractie,
                ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, PerfectLink.NumeAplicatie),
                MessageUuid = Guid.NewGuid().ToString()
            };

            _sistem.DeclansareEveniment(outputMessage);
        }
    }
}