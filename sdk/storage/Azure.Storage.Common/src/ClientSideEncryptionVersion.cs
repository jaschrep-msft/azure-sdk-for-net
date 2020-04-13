// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace Azure.Storage
{
    /// <summary>
    /// Specifies the version of Azure Storage's client-side encryption offering.
    /// </summary>
    public readonly struct ClientSideEncryptionVersion
    {
        private const string v1_0 = "1.0";

        private readonly string _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSideEncryptionVersion"/> structure.
        /// </summary>
        /// <param name="value">The string value of the instance.</param>
        public ClientSideEncryptionVersion(string value)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Version 1.0.
        /// </summary>
        public static ClientSideEncryptionVersion V1_0 { get; } = new ClientSideEncryptionVersion(v1_0);

        /// <summary>
        /// Determines if two <see cref="ClientSideEncryptionVersion"/> values are the same.
        /// </summary>
        /// <param name="left">The first <see cref="ClientSideEncryptionVersion"/> to compare.</param>
        /// <param name="right">The second <see cref="ClientSideEncryptionVersion"/> to compare.</param>
        /// <returns>True if <paramref name="left"/> and <paramref name="right"/> are the same; otherwise, false.</returns>
        public static bool operator ==(ClientSideEncryptionVersion left, ClientSideEncryptionVersion right) => left.Equals(right);

        /// <summary>
        /// Determines if two <see cref="ClientSideEncryptionVersion"/> values are different.
        /// </summary>
        /// <param name="left">The first <see cref="ClientSideEncryptionVersion"/> to compare.</param>
        /// <param name="right">The second <see cref="ClientSideEncryptionVersion"/> to compare.</param>
        /// <returns>True if <paramref name="left"/> and <paramref name="right"/> are different; otherwise, false.</returns>
        public static bool operator !=(ClientSideEncryptionVersion left, ClientSideEncryptionVersion right) => !left.Equals(right);

        /// <summary>
        /// Converts a string to a <see cref="ClientSideEncryptionVersion"/>.
        /// </summary>
        /// <param name="value">The string value to convert.</param>
        public static implicit operator ClientSideEncryptionVersion(string value) => new ClientSideEncryptionVersion(value);

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => obj is ClientSideEncryptionVersion other && Equals(other);

        /// <inheritdoc/>
        public bool Equals(ClientSideEncryptionVersion other) => string.Equals(_value, other._value, StringComparison.Ordinal);

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => _value?.GetHashCode() ?? 0;

        /// <inheritdoc/>
        public override string ToString() => _value;
    }
}
