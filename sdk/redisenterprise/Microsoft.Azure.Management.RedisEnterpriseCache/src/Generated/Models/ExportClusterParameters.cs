// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.Management.RedisEnterprise.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    /// <summary>
    /// Export an RDB file into a target database
    /// </summary>
    /// <remarks>
    /// Parameters for a Redis Enterprise export operation.
    /// </remarks>
    public partial class ExportClusterParameters
    {
        /// <summary>
        /// Initializes a new instance of the ExportClusterParameters class.
        /// </summary>
        public ExportClusterParameters()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the ExportClusterParameters class.
        /// </summary>
        /// <param name="sasUri">SAS URI for the target directory to export
        /// to</param>
        public ExportClusterParameters(string sasUri)
        {
            SasUri = sasUri;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets SAS URI for the target directory to export to
        /// </summary>
        [JsonProperty(PropertyName = "sasUri")]
        public string SasUri { get; set; }

    }
}
