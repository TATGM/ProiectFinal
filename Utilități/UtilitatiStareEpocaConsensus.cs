using ProiectFinal.Abstracții;
using System.Collections.Generic;
using System.Linq;

namespace ProiectFinal.Utilități
{
    static class UtilitatiStareEpocaConsensus
    {
        public static StareEpocaConsensus GasireCelMaiMare(IEnumerable<StareEpocaConsensus> stari)
        {
            return (stari.Count() == 0) ? null : stari.OrderBy(stare => stare.ValoareTimestamp).Last();
        }
    }
}