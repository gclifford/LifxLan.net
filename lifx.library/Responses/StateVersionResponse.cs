using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace lifx.Library.Responses
{
    public class StateVersionResponse : Response
    {
        public StateVersionResponse(byte[] payload)
            : base(payload)
        {
            
        }

        public UInt32 Vendor { get; private set; }
        public UInt32 Product { get; private set; }
        public UInt32 Version { get; private set; }

        protected override void ParsePayload(BinaryReader reader)
        {
            Vendor = reader.ReadUInt32();
            Product = reader.ReadUInt32();
            Version = reader.ReadUInt32();
        }
    }
}
