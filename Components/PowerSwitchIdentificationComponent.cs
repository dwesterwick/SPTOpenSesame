using Comfort.Common;
using EFT.Interactive;
using SPTOpenSesame.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTOpenSesame.Components
{
    internal class PowerSwitchIdentificationComponent : MonoBehaviour
    {
        private static readonly string[] _powerSwitchIds = new string[]
        {
            "custom_DesignStuff_00034",
            "Shopping_Mall_DesignStuff_00055"
        };

        public EFT.Interactive.Switch PowerSwitch { get; private set; } = null;

        protected void Awake()
        {
            FindPowerSwitch();
        }

        private void FindPowerSwitch()
        {
            IEnumerable<Switch> powerSwitches = FindObjectsOfType<Switch>()
                .Where(s => _powerSwitchIds.Contains(s.Id));

            int count = powerSwitches.Count();
            switch (count)
            {
                case 0:
                    PowerSwitch = null;
                    break;
                case 1:
                    PowerSwitch = powerSwitches.First();
                    Singleton<LoggingUtil>.Instance.LogInfo($"Found power switch {PowerSwitch.Id}");
                    break;
                default:
                    string powerSwitchIds = string.Join(", ", powerSwitches.Select(sw => sw.Id));
                    throw new InvalidOperationException($"Found {count} power switches: {powerSwitchIds}");
            }
        }
    }
}
