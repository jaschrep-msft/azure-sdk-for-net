// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Blobs.Specialized.Models;
using Azure.Storage.Common.Cryptography;
using Metadata = System.Collections.Generic.IDictionary<string, string>;

namespace Azure.Storage.Blobs.Cryptography
{
    /// <summary>
    /// Cryptography extensions for <see cref="BlobClientOptions"/>.
    /// </summary>
    public static class BlobClientOptionsExtensions
    {
        /// <summary>
        /// Appends client-side encryption transforms to these client options.
        /// </summary>
        /// <param name="clientOptions"></param>
        /// <param name="encryptionOptions"></param>
        public static void AddClientSideEncryption(
           this BlobClientOptions clientOptions,
           ClientSideEncryptionOptions encryptionOptions)
        {
            encryptionOptions = encryptionOptions ?? throw new ArgumentException("Missing encryption options for client-side encryption.", nameof(encryptionOptions));

            clientOptions.AddUploadParametersTransform(GetCryptoUploadParameterTransform(encryptionOptions));
            clientOptions.AddDownloadParametersTransform(GetCryptoDownloadParametersTransform());
            clientOptions.AddDownloadResultTransform(GetCryptoDownloadResultTransform(encryptionOptions));
        }

        private static IBlobUploadParameterTransform GetCryptoUploadParameterTransform(ClientSideEncryptionOptions encryptionOptions)
            => new BlobCryptoUploadParameterTransform(encryptionOptions);

        private static IBlobDownloadParameterTransform GetCryptoDownloadParametersTransform()
            => throw new NotImplementedException();

        private static IBlobDownloadResultTransform GetCryptoDownloadResultTransform(ClientSideEncryptionOptions encryptionOptions)
        {
            throw new NotImplementedException();
        }
    }
}
