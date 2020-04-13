// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Metadata = System.Collections.Generic.IDictionary<string, string>;

namespace Azure.Storage.Blobs.Models
{
    /// <summary>
    /// Transformation of a requested upload.
    /// </summary>
    public interface IBlobUploadParameterTransform
    {
        /// <summary>
        /// Applies a transform to the defined download request.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        void Transform(BlobUploadParameters parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Applies a transform asynchronously to the defined download request.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task TransformAsync(BlobUploadParameters parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Arguments for a download request.
        /// </summary>
        public class BlobUploadParameters
        {
            /// <summary>
            /// Data to upload.
            /// </summary>
            public Stream ContentStream { get; set; }

            /// <summary>
            /// HTTP headers for blob data.
            /// </summary>
            public BlobHttpHeaders HttpHeaders { get; set; }

            /// <summary>
            /// Blob metadata.
            /// </summary>
            public Metadata Metadata { get; set; }

            /// <summary>
            /// Access conditions on this blob request.
            /// </summary>
            public BlobRequestConditions Conditions { get; set; }

            /// <summary>
            /// Blob access tier.
            /// </summary>
            public AccessTier? AccessTier { get; set; }

            // IProgress<long> progressHandler = default // I don't think we need this
        }
    }
}
