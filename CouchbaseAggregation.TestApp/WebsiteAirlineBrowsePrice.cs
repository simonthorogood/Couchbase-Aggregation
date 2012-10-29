using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace CouchbaseAggregation.TestApp
{
	[Serializable]
	public class WebsiteAirlineBrowsePrice
	{
		public string Website { get; set; }

		public string Airline { get; set; }

		public string FromAirport { get; set; }

		public string ToAirport { get; set; }

		public DateTime DepartDate { get; set; }

		public Decimal MinPrice { get; set; }

		public Decimal PriceDelta { get; set; }

		[JsonIgnore]
		public string Key
		{
			get { return String.Format("{0}.{1}.{2}.{3}.{4}", Website, Airline, FromAirport, ToAirport, DepartDate.ToString("yyyy-MM-dd")); }
		}
	}
}
