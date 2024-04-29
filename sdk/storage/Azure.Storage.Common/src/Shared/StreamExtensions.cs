// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Azure.Storage
{
    /// <summary>
    /// Extension methods for working with Streams.
    /// </summary>
    internal static partial class StreamExtensions
    {
        public static async Task<int> ReadInternal(
            this Stream stream,
            byte[] buffer,
            int offset,
            int count,
            bool async,
            CancellationToken cancellationToken)
        {
            if (async)
            {
                return await stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return stream.Read(buffer, offset, count);
            }
        }

        public static async Task WriteInternal(
            this Stream stream,
            byte[] buffer,
            int offset,
            int count,
            bool async,
            CancellationToken cancellationToken)
        {
            if (async)
            {
                await stream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                stream.Write(buffer, offset, count);
            }
        }

        public static Task<long> CopyToInternal(
            this Stream src,
            Stream dest,
            bool async,
            CancellationToken cancellationToken)
            => CopyToInternal(
                src,
                dest,
                bufferSize: 81920, // default from .NET documentation
                async,
                cancellationToken);

        /// <summary>
        /// Reads the bytes from the source stream and writes them to the destination stream.
        /// </summary>
        /// <param name="src">
        /// Stream to copy from.
        /// </param>
        /// <param name="dest">
        /// Stream to copy to.
        /// </param>
        /// <param name="bufferSize">
        /// The size, in bytes, of the buffer. This value must be greater than zero.
        /// </param>
        /// <param name="async">
        /// Whether to perform the operation asynchronously.
        /// </param>
        /// <param name="cancellationToken">
        /// Cancellation token for the operation.
        /// </param>
        /// <returns></returns>
        public static async Task<long> CopyToInternal(
            this Stream src,
            Stream dest,
            int bufferSize,
            bool async,
            CancellationToken cancellationToken)
        {
            Argument.AssertNotNull(src, nameof(src));
            Argument.AssertNotNull(dest, nameof(dest));
            Argument.AssertInRange(bufferSize, 1, int.MaxValue, nameof(bufferSize));

            long copied = 0;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                if (async)
                {
                    int bytesRead;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    while ((bytesRead = await src.ReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(false)) != 0)
                    {
                        await dest.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken).ConfigureAwait(false);
                        copied += bytesRead;
                    }
#else
                    while ((bytesRead = await src.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
                    {
                        await dest.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                        copied += bytesRead;
                    }
#endif
                }
                else
                {
                    int bytesRead;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    while ((bytesRead = src.Read(new Span<byte>(buffer))) != 0)
                    {
                        dest.Write(buffer, 0, bytesRead);
                        copied += bytesRead;
                    }
#else
                    while ((bytesRead = src.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        dest.Write(buffer, 0, bytesRead);
                        copied += bytesRead;
                    }
#endif
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return copied;
        }
    }
}
