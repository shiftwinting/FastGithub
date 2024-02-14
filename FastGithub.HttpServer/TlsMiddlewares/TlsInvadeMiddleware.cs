using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace FastGithub.HttpServer.TlsMiddlewares
{
    /// <summary>
    /// https入侵中间件
    /// </summary>
    sealed class TlsInvadeMiddleware
    {  
        /// <summary>
        /// 执行中间件
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(ConnectionDelegate next, ConnectionContext context)
        {
            // 连接不是tls
            if (await IsTlsConnectionAsync(context) == false)
            {
                // 没有任何tls中间件执行过
                if (context.Features.Get<ITlsConnectionFeature>() == null)
                {
                    // 设置假的ITlsConnectionFeature，迫使https中间件跳过自身的工作
                    context.Features.Set<ITlsConnectionFeature>(FakeTlsConnectionFeature.Instance);
                }
            }
            await next(context);
        }


        /// <summary>
        /// 是否为tls协议
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static async Task<bool> IsTlsConnectionAsync(ConnectionContext context)
        {
            try
            {
                var result = await context.Transport.Input.ReadAtLeastAsync(2, context.ConnectionClosed);
                var state = IsTlsProtocol(result);
                context.Transport.Input.AdvanceTo(result.Buffer.Start);
                return state;
            }
            catch
            {
                return false;
            }

            static bool IsTlsProtocol(ReadResult result)
            {
                var reader = new SequenceReader<byte>(result.Buffer);
                return reader.TryRead(out var firstByte) &&
                    reader.TryRead(out var nextByte) &&
                    firstByte == 0x16 &&
                    nextByte == 0x3;
            }
        }
    }
}
