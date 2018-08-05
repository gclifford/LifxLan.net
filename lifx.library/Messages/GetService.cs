using System;
using System.Collections.Generic;
using System.Text;

namespace lifx.Library.Messages
{
    public class GetService : Message
    {
        public GetService(UInt32 source)
            : base(MessageType.DeviceGetService, string.Empty, source)
        {

        }

        public void parsePayload(byte[] bytes)
        {
            // No payload for this message
        }

        protected override byte[] payloadToBytes()
        {
            return new byte[0];
        }
    }
}
