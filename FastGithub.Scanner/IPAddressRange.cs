using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace FastGithub.Scanner
{
    abstract class IPAddressRange : IEnumerable<IPAddress>
    {
        public abstract int Size { get; }

        public abstract AddressFamily AddressFamily { get; }

        public abstract IEnumerator<IPAddress> GetEnumerator();


        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public static IEnumerable<IPAddressRange> From(IEnumerable<string> ranges)
        {
            foreach (var item in ranges)
            {
                if (TryParse(item, out var range))
                {
                    yield return range;
                }
            }
        }


        public static bool TryParse(ReadOnlySpan<char> range, [MaybeNullWhen(false)] out IPAddressRange value)
        {
            if (range.IsEmpty == false && IPNetwork.TryParse(range.ToString(), out var ipNetwork))
            {
                value = new NetworkIPAddressRange(ipNetwork);
                return true;
            }

            var index = range.IndexOf('-');
            if (index >= 0)
            {
                var start = range.Slice(0, index);
                var end = range.Slice(index + 1);

                if (IPAddress.TryParse(start, out var startIp) &&
                   IPAddress.TryParse(end, out var endIp) &&
                   startIp.AddressFamily == endIp.AddressFamily)
                {
                    value = new SplitIPAddressRange(startIp, endIp);
                    return true;
                }
            }

            value = null;
            return false;
        }


        private class NetworkIPAddressRange : IPAddressRange
        {
            private readonly IPAddressCollection addressCollection;

            private readonly AddressFamily addressFamily;

            public override int Size => (int)this.addressCollection.Count;

            public override AddressFamily AddressFamily => this.addressFamily;

            public NetworkIPAddressRange(IPNetwork network)
            {
                this.addressCollection = network.ListIPAddress(FilterEnum.All);
                this.addressFamily = network.AddressFamily;
            }

            public override IEnumerator<IPAddress> GetEnumerator()
            {
                return ((IEnumerable<IPAddress>)this.addressCollection).GetEnumerator();
            }
        }

        private class SplitIPAddressRange : IPAddressRange
        {
            private readonly IPAddress start;
            private readonly IPAddress end;

            private readonly AddressFamily addressFamily;

            public override AddressFamily AddressFamily => this.addressFamily;

            public SplitIPAddressRange(IPAddress start, IPAddress end)
            {
                this.start = start;
                this.end = end;
                this.addressFamily = start.AddressFamily;
            }

            public override int Size
            {
                get
                {
                    if (this.start.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        var startValue = BinaryPrimitives.ReadInt64BigEndian(this.start.GetAddressBytes());
                        var endValue = BinaryPrimitives.ReadInt64BigEndian(this.end.GetAddressBytes());
                        return (int)(endValue - startValue) + 1;
                    }
                    else
                    {
                        var startValue = BinaryPrimitives.ReadInt32BigEndian(this.start.GetAddressBytes());
                        var endValue = BinaryPrimitives.ReadInt32BigEndian(this.end.GetAddressBytes());
                        return endValue - startValue + 1;
                    }
                }
            }

            public override IEnumerator<IPAddress> GetEnumerator()
            {
                return this.GetIPAddresses().GetEnumerator();
            }

            private IEnumerable<IPAddress> GetIPAddresses()
            {
                for (var i = 0; i < this.Size; i++)
                {
                    var value = i;
                    yield return Add(this.start, value);
                }
            }

            /// <summary>
            /// 添加值
            /// </summary>
            /// <param name="address"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            private static IPAddress Add(IPAddress address, int value)
            {
                var span = address.GetAddressBytes().AsSpan();
                var hostValue = BinaryPrimitives.ReadInt32BigEndian(span);
                BinaryPrimitives.WriteInt32BigEndian(span, hostValue + value);
                return new IPAddress(span);
            }
        }
    }
}
