using ProiectFinal.Sistem;
using ProiectFinal.Utilități;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProiectFinal.Abstracții
{
    class PerfectLink : Abstractie
    {
        public static readonly string NumeAplicatie = "pl";

        public PerfectLink(string abstractionId, Sistem.Sistem system)
            : base(abstractionId, system)
        {
        }

        public override bool Manipulare(ProtoComm.Message mesaj)
        {
            if (mesaj.Type == ProtoComm.Message.Types.Type.NetworkMessage)
            {
                ManipulareFurnizarePerfectLink(mesaj);
                return true;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.PlSend)
            {
                ManipulareTrimiterePerfectLink(mesaj);
                return true;
            }

            return false;
        }

        private void ManipulareFurnizarePerfectLink(ProtoComm.Message mesaj)
        {
            var networkMessage = mesaj.NetworkMessage;

            var plDeliverMessage = new ProtoComm.PlDeliver
            {
                Message = networkMessage.Message
            };

            ProtoComm.ProcessId foundProcessId = _sistem.GasireProcesHostPort(networkMessage.SenderHost, networkMessage.SenderListeningPort);
            if (foundProcessId != null)
            {
                plDeliverMessage.Sender = foundProcessId;
            }

            var outputMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlDeliver,
                PlDeliver = plDeliverMessage,
                SystemId = _sistem.IDSistem,
                FromAbstractionId = _IdAbstractie,
                ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieParinte(mesaj.ToAbstractionId),
                MessageUuid = Guid.NewGuid().ToString()
            };

            _sistem.DeclansareEveniment(outputMessage);
        }

        private void ManipulareTrimiterePerfectLink(ProtoComm.Message mesaj)
        {
            var plSendMessage = mesaj.PlSend;

            var networkMessage = new ProtoComm.NetworkMessage
            {
                Message = plSendMessage.Message,
                SenderHost = _sistem.IDProces.Host,
                SenderListeningPort = _sistem.IDProces.Port
            };

            var outMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NetworkMessage,
                NetworkMessage = networkMessage,
                SystemId = _sistem.IDSistem,
                FromAbstractionId = _IdAbstractie,
                ToAbstractionId = mesaj.ToAbstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _sistem.TrimitereMesajPrinRetea(outMessage, plSendMessage.Destination.Host, plSendMessage.Destination.Port);
        }
    }
}
