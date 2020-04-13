// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Common;
using Azure.Storage.Common.Cryptography;
using Azure.Storage.Common.Cryptography.Models;
using static Azure.Storage.Common.Cryptography.Utility;

namespace Azure.Storage.Blobs.Cryptography
{
    internal class BlobCryptoUploadParameterTransform : IBlobUploadParameterTransform
    {
        private readonly ClientSideEncryptionOptions _encryptionOptions;

        public BlobCryptoUploadParameterTransform(ClientSideEncryptionOptions encryptionOptions)
        {
            this._encryptionOptions = encryptionOptions;
        }

        public void Transform(IBlobUploadParameterTransform.BlobUploadParameters parameters, CancellationToken cancellationToken)
        {
            TransformInternal(parameters, false, cancellationToken).EnsureCompleted();
        }

        public async Task TransformAsync(IBlobUploadParameterTransform.BlobUploadParameters parameters, CancellationToken cancellationToken)
        {
            await TransformInternal(parameters, true, cancellationToken).ConfigureAwait(false);
        }

        private async Task TransformInternal(
            IBlobUploadParameterTransform.BlobUploadParameters parameters,
            bool async,
            CancellationToken cancellationToken)
        {
            (Stream nonSeekableCiphertext, EncryptionData encryptionData) = await EncryptInternal(
                parameters.ContentStream,
                _encryptionOptions.KeyEncryptionKey,
                _encryptionOptions.EncryptionKeyWrapAlgorithm,
                async,
                cancellationToken).ConfigureAwait(false);

            var updatedMetadata = new Dictionary<string, string>(parameters.Metadata ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase)
            {
                { EncryptionConstants.EncryptionDataKey, encryptionData.Serialize() }
            };

            parameters.ContentStream = new RollingBufferStream(
                nonSeekableCiphertext,
                EncryptionConstants.DefaultRollingBufferSize,
                parameters.ContentStream.Length + (EncryptionConstants.EncryptionBlockSize - parameters.ContentStream.Length % EncryptionConstants.EncryptionBlockSize));
            parameters.Metadata = updatedMetadata;
        }
    }
}
