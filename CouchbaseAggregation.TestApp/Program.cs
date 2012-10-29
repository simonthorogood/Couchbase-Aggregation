using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Couchbase;
using Enyim.Caching.Memcached;
using Newtonsoft.Json;

namespace CouchbaseAggregation.TestApp
{
	internal class Program
	{
		private static Random _random = new Random((int)DateTime.Now.Ticks);
		private static CouchbaseClient _client;

		private static List<Website> _websites;
		private static List<DateTime> _departDates;

		private static void Main(string[] args)
		{
			_client = new CouchbaseClient();
			setupReferenceData();

			//createBrowsePrices();
			//Console.WriteLine("Done");

			while (true)
			{
				var input = Console.ReadLine();
				if (input == "Q")
					break;

				var info = input.Split(',');
				updatePrice(info[0], info[1], Int32.Parse(info[2]), Decimal.Parse(info[3]));
			}
		}

		private static void updatePrice(string websiteId, string airlineId, int day, decimal price)
		{
			var date = new DateTime(2013, 1, day);

			var browsePrices = _client.GetView<WebsiteAirlineBrowsePrice>("test", "ByRouteDate", true)
				.StartKey(new object[] { "EDI", "LHR", 2013, 1, day })
				.EndKey(new object[] { "EDI", "LHR", 2013, 1, day + 1})
				.ToList();

			Console.WriteLine();

			Console.WriteLine("Before:");
			outputBrowsePrices(browsePrices, date);
			outputStats(date);

			var selectedBrowsePrice = browsePrices.FirstOrDefault(d => d.Website == websiteId && d.Airline == airlineId);
			if (selectedBrowsePrice != null)
			{
				selectedBrowsePrice.MinPrice = price;
			}
			else
			{
				selectedBrowsePrice = new WebsiteAirlineBrowsePrice
				      {
				      	Website = websiteId,
				      	Airline = airlineId,
				      	FromAirport = "EDI",
				      	ToAirport = "LHR",
						DepartDate = date,
				      	MinPrice = GenerateRandomPrice()
				      };
				browsePrices.Add(selectedBrowsePrice);
			}

			Console.WriteLine("Updated Price: {0}", selectedBrowsePrice.MinPrice.ToString("C"));
			Console.WriteLine();

			var minOverallPrice = browsePrices.Min(d => d.MinPrice);
			foreach (var browsePrice in browsePrices)
			{
				browsePrice.PriceDelta = browsePrice.MinPrice - minOverallPrice;
				storeBrowsePrice(browsePrice);
			}

			Thread.Sleep(200);

			Console.WriteLine("After:");
			outputBrowsePrices(browsePrices, date);
			outputStats(date);
		}

		private static void outputBrowsePrices(IEnumerable<WebsiteAirlineBrowsePrice> browsePrices, DateTime date)
		{
			Console.WriteLine("Date Stats ({0})", date.ToShortDateString());
			foreach (var browsePrice in browsePrices)
			{
				Console.WriteLine(
					"{0}.{1}:\t{2}\t{3}", browsePrice.Website, browsePrice.Airline, browsePrice.MinPrice.ToString("C"), browsePrice.PriceDelta.ToString("C"));
			}
			Console.WriteLine();
		}

		private static void outputStats(DateTime date)
		{
			var dayOfWeek = (int)date.DayOfWeek;
			var dowView = _client.GetView<WebsiteAirlineBrowsePriceSummary>("test", "ByRouteDayOfWeek")
				.StartKey(new object[] { "EDI", "LHR", dayOfWeek})
				.EndKey(new object[] { "EDI", "LHR", dayOfWeek + 1})
				.Group(true)
				.Stale(StaleMode.False);

			Console.WriteLine("Day Of Week Stats ({0})", dayOfWeek);
			foreach (var viewRow in dowView)
			{
				var avgMinPrice = viewRow.SumMinPrice / viewRow.Count;
				var avgPriceDelta = viewRow.SumPriceDelta / viewRow.Count;
				Console.WriteLine("{0}.{1}:\t{2}\t{3}", viewRow.Website, viewRow.Airline, avgMinPrice.ToString("C"), avgPriceDelta.ToString("C"));
			}
			Console.WriteLine();

			var routeView = _client.GetView<WebsiteAirlineBrowsePriceSummary>("test", "ByRoute")
				.Group(true)
				.Stale(StaleMode.False);

			Console.WriteLine("Route Stats");
			foreach (var viewRow in routeView)
			{
				var avgMinPrice = viewRow.SumMinPrice / viewRow.Count;
				var avgPriceDelta = viewRow.SumPriceDelta / viewRow.Count;
				Console.WriteLine("{0}.{1}:\t{2}\t{3}", viewRow.Website, viewRow.Airline, avgMinPrice.ToString("C"), avgPriceDelta.ToString("C"));
			}
			Console.WriteLine();
		}

		private static void setupReferenceData()
		{
			_websites = new List<Website>
			            {
			            	new Website
			            	{
			            		Id = "ba__",
			            		AirlinesIds = new List<string>
			            		              {
			            		              	"BA"
			            		              }
			            	},
							new Website
			            	{
			            		Id = "airf",
			            		AirlinesIds = new List<string>
			            		              {
			            		              	"AF"
			            		              }
			            	},
												new Website
			            	{
			            		Id = "luft",
			            		AirlinesIds = new List<string>
			            		              {
			            		              	"LH"
			            		              }
			            	},
			            	new Website
			            	{
			            		Id = "opuk",
			            		AirlinesIds = new List<string>
			            		              {
			            		              	"BA",
			            		              	"AF",
			            		              	"LH"
			            		              }
			            	}
			            };

			_departDates = new List<DateTime>();
			for (int i = 0; i < 28; i++)
			{
				_departDates.Add(new DateTime(2013, 1, 1).AddDays(i));
			}
		}

		private static void createBrowsePrices()
		{
			foreach (var departDate in _departDates)
			{
				var perDatePrices = new List<WebsiteAirlineBrowsePrice>();

				foreach (var website in _websites)
				{
					foreach (var airlineId in website.AirlinesIds)
					{
						var browsePrice = new WebsiteAirlineBrowsePrice
						                  {
						                  	Website = website.Id,
						                  	Airline = airlineId,
						                  	FromAirport = "EDI",
						                  	ToAirport = "LHR",
						                  	DepartDate = departDate,
						                  	MinPrice = GenerateRandomPrice()
						                  };

						perDatePrices.Add(browsePrice);
					}
				}

				var minPricePerDate = perDatePrices.Min(bp => bp.MinPrice);

				foreach (var browsePrice in perDatePrices)
				{
					browsePrice.PriceDelta = browsePrice.MinPrice - minPricePerDate;
					storeBrowsePrice(browsePrice);
				}
			}
		}

		private static decimal GenerateRandomPrice()
		{
			return ((decimal)_random.NextDouble()) * 100.0m + _random.Next(10, 100);
		}

		private static void storeBrowsePrice(WebsiteAirlineBrowsePrice browsePrice)
		{
			var result =
				_client.Store(
					StoreMode.Set, browsePrice.Key,
					JsonConvert.SerializeObject(browsePrice, new JsonSerializerSettings {DateFormatHandling = DateFormatHandling.IsoDateFormat}));

			if (!result)
				Console.WriteLine("Failed to store: {0}", browsePrice.Key);
		}
	}
}