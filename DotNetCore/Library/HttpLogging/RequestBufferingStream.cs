using LogHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace DotNetCore.Library.HttpLogging
{
    internal sealed class RequestBufferingStream : Stream
    {
        private readonly HttpContext _context;
        private readonly Stream _innerStream;
        private readonly Encoding _encoding;
        private readonly HttpLoggingOptions _options;
        private readonly List<string> _allowHeader = new() { "Authorization", "Content-Type", "Content-Length", "User-Agent" };
        public bool HasLogged { get; private set; }

        public RequestBufferingStream(HttpContext context, HttpLoggingOptions options, Encoding encoding)
        {
            _context = context;
            _innerStream = _context.Request.Body;
            _options = options;
            _encoding = encoding;
        }

        public Stream Stream => this;

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => _innerStream.CanWrite;

        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _innerStream.FlushAsync(cancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var res = await _innerStream.ReadAsync(buffer, cancellationToken);
            if (!HasLogged)
            {
                var text = System.Text.Encoding.UTF8.GetString(buffer.Slice(0, res).Span);
                LogData(text);
                HasLogged = true;
            }
            return res;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var res = await _innerStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
            if (!HasLogged)
            {
                var text = System.Text.Encoding.UTF8.GetString(buffer, offset, count);
                LogData(text);
                HasLogged = true;
            }
            return res;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _innerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!HasLogged)
            {
                var text = System.Text.Encoding.UTF8.GetString(buffer, offset, count);
                LogHelper.Log.Info(text);
                HasLogged = true;
            }
            return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!HasLogged)
            {
                var text = System.Text.Encoding.UTF8.GetString(buffer.Span);
                LogHelper.Log.Info(text);
                HasLogged = true;
            }
            return _innerStream.WriteAsync(buffer, cancellationToken);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _innerStream.Write(buffer);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer.AsSpan(offset, count));
        }

        private void LogData(string data)
        {
            var request = _context.Request;
            if (_options.LoggingFields.HasFlag(HttpLoggingFields.RequestProtocol))
                Log.Info($"[{nameof(request.Protocol)}] {request.Protocol}");

            if (_options.LoggingFields.HasFlag(HttpLoggingFields.RequestMethod) &&
                _options.LoggingFields.HasFlag(HttpLoggingFields.RequestScheme) &&
                _options.LoggingFields.HasFlag(HttpLoggingFields.RequestPath))
                Log.Info($"[{request.Method}] {request.Scheme}://{request.Host}{request.PathBase}{request.Path}");

            if (_options.LoggingFields.HasFlag(HttpLoggingFields.RequestQuery))
                Log.Info($"[{nameof(request.QueryString)}] {request.QueryString.Value}");

            if (_options.LoggingFields.HasFlag(HttpLoggingFields.RequestHeaders))
                FilterHeaders(request.Headers);

            if (_options.LoggingFields.HasFlag(HttpLoggingFields.RequestBody))
                Log.Info(data);
        }

        internal void FilterHeaders(IHeaderDictionary headers)
        {
            foreach (var (key, value) in headers)
                if (_allowHeader.Contains(key))
                    Log.Info($"[{key}] {value}");
        }
    }
}