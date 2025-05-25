using System;
using System.Collections.Generic;
using System.Linq;
using ProiectFinal.Utilități;

namespace ProiectFinal.Abstracții
{
    class DetectorLiderEventual : Abstractie
    {
        public static readonly string NumeAplicatie = "eld";

        private HashSet<ProtoComm.ProcessId> _suspectat = new HashSet<ProtoComm.ProcessId>();
        private ProtoComm.ProcessId _lider;

        public DetectorLiderEventual(string IdAbstractie, Sistem.Sistem sistem) : base(IdAbstractie, sistem)
        {
            _sistem.InregistrareAbstractie(new DetectorEventualPerfectEsec(UtilitatiIDAbstractie.GetIDAbstractieCopil(_IdAbstractie, DetectorEventualPerfectEsec.NumeAplicatie), _sistem));
        }

        public override bool Manipulare(ProtoComm.Message mesaj)
        {
            if (mesaj.Type == ProtoComm.Message.Types.Type.EpfdSuspect)
            {
                ManipulareSuspectareEPFD(mesaj);
                return true;
            }

            if (mesaj.Type == ProtoComm.Message.Types.Type.EpfdRestore)
            {
                ManipulareReluareEPFD(mesaj);
                return true;
            }
            return false;
        }

        private void ManipulareReluareEPFD(ProtoComm.Message mesaj)
        {
            var IDProces = mesaj.EpfdRestore.Process;
            _suspectat.Remove(IDProces);
            ManipulareVerificareInterna();
        }
        private void ManipulareSuspectareEPFD(ProtoComm.Message mesaj)
        {
            var IDProces = mesaj.EpfdSuspect.Process;
            _suspectat.Add(IDProces);
            ManipulareVerificareInterna();
        }

        private void ManipulareVerificareInterna()
        {
            var tmpLeader = UtilitatiIDProcese.GasireRangMaxim(_sistem.Procese.Except(_suspectat));
            if (tmpLeader == null)
                return;

            if (!tmpLeader.Equals(_lider))
            {
                _lider = tmpLeader;
                var mesaj = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.EldTrust,
                    EldTrust = new ProtoComm.EldTrust
                    {
                        Process = _lider
                    },
                    SystemId = _sistem.IDSistem,
                    ToAbstractionId = UtilitatiIDAbstractie.GetIDAbstractieParinte(_IdAbstractie),
                    FromAbstractionId = _IdAbstractie,
                    MessageUuid = Guid.NewGuid().ToString()
                };
                _sistem.DeclansareEveniment(mesaj);
            }
        }
    }
}