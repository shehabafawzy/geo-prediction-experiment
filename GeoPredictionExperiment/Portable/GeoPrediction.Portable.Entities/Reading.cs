﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeoPrediction.Portable.Entities
{
    public class Reading
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Heading { get; set; }
        public double Altitude { get; set; }
        public int Speed { get; set; }
        public string TimestampUTC { get; set; }
        public long Ticks { get; set; }
        public string HHMMSSUTC { get; set; }
        public string ReverseGeocode { get; set; }
        public string UniqueDeviceId { get; set; }
        public string Name { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public int DurationInMinutes { get; set; }
    }
}
