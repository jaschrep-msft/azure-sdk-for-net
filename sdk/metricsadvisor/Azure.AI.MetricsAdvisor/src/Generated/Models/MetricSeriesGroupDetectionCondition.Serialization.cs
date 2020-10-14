// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// <auto-generated/>

#nullable disable

using System.Text.Json;
using Azure.Core;

namespace Azure.AI.MetricsAdvisor.Models
{
    public partial class MetricSeriesGroupDetectionCondition : IUtf8JsonSerializable
    {
        void IUtf8JsonSerializable.Write(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("group");
            writer.WriteObjectValue(SeriesGroupKey);
            if (Optional.IsDefined(CrossConditionsOperator))
            {
                writer.WritePropertyName("conditionOperator");
                writer.WriteStringValue(CrossConditionsOperator.Value.ToString());
            }
            if (Optional.IsDefined(SmartDetectionCondition))
            {
                writer.WritePropertyName("smartDetectionCondition");
                writer.WriteObjectValue(SmartDetectionCondition);
            }
            if (Optional.IsDefined(HardThresholdCondition))
            {
                writer.WritePropertyName("hardThresholdCondition");
                writer.WriteObjectValue(HardThresholdCondition);
            }
            if (Optional.IsDefined(ChangeThresholdCondition))
            {
                writer.WritePropertyName("changeThresholdCondition");
                writer.WriteObjectValue(ChangeThresholdCondition);
            }
            writer.WriteEndObject();
        }

        internal static MetricSeriesGroupDetectionCondition DeserializeMetricSeriesGroupDetectionCondition(JsonElement element)
        {
            DimensionKey group = default;
            Optional<DetectionConditionsOperator> conditionOperator = default;
            Optional<SmartDetectionCondition> smartDetectionCondition = default;
            Optional<HardThresholdCondition> hardThresholdCondition = default;
            Optional<ChangeThresholdCondition> changeThresholdCondition = default;
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("group"))
                {
                    group = DimensionKey.DeserializeDimensionKey(property.Value);
                    continue;
                }
                if (property.NameEquals("conditionOperator"))
                {
                    if (property.Value.ValueKind == JsonValueKind.Null)
                    {
                        property.ThrowNonNullablePropertyIsNull();
                        continue;
                    }
                    conditionOperator = new DetectionConditionsOperator(property.Value.GetString());
                    continue;
                }
                if (property.NameEquals("smartDetectionCondition"))
                {
                    if (property.Value.ValueKind == JsonValueKind.Null)
                    {
                        property.ThrowNonNullablePropertyIsNull();
                        continue;
                    }
                    smartDetectionCondition = Models.SmartDetectionCondition.DeserializeSmartDetectionCondition(property.Value);
                    continue;
                }
                if (property.NameEquals("hardThresholdCondition"))
                {
                    if (property.Value.ValueKind == JsonValueKind.Null)
                    {
                        property.ThrowNonNullablePropertyIsNull();
                        continue;
                    }
                    hardThresholdCondition = Models.HardThresholdCondition.DeserializeHardThresholdCondition(property.Value);
                    continue;
                }
                if (property.NameEquals("changeThresholdCondition"))
                {
                    if (property.Value.ValueKind == JsonValueKind.Null)
                    {
                        property.ThrowNonNullablePropertyIsNull();
                        continue;
                    }
                    changeThresholdCondition = Models.ChangeThresholdCondition.DeserializeChangeThresholdCondition(property.Value);
                    continue;
                }
            }
            return new MetricSeriesGroupDetectionCondition(Optional.ToNullable(conditionOperator), smartDetectionCondition.Value, hardThresholdCondition.Value, changeThresholdCondition.Value, group);
        }
    }
}