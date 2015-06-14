using GeoPrediction.Portable.Entities;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoPrediction.ProcessorWorkerRole.EventProcessing
{
    public class ReadingEntity : TableEntity
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Heading { get; set; }
        public double Altitude { get; set; }
        public double? Speed { get; set; }
        public DateTime TimestampUTC { get; set; }
        public string HHMMSSUTC { get; set; }
        public string ReverseGeocode { get; set; }
        public string Name { get; set; }

        public ReadingEntity(Reading reading)
        {
            this.Latitude = reading.Latitude;
            this.Longitude = reading.Longitude;
            this.Heading = reading.Heading;
            this.Altitude = reading.Altitude;
            this.Speed = reading.Speed;
            this.TimestampUTC = reading.TimestampUTC;
            this.HHMMSSUTC = reading.HHMMSSUTC;
            this.ReverseGeocode = reading.ReverseGeocode;
            this.Name = reading.Name;
            this.PartitionKey = reading.UniqueDeviceId;
            this.RowKey = reading.Ticks.ToString();
        }
    }
}
