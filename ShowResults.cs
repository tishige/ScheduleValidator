using Spectre.Console;

namespace ScheduleValidator
{
	internal class ShowResults
	{
		internal static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		internal static void ShowEvaluationResult(List<ValidationResults> evaluationResultsList,DateTime targetDateTime,bool isPastTargetDateTime)
		{
			var table = new Table();

			table.AddColumn("DID");
			table.AddColumn("DateTime");
			table.AddColumn("CallRoute Name");
			table.AddColumn("Status");
			table.AddColumn("Schedule Name");
			table.AddColumn("CallFlow Name");

			table.Title = new TableTitle("<<< Scheduled Call Flow Results >>>");

			foreach (var item in evaluationResultsList)
			{
				string dateTimeStr = $"{item.Date} {item.Time}";
				DateTime resultDatetime = DateTime.Parse(dateTimeStr);

				if (string.IsNullOrEmpty(item.Did)) item.Did = "-";
				if (string.IsNullOrEmpty(item.CallRouteName)) item.CallRouteName = "-";
				if (string.IsNullOrEmpty(item.ScheduleName)) item.ScheduleName = "-";
				if (string.IsNullOrEmpty(item.CallFlowName)) item.CallFlowName = "-";

				if (!isPastTargetDateTime)
				{
					if (item.Status.StartsWith("Open"))
					{
						table.AddRow(item.Did, item.FormattedDateTime, item.CallRouteName, "[lime]" + item.Status + "[/]",item.ScheduleName,item.CallFlowName);
					}

					if (item.Status.StartsWith("Closed"))
					{
						table.AddRow(item.Did, item.FormattedDateTime, item.CallRouteName, "[red]" + item.Status + "[/]", item.ScheduleName,item.CallFlowName);
					}

					if (item.Status == "Emergency")
					{
						table.AddRow(item.Did, item.FormattedDateTime, item.CallRouteName, "[yellow]" + item.Status + "[/]", item.ScheduleName, item.CallFlowName);
					}

					if (item.Status == "Not Found")
					{
						table.AddRow(item.Did, item.FormattedDateTime, item.CallRouteName, "[fuchsia]" + item.Status + "[/]", item.ScheduleName, item.CallFlowName);
					}

				}
				else
				{
					if (item.Status.StartsWith("Open"))
					{
						table.AddRow(item.Did, "[grey]" + item.FormattedDateTime + "[/]", item.CallRouteName, "[grey]" + item.Status + "[/]", item.ScheduleName, item.CallFlowName);
					}

					if (item.Status.StartsWith("Closed"))
					{
						table.AddRow(item.Did, "[grey]" + item.FormattedDateTime + "[/]", item.CallRouteName, "[grey]" + item.Status + "[/]", item.ScheduleName, item.CallFlowName);
					}

					if (item.Status == "Emergency")
					{
						table.AddRow(item.Did, "[grey]" + item.FormattedDateTime + "[/]", item.CallRouteName, "[grey]" + item.Status + "[/]", item.ScheduleName, item.CallFlowName);
					}

					if (item.Status == "Not Found")
					{
						table.AddRow(item.Did, "[grey]" + item.FormattedDateTime + "[/]", item.CallRouteName, "[fuchsia]" + item.Status + "[/]", item.ScheduleName, item.CallFlowName);
					}

				}

			}

			Logger.Info($"ShowEvaluationResult done.");
			AnsiConsole.Write(table);

		}

	}
}
