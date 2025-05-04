using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ProiectFinal.Rețelistică
{
    class ManipulatorRetea
    {
        private static readonly NLog.Logger registrator_date = NLog.LogManager.GetCurrentClassLogger();

        private readonly string _hostProces;
        private readonly int _portProces;

        private ManualResetEvent _pregatireReceptor = new ManualResetEvent(false);
        private CancellationToken _tokenAnulare;
        private CancellationTokenSource _sursaTokenAnulare = new CancellationTokenSource();

        public delegate void Notificare(ManipulatorRetea editor, byte[] mesajSerializat);
        public event Notificare LaPublicare;

        private bool _ruleaza;

        public ManipulatorRetea(string processHost, int processPort)
        {
            _hostProces = processHost;
            _portProces = processPort;
        }

        public void TrimiteMesaj(byte[] mesajSerializat, string remoteHost, int remotePort)
        {
            byte[] BigByteOrderMessageLength = BitConverter.GetBytes(mesajSerializat.Length);
            Array.Reverse(BigByteOrderMessageLength);

            try
            {
                using (var connection = new TcpClient(remoteHost, remotePort))
                {
                    using (var networkStream = connection.GetStream())
                    {
                        using (var writer = new BinaryWriter(networkStream))
                        {
                            writer.Write(BigByteOrderMessageLength);
                            writer.Write(mesajSerializat);
                        }
                    }
                }
            }
            catch (Exception exceptie)
            {
                throw new ExceptieRetea($"O excepție a avut loc în TrimiteMesaj", exceptie);
            }
        }

        public void ReceptarePentruConexiuni()
        {
            _ruleaza = true;
            registrator_date.Debug($"[{_portProces}]: Așteptare pentru cereri");

            var adresa = IPAddress.Parse(_hostProces);
            TcpListener _receptor = null;

            try
            {
                _receptor = new TcpListener(adresa, _portProces);
                _receptor.Start();
                _tokenAnulare = _sursaTokenAnulare.Token;

                while(!_tokenAnulare.IsCancellationRequested)
                {
                    _pregatireReceptor.Reset();

                    try
                    {
                        _receptor.BeginAcceptTcpClient(new AsyncCallback(ConectareProces), _receptor);
                    }
                    catch (SocketException)
                    {
                        registrator_date.Error($"[{_portProces}]: Conexiunea nu a putut fi manipulată. Conexiune ignorată");
                        continue;
                    }

                    _pregatireReceptor.WaitOne();
                }
            }
            catch(SocketException exceptie)
            {
                throw new ExceptieRetea("Excepție în receptor", exceptie);
            }
            finally
            {
                _receptor?.Stop();
                registrator_date.Debug($"[{_portProces}]: Receptor oprit");
                _ruleaza = false;
            }
        }

        public void OprireReceptor()
        {
            if (!_ruleaza)
                return;

            if (_tokenAnulare.IsCancellationRequested)
                return;

            _sursaTokenAnulare?.Cancel();
            _pregatireReceptor.Set();
            registrator_date.Debug($"[{_portProces}]: Cerere oprită");
        }

        private void ConectareProces(IAsyncResult rezultatAsincron)
        {
            if (_tokenAnulare.IsCancellationRequested)
                return;

            var listener = rezultatAsincron.AsyncState as TcpListener;
            if (listener == null)
                return;

            _pregatireReceptor.Set();

            try
            {
                using (var connection = listener.EndAcceptTcpClient(rezultatAsincron))
                {
                    registrator_date.Trace($"[{_portProces}]: Nouă conexiune acceptată");
                    if (LaPublicare == null)
                        return;

                    using (var networkStream = connection.GetStream())
                    {
                        using (var reader = new BinaryReader(networkStream))
                        {
                            var messageLengthArray = reader.ReadBytes(4);
                            Array.Reverse(messageLengthArray);
                            int messageSize = BitConverter.ToInt32(messageLengthArray, 0);

                            byte[] mesajSerializat = reader.ReadBytes(messageSize);
                            if (messageSize != mesajSerializat.Length)
                            {
                                registrator_date.Error($"[{_portProces}]: Mesaj malformat primit: [{mesajSerializat.Length}/{messageSize}]. Mesaj ignorat");
                                return;
                            }

                            if (LaPublicare != null)
                                LaPublicare(this, mesajSerializat);
                        }
                    }
                }
            }
            catch (Exception)
            {
                registrator_date.Error($"[{_portProces}]: Nu s-a primit mesaj de la conexiunea care urmează. Mesaj ignorat");
            }
        }
    }
}
