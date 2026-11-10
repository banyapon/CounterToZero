namespace CountToZero
{
    // Advice describes the actual final hit, not a random tip unrelated to the loss.
    public static class DefeatAdvice
    {
        public static string For(string cause) => cause switch
        {
            "Enemy projectile" => "Move through gaps, not toward bullets.\nSave your slide for a closing gap.",
            "Falling cube" => "Leave the red landing circle before impact.\nKeep a clear route around purple blocks.",
            "Falling fireball" => "Leave the marked landing area early.\nKeep moving until the fireball lands.",
            "Red laser / 50% max HP" => "A live red beam removes half your max HP.\nWait for it to switch off before crossing.",
            "Crushed at bottom edge" => "Move sideways out of a descending block's path.\nKeep space between you and the bottom edge.",
            "Tracking lightning" => "Leave the warning circle, then keep moving.\nThe lightning follows you after it strikes.",
            "Virus segment" => "Keep clear of the moving body segments.\nDestroy the segments before attacking the core.",
            "Sentinel contact" => "Back away before the sentinel reaches you.\nAuto-fire targets the nearest enemy.",
            "Unstable floor" => "Move off the marked floor hazard.\nDo not wait inside its warning area.",
            "Boss Lucky counter" => "Watch which number orbs the boss collects.\nShoot +/- orbs to disrupt its Lucky match.",
            "VALUE overflow (+/-100)" => "Keep your value between -99 and +99.\nChoose division or a smaller change near the limit.",
            "Whip arm" => "Keep clear of the sweeping arm's path.\nMove into the space behind its sweep.",
            "Chain flail" => "Give the swinging chain a wide berth.\nAvoid getting pinned near the arena edge.",
            "Whip shock" => "Leave the whip's marked impact area.\nKeep moving after the arm strikes.",
            "Omega 8-way laser" => "Stand between the eight laser spokes.\nAvoid crossing a beam while it is active.",
            "Omega rush" => "Move sideways out of the boss's rush path.\nKeep a slide ready when the boss approaches.",
            _ => "Keep an escape route through the arena.\nWatch warning marks before moving forward."
        };
    }
}
