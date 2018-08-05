using System;
using System.Collections.Generic;
using System.Text;

namespace lifx.Library.Models
{
    public class Device
    {
        public string Hostname { get; internal set; }
        public byte Service { get; internal set; }
        public UInt32 Port { get; internal set; }
        public byte[] MacAddress { get; internal set; }
        public string MacAddressString
        {
            get
            {
                if (MacAddress == null)
                {
                    return null;
                }

                return BitConverter.ToString(MacAddress);
            }
        }
    }
}
