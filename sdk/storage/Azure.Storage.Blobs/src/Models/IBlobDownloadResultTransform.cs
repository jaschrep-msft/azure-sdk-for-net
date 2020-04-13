// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Metadata = System.Collections.Generic.IDictionary<string, string>;

namespace Azure.Storage.Blobs.Models
{
    /// <summary>
    /// Transformation of a requested download.
    /// </summary>
    public interface IBlobDownloadResultTransform
    {
        /// <summary>
        /// Applies a transform to the defined download request.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="cancellationToken"></param>
        void Transform(BlobDownloadResult result, CancellationToken cancellationToken);

        /// <summary>
        /// Applies a transform asynchronously to the defined download request.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task TransformAsync(BlobDownloadResult result, CancellationToken cancellationToken);

        // different from blob download info as we need a mutable class and one that contains the context
        /// <summary>
        /// Mutable representation of a download result.
        /// </summary>
        public class BlobDownloadResult
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
            /// Returned content range.
            /// </summary>
            public string ContentRange { get; set; }

            /// <summary>
            /// Context for transforms to reference.
            /// </summary>
            public IReadOnlyDictionary<object, object> Context => _context;
            internal readonly Dictionary<object, object> _context = new Dictionary<object, object>();

            /// <summary>
            /// Adds a key/value pair to the context.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            public void AddContext(object key, object value) => _context.Add(key, value);
        }
    }
}
