using ProiectFinal.Abstracții;

namespace ProiectFinal.Utilități
{
    static class UtilitatiIDAbstractie
    {
        public static string GetIDAbstractieParinte(string originalAbstractionId)
        {
            if ((originalAbstractionId == null) || (originalAbstractionId == ""))
                return "";

            int pos = originalAbstractionId.LastIndexOf('.');
            if (pos < 0)
                return originalAbstractionId;

            return originalAbstractionId[0..pos];
        }

        public static string GetIDAbstractieCopil(string parentAbstractionId, string childAbstractionId)
        {
            if ((parentAbstractionId == null) || (parentAbstractionId == ""))
                return "";

            if ((childAbstractionId == null) || (childAbstractionId == ""))
                return parentAbstractionId;

            return parentAbstractionId + '.' + childAbstractionId;
        }

        public static string GetIDAbstractieRegistruAtomicNN(string parentAbstractionId, string nnarId)
        {
            if ((parentAbstractionId == null) || (parentAbstractionId == ""))
                return "";

            return parentAbstractionId + '.' + RegistruAtomicNN.Nume + '[' + nnarId + ']';
        }

        public static string GetNumeRegistruAtomicNN(string nnarAbstractionId)
        {
            int nnarKeywordIndex = nnarAbstractionId.IndexOf(RegistruAtomicNN.Nume);
            if (nnarKeywordIndex < 0)
                return "";

            var nnarIdSubstring = nnarAbstractionId.Substring(nnarKeywordIndex);

            int openingNnarScopeIndex = RegistruAtomicNN.Nume.Length;
            if (nnarIdSubstring[openingNnarScopeIndex] != '[')
                return "";

            int closingNnarScopeIndex = nnarIdSubstring.IndexOf(']');
            if (closingNnarScopeIndex < 0)
                return "";

            return nnarIdSubstring.Substring(openingNnarScopeIndex + 1, closingNnarScopeIndex - openingNnarScopeIndex - 1);
        }
    }
}
