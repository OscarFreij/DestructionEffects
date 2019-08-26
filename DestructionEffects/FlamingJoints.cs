using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using CompoundParts;
using UnityEngine;

namespace DestructionEffects
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlamingJoints : MonoBehaviour
    {
        private const string NewFlameModelPath = "DestructionEffects/Models/FlameEffect2/model";
        private const string LegacyFlameModelPath = "DestructionEffects/Models/FlameEffect_Legacy/model";
        //private float timeNoFlames;
        //private Vessel LastVesselLoaded = null;
        public static List<GameObject> FlameObjects = new List<GameObject>();             
        public List<Vessel> vesselsAllowed = new List<Vessel>();
        private static readonly string[] PartTypesTriggeringUnwantedJointBreakEvents = new string[]
        {
            "decoupler",
            "separator",
            "docking",
            "grappling",
            "landingleg",
            "clamp",
            "gear",
            "wheel",
            "mast",
            "heatshield",
            "turret",
            "missilelauncher",
            "moudleturret",
            "missileturret",
            "missilefire",
            "kas.",
            "kis.",
            "cport,",
            "torpedo",
            "slw",
            "mortar",
            "hedg"
        };

        private static readonly string[] _PartTypesTriggeringUnwantedJointBreakEvents = new string[DESettings.PartIgnoreList.Length + PartTypesTriggeringUnwantedJointBreakEvents.Length];

        //1553 void OnPartJointBreak(PartJoint j, float breakForce)
        public void Start()
        {
            GameEvents.onPhysicsEaseStop.Add(OnPhysicsEaseStop);
            GameEvents.onPartJointBreak.Add(OnPartJointBreak);
            PartTypesTriggeringUnwantedJointBreakEvents.CopyTo(_PartTypesTriggeringUnwantedJointBreakEvents,0);
            DESettings.PartIgnoreList.CopyTo(_PartTypesTriggeringUnwantedJointBreakEvents, PartTypesTriggeringUnwantedJointBreakEvents.Length);
        }

        public void OnPhysicsEaseStop(Vessel data)
        {
            vesselsAllowed.Add(data);
        }

        public void OnPartJointBreak(PartJoint partJoint, float breakForce)
        {
            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                return;
            }
            if (partJoint.Target == null)
            {
                return;
            }
            if (partJoint.Target.PhysicsSignificance == 1)
            {
                return;
            }

            if (vesselsAllowed.Count == 0)
            {
                return;
            }
            if (!vesselsAllowed.Contains(partJoint.Target.vessel))
            {
                return;
            }
            if (!ShouldFlamesBeAttached(partJoint))
            {
                return;
            }
            if (breakForce == 0 && !BDACheck.bdaAvailable)
                return;

            AttachFlames(partJoint);
        }

        private static void AttachFlames(PartJoint partJoint)
        {
            var modelUrl = DESettings.LegacyEffect ? LegacyFlameModelPath : NewFlameModelPath;

            var flameObject =
                (GameObject)
                    Instantiate(
                        GameDatabase.Instance.GetModel(modelUrl),
                        partJoint.transform.position,
                        Quaternion.identity);

            flameObject.SetActive(true);
            flameObject.transform.parent = partJoint.Target.transform;
            flameObject.AddComponent<FlamingJointScript>();

            foreach (var pe in flameObject.GetComponentsInChildren<KSPParticleEmitter>())
            {
                if (!pe.useWorldSpace) continue;

                var gpe = pe.gameObject.AddComponent<DeGaplessParticleEmitter>();
                gpe.Part = partJoint.Target;
                gpe.Emit = true;
            }
        }

        private static bool ShouldFlamesBeAttached(PartJoint partJoint)
        {
            if (partJoint == null) return false;
            if (partJoint.Host == null) return false;
            if (!partJoint.Host) return false;
            if (partJoint.Host.Modules == null)return false;

            if (partJoint.Target == null) return false;
            if (partJoint.Target.Modules == null) return false;
            if (!partJoint.Target) return false;

            if (partJoint.Child == null) return false;
            if (partJoint.Child.Modules == null) return false;
            if (!partJoint.Child) return false;


            if (partJoint.joints.All(x => x == null)) return false;


            if (partJoint.Parent != null && partJoint.Parent.vessel != null)
            {
                if (partJoint.Parent.vessel.atmDensity <= 0.1)
                {
                    return false;
                }
            }

            var part = partJoint.Target;//SM edit for DE on ships and ship parts, adding bow, hull, stern, superstructure
       
            if (partJoint.Target.FindModulesImplementing<ModuleDecouple>().Count > 0)
            {
                return false;
            }
            
            if (partJoint.Target.FindModulesImplementing<CModuleStrut>().Count > 0 ||
                partJoint.Host.FindModulesImplementing<CModuleStrut>().Count > 0 ||
                partJoint.Child.FindModulesImplementing<CModuleStrut>().Count > 0 ||
                partJoint.Parent?.FindModulesImplementing<CModuleStrut>().Count > 0)
            {
                return false;
            }

            if (partJoint.Target.Modules.Contains("ModuleTurret"))
            {
                return false;
            }
;
            if (IsPartHostTypeAJointBreakerTrigger(partJoint.Host.name.ToLower()))
            {
                return false;
            }

            if (part.Resources
                .Any(resource => resource.resourceName.Contains("Fuel") ||
                                 resource.resourceName.Contains("Ox") ||
                                 resource.resourceName.Contains("Elec") ||
                                 resource.resourceName.Contains("Amm") ||
                                 resource.resourceName.Contains("Cann")))
            {
                return true;
            }
                
            if (part.partInfo.title.Contains("Wing") ||
                part.partInfo.title.Contains("Fuselage") ||
                part.partInfo.title.Contains("Bow") ||
                part.partInfo.title.Contains("Stern") ||
                part.partInfo.title.Contains("Hull") ||
                part.partInfo.title.Contains("Superstructure") ||
                part.FindModuleImplementing<ModuleEngines>() != null ||
                part.FindModuleImplementing<ModuleEnginesFX>() != null)/*|| part.partInfo.title.Contains("Turret") */
            {
                return true;
            }

          
            return false;
        }

        private static bool IsPartHostTypeAJointBreakerTrigger(string hostPartName)
        {
            return _PartTypesTriggeringUnwantedJointBreakEvents.Any(hostPartName.Contains);
        }
    }
}