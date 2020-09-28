// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// <auto-generated/>

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Core;

namespace Azure.ResourceManager.Insights.Models
{
    /// <summary> The autoscale setting resource. </summary>
    public partial class AutoscaleSettingResource : Resource
    {
        /// <summary> Initializes a new instance of AutoscaleSettingResource. </summary>
        /// <param name="location"> Resource location. </param>
        /// <param name="profiles"> the collection of automatic scaling profiles that specify different scaling parameters for different time periods. A maximum of 20 profiles can be specified. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="location"/> or <paramref name="profiles"/> is null. </exception>
        public AutoscaleSettingResource(string location, IEnumerable<AutoscaleProfile> profiles) : base(location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }
            if (profiles == null)
            {
                throw new ArgumentNullException(nameof(profiles));
            }

            Profiles = profiles.ToList();
            Notifications = new ChangeTrackingList<AutoscaleNotification>();
        }

        /// <summary> Initializes a new instance of AutoscaleSettingResource. </summary>
        /// <param name="id"> Azure resource Id. </param>
        /// <param name="name"> Azure resource name. </param>
        /// <param name="type"> Azure resource type. </param>
        /// <param name="location"> Resource location. </param>
        /// <param name="tags"> Resource tags. </param>
        /// <param name="profiles"> the collection of automatic scaling profiles that specify different scaling parameters for different time periods. A maximum of 20 profiles can be specified. </param>
        /// <param name="notifications"> the collection of notifications. </param>
        /// <param name="enabled"> the enabled flag. Specifies whether automatic scaling is enabled for the resource. The default value is &apos;true&apos;. </param>
        /// <param name="namePropertiesName"> the name of the autoscale setting. </param>
        /// <param name="targetResourceUri"> the resource identifier of the resource that the autoscale setting should be added to. </param>
        internal AutoscaleSettingResource(string id, string name, string type, string location, IDictionary<string, string> tags, IList<AutoscaleProfile> profiles, IList<AutoscaleNotification> notifications, bool? enabled, string namePropertiesName, string targetResourceUri) : base(id, name, type, location, tags)
        {
            Profiles = profiles;
            Notifications = notifications;
            Enabled = enabled;
            NamePropertiesName = namePropertiesName;
            TargetResourceUri = targetResourceUri;
        }

        /// <summary> the collection of automatic scaling profiles that specify different scaling parameters for different time periods. A maximum of 20 profiles can be specified. </summary>
        public IList<AutoscaleProfile> Profiles { get; }
        /// <summary> the collection of notifications. </summary>
        public IList<AutoscaleNotification> Notifications { get; }
        /// <summary> the enabled flag. Specifies whether automatic scaling is enabled for the resource. The default value is &apos;true&apos;. </summary>
        public bool? Enabled { get; set; }
        /// <summary> the name of the autoscale setting. </summary>
        public string NamePropertiesName { get; set; }
        /// <summary> the resource identifier of the resource that the autoscale setting should be added to. </summary>
        public string TargetResourceUri { get; set; }
    }
}