using System;
using System.Collections.Generic;
using System.Text;

namespace lifx.Library.Messages
{
    public class DeviceGetVersion : Message
    {
        public DeviceGetVersion(string target, uint source) 
            : base(MessageType.DeviceGetVersion, target, source)
        {
        }

        protected override byte[] payloadToBytes()
        {
            return new byte[0];
        }
    }
}
