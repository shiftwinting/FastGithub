using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// IPAddress集合
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    sealed class IPAddressCollection
    {
        private readonly object syncRoot = new();
        private readonly HashSet<IPAddressItem> hashSet = new();

        /// <summary>
        /// 获取元素数量
        /// </summary>
        public int Count => this.hashSet.Count;

        /// <summary>
        /// 添加元素
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool Add(IPAddress address)
        {
            lock (this.syncRoot)
            {
                return this.hashSet.Add(new IPAddressItem(address));
            }
        }

        /// <summary>
        /// 转后为数组
        /// </summary>
        /// <returns></returns>
        public IPAddress[] ToArray()
        {
            return this.ToItemArray().OrderBy(item => item.PingElapsed).Select(item => item.Address).ToArray();
        }

        /// <summary>
        /// Ping所有IP
        /// </summary>
        /// <returns></returns>
        public Task PingAllAsync()
        {
            var items = this.ToItemArray();
            if (items.Length == 0)
            {
                return Task.CompletedTask;
            }
            if (items.Length == 1)
            {
                return items[0].PingAsync();
            }
            var tasks = items.Select(item => item.PingAsync());
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// 转换为数组
        /// </summary>
        /// <returns></returns>
        private IPAddressItem[] ToItemArray()
        {
            lock (this.syncRoot)
            {
                return this.hashSet.ToArray();
            }
        }

        /// <summary>
        /// IP地址项
        /// </summary>
        [DebuggerDisplay("Address = {Address}, PingElapsed = {PingElapsed}")]
        private class IPAddressItem : IEquatable<IPAddressItem>
        {
            /// <summary>
            /// Ping的时间点
            /// </summary>
            private int? pingTicks;

            /// <summary>
            /// 地址
            /// </summary>
            public IPAddress Address { get; }

            /// <summary>
            /// Ping耗时
            /// </summary>
            public TimeSpan PingElapsed { get; private set; } = TimeSpan.MaxValue;

            /// <summary>
            /// IP地址项
            /// </summary>
            /// <param name="address"></param>
            public IPAddressItem(IPAddress address)
            {
                this.Address = address;
            }
            /// <summary>
            /// 发起ping请求
            /// </summary>
            /// <returns></returns>
            public async Task PingAsync()
            {
                if (this.NeedToPing() == false)
                {
                    return;
                }

                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(this.Address);
                    this.PingElapsed = reply.Status == IPStatus.Success
                        ? TimeSpan.FromMilliseconds(reply.RoundtripTime)
                        : TimeSpan.MaxValue;
                }
                catch (Exception)
                {
                    this.PingElapsed = TimeSpan.MaxValue;
                }
                finally
                {
                    this.pingTicks = Environment.TickCount;
                }
            }

            /// <summary>
            /// 是否需要ping
            /// 5分钟内只ping一次
            /// </summary>
            /// <returns></returns>
            private bool NeedToPing()
            {
                var ticks = this.pingTicks;
                if (ticks == null)
                {
                    return true;
                }

                var pingTimeSpan = TimeSpan.FromMilliseconds(Environment.TickCount - ticks.Value);
                return pingTimeSpan > TimeSpan.FromMinutes(5d);
            }

            public bool Equals(IPAddressItem? other)
            {
                return other != null && other.Address.Equals(this.Address);
            }

            public override bool Equals(object? obj)
            {
                return obj is IPAddressItem other && this.Equals(other);
            }

            public override int GetHashCode()
            {
                return this.Address.GetHashCode();
            }
        }
    }
}
