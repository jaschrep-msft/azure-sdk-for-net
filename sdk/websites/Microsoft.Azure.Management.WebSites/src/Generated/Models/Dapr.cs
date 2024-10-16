// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.Management.WebSites.Models
{
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Container App Dapr configuration.
    /// </summary>
    public partial class Dapr
    {
        /// <summary>
        /// Initializes a new instance of the Dapr class.
        /// </summary>
        public Dapr()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the Dapr class.
        /// </summary>
        /// <param name="enabled">Boolean indicating if the Dapr side car is
        /// enabled</param>
        /// <param name="appId">Dapr application identifier</param>
        /// <param name="appPort">Port on which the Dapr side car</param>
        /// <param name="components">Collection of Dapr components</param>
        public Dapr(bool? enabled = default(bool?), string appId = default(string), int? appPort = default(int?), IList<DaprComponent> components = default(IList<DaprComponent>))
        {
            Enabled = enabled;
            AppId = appId;
            AppPort = appPort;
            Components = components;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets boolean indicating if the Dapr side car is enabled
        /// </summary>
        [JsonProperty(PropertyName = "enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets dapr application identifier
        /// </summary>
        [JsonProperty(PropertyName = "appId")]
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets port on which the Dapr side car
        /// </summary>
        [JsonProperty(PropertyName = "appPort")]
        public int? AppPort { get; set; }

        /// <summary>
        /// Gets or sets collection of Dapr components
        /// </summary>
        [JsonProperty(PropertyName = "components")]
        public IList<DaprComponent> Components { get; set; }

    }
}
