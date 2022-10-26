using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ApiAccess
{
    public static class CodeFormat
    {
        public const string unique = "unique";
        public const string multi = "multi";
        public const string non_residential_zoning = "non_residential_zoning";
    }

    // This is a coding exercise that I worked on recently. Jessie Cruz
    // Api call
    // Csv read/write

    //Instruction:
    //You are given a customer file which contains Id of the Store the Customer
    //visited, Customer Id, Customer postal code and the number of total visits.You can assume that the file
    //format is always the same.
    //You need to read the file and using EA Prizm API assign a PRIZM segment code to each record in the file
    //based on Customer postal code.API is publicly available and you don’t need to obtain any
    //authentication credentials.But keep in mind that there still could be a quota on number of request.
    //See appendix for API details. You can also learn more about PRIZM segmentation system on our website.
    //As a second step of the process, you need to split Customers into two target groups based on their
    //PRIZM segment codes.Customers in the first target group have segment codes between 1 and 30
    //inclusive and customers in the second group have segment codes between 31 and 67.
    //Once customers are split into target groups summarize Total_Visits values for each target group.
  
    internal class Program
    {
        static void Main(string[] args)
        {
            ApiHelper.Initialize();
            Process();
            Console.ReadLine();
        }

        /// <summary>
        /// Main method
        /// </summary>
        private static async void Process()
        {
            IList<string> postalCodeList = new List<string>();
            IList<Customer> customerList = new List<Customer>();
            IList<Customer> firstSegmentList = new List<Customer>();
            IList<Customer> secondSegmentList = new List<Customer>();

            //Get data from source csv
            RetrieveSource(ref customerList, ref postalCodeList);

            //Initialize api
            ApiHelper.Initialize();

            //Populate segment code dictionary
            Dictionary<string, int>  segmentCodeDictionary = await BuildCodeDictionary(postalCodeList);

            int totalVisitSegment1 = 0;
            int totalVisitSegment2 = 0;

            foreach (var item in customerList)
            {
                item.SegmentCode = segmentCodeDictionary.FirstOrDefault(x => x.Key == item.CELPostalCode).Value;

                if (item.SegmentCode >= 1 && item.SegmentCode <= 30)
                {
                    firstSegmentList.Add(item);
                    totalVisitSegment1 += item.TotalVisits;
                }
                else if (item.SegmentCode >= 31 && item.SegmentCode <= 67)
                {
                    secondSegmentList.Add(item);
                    totalVisitSegment2 += item.TotalVisits;
                }
            }

            //Write data to target csv
            PopulateTarget(firstSegmentList, secondSegmentList, totalVisitSegment1, totalVisitSegment2);
        }

        /// <summary>
        /// Create dictionary of postal code and segment code 
        /// </summary>
        /// <param name="postalCodeList"></param>
        /// <returns></returns>
        private static async Task<Dictionary<string, int>> BuildCodeDictionary(IList<string> postalCodeList)
        {
            StringBuilder uri = new StringBuilder();
            Dictionary<string, int> segmentCodeDictionary = new Dictionary<string, int>();

            foreach (var item in postalCodeList)
            {
                uri.Clear();
                uri.Append(string.Format("https://prizm.environicsanalytics.com/api/pcode/get_segment?postal_code=" + item));

                using (HttpResponseMessage response = await ApiHelper.ApiClient.GetAsync(uri.ToString()))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();

                        dynamic dynJson = JsonConvert.DeserializeObject(result);

                        if (dynJson["format"].Value == CodeFormat.multi)
                        {
                            segmentCodeDictionary.Add(item, int.Parse((dynJson.data[0])["prizm_id"].Value.ToString()));
                        }
                        else if (dynJson["format"].Value == CodeFormat.unique)
                        {
                            segmentCodeDictionary.Add(item, int.Parse((dynJson.data).Value.ToString()));
                        }
                        else if (dynJson["format"].Value == CodeFormat.non_residential_zoning)
                        {
                            segmentCodeDictionary.Add(item, 0);
                        }
                    }
                }
            }

            return segmentCodeDictionary;
        }

        /// <summary>
        /// Get data from source csv
        /// </summary>
        /// <param name="customerList"></param>
        /// <param name="postalCodeList"></param>
        private static void RetrieveSource(ref IList<Customer> customerList, ref IList<string> postalCodeList)
        {
            var projectFolder = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
            var file = Path.Combine(projectFolder, @"File\Source.csv");

            using (var reader = new StreamReader(file))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    if (line.Contains("StoreID"))
                    {
                        continue;
                    }

                    var values = line.Split(',');

                    postalCodeList.Add(values[2]);

                    customerList.Add(new Customer
                    {
                        StoreId = int.Parse(values[0]),
                        CustomerId = values[1],
                        CELPostalCode = values[2],
                        TotalVisits = int.Parse(values[3])
                    });
                }

                postalCodeList = postalCodeList.Distinct().ToList();
            }
        }

        /// <summary>
        /// Write data to target csv
        /// </summary>
        /// <param name="firstSegmentList"></param>
        /// <param name="secondSegmentList"></param>
        /// <param name="totalVisitSegment1"></param>
        /// <param name="totalVisitSegment2"></param>
        private static void PopulateTarget(IList<Customer> firstSegmentList, IList<Customer> secondSegmentList, int totalVisitSegment1, int totalVisitSegment2)
        {
            var projectFolderTarget = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
            var target = Path.Combine(projectFolderTarget, @"File\Target.csv");

            using (var stream = File.CreateText(target))
            {
                stream.WriteLine("Segment Code: 1-30");
                string headerRow = string.Format("{0},{1},{2},{3},{4}",
                    nameof(Customer.StoreId),
                    nameof(Customer.CustomerId),
                    nameof(Customer.CELPostalCode),
                    nameof(Customer.TotalVisits),
                    nameof(Customer.SegmentCode));
                stream.WriteLine(headerRow);

                for (int i = 0; i < firstSegmentList.Count(); i++)
                {
                    string storeId = firstSegmentList[i].StoreId.ToString();
                    string customerId = firstSegmentList[i].CustomerId.ToString();
                    string celPostal = firstSegmentList[i].CELPostalCode.ToString();
                    string totalVisits = firstSegmentList[i].TotalVisits.ToString();
                    string segmentCode = firstSegmentList[i].SegmentCode.ToString();
                    string csvRow = string.Format("{0},{1},{2},{3},{4}", storeId, customerId, celPostal, totalVisits, segmentCode);

                    stream.WriteLine(csvRow);
                }
                stream.WriteLine("Total no. of visits:" + totalVisitSegment1);

                stream.WriteLine(string.Empty);
                stream.WriteLine("Segment Code: 30-67");
                stream.WriteLine(headerRow);

                for (int i = 0; i < secondSegmentList.Count(); i++)
                {
                    string storeID = secondSegmentList[i].StoreId.ToString();
                    string customerID = secondSegmentList[i].CustomerId.ToString();
                    string celPostal = secondSegmentList[i].CELPostalCode.ToString();
                    string totalVisits = secondSegmentList[i].TotalVisits.ToString();
                    string segmentCode = secondSegmentList[i].SegmentCode.ToString();
                    string csvRow = string.Format("{0},{1},{2},{3},{4}", storeID, customerID, celPostal, totalVisits, segmentCode);

                    stream.WriteLine(csvRow);
                }

                stream.WriteLine("Total no. of visits:" + totalVisitSegment2);
            }
        }
    }
}