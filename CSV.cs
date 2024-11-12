using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ScheduleValidator
{
	internal class CSV
	{
		internal static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		internal List<TestParameters> Import(string csvFileName, bool convertToE164, string countryCode, string removeFirstDigitIf)
		{

			var config = new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				MissingFieldFound = null,

			};

			try
			{
				using (var reader = new StreamReader(csvFileName)){ }
			}
			catch (Exception e)
			{
				ColorConsole.WriteError("Error: " + e.Message);
				Environment.Exit(1);

			}


			using (var reader = new StreamReader(csvFileName))
			{
				using (var csv = new CsvReader(reader, config))
				{
					csv.Context.RegisterClassMap<TestParametersMap>();

					try
					{
						var records = csv.GetRecords<TestParameters>().ToList();

						foreach (var item in records)
						{
							if (!string.IsNullOrEmpty(item.DID))
							{

								if (convertToE164)
								{
									item.DID = ConvertToE164(item.DID, countryCode, removeFirstDigitIf);

								}
								else
								{
									item.DID = item.DID.Replace("-", "").Replace(" ", "");

								}

							}

						}
						Logger.Info($"csv import done.");
						return records;
					}
					catch (Exception e)
					{
						ColorConsole.WriteError("Error: " + e.Message);
						Environment.Exit(1);
						return null;
					}

				}
			}


		}

		internal static void Export(List<ValidationResults> evaluationResultsList,string csvFileName)
		{

			string currentPath = Directory.GetCurrentDirectory();
			createCSVFolder(currentPath);

			if (string.IsNullOrEmpty(Path.GetExtension(csvFileName)))
			{
				csvFileName = Path.ChangeExtension(csvFileName, ".csv");
			}

			csvFileName = Path.Combine(currentPath, "csv", csvFileName);

			Logger.Info($"csv export path:${csvFileName}");

			if (File.Exists(csvFileName))
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(csvFileName);
				string newFileName = fileNameWithoutExtension + "_" + DateTime.Now.ToString(@"yyyyMMdd-HHmmss") + ".csv";
				csvFileName = Path.Combine(currentPath, "csv", newFileName);
				Logger.Info($"csv export path:${csvFileName}");

			}

			var config = new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				ShouldQuote = (context) => true
			};

			using (var streamWriter = new StreamWriter(csvFileName, false, Encoding.Default))
			using (var csv = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
			{
				csv.Context.RegisterClassMap<EvaluationResultsMap>();
				csv.WriteRecords(evaluationResultsList);

			}

			ColorConsole.WriteLine($"{csvFileName} has been generated.", ConsoleColor.Yellow);

		}

		// Create CSV folder if it does not exists
		private static void createCSVFolder(string currentPath)
		{
			try
			{
				if (!Directory.Exists(Path.Combine(currentPath, "CSV")))
					Directory.CreateDirectory(Path.Combine(currentPath, "CSV"));

			}
			catch (Exception)
			{
				ColorConsole.WriteError("Failed to create CSV folder.Check file access permission.");
				Environment.Exit(1);
			}

		}

		public static string ConvertToE164(string did, string countryCode, string removeFirstDigitIf)
		{
			string didE164 = null;
			did = did.Replace("-", "").Replace(" ", "");

			if (IsValidDID(did))
			{
				return did;
			}


			if (did.StartsWith(removeFirstDigitIf))
			{
				didE164 = did.Remove(0, removeFirstDigitIf.Length);

				didE164 = countryCode + didE164;
			}
			else
			{
				didE164 = countryCode + did;
			}

			return didE164;

		}

		public static bool IsValidDID(string did)
		{
			if (string.IsNullOrEmpty(did)) return true;
			did = did.Replace("-", "").Replace(" ", "");
			Regex regEx = new Regex(@"(^(\+[1-9]\d{1,14})$)");
			return regEx.Match(did).Success;
		}


	}

	public class TestParameters
	{
		public string Date { get; set; }
		public string Time { get; set; }
		public string DID { get; set; }	
		public string CallRouteName { get; set; }
		public string CallFlowName { get; set; }

		public DateTime targetDateTime
		{
			get
			{
				var now = DateTime.Now;
				string dateTimeStr = (string.IsNullOrEmpty(Date) ? now.ToString("yyyyMMdd") : Date) +
									 (string.IsNullOrEmpty(Time) ? now.ToString("HHmmss") : Time);
				DateTime.TryParseExact(dateTimeStr, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime);
				return dateTime;
			}
		}

	}

	public class TestParametersMap : ClassMap<TestParameters>
	{
		public TestParametersMap()
		{

			Map(m => m.Date).Name("Date").Validate(f =>
			{
				return IsValidDate(f.Field);			

			});

			Map(m => m.Time).Name("Time").Validate(f =>
			{
				return IsValidTime(f.Field);

			});


			Map(m => m.DID).Name("DID").Validate(f =>
			{
				//return IsValidDID(f.Field);
				return CSV.IsValidDID(f.Field);

			});


			Map(m => m.DID).Convert(args => string.IsNullOrEmpty(args.Row.GetField("DID")) ? null : args.Row.GetField("DID"));
			Map(m => m.CallRouteName).Convert(args => string.IsNullOrEmpty(args.Row.GetField("CallRouteName")) ? null : args.Row.GetField("CallRouteName"));
			Map(m => m.CallFlowName).Convert(args => string.IsNullOrEmpty(args.Row.GetField("CallFlowName")) ? null : args.Row.GetField("CallFlowName"));
		}

		private bool IsValidDate(string date)
		{
			//return DateTime.TryParseExact(date, "yyyyMMdd", null, DateTimeStyles.None, out _);
			if (string.IsNullOrEmpty(date)) return true;
			return DateTime.TryParseExact(date, "yyyyMMdd", null, DateTimeStyles.None, out _);
		}

		private bool IsValidTime(string time)
		{
			if(string.IsNullOrEmpty(time)) return true;
			return DateTime.TryParseExact(time, "HHmmss", null, DateTimeStyles.None, out _);
		}

		//private bool IsValidDID(string did)
		//{
		//	if (string.IsNullOrEmpty(did)) return true;
		//	did = did.Replace("-", "").Replace(" ", "");
		//	Regex regEx = new Regex(@"(^(\+[1-9]\d{1,14})$)");
		//	return regEx.Match(did).Success;
		//}



	}



	public class EvaluationResultsMap : ClassMap<ValidationResults>
	{
		public EvaluationResultsMap()
		{
			Map(m => m.Did);
			Map(m => m.FormattedDateTime).Name("DateTime");
			Map(m => m.CallRouteName);
			Map(m => m.ScheduleName);
			Map(m => m.CallFlowName);
			Map(m => m.Status);
			Map(m => m.Emergency);
			Map(m => m.EmergencyFlowName);

		}
	}








}
