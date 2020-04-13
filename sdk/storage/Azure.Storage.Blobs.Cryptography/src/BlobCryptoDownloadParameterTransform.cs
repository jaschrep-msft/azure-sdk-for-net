// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized.Models;
using Azure.Storage.Common.Cryptography;

namespace Azure.Storage.Blobs.Cryptography
{
    internal class BlobCryptoDownloadParameterTransform : IBlobDownloadParameterTransform
    {
        public void Transform(IBlobDownloadParameterTransform.BlobDownloadParameters parameters, CancellationToken cancellationToken)
        {
            var encryptedRange = new EncryptedBlobRange(parameters.RequestedRange);

            parameters.RequestedRange = encryptedRange.AdjustedRange;
            parameters.AddContext(EncryptionConstants.AdjustedRangeContextKey, encryptedRange);
        }

        public Task TransformAsync(IBlobDownloadParameterTransform.BlobDownloadParameters parameters, CancellationToken cancellationToken)
        {
            Transform(parameters, cancellationToken);
            return Task.CompletedTask;
        }
    }
}
