using Microsoft.Extensions.Configuration;
using NLog;
using PureCloudPlatform.Client.V2.Api;
using PureCloudPlatform.Client.V2.Client;
using PureCloudPlatform.Client.V2.Model;
using ShellProgressBar;
using System.Diagnostics;

namespace ScheduleValidator
{

	internal class FetchCallRouting
    {
        internal static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        internal static List<CallRoutingData> FetchSchedules()
        {
            var configRoot = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(path: "appsettings.json").Build();

            ArchitectApi gcArchApi = new();
            var page = 1;
            int pageSize = configRoot.GetSection("gcSettings").Get<GcSettings>().PageSize;
            int pageCount;
            float prog;

            List<CallRoutingData> callRoutingDataList = new();
			List<Emergencygroups> emergencygroupsDataList = new();
			List<Schedulegroups> schedulegroupsDataList = new();
			List<Schedule> schedulesDataList = new();

			// org name
			OrganizationApi gcOrgApi = new();
            Organization result = gcOrgApi.GetOrganizationsMe();
            string orgName = result.Name;

            var pboptions = new ProgressBarOptions
            {
                ProgressCharacter = '─',
                ProgressBarOnBottom = true
            };

            // Fetch Call Routings
			ColorConsole.WriteLine($"Fetch Schedule Settings from [{orgName}] pageSize:{pageSize}", ConsoleColor.Yellow);
			using (ProgressBar progressBar = new ProgressBar(10000, "Fetch Call Routings", pboptions))
			{
				IProgress<float> progress = progressBar.AsProgress<float>();
				IVREntityListing ivrEntityListing = new();
				try
				{
					do
					{
						Logger.Info($"Fetch Call Routing Page:{page}");
						ivrEntityListing = gcArchApi.GetArchitectIvrs(pageNumber: page, pageSize: pageSize);

						pageCount = (int)ivrEntityListing.PageCount;

						foreach (var item in ivrEntityListing.Entities)
						{

							CallRoutingData callRoutingData = new();
							callRoutingData.Id = item.Id.ToString();
							callRoutingData.CallRouteName = item.Name.ToString();
							callRoutingData.Dnis = item.Dnis;
							callRoutingData.Open = "Always";

							if (item.OpenHoursFlow != null)
							{
								callRoutingData.openHoursFlows = new OpenHoursFlow
								{
									Id = item.OpenHoursFlow.Id,
									Name = item.OpenHoursFlow.Name
								};
							}

							if (item.ClosedHoursFlow != null)
							{
								callRoutingData.closedHoursFlows = new ClosedHoursFlow
								{
									Id = item.ClosedHoursFlow.Id,
									Name = item.ClosedHoursFlow.Name
								};
							}

							if (item.HolidayHoursFlow != null)
							{
								callRoutingData.holidayHoursFlows = new HolidayHoursFlow
								{
									Id = item.HolidayHoursFlow.Id,
									Name = item.HolidayHoursFlow.Name
								};
							}

							if (item.ScheduleGroup != null)
							{
								callRoutingData.scheduleGroup = new ScheduleGroup
								{
									Id = item.ScheduleGroup.Id,
									Name = item.ScheduleGroup.Name
								};
								callRoutingData.Open = "Based on a schedule group";
							}

							callRoutingDataList.Add(callRoutingData);

						}
						prog = ((float)page / pageCount);// * 100;
						progress.Report(prog);

						page++;

					} while (page <= pageCount);


				}
				catch (Exception e)
				{
					Debug.Print("Exception when calling Architect.GetArchitectIvrs: " + e.Message);
					ColorConsole.WriteError("Exception when calling Architect.GetArchitectIvrs: " + e.Message);
					Environment.Exit(1);

				}

			}
			Logger.Info($"Fetch Call Routings completed!");


			// Fetch Emergency group
			using (ProgressBar progressBar = new ProgressBar(10000, "Fetch Emergency Groups", pboptions))
			{
				EmergencyGroupListing emergencyGroupListing = new EmergencyGroupListing();
				IProgress<float> progress = progressBar.AsProgress<float>();
				page = 1;
				try
				{
					do
					{
						Logger.Info($"Fetch Emergency Groups Page:{page}");
						emergencyGroupListing = gcArchApi.GetArchitectEmergencygroups(pageNumber: page, pageSize: pageSize);

						pageCount = (int)emergencyGroupListing.PageCount;

						foreach (var item in emergencyGroupListing.Entities)
						{
							Emergencygroups emergencygroups = new();

							emergencygroups.Id = item.Id;
							emergencygroups.Name = item.Name;

							if (item.EmergencyCallFlows != null)
							{
								emergencygroups.emergencyCallFLowId = item.EmergencyCallFlows.FirstOrDefault().EmergencyFlow.Id;
								emergencygroups.emergencyCallFLowName = item.EmergencyCallFlows.FirstOrDefault().EmergencyFlow.Name;
								emergencygroups.impactedCallRoutesId = item.EmergencyCallFlows.FirstOrDefault().Ivrs.FirstOrDefault().Id;
								emergencygroups.impactedCallRoutesName = item.EmergencyCallFlows.FirstOrDefault().Ivrs.FirstOrDefault().Name;

							}

							emergencygroups.enabled = (bool)item.Enabled;
							emergencygroupsDataList.Add(emergencygroups);

						}
						prog = ((float)page / pageCount);// * 100;
						progress.Report(prog);

						page++;

					} while (page <= pageCount);


				}
				catch (Exception e)
				{
					Debug.Print("Exception when calling Architect.GetArchitectEmergencygroups: " + e.Message);
					ColorConsole.WriteError("Exception when calling Architect.GetArchitectEmergencygroups: " + e.Message);
					Environment.Exit(1);

				}

			}

			Logger.Info($"Fetch Emergency Groups completed!");

			// Fetch schedules
			using (ProgressBar progressBar = new ProgressBar(10000, "Fetch Schedules", pboptions))
			{
				ScheduleEntityListing scheduleEntityListing = new ScheduleEntityListing();
				IProgress<float> progress = progressBar.AsProgress<float>();

				page = 1;
				try
				{
					do
					{
						Logger.Info($"Fetch Schedule Page:{page}");
						scheduleEntityListing = gcArchApi.GetArchitectSchedules(pageNumber: page, pageSize: pageSize);


						pageCount = (int)scheduleEntityListing.PageCount;

						foreach (var item in scheduleEntityListing.Entities)
						{
							Schedule schedule = new Schedule();

							schedule.Id = item.Id;
							schedule.Name = item.Name;
							schedule.Start = (DateTime)item.Start;
							schedule.End = (DateTime)item.End;

							if (item.Rrule != null)
							{
								schedule.rrule = item.Rrule;

							}

							schedulesDataList.Add(schedule);

						}
						prog = ((float)page / pageCount);// * 100;
						progress.Report(prog);

						page++;

					} while (page <= pageCount);

				}
				catch (Exception e)
				{
					Debug.Print("Exception when calling Architect.GetArchitectSchedules: " + e.Message);
					ColorConsole.WriteError("Exception when calling Architect.GetArchitectSchedules: " + e.Message);
					Environment.Exit(1);

				}

			}

			Logger.Info($"Fetch Schedule completed!");

			// Fetch scheduleGroups
			using (ProgressBar progressBar = new ProgressBar(10000, "Fetch SchedulesGroups", pboptions))
			{
				ScheduleGroupEntityListing scheduleGroupEntityListing = new ScheduleGroupEntityListing();
				IProgress<float> progress = progressBar.AsProgress<float>();

				page = 1;
				try
				{
					do
					{
						Logger.Info($"Fetch Schedule Groups Page:{page}");
						scheduleGroupEntityListing = gcArchApi.GetArchitectSchedulegroups(pageNumber: page, pageSize: pageSize);

						pageCount = (int)scheduleGroupEntityListing.PageCount;

						foreach (var item in scheduleGroupEntityListing.Entities)
						{
							Schedulegroups schedulegroups = new();

							schedulegroups.Id = item.Id;
							schedulegroups.Name = item.Name;

							if (item.OpenSchedules != null)
							{

								foreach (var childitem in item.OpenSchedules)
								{
									OpenSchedule openSchedule = new();

									openSchedule.Id = childitem.Id;
									openSchedule.Name = childitem.Name;

									var eachSchedule = schedulesDataList.FirstOrDefault(x => x.Id == childitem.Id);
									if (eachSchedule != null)
									{
										openSchedule.Start = eachSchedule.Start;
										openSchedule.End = eachSchedule.End;
										openSchedule.rrule = eachSchedule.rrule;

									}

									schedulegroups.OpenSchedules.Add(openSchedule);
								}

							}

							if (item.ClosedSchedules != null)
							{

								foreach (var childitem in item.ClosedSchedules)
								{
									ClosedSchedule closeSchedule = new();

									closeSchedule.Id = childitem.Id;
									closeSchedule.Name = childitem.Name;

									var eachSchedule = schedulesDataList.FirstOrDefault(x => x.Id == childitem.Id);
									if (eachSchedule != null)
									{
										closeSchedule.Start = eachSchedule.Start;
										closeSchedule.End = eachSchedule.End;
										closeSchedule.rrule = eachSchedule.rrule;

									}

									schedulegroups.ClosedSchedules.Add(closeSchedule);
								}
							}

							if (item.HolidaySchedules != null)
							{

								foreach (var childitem in item.HolidaySchedules)
								{
									HolidaySchedule holidaySchedule = new();

									holidaySchedule.Id = childitem.Id;
									holidaySchedule.Name = childitem.Name;

									var eachSchedule = schedulesDataList.FirstOrDefault(x => x.Id == childitem.Id);
									if (eachSchedule != null)
									{
										holidaySchedule.Start = eachSchedule.Start;
										holidaySchedule.End = eachSchedule.End;
										holidaySchedule.rrule = eachSchedule.rrule;

									}

									schedulegroups.HolidaySchedules.Add(holidaySchedule);
								}
							}

							schedulegroupsDataList.Add(schedulegroups);

						}
						prog = ((float)page / pageCount);// * 100;
						progress.Report(prog);

						page++;

					} while (page <= pageCount);
				}
				catch (Exception e)
				{
					Debug.Print("Exception when calling Architect.GetArchitectSchedulegroups: " + e.Message);
					ColorConsole.WriteError("Exception when calling Architect.GetArchitectSchedulegroups: " + e.Message);
					Environment.Exit(1);

				}

			}

			Logger.Info($"Fetch Schedule Groups completed!");

			// Assigin schedule and emergency data to callRoutindDataList
			foreach (var item in emergencygroupsDataList)
			{
				var emergengyAssigned = callRoutingDataList.FirstOrDefault(x => x.Id == item.impactedCallRoutesId);
				if (emergengyAssigned != null)
				{
					emergengyAssigned.emergencygroups = item;
				}
			}

			foreach (var item in schedulegroupsDataList)
			{
				var foundScheduleGroup = callRoutingDataList.FirstOrDefault(x=>x.scheduleGroup.Id==item.Id);
				if (foundScheduleGroup != null)
				{
					foundScheduleGroup.scheduleGroups = item;
				}
			}

			foreach (var item in callRoutingDataList)
			{
				var foundScheduleGroup = schedulegroupsDataList.FirstOrDefault(x => x.Id == item.scheduleGroup.Id);
				if (foundScheduleGroup != null)
				{
					item.scheduleGroups = foundScheduleGroup;
				}
			}

			Logger.Info($"Fetch Schedules done.");
			return callRoutingDataList;

		}
        
	}


    public class CallRoutingData
    {
        internal string Id { get; set; } = null!;
        internal string CallRouteName { get; set; } = null!;
        internal string Open { get; set; } = null!;
		internal List<string> Dnis {  get; set; }=new List<string>();
        internal OpenHoursFlow openHoursFlows { get; set; }  = new OpenHoursFlow();
		internal ClosedHoursFlow closedHoursFlows { get; set; } = new ClosedHoursFlow();
		internal HolidayHoursFlow holidayHoursFlows { get; set; } = new HolidayHoursFlow();
		internal Emergencygroups emergencygroups { get; set; } = new Emergencygroups();
		internal ScheduleGroup scheduleGroup { get; set; } = new ScheduleGroup();
		internal Schedulegroups scheduleGroups { get; set; } = new Schedulegroups();

	}

	public class ScheduleResultBase
	{
		internal string Id { get; set; }
		internal string Name { get; set; }

	}
	internal class OpenHoursFlow : ScheduleResultBase { }
	internal class ClosedHoursFlow : ScheduleResultBase { }
	internal class HolidayHoursFlow : ScheduleResultBase { }
	internal class ScheduleGroup : ScheduleResultBase { }
	internal class Emergencygroups : ScheduleResultBase
	{
		internal string emergencyCallFLowId { get; set; }
		internal string emergencyCallFLowName { get; set; }
		internal string impactedCallRoutesId { get; set; }
		internal string impactedCallRoutesName { get; set; }
		internal bool enabled {  get; set; }
	}
	internal class Schedulegroups : ScheduleResultBase
	{
		internal List<OpenSchedule> OpenSchedules { get; set; } = new List<OpenSchedule>();
		internal List<ClosedSchedule> ClosedSchedules { get; set; } = new List<ClosedSchedule>();
		internal List<HolidaySchedule> HolidaySchedules { get; set; } = new List<HolidaySchedule>();

	}

	public class ScheduleConfigResultBase : ScheduleResultBase
	{
		internal DateTime Start { get; set; }
		internal DateTime End { get; set; }
		internal string rrule { get; set; }

	}
	internal class OpenSchedule : ScheduleConfigResultBase { }
	internal class ClosedSchedule : ScheduleConfigResultBase { }
	internal class HolidaySchedule : ScheduleConfigResultBase { }
	internal class Schedule : ScheduleConfigResultBase { }

}
