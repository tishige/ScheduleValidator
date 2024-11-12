using CommandLine;
using Microsoft.Extensions.Configuration;
using ShellProgressBar;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;


namespace ScheduleValidator
{
	internal class Program
	{

		internal static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		static void Main(string[] args)
		{

			IConfigurationRoot configRoot = null;
			try
			{
				configRoot = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(path: "appsettings.json").Build();

			}
			catch (Exception)
			{
				ColorConsole.WriteError($"The configuration file 'appsettings.json' was not found in this directory.");
				PrintUsage();

			}

			var parseResult = Parser.Default.ParseArguments<Options>(args);
			Options opt = new();
			DateTime targetDateTime = DateTime.Now;
			bool isPastTargetDateTime = false;

			bool convertToE164 = configRoot.GetSection("appSettings").Get<AppSettings>().ConvertToE164;
			string countryCode = configRoot.GetSection("appSettings").Get<AppSettings>().CountryCode;
			string removeFirstDigitIf = configRoot.GetSection("appSettings").Get<AppSettings>().RemoveFirstDigitIfStartWith;
			string did = null;

			if (convertToE164 && !countryCode.StartsWith("+")) 
			{
				countryCode = "+" + countryCode;
				Logger.Info($"country code added: {countryCode}");
			} 

			List<TestParameters> testParametersList = new();

			switch (parseResult.Tag)
			{
				case ParserResultType.Parsed:
					var parsed = parseResult as Parsed<Options>;
					opt = parsed.Value;
					did = opt.DID;

					if (opt.DID != null)
					{
						if (convertToE164)
						{
							did = CSV.ConvertToE164(opt.DID, countryCode, removeFirstDigitIf);
							Logger.Info($"did specified: {did}");

						}

						if (!CSV.IsValidDID(did))
						{
							ColorConsole.WriteError($"DID is not a E.164 format. Try to set convertToE164 to true.");
							PrintUsage();

						}


					}


					// Load Testparameters.csv
					if (opt.InputFilename != null)
					{
						// Check if the other options are also specified  
						if (opt.DID != null || opt.CallFlowName != null || opt.CallRouteName != null)
						{
							ColorConsole.WriteError("Cannot specify DID, CallFlow name, or CallRoute name with inputFilename.");
							Environment.Exit(1);
						}

						string filePath;

						if (Path.IsPathRooted(opt.InputFilename))
						{
							filePath = opt.InputFilename;
							Logger.Info($"filepath specified: {filePath}");
						}
						else
						{
							string exeLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
							string exeDirectory = Path.GetDirectoryName(exeLocation);
							filePath = Path.Combine(exeDirectory, opt.InputFilename);
							Logger.Info($"filepath: {filePath}");
						}

						if (!File.Exists(filePath))
						{
							ColorConsole.WriteError($"{filePath} does not exist.  Check if the file exists.");
							Environment.Exit(1);
						}

						CSV csv = new CSV();

						Logger.Info($"csv import");
						testParametersList = csv.Import(filePath,convertToE164,countryCode,removeFirstDigitIf);

					}
					else
					{
						//Check Date time argument or Set current Date time
						string dateStr = opt.Date ?? DateTime.Now.ToString("yyyyMMdd");
						string timeStr = opt.Time ?? DateTime.Now.ToString("HHmmss");

						string dateTimeStr = dateStr + timeStr;
						Logger.Info($"dateTimeStr:${dateTimeStr}");
						if (DateTime.TryParseExact(dateTimeStr, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out targetDateTime))
						{
							if (targetDateTime.AddSeconds(2) < DateTime.Now) isPastTargetDateTime = true;
							Logger.Info($"isPastTargetDateTime:{isPastTargetDateTime}");
							break;
						}
						else
						{
							ColorConsole.WriteError("Date or Time is not in the correct format.");
							PrintUsage();
						}

					}

					break;

				case ParserResultType.NotParsed:

					PrintUsage();
					break;

			}

			// Get Token
			FetchGCAccessToken.GetAccessToken(opt.profile);

			// Fetch Call Routing Data
			List<CallRoutingData> callRoutingDataList = FetchCallRouting.FetchSchedules();
			List<ValidationResults> evaluationResultsList = new();

			var extendedEndDate= ValidateSchedule.CalcExtendedDate(callRoutingDataList);
			int extendedYears = extendedEndDate.Item1;

			DateTime acceptableEndDate = extendedEndDate.Item2;

			var pboptions = new ProgressBarOptions
			{
				ProgressCharacter = '─',
				ProgressBarOnBottom = true
			};
			float prog;

			using (ProgressBar progressBar = new ProgressBar(10000, "Validating Schedules", pboptions))
			{
				IProgress<float> progress = progressBar.AsProgress<float>();
				int i = 0;

				if (opt.InputFilename != null)
				{
					foreach (var item in testParametersList)
					{

						if(item.targetDateTime> acceptableEndDate)
						{
							ColorConsole.WriteError("Specified DateTime is out of range.");
							Environment.Exit(0);
						}

						var evaluationResults = ValidateSchedule.Validate(callRoutingDataList, item.targetDateTime, extendedYears, item.DID, item.CallRouteName, item.CallFlowName);
						evaluationResultsList.AddRange(evaluationResults);
						i++;
						prog = ((float)i / testParametersList.Count());// * 100;
						progress.Report(prog);

					}
				}
				else
				{
					progress.Report((float)0.3);
					var result = Parser.Default.ParseArguments<Options>(args)
						.WithParsed(o =>
						{
							if (targetDateTime > acceptableEndDate)
							{
								ColorConsole.WriteError("Specified DateTime is out of range.");
								Environment.Exit(0);
							}

							evaluationResultsList = ValidateSchedule.Validate(callRoutingDataList, targetDateTime, extendedYears, did, o.CallRouteName, o.CallFlowName);
						});
					progress.Report(1);

				}

			}

			Console.WriteLine();



			// Show result
			Logger.Info($"ShowEvaluationResult targetDateTime:${targetDateTime}");
			ShowResults.ShowEvaluationResult(evaluationResultsList,targetDateTime, isPastTargetDateTime);

			if (opt.OutputFilename != null)
			{
				CSV.Export(evaluationResultsList,opt.OutputFilename);
			}

			ColorConsole.WriteLine($"Completed!", ConsoleColor.Yellow);

		}

		private static void PrintUsage()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine();

			sb.AppendLine("Usage:");
			sb.AppendLine(" ScheduleValidator.exe   Show the schedule validation result for the current date and time.");

			sb.AppendLine();
			sb.AppendLine("Options:");
			sb.AppendLine(@"  -F --flow              Specify Call flow name");
			sb.AppendLine(@"  -C --callRoute         Specify Call route name");
			sb.AppendLine(@"  -D --did               Specify DID number");
			sb.AppendLine(@"  -d --date              Date");
			sb.AppendLine(@"  -t --time              Time");
			sb.AppendLine(@"  -i --input             Load test pattern csv file");
			sb.AppendLine(@"  -o --output            Export validation results to csv file");
			sb.AppendLine(@"  -p --profile           Specify GenesysCloud Organization name");

			sb.AppendLine(@"  --help                 Show this screen.");
			sb.AppendLine(@"  --version              Show version.");

			sb.AppendLine();
			sb.AppendLine("Examples:");
			sb.AppendLine("  ScheduleValidator.exe -D +13175551234");
			sb.AppendLine("  ScheduleValidator.exe -d 20241231 -t 230000");
			sb.AppendLine("  ScheduleValidator.exe -F MarketingDev -C Holiday -D +13175551234 -d 20241231 -t 230000");
			sb.AppendLine();

			Console.Out.Write(sb.ToString());
			Environment.Exit(1);
		}


	}
}
