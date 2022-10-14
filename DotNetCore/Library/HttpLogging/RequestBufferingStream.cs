using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

#nullable enable
namespace DotNetCore.Library.HttpLogging
{
    internal sealed class RequestBufferingStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly Encoding _encoding;

        public bool HasLogged { get; private set; }

        public RequestBufferingStream(Stream innerStream, Encoding encoding)
        {
            _innerStream = innerStream;
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
                LogHelper.Log.Info(text);
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
                LogHelper.Log.Info(text);
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
    }
}

