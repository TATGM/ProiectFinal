using System.Collections.Generic;
using System.Linq;

namespace ProiectFinal.Utilități
{
    static class UtilitatiIDProcese
    {
        public static ProtoComm.ProcessId GasireRangMaxim(IEnumerable<ProtoComm.ProcessId> procese)
        {
            return (procese.Count() == 0) ? null : procese.OrderBy(procId => procId.Rank).LastOrDefault();
        }
    }
}
