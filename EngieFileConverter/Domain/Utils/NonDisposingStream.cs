using System;
using System.IO;

namespace Nyerguds.Util
{
    /// <summary>
    /// A wrapper for a stream that allows using it in a StreamWriter in a way that
    /// prevents the stream from being closed when the StreamWriter is disposed. Can be used as:
    /// <i>using (StreamWriter sw = new StreamWriter(new NonDisposingStream(stream), encoding))</i>
    /// </summary>
    /// <author>Maarten Meuris</author>
    public class NonDisposingStream : Stream
    {
        private Stream stream;

        /// <summary>
        /// A wrapper for a stream that allows using it in a StreamWriter in a way that
        /// prevents the stream from being closed when the StreamWriter is disposed. Can be used as:
        /// <i>using (StreamWriter sw = new StreamWriter(new NonDisposingStream(stream), encoding))</i>
        /// </summary>
        /// <param name="stream">The stream to wrap.</param>
        public NonDisposingStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            this.stream = stream;
        }

        public override Boolean CanRead
        {
            get { return this.stream.CanRead; }
        }

        public override Boolean CanSeek
        {
            get { return this.stream.CanSeek; }
        }

        public override Boolean CanTimeout
        {
            get { return this.stream.CanTimeout; }
        }

        public override Boolean CanWrite
        {
            get { return this.stream.CanWrite; }
        }

        public override Int64 Length
        {
            get { return this.stream.Length; }
        }

        public override Int64 Position
        {
            get { return this.stream.Position; }
            set { this.stream.Position = value; }
        }

        public override Int32 ReadTimeout
        {
            get { return this.stream.ReadTimeout; }
            set { this.stream.ReadTimeout = value; }
        }

        public override Int32 WriteTimeout
        {
            get { return this.stream.WriteTimeout; }
            set { this.stream.WriteTimeout = value; }
        }

        public override IAsyncResult BeginRead(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback callback, Object state)
        {
            return this.stream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback callback, Object state)
        {
            return this.stream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void Close()
        {
            // do nothing. Do not close the stream.
        }

        //public void Dispose()
        // No need to override this one; it just calls Close()

        protected override void Dispose(Boolean disposing)
        {
            // do nothing. Do not close the stream.
        }

        public override Int32 EndRead(IAsyncResult asyncResult)
        {
            return this.stream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            this.stream.EndWrite(asyncResult);
        }

        public override void Flush()
        {
            this.stream.Flush();
        }

        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
        {
            return this.stream.Read(buffer, offset, count);
        }

        public override Int32 ReadByte()
        {
            return this.stream.ReadByte();
        }

        public override Int64 Seek(Int64 offset, SeekOrigin origin)
        {
            return this.stream.Seek(offset, origin);
        }

        public override void SetLength(Int64 value)
        {
            this.stream.SetLength(value);
        }

        public override void Write(Byte[] buffer, Int32 offset, Int32 count)
        {
            this.stream.Write(buffer, offset, count);
        }

        public override void WriteByte(Byte value)
        {
            this.stream.WriteByte(value);
        }
    }
}