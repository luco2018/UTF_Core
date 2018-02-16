namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // Broadcast
    // - Delegates for broadcasting messages between scripts

    public abstract class Broadcast
	{
        // TestLogic > TesList
        // Current test complete. Continue.
        public delegate void EndTestAction ();

        // TODO - Set this up
        public delegate void EndBuildSetup ();

        // CloudIO > TestLogic
        // Current results saved by CloudIO. End test and continue.
        // --------
        // CloudIO > ViewerToolbar
        // Current results saved by CloudIO. View next test. Used on Baseline resolve path.
        public delegate void EndResultsSave ();

        // ResultsIO > TestStructure
        // Baselines have been parsed. Start structure generation.
		public delegate void LocalBaselineParsed ();

        // TestLogic
        // Custom delegate for waiting in custom test types
        public delegate void ContinueTest();

        // AltBaselines Set
        // Is called when the current baseline set has changed.
        public delegate void AltBaselineChanged();

        // Settings Save
        // Is called when the save button in the settings menu is clicked, use to apply any settings changed in the settings menu
        public delegate void SaveMenuSettings();

        // Revert Settings
        // Is called when the close button in the settings menu is clicked, use to revert and settings changed while in the settings menu
        public delegate void RevertMenuSettings();
    }
}
