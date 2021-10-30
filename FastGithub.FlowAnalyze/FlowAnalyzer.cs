using System;
using System.Collections.Concurrent;
using System.Linq;

namespace FastGithub.FlowAnalyze
{
    sealed class FlowAnalyzer : IFlowAnalyzer
    {
        private const int INTERVAL_SECONDS = 5;
        private readonly ConcurrentQueue<QueueItem> readQueue = new();
        private readonly ConcurrentQueue<QueueItem> writeQueue = new();

        private record QueueItem(long Ticks, int Length);

        /// <summary>
        /// 收到数据
        /// </summary>
        /// <param name="flowType"></param>
        /// <param name="length"></param>
        public void OnFlow(FlowType flowType, int length)
        {
            if (flowType == FlowType.Read)
            {
                Add(this.readQueue, length);
            }
            else
            {
                Add(this.writeQueue, length);
            }
        }

        private static void Add(ConcurrentQueue<QueueItem> quques, int length)
        {
            var ticks = Flush(quques);
            quques.Enqueue(new QueueItem(ticks, length));
        }

        /// <summary>
        /// 刷新队列
        /// </summary>
        /// <param name="quques"></param>
        /// <returns></returns>
        private static long Flush(ConcurrentQueue<QueueItem> quques)
        {
            var ticks = Environment.TickCount64;
            while (quques.TryPeek(out var item))
            {
                if (ticks - item.Ticks < INTERVAL_SECONDS * 1000)
                {
                    break;
                }
                else
                {
                    quques.TryDequeue(out _);
                }
            }
            return ticks;
        }



        /// <summary>
        /// 获取速率
        /// </summary>
        /// <returns></returns>
        public FlowRate GetFlowRate()
        {
            Flush(this.readQueue);
            var readRate = (double)this.readQueue.Sum(item => item.Length) / INTERVAL_SECONDS;

            Flush(this.writeQueue);
            var writeRate = (double)this.writeQueue.Sum(item => item.Length) / INTERVAL_SECONDS;

            return new FlowRate { ReadRate = readRate, WriteRate = writeRate };
        }
    }
}
