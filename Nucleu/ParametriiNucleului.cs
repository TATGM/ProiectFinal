using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProiectFinal.Nucleu
{
    internal class ParametriiNucleului
    {
        public string Proprietar { get; set; }
        public string HostProcese { get; set; }
        public List<int> PorturiProcese { get; set; }

        public string IDSistem { get; set; }
        public string HostHub { get; set; }
        public int PortHub { get; set; }
    }
}
