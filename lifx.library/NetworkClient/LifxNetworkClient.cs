using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace lifx.Library.NetworkClient
{
    internal sealed class PacketReceivedEventArgs : EventArgs
    {
        public LIFXProtocolPacket Packet { get; internal set; }
        public string RemoteAddress { get; internal set; }
    }

    internal class LifxNetworkClient
    {
        private static NLog.Logger __logger = NLog.LogManager.GetCurrentClassLogger();

        private const int PORT = 56700;
        private UdpClient _socket;
        private bool _isRunning;

        public event EventHandler<PacketReceivedEventArgs> PacketReceived;

        internal LifxNetworkClient()
        {
        }

        public void Initialize()
        {
            IPEndPoint end = new IPEndPoint(IPAddress.Any, PORT);
            _socket = new UdpClient(end);
            _socket.Client.Blocking = false;
            _socket.DontFragment = true;
            _socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _isRunning = true;

            Task.Run(async () =>
            {
                while (_isRunning)
                    try
                    {
                        var result = await _socket.ReceiveAsync();
                        if (result.Buffer.Length > 0)
                        {
                            HandleIncomingMessages(result.Buffer, result.RemoteEndPoint);
                        }
                    }
                    catch { }
            });
        }

        public async Task SendMessage(string hostName, LIFXProtocolPacket packet)
        {
            if (hostName == null)
            {
                hostName = "255.255.255.255";
            }

            var bytes = packet.GetBytes();
            await _socket.SendAsync(bytes, bytes.Length, hostName, PORT);
        }

        private void HandleIncomingMessages(byte[] data, System.Net.IPEndPoint endpoint)
        {
            __logger.Debug("Received from {0}:{1}", endpoint.ToString(),
                string.Join(",", (from a in data select a.ToString("X2")).ToArray()));

            if (PacketReceived != null)
            {
                var split = endpoint.ToString().Split(':');

                var packet = ParseBytesToLIFXProtocolPacket(data);
                PacketReceived(this, new PacketReceivedEventArgs() {
                    Packet = packet,
                    RemoteAddress = split[0]
                });
            }
        }

        /// <summary>
        /// Disposes the client
        /// </summary>
        public void Dispose()
        {
            _isRunning = false;
            _socket.Dispose();
        }

        private LIFXProtocolPacket ParseBytesToLIFXProtocolPacket(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                var packet = new LIFXProtocolPacket();

                BinaryReader br = new BinaryReader(ms);
                //frame
                var size = br.ReadUInt16();
                if (data.Length != size || size < 36)
                    throw new Exception("Invalid packet");

                // Frame
                packet.Frame = new LIFXProtocolFrame();
                packet.Frame.Size = size;
                // Orgin
                var partialFramData = br.ReadUInt16();
                // TODO: Get tagged out of data
                packet.Frame.Tagged = false;
                packet.Frame.Source = br.ReadUInt32();

                // Frame Address
                packet.FrameAddress = new LIFXProtocolFrameAddress();
                packet.FrameAddress.Target = br.ReadBytes(8);

                ms.Seek(6, SeekOrigin.Current); //skip reserved

                var partialFrameAddressData = br.ReadByte();
                // TODO: Get 2 bits out out of data
                packet.FrameAddress.AckRequired = false;
                packet.FrameAddress.ResRequired = false;
                packet.FrameAddress.Sequence = br.ReadByte();

                // Protocol Header
                packet.ProtocolHeader = new LIFXProtocolProtocolHeader();

                br.ReadUInt64();

                packet.ProtocolHeader.Type = br.ReadUInt16();

                ms.Seek(2, SeekOrigin.Current); //skip reserved

                if (size > 36)
                    packet.Payload = br.ReadBytes(size - 36);

                return packet;
            }
        }
    }
}
