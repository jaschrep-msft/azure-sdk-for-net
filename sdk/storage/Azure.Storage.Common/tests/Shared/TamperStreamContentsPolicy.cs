// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using Azure.Core;
using Azure.Core.Pipeline;

namespace Azure.Storage.Test.Shared
{
    internal class TamperStreamContentsPolicy : HttpPipelineSynchronousPolicy
    {
        /// <summary>
        /// Default tampering that changes the first byte of the stream.
        /// </summary>
        private static Func<Stream, Stream> GetByteShiftTransform(int index) => stream =>
        {
            if (stream is not MemoryStream memoryStream)
            {
                memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
            }

            memoryStream.Position = index;
            int b = stream.ReadByte();

            memoryStream.Position = index;
            memoryStream.WriteByte((byte)((b + 1) % byte.MaxValue));

            memoryStream.Position = 0;
            return memoryStream;
        };

        private readonly Func<Stream, Stream> _streamTransform;

        public TamperStreamContentsPolicy(Func<Stream, Stream> streamTransform = default)
        {
            _streamTransform = streamTransform ?? GetByteShiftTransform(0);
        }

        public static TamperStreamContentsPolicy ShiftBytePolicy(int index)
            => new TamperStreamContentsPolicy(GetByteShiftTransform(index));

        public bool TransformRequestBody { get; set; }

        public bool TransformResponseBody { get; set; }

        public override void OnSendingRequest(HttpMessage message)
        {
            if (TransformRequestBody && message.Request.Content != default)
            {
                var sendContents = new MemoryStream();
                message.Request.Content.WriteTo(sendContents, CancellationToken.None);
                message.Request.Content = RequestContent.Create(_streamTransform(sendContents));
            }
        }

        public override void OnReceivedResponse(HttpMessage message)
        {
            if (TransformResponseBody && message.Response.ContentStream != default)
            {
                message.Response.ContentStream = _streamTransform(message.Response.ContentStream);
            }
        }
    }
}
