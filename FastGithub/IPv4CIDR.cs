using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace FastGithub
{
    sealed class IPv4CIDR
    {
        public IPAddress IPAddress { get; }

        public int Mask { get; }

        public int Size { get; }

        public IPv4CIDR(IPAddress ipAddress, int mask)
        {
            this.IPAddress = ipAddress;
            this.Mask = mask;
            this.Size = Math.Abs((int)(uint.MaxValue << mask >> mask));
        }

        public IEnumerable<IPAddress> GetAllIPAddress()
        {
            for (var i = 0; i < this.Size; i++)
            {
                var value = i;
                yield return Add(this.IPAddress, value);
            }
        }

        /// <summary>
        /// 添加值
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static IPAddress Add(IPAddress ip, int value)
        {
            var span = ip.GetAddressBytes().AsSpan();
            var hostValue = BinaryPrimitives.ReadInt32BigEndian(span);
            BinaryPrimitives.WriteInt32BigEndian(span, hostValue + value);
            return new IPAddress(span);
        }

        public static IEnumerable<IPv4CIDR> From(IEnumerable<string> cidrs)
        {
            foreach (var item in cidrs)
            {
                if (TryParse(item, out var value))
                {
                    yield return value;
                }
            }
        }

        public static bool TryParse(ReadOnlySpan<char> cidr, [MaybeNullWhen(false)] out IPv4CIDR value)
        {
            value = null;
            var index = cidr.IndexOf('/');
            if (index <= 0)
            {
                return false;
            }

            var addressSpan = cidr.Slice(0, index);
            if (IPAddress.TryParse(addressSpan, out var address) == false
                || address.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }

            var maskSpan = cidr.Slice(index + 1);
            if (int.TryParse(maskSpan, out var mask) == false)
            {
                return false;
            }

            value = new IPv4CIDR(address, mask);
            return true;
        }

        public override string ToString()
        {
            return $"{this.IPAddress}/{this.Mask}";
        }
    }
}
