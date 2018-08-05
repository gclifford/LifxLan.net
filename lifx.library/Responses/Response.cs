using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace lifx.Library.Responses
{
    public abstract class Response
    {
        public Response(byte[] payload)
        {
            using (MemoryStream ms = new MemoryStream(payload))
            {
                BinaryReader br = new BinaryReader(ms);
                ParsePayload(br);
            }
        }

        protected abstract void ParsePayload(BinaryReader reader);
    }
}
