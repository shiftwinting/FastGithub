using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace FastGithub.Scanner
{
    sealed class IPRange : IEnumerable<IPAddress>
    {
        private readonly IPNetwork network;

        public AddressFamily AddressFamily => this.network.AddressFamily;

        public int Size => (int)this.network.Total;

        private IPRange(IPNetwork network)
        {
            this.network = network;
        }

        public IEnumerator<IPAddress> GetEnumerator()
        {
            return new Enumerator(this.network);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private class Enumerator : IEnumerator<IPAddress>
        {
            private IPAddress? currrent;
            private readonly IPNetwork network;
            private readonly IPAddress maxAddress;

            public Enumerator(IPNetwork network)
            {
                this.network = network;
                this.maxAddress = Add(network.LastUsable, 1);
            }

            public IPAddress Current => this.currrent ?? throw new NotImplementedException();

            object IEnumerator.Current => this.Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                var value = this.currrent == null
                     ? this.network.FirstUsable
                     : Add(this.currrent, 1);

                if (value.Equals(maxAddress))
                {
                    return false;
                }

                this.currrent = value;
                return true;
            }

            public void Reset()
            {
                this.currrent = null;
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

        public static IEnumerable<IPRange> From(IEnumerable<string> networks)
        {
            foreach (var item in networks)
            {
                if (TryParse(item, out var value))
                {
                    yield return value;
                }
            }
        }

        public static bool TryParse(ReadOnlySpan<char> network, [MaybeNullWhen(false)] out IPRange value)
        {
            if (network.IsEmpty == false && IPNetwork.TryParse(network.ToString(), out var ipNetwork))
            {
                value = new IPRange(ipNetwork);
                return true;
            }

            value = null;
            return false;
        }

        public override string ToString()
        {
            return this.network.ToString();
        }

    }
}
