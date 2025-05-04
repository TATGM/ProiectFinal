using ProiectFinal.Sistem;
using ProiectFinal.Utilități;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProiectFinal.Abstracții
{
    class BestEffortBroadcast : Abstractie
    {
        public static readonly string NumeAplicatie = "beb";

        public BestEffortBroadcast(string abstractionId, Sistem.Sistem system) : base(abstractionId, system)
        {
            _sistem.InregistrareAbstractie(new PerfectLink(UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, PerfectLink.NumeAplicatie), _sistem));
        }

        public override bool Manipulare(ProtoComm.Message mesaj)
        {
            if (mesaj.Type == ProtoComm.Message.Types.Type.BebBroadcast)
            {
                ManipulareBroadcastBeb(mesaj);
                return true;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.PlDeliver)
            {
                ManipulareFurnizarePerfectLink(mesaj);
                return true;
            }

            return false;
        }

        private void ManipulareBroadcastBeb(ProtoComm.Message mesaj)
        {
            var bebBroadcastMessage = mesaj.BebBroadcast;

            foreach (var proces in _sistem.Procese)
            {
                var PerfectLinkSendMessage = new ProtoComm.PlSend
                {
                    Destination = proces,
                    Message = bebBroadcastMessage.Message
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

        private void ManipulareFurnizarePerfectLink(ProtoComm.Message mesaj)
        {
            var PerfectLinkDeliverMessage = mesaj.PlDeliver;
            var InputMessage = PerfectLinkDeliverMessage.Message;

            var OutputMessage = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.BebDeliver,
                BebDeliver = new ProtoComm.BebDeliver
                {
                    Message = InputMessage,
                    Sender = PerfectLinkDeliverMessage.Sender
                },
                SystemId = _sistem.IDSistem,
                ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieParinte(_IdAbstractie),
                FromAbstractionId = _IdAbstractie,
                MessageUuid = Guid.NewGuid().ToString()
            };
            _sistem.DeclansareEveniment(OutputMessage);
        }
    }
}