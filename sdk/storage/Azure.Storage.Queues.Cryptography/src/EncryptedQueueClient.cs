// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Cryptography;
using Azure.Core.Pipeline;
using Azure.Storage.Common.Cryptography;
using Azure.Storage.Common.Cryptography.Models;
using Azure.Storage.Queues.Specialized.Models;
using static Azure.Storage.Common.Cryptography.Utility;

namespace Azure.Storage.Queues.Specialized
{
    /// <summary>
    /// The <see cref="EncryptedQueueClient"/> allows you to manipulate
    /// Azure Storage queues with client-side encryption. See
    /// <see cref="QueueClient"/> for more details.
    /// Note that encrypting messages may make their content slightly
    /// longer, and encryption metadata will also extent that length.
    /// Impact is estimated to be no more than 0.5 KB towards the maximum
    /// message size, and so impact should be manageable.
    /// </summary>
    public class EncryptedQueueClient : QueueClient
    {
        /// <summary>
        /// The wrapper used to wrap the content encryption key.
        /// </summary>
        private IKeyEncryptionKey KeyWrapper { get; }

        /// <summary>
        /// Resolver to get the correct <see cref="IKeyEncryptionKey"/>
        /// for unwrapping a message's content encryption key.
        /// </summary>
        private IKeyEncryptionKeyResolver KeyResolver { get;  }

        /// <summary>
        /// The algorithm identifier to use with the <see cref="KeyWrapper"/>. Value to pass into
        /// <see cref="IKeyEncryptionKey.WrapKey(string, ReadOnlyMemory{byte}, CancellationToken)"/>
        /// and it's async counterpart.
        /// </summary>
        private string KeyWrapAlgorithm { get; }

        /// <inheritdoc/>
        protected override string TransformMessageUpload(string messageToUpload, CancellationToken cancellationToken)
            => TransformMessageUploadInternal(messageToUpload, false, cancellationToken).EnsureCompleted();

        /// <inheritdoc/>
        protected override async Task<string> TransformMessageUploadAsync(string messageToUpload, CancellationToken cancellationToken)
            => await TransformMessageUploadInternal(messageToUpload, true, cancellationToken).ConfigureAwait(false);

        private async Task<string> TransformMessageUploadInternal(string messageToUpload, bool async, CancellationToken cancellationToken)
        {
            var bytesToEncrypt = Encoding.UTF8.GetBytes(messageToUpload);
            (byte[] ciphertext, EncryptionData encryptionData) = await BufferedEncryptInternal(
                new MemoryStream(bytesToEncrypt),
                KeyWrapper,
                KeyWrapAlgorithm,
                async: async,
                cancellationToken).ConfigureAwait(false);

            return EncryptedMessageSerializer.Serialize(new EncryptedMessage
            {
                EncryptedMessageContents = Convert.ToBase64String(ciphertext),
                EncryptionData = encryptionData
            });
        }

        /// <inheritdoc/>
        protected override string TransformMessageDownload(string downloadedMessage, CancellationToken cancellationToken)
            => TransformMessageDownloadInternal(downloadedMessage, false, cancellationToken).EnsureCompleted();

        /// <inheritdoc/>
        protected override async Task<string> TransformMessageDownloadAsync(string downloadedMessage, CancellationToken cancellationToken)
            => await TransformMessageDownloadInternal(downloadedMessage, true, cancellationToken).ConfigureAwait(false);

        private async Task<string> TransformMessageDownloadInternal(string downloadedMessage, bool async, CancellationToken cancellationToken)
        {
            if (!EncryptedMessageSerializer.TryDeserialize(downloadedMessage, out var encryptedMessage))
            {
                return downloadedMessage; // not recognized as client-side encrypted message
            }
            var decryptedMessageStream = await DecryptInternal(
                new MemoryStream(Convert.FromBase64String(encryptedMessage.EncryptedMessageContents)),
                encryptedMessage.EncryptionData,
                ivInStream: false,
                KeyResolver,
                KeyWrapper,
                noPadding: false,
                async: async,
                cancellationToken).ConfigureAwait(false);

            return new StreamReader(decryptedMessageStream, Encoding.UTF8).ReadToEnd();
        }
    }
}
