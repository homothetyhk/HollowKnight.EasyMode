using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyMode
{
    public class Settings
    {
        private bool _noShadeOnDeath = true;
        public bool NoShadeOnDeath
        {
            get
            {
                return _noShadeOnDeath;
            }
            set
            {
                _noShadeOnDeath = value;
                EasyMode.ToggleShadeSpawn();
            }
        }

        public bool ExtraBlueHP { get; set; } = true;
        public int ExtraBlueHPAmount { get; set; } = 2;

        public bool ExtraNailDamage { get; set; } = true;
        public int ExtraNailDamageBase { get; set; } = 8;
        public int ExtraNailDamagePerUpgrade { get; set; } = 5;

        public bool ExtraSoulRecharge { get; set; } = true;
        public int ExtraSoulPerHitAmount { get; set; } = 6;

        private bool _fasterFocus = true;
        public bool FasterFocus
        {
            get
            {
                return _fasterFocus;
            }
            set
            {
                _fasterFocus = value;
                EasyMode.ToggleFastFocus();
            }
        }
        public float FasterFocusTimeMultipler { get; set; } = 0.5f;

        public bool ReducedCharmCost { get; set; } = true;
    }
}
