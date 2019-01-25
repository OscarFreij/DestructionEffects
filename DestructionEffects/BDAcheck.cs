using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using CompoundParts;
using UnityEngine;

namespace DestructionEffects
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class BDACheck : MonoBehaviour
    {        
        public static bool bdaAvailable = false;
        void Start()
        {
            bdaAvailable = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name == "BDArmory");
        }
    }
}