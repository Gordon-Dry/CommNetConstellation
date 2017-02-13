﻿using CommNet;
using CommNetConstellation.UI;
using System.Collections.Generic;
using System.Linq;

namespace CommNetConstellation.CommNetLayer
{
    /// <summary>
    /// PartModule to be inserted into every ModuleCommand
    /// </summary>
    //This class is coupled with the MM patch (cnc_module_MM.cfg) that inserts CNConstellationModule into every command part
    public class CNConstellationModule : PartModule
    {
        [KSPField(isPersistant = true)]
        public short radioFrequency = CNCSettings.Instance.PublicRadioFrequency;

        [KSPEvent(guiActive = true, guiActiveEditor =true, guiActiveUnfocused = false, guiName = "CommNet Constellation", active = true)]
        public void KSPEventConstellationSetup()
        {
            new VesselSetupDialog("Vessel - <color=#00ff00>Setup</color>", this.vessel, this.part, null).launch();
        }
    }

    /// <summary>
    /// Data structure for a CommNetVessel
    /// </summary>
    public class CNCCommNetVessel : CommNetVessel
    {
        protected short radioFrequency;

        /// <summary>
        /// Retrieve the radio frequency from the vessel's parts (selection applied on multiple frequencies)
        /// </summary>
        protected override void OnNetworkInitialized()
        {
            base.OnNetworkInitialized();
            this.radioFrequency = getRadioFrequency(true);
        }
        
        /// <summary>
        /// Not utilised yet
        /// </summary>
        public override void OnNetworkPostUpdate()
        {
            base.OnNetworkPostUpdate();
        }

        /// <summary>
        /// Not utilised yet
        /// </summary>
        public override void OnNetworkPreUpdate()
        {
            base.OnNetworkPreUpdate();
        }

        /// <summary>
        /// Not utilised yet
        /// </summary>
        protected override void UpdateComm()
        {
            base.UpdateComm();
        }

        /// <summary>
        /// Update the frequency of every command part of this vessel, even if one or more have different frequencies
        /// </summary>
        public void updateRadioFrequency(short newFrequency)
        {
            if (!Constellation.isFrequencyValid(newFrequency))
            {
                CNCLog.Error("The new frequency {0} is out of the range [0,{1}]!", newFrequency, short.MaxValue);
                return;
            }

            bool success = false;
            if (this.Vessel.loaded)
            {
                List<CNConstellationModule> modules = this.Vessel.FindPartModulesImplementing<CNConstellationModule>();
                for(int i=0; i<modules.Count;i++)
                    modules[i].radioFrequency = newFrequency;
                success = true;
            }
            else
            {
                List<ProtoPartSnapshot> parts = this.Vessel.protoVessel.protoPartSnapshots;
                for (int i = 0; i < parts.Count; i++)
                {
                    ProtoPartModuleSnapshot thisModule = parts[i].FindModule("CNConstellationModule");
                    if (thisModule == null)
                        continue;

                    success = thisModule.moduleValues.SetValue("radioFrequency", newFrequency);
                }
            }

            if (success)
            {
                this.radioFrequency = newFrequency;
                CNCLog.Debug("Update CommNet vessel '{0}''s frequency to {1}", this.Vessel.GetName(), newFrequency);
            }
            else
            {
                CNCLog.Error("Can't update CommNet vessel '{0}''s frequency to {1}!", this.Vessel.GetName(), newFrequency);
            }
        }

        /// <summary>
        /// Update the frequency of the specific command part of this active vessel only
        /// </summary>
        public void updateRadioFrequency(short newFrequency, Part commandPart)
        {
            if (commandPart == null)
                return;

            CNConstellationModule cncModule = commandPart.FindModuleImplementing<CNConstellationModule>();
            CNCLog.Verbose("Update the frequency of the command part '{1}' in the CommNet vessel '{0}' from {3} to {2}", this.Vessel.GetName(), commandPart.partInfo.title, newFrequency, cncModule.radioFrequency);
            cncModule.radioFrequency = newFrequency;
            getRadioFrequency(true);
        }

        /// <summary>
        /// If multiple command parts of this vessel have different frequencies, pick the frequency of the first part in order of part addition in editor
        /// </summary>
        public short getRadioFrequency(bool forceRetrievalFromModule = false)
        {
            if (forceRetrievalFromModule)
            {
                if (this.Vessel.loaded)
                {
                    CNConstellationModule cncModule = firstCommandPartSelection(this.Vessel.parts);
                    if(cncModule != null)
                    {
                        this.radioFrequency = cncModule.radioFrequency;
                    }
                    else // does this even occurs, given KSP adds the CNC module to active vessel?
                    {
                        CNCLog.Error("Active CommNet vessel '{0}' does not have any CNConstellationModule! Reset to freq {1}", this.Vessel.GetName(), this.radioFrequency);
                        this.radioFrequency = CNCSettings.Instance.PublicRadioFrequency;
                    }                        
                }
                else
                {
                    ProtoPartModuleSnapshot cncModule = firstCommandPartSelection(this.Vessel.protoVessel.protoPartSnapshots);
                    if (cncModule != null)
                    {
                        this.radioFrequency = short.Parse(cncModule.moduleValues.GetValue("radioFrequency"));
                    }
                    else
                    {
                        CNConstellationModule realcncModule = gameObject.AddComponent<CNConstellationModule>(); // don't use new keyword. PartModule is Monobehavior
                        addModuleToCommandParts(this.Vessel.protoVessel.protoPartSnapshots, realcncModule);
                        CNCLog.Verbose("Unloaded CommNet vessel '{0}' don't have any CNConstellationModule. All command parts receive new CNConstellationModule with default freq {1}", this.Vessel.GetName(), realcncModule.radioFrequency);

                        this.radioFrequency = realcncModule.radioFrequency;
                    }
                }

                CNCLog.Debug("Read the frequency {1} from the CommNet vessel '{0}''s data", this.Vessel.GetName(), this.radioFrequency);
            }

            return this.radioFrequency;
        }

        /// <summary>
        /// Selection algorithm on multiple parts with different frequenices
        /// </summary>
        public CNConstellationModule firstCommandPartSelection(List<Part> parts)
        {
            for (int i = 0; i < parts.Count; i++) // grab the first command part (part list is sorted in order of part addition in editor)
            {
                CNConstellationModule thisModule;
                if ((thisModule = parts[i].FindModuleImplementing<CNConstellationModule>()) != null)
                {
                    return thisModule;
                }
            }

            return null;
        }

        /// <summary>
        /// Selection algorithm on multiple protoparts with different frequenices
        /// </summary>
        public ProtoPartModuleSnapshot firstCommandPartSelection(List<ProtoPartSnapshot> parts)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                ProtoPartModuleSnapshot partModule;
                if ((partModule = parts[i].FindModule("CNConstellationModule")) != null)
                {
                    return partModule;
                }
            }

            return null;
        }

        /// <summary>
        /// Add given module to all command parts of unloaded vessels
        /// </summary>
        private void addModuleToCommandParts(List<ProtoPartSnapshot> parts, CNConstellationModule newModule)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].FindModule("ModuleCommand") != null)
                {
                    parts[i].modules.Add(new ProtoPartModuleSnapshot(newModule));
                }
            }
        }
    }
}
