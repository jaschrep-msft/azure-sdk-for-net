// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Cryptography;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Blobs.Specialized.Models;
using Azure.Storage.Common.Cryptography;
using Azure.Storage.Common.Cryptography.Models;
using Metadata = System.Collections.Generic.IDictionary<string, string>;

namespace Azure.Storage.Blobs.Cryptography
{
    internal class BlobCryptoDownloadResultTransform : IBlobDownloadResultTransform
    {
        private IKeyEncryptionKeyResolver _keyResolver;

        public BlobCryptoDownloadResultTransform(IKeyEncryptionKeyResolver keyResolver)
        {
            _keyResolver = keyResolver;
        }

        public void Transform(IBlobDownloadResultTransform.BlobDownloadResult result, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task TransformAsync(IBlobDownloadResultTransform.BlobDownloadResult result, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task TransformInternal(IBlobDownloadResultTransform.BlobDownloadResult result, bool async, CancellationToken cancellationToken)
        {
            EncryptedBlobRange encryptedBlobRange;
            if (result.Context.TryGetValue(EncryptionConstants.AdjustedRangeContextKey, out object objEncryptedBlobRange) && objEncryptedBlobRange is EncryptedBlobRange)
            {
                encryptedBlobRange = (EncryptedBlobRange)objEncryptedBlobRange;
            }
            else
            {
                throw new Exception(); //TODO corrupt state if thrown
            }

            ContentRange? contentRange = string.IsNullOrWhiteSpace(result.ContentRange)
                ? default
                : ContentRange.Parse(result.ContentRange);

            EncryptionData encryptionData = GetAndValidateEncryptionData(result.Metadata);
            if (encryptionData == default)
            {
                return; // TODO readjust range
            }

            bool ivInStream = encryptedBlobRange.OriginalRange.Offset >= 16;

            var plaintext = await Utility.DecryptInternal(
                result.ContentStream,
                encryptionData,
                ivInStream,
                _keyResolver,
                default,
                CanIgnorePadding(contentRange),
                async,
                cancellationToken).ConfigureAwait(false);

            // retrim start of stream to original requested location
            // keeping in mind whether we already pulled the IV out of the stream as well
            int gap = (int)(encryptedBlobRange.OriginalRange.Offset - (contentRange?.Start ?? 0))
                - (ivInStream ? EncryptionConstants.EncryptionBlockSize : 0);
            if (gap > 0)
            {
                // throw away initial bytes we want to trim off; stream cannot seek into future
                if (async)
                {
                    await plaintext.ReadAsync(new byte[gap], 0, gap, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    plaintext.Read(new byte[gap], 0, gap);
                }
            }

            result.ContentStream = new LengthLimitingStream(plaintext, encryptedBlobRange.OriginalRange.Length);
        }

        internal static EncryptionData GetAndValidateEncryptionData(Metadata metadata)
        {
            if (metadata == default)
            {
                return default;
            }
            if (!metadata.TryGetValue(EncryptionConstants.EncryptionDataKey, out string encryptedDataString))
            {
                return default;
            }

            EncryptionData encryptionData = EncryptionData.Deserialize(encryptedDataString);

            _ = encryptionData.ContentEncryptionIV ?? throw EncryptionErrors.MissingEncryptionMetadata(
                nameof(EncryptionData.ContentEncryptionIV));
            _ = encryptionData.WrappedContentKey.EncryptedKey ?? throw EncryptionErrors.MissingEncryptionMetadata(
                nameof(EncryptionData.WrappedContentKey.EncryptedKey));

            // Throw if the encryption protocol on the message doesn't match the version that this client library
            // understands and is able to decrypt.
            if (EncryptionConstants.EncryptionProtocolV1 != encryptionData.EncryptionAgent.Protocol)
            {
                throw EncryptionErrors.BadEncryptionAgent(encryptionData.EncryptionAgent.Protocol);
            }

            return encryptionData;
        }

        /// <summary>
        /// Gets whether to ignore padding options for decryption.
        /// </summary>
        /// <param name="contentRange">Downloaded content range.</param>
        /// <returns>True if we should ignore padding.</returns>
        /// <remarks>
        /// If the last cipher block of the blob was returned, we need the padding. Otherwise, we can ignore it.
        /// </remarks>
        private static bool CanIgnorePadding(ContentRange? contentRange)
        {
            // if Content-Range not present, we requested the whole blob
            if (!contentRange.HasValue)
            {
                return false;
            }

            // if range is wildcard, we requested the whole blob
            if (!contentRange.Value.End.HasValue)
            {
                return false;
            }

            // blob storage will always return ContentRange.Size
            // we don't have to worry about the impossible decision of what to do if it doesn't

            // did we request the last block?
            // end is inclusive/0-index, so end = n and size = n+1 means we requested the last block
            if (contentRange.Value.Size - contentRange.Value.End == 1)
            {
                return false;
            }

            return true;
        }
    }
}
