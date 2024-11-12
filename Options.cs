using CommandLine;

namespace ScheduleValidator
{
    /// <summary>
    /// CommandLine arguments
    /// </summary>
    internal class Options
    {

        // Fetch flow from Genesys Cloud
        [Option('p', "profile", Required = false, Default="default",HelpText = "Profile")]
        public string profile { get; set; } = null!;

		// Fetch flow from Genesys Cloud
		[Option('F', "flow", Required = false, HelpText = "callFlow name")]
		public string CallFlowName { get; set; } = null!;

		[Option('C', "callRoute", Required = false, HelpText = "Callroute Name")]
		public string CallRouteName { get; set; } = null!;

		// Fetch flow from Genesys Cloud
		[Option('S', "Schedule", Required = false, HelpText = "Schedule name")]
		public string ScheduleName { get; set; } = null!;

		// Fetch flow from Genesys Cloud
		[Option('D', "did", Required = false, HelpText = "DID Number")]
		public string DID { get; set; } = null!;

		// Fetch flow from Genesys Cloud
		[Option('d', "date", Required = false, HelpText = "DID Number")]
		public string Date { get; set; } = null!;

		// Fetch flow from Genesys Cloud
		[Option('t', "time", Required = false, HelpText = "DID Number")]
		public string Time { get; set; } = null!;

		// Fetch flow from Genesys Cloud
		[Option('o', "output", Required = false, HelpText = "output file name")]
		public string OutputFilename { get; set; } = null!;

		// Fetch flow from Genesys Cloud
		[Option('i', "input", Required = false, HelpText = "input file name")]
		public string InputFilename { get; set; } = null!;

    }

}

