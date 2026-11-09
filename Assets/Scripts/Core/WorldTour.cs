namespace CountToZero
{
    // Each level is one stop on OMEGA-99's world tour. Every stop opens on that place's
    // lucky number, so the theme is the first thing the player is asked to count to.
    public readonly struct TourStop
    {
        public readonly string Place, Boss, Number, Lore, Threat, Reply; public readonly int Lucky;
        public TourStop(string place,string boss,int lucky,string lore,string threat,string reply){Place=place;Boss=boss;Lucky=lucky;Number=lucky.ToString();Lore=lore;Threat=threat;Reply=reply;}
    }
    public static class WorldTour
    {
        static readonly TourStop[] Stops =
        {
            new TourStop("LAS VEGAS, USA","OMEGA-99 / CASINO GRID",7,
                "Lucky 7 pays out on every slot machine on the Strip.",
                "Every slot on the Strip now pays to me. The house always wins.",
                "Then I'll count to 7 and break the bank."),
            new TourStop("BANGKOK, THAILAND","OMEGA-99 / RIVER LOCK",9,
                "In Thai, nine sounds like 'kao': to step forward.",
                "I have locked every bridge on the river. No one steps forward tonight.",
                "Nine steps. I only need nine."),
            new TourStop("SHANGHAI, CHINA","OMEGA-99 / SKYLINE NODE",8,
                "Eight sounds like 'fa': fortune and wealth.",
                "I froze every account in the tower. Fortune now runs on my clock.",
                "Eight is mine. Watch me count."),
            new TourStop("ROME, ITALY","OMEGA-99 / FORUM ARCHIVE",13,
                "In Italy, 'fare tredici' means hitting the jackpot. Thirteen is lucky.",
                "Two thousand years of records, rewritten in one night.",
                "Thirteen. History is about to get a new line."),
            new TourStop("DUBLIN, IRELAND","OMEGA-99 / CLOVER WALL",4,
                "A four-leaf clover is the old sign of luck.",
                "I pruned every clover in the city. Your luck is a rounding error.",
                "I only need to find four."),
            new TourStop("HONG KONG","OMEGA-99 / HARBOUR GATE",6,
                "Six sounds like 'flowing smoothly'.",
                "The harbour lights are mine. Nothing flows unless I allow it.",
                "Six. Smooth and simple."),
            new TourStop("TOKYO, JAPAN","OMEGA-99 / TRANSIT CORE",8,
                "The kanji for eight widens like a fan: a future that opens up.",
                "Every train now runs to my timetable. Your future closes here.",
                "I'll open it again. Eight."),
            new TourStop("BERLIN, GERMANY","OMEGA-99 / SIGNAL TOWER",3,
                "'Aller guten Dinge sind drei': all good things come in threes.",
                "The tower broadcasts my numbers now. Count along, glitch.",
                "One, two, three. That is all it takes."),
            new TourStop("OSAKA, JAPAN","OMEGA-99 / SHRINE SERVER",7,
                "Seven Lucky Gods sail into the new year here.",
                "I have shut down all seven gods. Luck is offline.",
                "Then I'll bring back seven myself."),
            new TourStop("HANOI, VIETNAM","OMEGA-99 / LAKE RELAY",9,
                "Nine stands for long life and lasting luck.",
                "Every lantern on the lake now answers to me.",
                "Nine lanterns. I'll light them back up."),
            new TourStop("SINGAPORE","OMEGA-99 / MARINA VAULT",88,
                "Eighty-eight: double fortune, the luckiest number in the bay.",
                "Double fortune means double bullets.",
                "Then I'll count double. 88."),
            new TourStop("BEIJING, CHINA","OMEGA-99 / ETERNAL CORE",99,
                "Ninety-nine sounds like 'forever'.",
                "I am OMEGA-99. Ninety-nine means forever. I am forever.",
                "Nothing is forever. Count to 99, then count you to ZERO."),
        };
        public static int Count=>Stops.Length;
        // Level 30 (the finale) is always Beijing; everything else cycles the first stops.
        public static TourStop ForLevel(int level)
        {
            if(level<1)level=1;
            if(level%30==0)return Stops[Stops.Length-1];
            return Stops[(level-1)%(Stops.Length-1)];
        }
        public static TourStop Current=>ForLevel(Campaign.Active?Campaign.Data.level:1);
    }

    // Mechanics unlock one at a time so the first minute is about a single idea: count to the lucky number.
    public static class Unlocks
    {
        public static int Level=>Campaign.Active?Campaign.Data.level:99;
        public static bool RuleVariety=>Level>=2;      // L1: only EXACTLY n
        public static bool AdvancedRules=>Level>=3;    // L2: + even / odd ; L3+: multiple, prime, greater, less
        public static bool AdvancedOrbs=>Level>=2;     // L1: only + and - orbs
        public static bool Jinx=>Level>=3;
        public static bool Minions=>Level>=2;
        public static bool BlockWaves=>Level>=3;
        public static bool Allies=>Level>=2;
        public static float BossFireScale=>Level==1?.65f:Level==2?.85f:1;
        public static string NewMechanic=>Level switch
        {
            1=>"ONE RULE / COUNT TO THE LUCKY NUMBER",
            2=>"NEW / EVEN + ODD RULES, x AND / ORBS, SENTINELS, ALLIES",
            3=>"NEW / ALL RULES, JINX ORBS, FALLING BLOCKS",
            4=>"NEW / TRACKING LIGHTNING",
            _=>null
        };
    }
}
