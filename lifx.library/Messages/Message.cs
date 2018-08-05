using lifx.Library.NetworkClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace lifx.Library.Messages
{
    public abstract class Message
    {
        private MessageType _type;
        private bool _isTagged;
        private UInt32 _source;

        public Message(MessageType type, string target, UInt32 source)
        {
            _type = type;
            _source = source;
            _isTagged = string.IsNullOrWhiteSpace(target);
        }

        protected abstract byte[] payloadToBytes();

        internal LIFXProtocolPacket GetPacket()
        {
            UInt16 payloadLength = (UInt16)payloadToBytes().Length;
            UInt16 size = (UInt16)(LIFXProtocolFrame.BASE_SIZE + payloadLength);

            var packet = new LIFXProtocolPacket();
            packet.Frame = new LIFXProtocolFrame
            {
                Size = size,
                Source = _source,
                Tagged = _isTagged
            };

            packet.FrameAddress = new LIFXProtocolFrameAddress
            {
                Target = LIFXProtocolFrameAddress.EMPTY_TARGET,
                AckRequired = true,
                ResRequired = true,
                Sequence = 0
            };

            packet.ProtocolHeader = new LIFXProtocolProtocolHeader
            {
                Type = (UInt16)_type
            };

            packet.Payload = this.payloadToBytes();

            return packet;
        }
    }
}
