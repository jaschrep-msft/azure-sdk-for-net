// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Storage.Blobs.Models
{
    /// <summary>
    /// Transformation of a requested download.
    /// </summary>
    public interface IBlobDownloadParameterTransform
    {
        /// <summary>
        /// Applies a transform to the defined download request.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        void Transform(BlobDownloadParameters parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Applies a transform asynchronously to the defined download request.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task TransformAsync(BlobDownloadParameters parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Arguments for a download request.
        /// </summary>
        public class BlobDownloadParameters
        {
            internal BlobDownloadParameters() { }

            /// <summary>
            /// The range to request from the service.
            /// </summary>
            public HttpRange RequestedRange { get; set; }

            /// <summary>
            /// The conditions on the request.
            /// </summary>
            public BlobRequestConditions RequestConditions { get; set; }

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
