using ProiectFinal.Abstracții;

namespace ProiectFinal.Utilități
{
    static class UtilitatiIDAbstractie
    {
        public static string GetIDAbstractieParinte(string IDAbstractieOriginala)
        {
            if ((IDAbstractieOriginala == null) || (IDAbstractieOriginala == ""))
                return "";

            int pozitie = IDAbstractieOriginala.LastIndexOf('.');
            if (pozitie < 0)
                return IDAbstractieOriginala;

            return IDAbstractieOriginala[0..pozitie];
        }

        public static string GetIDAbstractieCopil(string IDAbstractieParinte, string IDAbstractieCopil)
        {
            if ((IDAbstractieParinte == null) || (IDAbstractieParinte == ""))
                return "";

            if ((IDAbstractieCopil == null) || (IDAbstractieCopil == ""))
                return IDAbstractieParinte;

            return IDAbstractieParinte + '.' + IDAbstractieCopil;
        }

        public static string GetIDAbstractieRegistruAtomicNN(string IDAbstractieParinte, string nnarId)
        {
            if ((IDAbstractieParinte == null) || (IDAbstractieParinte == ""))
                return "";

            return IDAbstractieParinte + '.' + RegistruAtomicNN.Nume + '[' + nnarId + ']';
        }

        public static string GetIDAbstractieConsensusUniform(string IDAbstractieParinte, string IDConsensusUniform)
        {
            if ((IDAbstractieParinte == null) || (IDAbstractieParinte == ""))
                return "";

            return IDAbstractieParinte + '.' + ConsensusUniform.NumeAplicatie + '[' + IDConsensusUniform + ']';
        }

        public static string GetIDAbstractieEpoca(string IDAbstractieParinte, int IDEpoca)
        {
            if ((IDAbstractieParinte == null) || (IDAbstractieParinte == ""))
                return "";

            return IDAbstractieParinte + '.' + EpocaConsensus.NumeAplicatie + '[' + IDEpoca.ToString() + ']';
        }

        public static string GetNumeRegistruAtomicNN(string IDAbstractieRegistruAtomicNN)
        {
            int IndiceCuvantCheieRegistruAtomic = IDAbstractieRegistruAtomicNN.IndexOf(RegistruAtomicNN.Nume);
            if (IndiceCuvantCheieRegistruAtomic < 0)
                return "";

            var IDSubsirRegistruAtomicNN = IDAbstractieRegistruAtomicNN.Substring(IndiceCuvantCheieRegistruAtomic);

            int IndicePornireDomeniuRegistruAtomicNN = RegistruAtomicNN.Nume.Length;
            if (IDSubsirRegistruAtomicNN[IndicePornireDomeniuRegistruAtomicNN] != '[')
                return "";

            int IndiceOprireDomeniuRegistruAtomicNN = IDSubsirRegistruAtomicNN.IndexOf(']');
            if (IndiceOprireDomeniuRegistruAtomicNN < 0)
                return "";

            return IDSubsirRegistruAtomicNN.Substring(IndicePornireDomeniuRegistruAtomicNN + 1, IndiceOprireDomeniuRegistruAtomicNN - IndicePornireDomeniuRegistruAtomicNN - 1);
        }
    }
}
