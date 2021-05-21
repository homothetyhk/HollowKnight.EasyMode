using Modding;

namespace EasyMode
{
    public class GlobalModSettings : ModSettings
    {
        public bool extra_lifeblood = true;
        public bool reduced_charm_cost = true;
        public bool more_damage = true;
        public bool more_soul = true;
        public bool no_shade = true;
        public bool fast_focus = true;

        //extra lifeblood
        public int more_lifeblood = 2;

        //more damage
        public int base_nail = 8;
        public int increase_per_upgrade = 5;

        //more soul
        public int increased_soul = 6;

        //focus multiplier
        public float focus_multiplier = 0.5f;

    }
}
