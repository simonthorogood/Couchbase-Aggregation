using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace CouchbaseAggregation.TestApp
{
	[Serializable]
	public class WebsiteAirlineBrowsePriceSummary
	{
		public string Website { get; set; }

		public string Airline { get; set; }

		public int Count { get; set; }

		public decimal SumMinPrice { get; set; }

		public decimal SumPriceDelta { get; set; }
		
		[JsonIgnore]
		public string Key
		{
			get { return String.Format("{0}.{1}", Website, Airline); }
		}
	}
}
