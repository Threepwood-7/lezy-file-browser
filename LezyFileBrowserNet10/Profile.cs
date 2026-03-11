namespace LezyFileBrowser
{
    public class Profile
    {
        public string Name        { get; set; } = "";
        public string Description { get; set; } = "";

        // Directories
        public string InputDir { get; set; } = "";
        public string OkDir    { get; set; } = "";

        // Behavior
        public int  SortDir         { get; set; } = 7;           // 1=date desc  3=name asc  7=random
        public bool InplaceBrowsing { get; set; } = false;
        public long MinFileSizeMb   { get; set; } = 99;
        public int  MaxListItems    { get; set; } = int.MaxValue; // int.MaxValue = unlimited

        // Advanced
        public string MoveWithRobocopy { get; set; } = "";       // "" = auto  "true"/"false" = override
        public bool   NoCache          { get; set; } = false;
        public bool   NoDupeCheck      { get; set; } = false;
        public bool   ProfileLog       { get; set; } = false;

        public Profile Clone(string newName) => new Profile
        {
            Name             = newName,
            Description      = Description,
            InputDir         = InputDir,
            OkDir            = OkDir,
            SortDir          = SortDir,
            InplaceBrowsing  = InplaceBrowsing,
            MinFileSizeMb    = MinFileSizeMb,
            MaxListItems     = MaxListItems,
            MoveWithRobocopy = MoveWithRobocopy,
            NoCache          = NoCache,
            NoDupeCheck      = NoDupeCheck,
            ProfileLog       = ProfileLog,
        };
    }
}
