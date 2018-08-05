using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using lifx.Library.Messages;
using lifx.Library.Models;
using lifx.Library.NetworkClient;
using lifx.Library.Extensions;
using lifx.Library.Responses;

namespace lifx.Library
{
    public class Client : IDisposable
    {
        static NLog.Logger __logger = NLog.LogManager.GetCurrentClassLogger();
        static Random _rnd = new Random();

        bool _isDisposed = false;
        private UInt32 _discoverSourceID;
        private LifxNetworkClient _networkClient = null;

        public Client()
        {
            _discoverSourceID = (UInt32)_rnd.Next(int.MaxValue);
            _networkClient = new LifxNetworkClient();
            _networkClient.PacketReceived += _networkClient_PacketReceived;
        }

        public void Initialize()
        {
            _networkClient.Initialize();
        }

        private void _networkClient_PacketReceived(object sender, PacketReceivedEventArgs e)
        {
            var type = (MessageType)e.Packet.ProtocolHeader.Type;
            
            if(type == MessageType.DeviceStateService)
            {
                HandleDeviceStateService(e.Packet, e.RemoteAddress);
            }
            else
            {
                __logger.Info($"Packet Event: {type} -- {e.Packet.Frame.Source}");
                if (_tcsDictionary.ContainsKey(e.Packet.Frame.Source))
                {
                    TaskCompletionSource<LIFXProtocolPacket> v = null;
                    var success = _tcsDictionary.TryGetValue(e.Packet.Frame.Source, out v);
                    if (!success)
                    {
                        throw new Exception($"Failed to get key {e.Packet.Frame.Source} from dictionary");
                    }

                    v.SetResult(e.Packet);
                }

                //switch (type)
                //{
                //    case MessageType.DeviceStateVersion:
                //        HandleDeviceStateService(e.Packet, e.RemoteAddress);
                //        break;
                //    default:
                //        __logger.Warn($"Unknown Packet: Type: {e.Packet.ProtocolHeader.Type} - Remote Address: {e.RemoteAddress}");
                //        // Todo: Log detail of this packet for futher investigation
                //        break;
                //}
            }
            
        }

        public void Dispose()
        {
            _isDisposed = true;

            if (_networkClient != null)
            {
                _networkClient.Dispose();
                _networkClient = null;
            }
        }

        private void CheckForInitilizedAndNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("Object has been disposed.");
            }

            if (_networkClient == null)
            {
                throw new InvalidOperationException("Must call Initialize() before.");
            }       
        }


        private ConcurrentDictionary<UInt32, TaskCompletionSource<LIFXProtocolPacket>> _tcsDictionary = new ConcurrentDictionary<UInt32, TaskCompletionSource<LIFXProtocolPacket>>();

        private async Task<LIFXProtocolPacket> SendReceiveAsync(UInt32 source, string hostName, LIFXProtocolPacket packet, int timeout = 30000)
        {
            try
            {
                var tokenSource = new CancellationTokenSource(timeout);
                var token = tokenSource.Token;

                var tcs = new TaskCompletionSource<LIFXProtocolPacket>();
                var successfullAdd = _tcsDictionary.TryAdd(source, tcs);
                if (!successfullAdd)
                {
                    throw new Exception($"Failed to add {source} to dictionary");
                }
         
                await _networkClient.SendMessage(hostName, packet);
                var result = await tcs.Task.WithCancellation(token);

                if (_tcsDictionary.ContainsKey(source))
                {
                    TaskCompletionSource<LIFXProtocolPacket> value = null;
                    _tcsDictionary.TryRemove(source, out value);
                }

                return result;
            }
            finally
            {
                if (_tcsDictionary.ContainsKey(source))
                {
                    TaskCompletionSource<LIFXProtocolPacket> value = null;
                    _tcsDictionary.TryRemove(source, out value);
                }
            }
        }


        #region Operations

        #region Discovery

        public async void DiscoverDevices()
        {
            CheckForInitilizedAndNotDisposed();

            var msg = new GetService(_discoverSourceID);
            await _networkClient.SendMessage(null, msg.GetPacket());
        }

        #endregion

        #region Device Info

		public async Task<StateVersionResponse> GetDeviceVersion(Device device)
        {
            var source = (UInt32)_rnd.Next(int.MaxValue);
            var msg = new DeviceGetVersion(device.MacAddressString, source);
            var packet = msg.GetPacket();

            var result = await SendReceiveAsync(source, device.Hostname, packet);

            return new StateVersionResponse(result.Payload);
        }

        ///// <summary>
        ///// Gets the device's host firmware
        ///// </summary>
        ///// <param name="device"></param>
        ///// <returns></returns>
        //public async Task<StateHostFirmwareResponse> GetDeviceHostFirmwareAsync(Device device)
        //{
        //    FrameHeader header = new FrameHeader()
        //    {
        //        Identifier = (uint)randomizer.Next(),
        //        AcknowledgeRequired = false
        //    };
        //    var resp = await BroadcastMessageAsync<StateHostFirmwareResponse>(device.HostName, header, MessageType.DeviceGetHostFirmware);
        //    return resp;
        //}

        #endregion

        #endregion


        #region Process Responses

        public System.Collections.ObjectModel.ObservableCollection<Device> Devices = new System.Collections.ObjectModel.ObservableCollection<Device>();

        private void HandleDeviceStateService(LIFXProtocolPacket packet, string hostname)
        {
            string id = BitConverter.ToString(packet.FrameAddress.Target);

            var device = Devices.FirstOrDefault(x => x.MacAddressString == id);
            if(device != null)
            {
                device.Hostname = hostname;
            }
            else
            {
                if (packet.Frame.Source != _discoverSourceID)
                    return;

                device = new LightBulb()
                {
                    Hostname = hostname,
                    Service = packet.Payload[0],
                    Port = BitConverter.ToUInt32(packet.Payload, 1),
                    MacAddress = packet.FrameAddress.Target
                };

                Devices.Add(device);
            }
        }

        private void HandleDeviceStateVersion(LIFXProtocolPacket packet, string hostname)
        {
            string id = BitConverter.ToString(packet.FrameAddress.Target);

            var device = Devices.FirstOrDefault(x => x.MacAddressString == id);
            if (device != null)
            {
                device.Hostname = hostname;
            }
            else
            {
                if (packet.Frame.Source != _discoverSourceID)
                    return;

                device = new LightBulb()
                {
                    Hostname = hostname,
                    Service = packet.Payload[0],
                    Port = BitConverter.ToUInt32(packet.Payload, 1),
                    MacAddress = packet.FrameAddress.Target
                };

                Devices.Add(device);
            }
        }

        #endregion
    }
}
