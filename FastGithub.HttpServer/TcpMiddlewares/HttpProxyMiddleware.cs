﻿using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace FastGithub.HttpServer.TcpMiddlewares
{
    /// <summary>
    /// 正向代理中间件
    /// </summary>
    sealed class HttpProxyMiddleware
    {
        private readonly HttpParser<HttpRequestHandler> httpParser = new();
        private readonly byte[] http200 = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
        private readonly byte[] http400 = Encoding.ASCII.GetBytes("HTTP/1.1 400 Bad Request\r\n\r\n");

        /// <summary>
        /// 执行中间件
        /// </summary>
        /// <param name="next"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(ConnectionDelegate next, ConnectionContext context)
        {
            var result = await context.Transport.Input.ReadAsync();
            var httpRequest = this.GetHttpRequestHandler(result, out var consumed);

            // 协议错误
            if (consumed == 0L)
            {
                await context.Transport.Output.WriteAsync(this.http400, context.ConnectionClosed);
            }
            else
            {
                // 隧道代理连接请求
                if (httpRequest.ProxyProtocol == ProxyProtocol.TunnelProxy)
                {
                    var position = result.Buffer.GetPosition(consumed);
                    context.Transport.Input.AdvanceTo(position);
                    await context.Transport.Output.WriteAsync(this.http200, context.ConnectionClosed);
                }
                else
                {
                    var position = result.Buffer.Start;
                    context.Transport.Input.AdvanceTo(position);
                }

                context.Features.Set<IHttpProxyFeature>(httpRequest);
                await next(context);
            }
        }


        /// <summary>
        /// 获取http请求处理者
        /// </summary>
        /// <param name="result"></param>
        /// <param name="consumed"></param>
        /// <returns></returns>
        private HttpRequestHandler GetHttpRequestHandler(ReadResult result, out long consumed)
        {
            var handler = new HttpRequestHandler();
            var reader = new SequenceReader<byte>(result.Buffer);

            if (this.httpParser.ParseRequestLine(handler, ref reader) &&
                this.httpParser.ParseHeaders(handler, ref reader))
            {
                consumed = reader.Consumed;
            }
            else
            {
                consumed = 0L;
            }
            return handler;
        }


        /// <summary>
        /// 代理请求处理器
        /// </summary>
        private class HttpRequestHandler : IHttpRequestLineHandler, IHttpHeadersHandler, IHttpProxyFeature
        {
            private HttpMethod method;

            public HostString ProxyHost { get; private set; }

            public ProxyProtocol ProxyProtocol
            {
                get
                {
                    if (this.ProxyHost.HasValue == false)
                    {
                        return ProxyProtocol.None;
                    }
                    if (this.method == HttpMethod.Connect)
                    {
                        return ProxyProtocol.TunnelProxy;
                    }
                    return ProxyProtocol.HttpProxy;
                }
            }

            void IHttpRequestLineHandler.OnStartLine(HttpVersionAndMethod versionAndMethod, TargetOffsetPathLength targetPath, Span<byte> startLine)
            {
                this.method = versionAndMethod.Method;
                var host = Encoding.ASCII.GetString(startLine.Slice(targetPath.Offset, targetPath.Length));
                if (versionAndMethod.Method == HttpMethod.Connect)
                {
                    this.ProxyHost = HostString.FromUriComponent(host);
                }
                else if (Uri.TryCreate(host, UriKind.Absolute, out var uri))
                {
                    this.ProxyHost = HostString.FromUriComponent(uri);
                }
            }

            void IHttpHeadersHandler.OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
            {
            }
            void IHttpHeadersHandler.OnHeadersComplete(bool endStream)
            {
            }
            void IHttpHeadersHandler.OnStaticIndexedHeader(int index)
            {
            }
            void IHttpHeadersHandler.OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
            {
            }
        }
    }
}