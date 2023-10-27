using LogHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace DotNetCore.Library.HttpLogging
{
    public class ResponseBufferingStream : Stream, IHttpResponseBodyFeature
    {
        private readonly HttpContext _context;
        private readonly IHttpResponseBodyFeature _innerBodyFeature;
        private readonly Stream _innerStream;
        private readonly double _elapsedTime;
        private PipeWriter? _pipeAdapter;
        private static readonly StreamPipeWriterOptions _pipeWriterOptions = new StreamPipeWriterOptions(leaveOpen: true);
        public bool HasLogged { get; private set; }

        public ResponseBufferingStream(HttpContext context, IHttpResponseBodyFeature innerBodyFeature, double ElapsedTime)
        {
            _context = context;
            _innerBodyFeature = innerBodyFeature;
            _innerStream = innerBodyFeature.Stream;
            _elapsedTime = ElapsedTime;
        }

        public Stream Stream => this;

        public PipeWriter Writer => _pipeAdapter ??= PipeWriter.Create(Stream, _pipeWriterOptions);

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => _innerStream.CanWrite;

        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public async Task CompleteAsync()
        {
            await _innerBodyFeature.CompleteAsync();
        }

        public void DisableBuffering()
        {
            _innerBodyFeature.DisableBuffering();
        }

        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
        {
            return _innerBodyFeature.SendFileAsync(path, offset, count, cancellationToken);
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return _innerBodyFeature.StartAsync(cancellationToken);
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _innerStream.FlushAsync(cancellationToken);
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
                Log.Info<ResponseBufferingStream>($"[ElapsedTime] {_elapsedTime} : {text}");
                HasLogged = true;
            }
            return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!HasLogged)
            {
                var text = System.Text.Encoding.UTF8.GetString(buffer.Span);
                Log.Info<ResponseBufferingStream>($"[ElapsedTime] {_elapsedTime} : {text}");
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