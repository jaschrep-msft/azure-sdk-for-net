// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.Management.Purview.Models
{
    using System.Linq;

    /// <summary>
    /// The URIs that are the public endpoints of the account.
    /// </summary>
    public partial class AccountPropertiesEndpoints : AccountEndpoints
    {
        /// <summary>
        /// Initializes a new instance of the AccountPropertiesEndpoints class.
        /// </summary>
        public AccountPropertiesEndpoints()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the AccountPropertiesEndpoints class.
        /// </summary>
        /// <param name="catalog">Gets the catalog endpoint.</param>
        /// <param name="guardian">Gets the guardian endpoint.</param>
        /// <param name="scan">Gets the scan endpoint.</param>
        public AccountPropertiesEndpoints(string catalog = default(string), string guardian = default(string), string scan = default(string))
            : base(catalog, guardian, scan)
        {
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

    }
}
