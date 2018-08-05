using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace lifx.Library.NetworkClient
{
    internal class LIFXProtocolPacket
    {
        static NLog.Logger __logger = NLog.LogManager.GetCurrentClassLogger();

        public LIFXProtocolFrame Frame;
        public LIFXProtocolFrameAddress FrameAddress;
        public LIFXProtocolProtocolHeader ProtocolHeader;

        public byte[] Payload;

        public byte[] GetBytes()
        {
            using (var outStream = new MemoryStream())
            {
                using (var dw = new BinaryWriter(outStream) { /*ByteOrder = ByteOrder.LittleEndian*/ })
                {
                    // Frame
                    dw.Write((UInt16)((Payload != null ? Payload.Length : 0) + 36)); // Length 16 bits

                    UInt16 a = 0;

                    // Origin 2 bits -- Always 0
                    if (Frame.Tagged)
                    {
                        a |= (1 << 13); // Tagged 1 bit
                    }

                    a |= (1 << 12); // Addressable 1 bit

                    a |= (UInt16)(a | Frame.Protocol); // Mask in protocol

                    dw.Write(a);
                    dw.Write(Frame.Source); // Source 32 bits header.Identifier

                    dw.Write(FrameAddress.Target);
                    dw.Write(FrameAddress.Reserved);

                    byte b = FrameAddress.Reserved2; // Reserved 6 bits

                    // Merge next two bytes into b
                    b |= (0 << 1); // ack_required 1 Bit
                    b |= (0 << 1); // res_required 1 bit

                    dw.Write(b);

                    dw.Write(FrameAddress.Sequence); // Sequence 8 bits


                    dw.Write(ProtocolHeader.Reserved); // Reserved 64 bits

                    dw.Write(ProtocolHeader.Type); // Type 16 bits
                    dw.Write(ProtocolHeader.Reserved2); // Reserved 16 bits

                    if (Payload != null)
                        dw.Write(Payload);

                    dw.Flush();

                    __logger.Trace(BitConverter.ToString(((MemoryStream)outStream).ToArray()).Replace("-", " "));
                }

                return outStream.ToArray();
            }
        }
    }

    internal class LIFXProtocolFrame
    {
        public const UInt16 BASE_SIZE = 36;

        public UInt16 Size { get; set; }
        public byte Origin { get { return 0; } }
        public bool Tagged { get; set; }
        public bool Addressable { get { return true; } }
        public UInt16 Protocol { get { return 1024; } }
        public UInt32 Source { get; set; }
}

    internal class LIFXProtocolFrameAddress
    {
        public static readonly Byte[] EMPTY_TARGET = new Byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };

        private byte[] _target;
        public byte[] Target {
            get
            {
                return _target;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("Cannot be null");

                if (value.Length != 8)
                    throw new ArgumentException("Array must be a lenght of 8");

                _target = value;
            }
        }
        public byte[] Reserved { get { return new Byte[] { 0, 0, 0, 0, 0, 0 }; } }
        public byte Reserved2 { get { return 0; } }
        public bool AckRequired { get; set; }
        public bool ResRequired { get; set; }
        public byte Sequence { get; set; }
    }

    internal class LIFXProtocolProtocolHeader
    {
        public UInt64 Reserved { get { return 0; } }
        public UInt16 Type { get; set; }
        public UInt16 Reserved2 { get { return 0; } }
    }
}
