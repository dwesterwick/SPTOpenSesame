using Comfort.Common;
using EFT.Interactive;
using SPTOpenSesame.Helpers;
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
        public Switch PowerSwitchOnMap { get; private set; } = null;

        protected void Awake()
        {
            FindPowerSwitch();
        }

        private void FindPowerSwitch()
        {
            IEnumerable<Switch> powerSwitches = FindObjectsOfType<Switch>()
                .Where(sw => sw.IsPowerSwitch());

            int count = powerSwitches.Count();
            switch (count)
            {
                case 0:
                    Singleton<LoggingUtil>.Instance.LogInfo("No power switch found on map");
                    PowerSwitchOnMap = null;
                    break;
                case 1:
                    PowerSwitchOnMap = powerSwitches.First();
                    Singleton<LoggingUtil>.Instance.LogInfo($"Found power switch {PowerSwitchOnMap.Id}");
                    break;
                default:
                    string powerSwitchIds = string.Join(", ", powerSwitches.Select(sw => sw.Id));
                    throw new InvalidOperationException($"Found {count} power switches: {powerSwitchIds}");
            }
        }
    }
}
