using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Evaluation;
using Ical.Net.Serialization.DataTypes;

namespace ScheduleValidator
{
	class ValidateSchedule
    {
		internal static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		internal static List<ValidationResults> Validate(List<CallRoutingData> callRoutingData,DateTime targetDateTime, int extendedYears, string did=null, string callRouteName=null, string callFlowName=null)
		{
			var routingDataResult = callRoutingData.Where(x => (did == null || x.Dnis.Contains(did)) && (callRouteName == null || x.CallRouteName == callRouteName) && (callFlowName == null || HasFlowName(x, callFlowName))).ToList();

			ValidationResult evaluationResult = new();
			List<ValidationResults> evaluationResultsList = new();

			if (routingDataResult.Count != 0)
			{
				foreach (var item in routingDataResult)
				{
					evaluationResult = EvaluateScheduleStatus(item, targetDateTime, extendedYears);
					ValidationResults evaluationResults = new ValidationResults();

					if (item.Open == "Always")
					{
						// Always
						evaluationResults.Status = "Open (Always)";
						evaluationResults.CallFlowName = item.openHoursFlows.Name;
						evaluationResults.ScheduleName = evaluationResult.ScheduleName;

					}
					else
					{
						// Base on a schedule
						evaluationResults.Status = evaluationResult.Status;
						evaluationResults.CallFlowName = evaluationResult.CallFlowName;
						evaluationResults.ScheduleName = evaluationResult.ScheduleName;

					}

					if (item.Dnis.Count() == 1)
					{
						evaluationResults.Did = item.Dnis[0];

					}
					else
					{
						evaluationResults.Did = string.Join("|", item.Dnis);
					}

					if (item.emergencygroups.emergencyCallFLowName != null)
					{
						if (item.emergencygroups.enabled)
						{
							evaluationResults.Status = "Emergency";
							evaluationResults.Emergency = "Enabled";
							evaluationResults.CallFlowName=item.emergencygroups.emergencyCallFLowName;
							evaluationResults.ScheduleName = item.emergencygroups.Name;

						}
						else
						{
							evaluationResults.Emergency = "Disabled";
						}

						evaluationResults.EmergencyFlowName = item.emergencygroups.emergencyCallFLowName;

					}
					else
					{
						evaluationResults.Emergency = "N/A";
						evaluationResults.EmergencyFlowName = "N/A";

					}

					evaluationResults.CallRouteName = item.CallRouteName;
					evaluationResults.Date = targetDateTime.ToString("yyyy-MM-dd");
					evaluationResults.Time = targetDateTime.ToString("HH:mm:ss");

					string dateTimeStr = $"{evaluationResults.Date} {evaluationResults.Time}";
					DateTime dt = DateTime.Parse(dateTimeStr);
					evaluationResults.FormattedDateTime = $"{dt.ToShortDateString()} {dt.ToLongTimeString()} {dt:ddd}";

					evaluationResultsList.Add(evaluationResults);
				}

			}
			else
			{
				ValidationResults evaluationResults = new();
				evaluationResults.Status = "Not Found";

				evaluationResults.Did = did;
				evaluationResults.CallRouteName = callRouteName;
				evaluationResults.CallFlowName = callFlowName;
				evaluationResults.Date = targetDateTime.ToString("yyyy-MM-dd");
				evaluationResults.Time = targetDateTime.ToString("HH:mm:ss");

				string dateTimeStr = $"{evaluationResults.Date} {evaluationResults.Time}";
				DateTime dt = DateTime.Parse(dateTimeStr);
				evaluationResults.FormattedDateTime = $"{dt.ToShortDateString()} {dt.ToLongTimeString()} {dt:ddd}";
				
				evaluationResultsList.Add(evaluationResults);
			}

			Logger.Info($"Validate done.");
			return evaluationResultsList;
		}

		private static bool HasFlowName(CallRoutingData callRoutingData, string flowName)
		{
			if (flowName == null) return true;

			return new[] { callRoutingData.openHoursFlows.Name, callRoutingData.closedHoursFlows.Name, callRoutingData.holidayHoursFlows.Name, callRoutingData.emergencygroups.emergencyCallFLowName }
				.Any(name => name == flowName);
		}

		private static ValidationResult EvaluateScheduleStatus(CallRoutingData callRoutingData,DateTime targetDateTime,int extendedYears)
		{
			ValidationResult evaluationResult = new();

			// Emergency?
			if (callRoutingData.emergencygroups.enabled)
			{
				evaluationResult.Status = "Emergency";
				evaluationResult.CallRouteName = callRoutingData.emergencygroups.Name;
				evaluationResult.CallFlowName=callRoutingData.emergencygroups.emergencyCallFLowName;
				return evaluationResult;
			}

			// Holiday?
			var holidayResult = EvaluateTimeRange(callRoutingData, targetDateTime, "Holiday", extendedYears);
			if (holidayResult.Status != null)
			{
				evaluationResult.Status = holidayResult.Status;
				evaluationResult.CallRouteName = holidayResult.CallRouteName;
				evaluationResult.CallFlowName = callRoutingData.holidayHoursFlows.Name;
				evaluationResult.ScheduleName = holidayResult.ScheduleName;

				return evaluationResult;
			}

			// Closed?
			var closedResult = EvaluateTimeRange(callRoutingData, targetDateTime, "Closed", extendedYears);
			if (closedResult.Status != null)
			{
				evaluationResult.Status = closedResult.Status;
				evaluationResult.CallRouteName = closedResult.CallRouteName;
				evaluationResult.CallFlowName = callRoutingData.closedHoursFlows.Name;
				evaluationResult.ScheduleName = closedResult.ScheduleName;


				return evaluationResult;
			}

			// Open?
			var openResult = EvaluateTimeRange(callRoutingData, targetDateTime, "Open", extendedYears);
			if (openResult.Status != null)
			{
				evaluationResult.Status = openResult.Status;
				evaluationResult.CallRouteName = openResult.CallRouteName;
				evaluationResult.CallFlowName = callRoutingData.openHoursFlows.Name;
				evaluationResult.ScheduleName = openResult.ScheduleName;

				return evaluationResult;
			}

			Logger.Info($"EvaluateScheduleStatus done.");

			return evaluationResult;
		}

		private static ValidationResult EvaluateTimeRange(CallRoutingData callRoutingData, DateTime targetDateTime,string scheduleType,int extendedYears)
		{
			string evaluateResult = null;
			string matchedScheduleName = null;

			switch (scheduleType)
			{
				case "Holiday":
					foreach(var item in callRoutingData.scheduleGroups.HolidaySchedules)
					{
						if (item.rrule != null)
						{
							if (IsInRRule(item, targetDateTime, extendedYears))
							{
								evaluateResult = "Closed (Holiday Sched.)";
								matchedScheduleName = item.Name;
								break;
							}

						}
						else
						{
							if (OneTime(item, targetDateTime))
							{
								evaluateResult = "Closed (Holiday Sched.)";
								matchedScheduleName = item.Name;
								break;
							}

						}

					}
					break;

				case "Closed":
					foreach (var item in callRoutingData.scheduleGroups.ClosedSchedules)
					{
						if (item.rrule != null)
						{
							if (IsInRRule(item, targetDateTime, extendedYears))
							{
								evaluateResult = "Closed (Closed Sched.)";
								matchedScheduleName = item.Name;
								break;
							}

						}
						else
						{
							if (OneTime(item, targetDateTime))
							{
								evaluateResult = "Closed (Closed Sched.)";
								matchedScheduleName = item.Name;
								break;
							}

						}
					}
					break;

				case "Open":
					foreach (var item in callRoutingData.scheduleGroups.OpenSchedules)
					{
						if (item.rrule != null)
						{
							if (IsInRRule(item, targetDateTime, extendedYears))
							{
								evaluateResult = "Open";
								matchedScheduleName = item.Name;
								break;
							}
							else
							{
								evaluateResult = "Closed (Open Sched.)";
								matchedScheduleName = item.Name;
							}

						}

						else
						{
							if (OneTime(item, targetDateTime))
							{
								evaluateResult = "Open";
								matchedScheduleName = item.Name;
								break;
							}

						}

					}
					break;

				default:
					break;
			}

			ValidationResult evaluationResult = new();
			evaluationResult.Status = evaluateResult;
			evaluationResult.ScheduleName= matchedScheduleName;

			Logger.Info($"EvaluateTimeRange done.");

			return evaluationResult;
		}

		internal static bool IsInRRule (ScheduleConfigResultBase scheduleConfig, DateTime targetDateTime,int extendedYears)
		{
			bool result = false;
			DateTime startSchedule = scheduleConfig.Start;
			DateTime endSchedule = scheduleConfig.End;
			DateTime extendedEndSchedule = scheduleConfig.Start;

			string schduleRule = null;
			bool isWithinSchedule = targetDateTime >= startSchedule && targetDateTime <= endSchedule;
			bool isFirstOccured = targetDateTime >= startSchedule && targetDateTime >= endSchedule;

			if (!isWithinSchedule && !isFirstOccured)
			{
				Logger.Info("TargetDate is out of range AND not occured yet");
				return false;
			}


			RecurrencePattern pattern;
			if (scheduleConfig.rrule != null)
			{
				schduleRule = scheduleConfig.rrule.ToString();
				if (isFirstOccured) extendedEndSchedule = extendedEndSchedule.AddYears(extendedYears);

				var serializer = new RecurrencePatternSerializer();

				pattern = (RecurrencePattern)serializer.Deserialize(new StringReader(schduleRule));
				var evaluator = new RecurrencePatternEvaluator(pattern);
				var vEvent = new CalendarEvent
				{
					Start = new CalDateTime(startSchedule),
					End = new CalDateTime(endSchedule),
					RecurrenceRules = new List<RecurrencePattern> { pattern }
				};

				var occurrencesExtended = vEvent.GetOccurrences(new CalDateTime(startSchedule), new CalDateTime(extendedEndSchedule));
				var targetDateWithinExtended = occurrencesExtended.Where(x => x.Period.StartTime != null && x.Period.EndTime != null && x.Period.StartTime.Value.Date <= targetDateTime.Date && x.Period.EndTime.Value.Date >= targetDateTime.Date).FirstOrDefault();

				if (targetDateWithinExtended != null)
				{
					result = targetDateWithinExtended.Period.StartTime.Value <= targetDateTime && targetDateWithinExtended.Period.EndTime.Value >= targetDateTime;
					Logger.Info($"{targetDateTime.ToString()} exists in rrule.");

				}

				Logger.Info($"IsInRRule true done.");

				return result;

			}

			Logger.Info($"IsInRRule false done.");

			return result;
		}

		internal static bool OneTime(ScheduleConfigResultBase scheduleConfig, DateTime targetDateTime)
		{
			bool result = false;
			var startSchedule = scheduleConfig.Start;
			var endSchedule = scheduleConfig.End;

			result = targetDateTime>=startSchedule && targetDateTime<=endSchedule;

			Logger.Info($"OneTime: {result} done.");
			return result;
		}


		internal static Tuple<int, DateTime> CalcExtendedDate(List<CallRoutingData> callRoutingDataList)
		{

			var years = new List<DateTime>();
			
			foreach (var data in callRoutingDataList)
			{
				if (data.scheduleGroups.HolidaySchedules != null)
					years.AddRange(data.scheduleGroups.HolidaySchedules.Select(y => y.Start));

				if (data.scheduleGroups.ClosedSchedules != null)
					years.AddRange(data.scheduleGroups.ClosedSchedules.Select(y => y.Start));

				if (data.scheduleGroups.OpenSchedules != null)
					years.AddRange(data.scheduleGroups.OpenSchedules.Select(y => y.Start));
			}

			if (years.Count > 0)
			{
				int minYear = years.Min().Year;
				int maxYear = years.Max().Year;

				int addYears = (maxYear - minYear) + 11;
				DateTime maxAcceptableDate = years.Min().AddYears(addYears);

				Logger.Info($"CalcExtendedDate years.count>0 done.");

				return new Tuple<int, DateTime>(addYears, maxAcceptableDate);

			}
			else
			{
				int addYears = 11;
				DateTime maxAcceptableDate = DateTime.Now.AddYears(addYears);

				Logger.Info($"CalcExtendedDate:added {addYears} years done.");

				return new Tuple<int, DateTime>(addYears, maxAcceptableDate);
			}

		}

	}

	public class ValidationResults
	{
		public string Did { get; set; }
		public string CallRouteName {  get; set; }
		public string ScheduleName { get; set; }
		public string CallFlowName { get; set; }
		public string Date { get; set; }
		public string Time { get; set; }
		public string Status { get; set; } //OPEN or Close
		public string Emergency { get; set; } //Enabled or Disabled
		public string EmergencyFlowName { get; set; }
		public string FormattedDateTime { get; set; }

	}

	internal class ValidationResult
	{
		internal string Status { get; set; }
		internal string ScheduleName { get; set; }
		internal string CallRouteName { get; set; }
		internal string CallFlowName { get; set; }

	}

}
