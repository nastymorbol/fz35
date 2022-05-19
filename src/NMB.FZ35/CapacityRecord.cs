using System;
using System.Text.Json.Serialization;

namespace FZ35
{
    public record CapacityRecord
    {
        public double Voltage { get; set; }
        public double Ampere { get; set; }
        public double Energy { get; set; }
        public double RemainingTime { get; set; }

        [JsonIgnore] public double Power => Voltage * Ampere;
        
        public DateTimeOffset TimeStamp { get; set; }
    }
}