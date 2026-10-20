using System;
using System.Collections.Generic;
using System.Text;

namespace UShort.SQLModels
{
    public class EventModel
    {
        public int Id { get; set; }
        public char EventType { get; set; }
        public int LinkId { get; set; }
        public DateTime Timestamp { get; set; }
        public int? ContinentGeonameId { get; set; }
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }
        public int? AccuracyRadius { get; set; }
        public int? CountryGeonameId { get; set; }
        public int? CityGeonameId { get; set; }
        public string? UserAgent { get; set; }
    }
}
