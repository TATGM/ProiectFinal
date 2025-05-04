namespace ProiectFinal.Abstracții
{
    abstract class Abstractie
    {
        protected string _IdAbstractie;
        protected Sistem.Sistem _sistem;

        protected Abstractie(string abstractionId, Sistem.Sistem system)
        {
            _IdAbstractie = abstractionId;
            _sistem = system;
        }

        public abstract bool Manipulare(ProtoComm.Message msg);

        public string GetId()
        {
            return _IdAbstractie;
        }
    }
}
