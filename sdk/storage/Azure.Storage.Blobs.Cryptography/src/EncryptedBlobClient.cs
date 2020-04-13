// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Cryptography;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Common;
using Azure.Storage.Common.Cryptography.Models;
using Azure.Storage.Common.Cryptography;

using Metadata = System.Collections.Generic.IDictionary<string, string>;
using static Azure.Storage.Common.Cryptography.Utility;
using Azure.Storage.Blobs.Specialized.Models;

namespace Azure.Storage.Blobs.Specialized
{
    /// <summary>
    /// The <see cref="EncryptedBlobClient"/> allows you to manipulate
    /// Azure Storage block blobs with client-side encryption. See
    /// <see cref="BlobClient"/> for more details.
    /// </summary>
    public class EncryptedBlobClient : BlobClient
    {

        /// <summary>
        /// The wrapper used to wrap the content encryption key.
        /// </summary>
        private IKeyEncryptionKey KeyWrapper { get; }

        /// <summary>
        /// The key resolver used to select the correct key for decrypting existing blobs.
        /// </summary>
        private IKeyEncryptionKeyResolver KeyResolver { get; }

        /// <summary>
        /// The algorithm identifier to use with the <see cref="KeyWrapper"/>. Value to pass into
        /// <see cref="IKeyEncryptionKey.WrapKey(string, ReadOnlyMemory{byte}, CancellationToken)"/>
        /// and it's async counterpart.
        /// </summary>
        private string KeyWrapAlgorithm { get; }

        #region Transform Upload
        /// <summary>
        /// Encrypts the upload stream.
        /// </summary>
        /// <param name="content">Blob content to encrypt.</param>
        /// <param name="cancellationToken">
        /// Optional <see cref="CancellationToken"/> to propagate
        /// notifications that the operation should be cancelled.
        /// </param>
        /// <returns>Transformed content stream.</returns>
        protected override BlobContent TransformUploadContent(BlobContent content, CancellationToken cancellationToken = default)
            => TransformUploadContentInternal(content, false, cancellationToken).EnsureCompleted();

        /// <summary>
        /// Encrypts the upload stream.
        /// </summary>
        /// <param name="content">Blob content to encrypt.</param>
        /// <param name="cancellationToken">
        /// Optional <see cref="CancellationToken"/> to propagate
        /// notifications that the operation should be cancelled.
        /// </param>
        /// <returns>Transformed content stream.</returns>
        protected override async Task<BlobContent> TransformUploadContentAsync(BlobContent content, CancellationToken cancellationToken = default)
            => await TransformUploadContentInternal(content, true, cancellationToken).ConfigureAwait(false);

        private async Task<BlobContent> TransformUploadContentInternal(BlobContent content, bool async, CancellationToken cancellationToken)
        {
            (Stream nonSeekableCiphertext, EncryptionData encryptionData) = await EncryptInternal(
                content.Content,
                KeyWrapper,
                KeyWrapAlgorithm,
                async: async,
                cancellationToken).ConfigureAwait(false);

            var updatedMetadata = new Dictionary<string, string>(content.Metadata ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase)
            {
                { EncryptionConstants.EncryptionDataKey, encryptionData.Serialize() }
            };

            return new BlobContent
            {
                Content = new RollingBufferStream(
                    nonSeekableCiphertext,
                    EncryptionConstants.DefaultRollingBufferSize,
                    content.Content.Length + (EncryptionConstants.EncryptionBlockSize - content.Content.Length % EncryptionConstants.EncryptionBlockSize)),
                Metadata = updatedMetadata
            };

            //var generatedKey = CreateKey(EncryptionConstants.EncryptionKeySizeBits);

            //using (AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider() { Key = generatedKey })
            //{

            //    var encryptionData = async
            //        ? await EncryptionData.CreateInternal(aesProvider.IV, KeyWrapAlgorithm, generatedKey, KeyWrapper, true, cancellationToken).ConfigureAwait(false)
            //        : EncryptionData.CreateInternal(aesProvider.IV, KeyWrapAlgorithm, generatedKey, KeyWrapper, false, cancellationToken).EnsureCompleted();

            //    var encryptedContent = new RollingBufferStream(
            //        new CryptoStream(plaintext, aesProvider.CreateEncryptor(), CryptoStreamMode.Read),
            //        EncryptionConstants.DefaultRollingBufferSize,
            //        plaintext.Length + (EncryptionConstants.EncryptionBlockSize - plaintext.Length % EncryptionConstants.EncryptionBlockSize));
            //    return (encryptedContent, encryptionData);
            //}
        }
        #endregion

        #region TransformDownload
        /// <summary>
        ///
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        protected override HttpRange TransformDownloadSliceRange(HttpRange range)
        {
            return new EncryptedBlobRange(range).AdjustedRange;
        }

        /// <inheritdoc/>
        protected override BlobContent TransformDownloadSliceContent(
            BlobContent content,
            HttpRange originalRange,
            string receivedContentRange,
            CancellationToken cancellationToken = default)
            => TransformDownloadSliceContentInternal(
                content,
                originalRange,
                receivedContentRange,
                false,
                cancellationToken).EnsureCompleted();

        /// <inheritdoc/>
        protected override async Task<BlobContent> TransformDownloadSliceContentAsync(
            BlobContent content,
            HttpRange originalRange,
            string receivedContentRange,
            CancellationToken cancellationToken = default)
            => await TransformDownloadSliceContentInternal(
                content,
                originalRange,
                receivedContentRange,
                true,
                cancellationToken).ConfigureAwait(false);

        private async Task<BlobContent> TransformDownloadSliceContentInternal(
            BlobContent content,
            HttpRange originalRange,
            string receivedContentRange,
            bool async,
            CancellationToken cancellationToken)
        {
            AssertKeyAccessPresent();

            ContentRange? contentRange = string.IsNullOrWhiteSpace(receivedContentRange)
                ? default
                : ContentRange.Parse(receivedContentRange);

            EncryptionData encryptionData = GetAndValidateEncryptionData(content.Metadata);
            if (encryptionData == default)
            {
                return content; // TODO readjust range
            }

            bool ivInStream = originalRange.Offset >= 16; // TODO should this check originalRange? tests seem to pass

            var plaintext = await Utility.DecryptInternal(
                content.Content,
                encryptionData,
                ivInStream,
                KeyResolver,
                KeyWrapper,
                CanIgnorePadding(contentRange),
                async,
                cancellationToken).ConfigureAwait(false);

            // retrim start of stream to original requested location
            // keeping in mind whether we already pulled the IV out of the stream as well
            int gap = (int)(originalRange.Offset - (contentRange?.Start ?? 0))
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

            return new BlobContent
            {
                Content = new LengthLimitingStream(plaintext, originalRange.Length),
                Metadata = content.Metadata
            };
        }

        private void AssertKeyAccessPresent()
        {
            if (KeyWrapper == default && KeyResolver == default)
            {
                throw EncryptionErrors.NoKeyAccessor();
            }
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
        #endregion
    }
}
