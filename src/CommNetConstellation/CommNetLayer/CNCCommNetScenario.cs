using CommNet;
using CommNetManagerAPI;
using System;
using System.Collections.Generic;

namespace CommNetConstellation.CommNetLayer
{
    /// <summary>
    /// This class is the key that allows to break into and customise KSP's CommNet. This is possibly the secondary model in the Model–view–controller sense
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] {GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.EDITOR })]
    public class CNCCommNetScenario : CommNetScenario
    {
        /* Note:
         * 1) On entering a desired scene, OnLoad() and then Start() are called.
         * 2) On leaving the scene, OnSave() is called
         * 3) GameScenes.SPACECENTER is recommended so that the constellation data can be verified and error-corrected in advance
         */

        //TODO: investigate to add extra ground stations to the group of existing stations

        private CNCCommNetUI CustomCommNetUI = null;
        private CommNetNetwork CustomCommNetNetwork = null;
        public List<Constellation> constellations; // leave the initialisation to OnLoad()
        public List<CNCCommNetHome> groundStations; // leave the initialisation to OnLoad()
        private List<CNCCommNetHomeData> persistentGroundStations; // leave the initialisation to OnLoad()
        private List<CNCCommNetVessel> commVessels;
        private bool dirtyCommNetVesselList;

        public static new CNCCommNetScenario Instance
        {
            get;
            protected set;
        }

        protected override void Start()
        {
            if (!CommNetManagerChecker.CommNetManagerInstalled)
            {
                CNCLog.Error("CommNet Manager is not installed! Please download and install this to use CommNet Constellation.");
                return;
            }

            CNCLog.Verbose("CommNet Scenario loading ...");

            CNCCommNetScenario.Instance = this;
            this.commVessels = new List<CNCCommNetVessel>();
            this.dirtyCommNetVesselList = true;

            //Replace the CommNet user interface
            CommNetUI ui = FindObjectOfType<CommNetUI>(); // the order of the three lines is important
            CustomCommNetUI = gameObject.AddComponent<CNCCommNetUI>(); // gameObject.AddComponent<>() is "new" keyword for Monohebaviour class
            UnityEngine.Object.DestroyImmediate(ui);

            //Request for CommNet network
            CommNetManagerChecker.SetCommNetManagerIfAvailable(this, typeof(CNCCommNetNetwork), out CustomCommNetNetwork);

            //Request for CommNetManager instance
            ICommNetManager CNM = (ICommNetManager)CommNetManagerChecker.GetCommNetManagerInstance();
            CNM.SetCommNetTypes();

            //Obtain ground stations
            for (int i = 0; i < CNM.Homes.Count; i++)
            {
                var cncHome = CNM.Homes[i].Components.Find(x => x is CNCCommNetHome) as CNCCommNetHome;
                if (cncHome != null)
                {
                    groundStations.Add(cncHome);
                }
            }

            //Apply the ground-station changes from persistent.sfs
            for (int i = 0; i < persistentGroundStations.Count; i++)
            {
                if (groundStations.Exists(x => x.ID.Equals(persistentGroundStations[i].ID)))
                {
                    groundStations.Find(x => x.ID.Equals(persistentGroundStations[i].ID)).applySavedChanges(persistentGroundStations[i]);
                }
            }
            persistentGroundStations.Clear();//dont need anymore

            CNCLog.Verbose("CommNet Scenario loading done! ");
        }

        public override void OnAwake()
        {
            //override to turn off CommNetScenario's instance check

            GameEvents.onVesselCreate.Add(new EventData<Vessel>.OnEvent(this.onVesselCountChanged));
            GameEvents.onVesselDestroy.Add(new EventData<Vessel>.OnEvent(this.onVesselCountChanged));
        }

        private void OnDestroy()
        {
            if (this.CustomCommNetUI != null)
                UnityEngine.Object.Destroy(this.CustomCommNetUI);

            if (this.CustomCommNetNetwork != null)
                UnityEngine.Object.Destroy(this.CustomCommNetNetwork);

            this.constellations.Clear();
            this.commVessels.Clear();
            this.groundStations.Clear();
            this.persistentGroundStations.Clear();

            GameEvents.onVesselCreate.Remove(new EventData<Vessel>.OnEvent(this.onVesselCountChanged));
            GameEvents.onVesselDestroy.Remove(new EventData<Vessel>.OnEvent(this.onVesselCountChanged));
        }

        public override void OnLoad(ConfigNode gameNode)
        {
            base.OnLoad(gameNode);
            CNCLog.Verbose("Scenario content to be read:\n{0}", gameNode);

            //Constellations
            if (gameNode.HasNode("Constellations"))
            {
                ConfigNode constellationNode = gameNode.GetNode("Constellations");
                ConfigNode[] constellationNodes = constellationNode.GetNodes();

                if (constellationNodes.Length < 1) // missing constellation list
                {
                    CNCLog.Error("The 'Constellations' node is malformed! Reverted to the default constellation list.");
                    constellations = CNCSettings.Instance.Constellations;
                }
                else
                {
                    constellations = new List<Constellation>();

                    for (int i = 0; i < constellationNodes.Length; i++)
                    {
                        Constellation newConstellation = new Constellation();
                        ConfigNode.LoadObjectFromConfig(newConstellation, constellationNodes[i]);
                        constellations.Add(newConstellation);
                    }
                }
            }
            else
            {
                CNCLog.Verbose("The 'Constellations' node is not found. The default constellation list is loaded.");
                constellations = CNCSettings.Instance.Constellations;
            }

            constellations.Sort();

            //Ground stations
            groundStations = new List<CNCCommNetHome>();
            persistentGroundStations = new List<CNCCommNetHomeData>();
            if (gameNode.HasNode("GroundStations"))
            {
                ConfigNode stationNode = gameNode.GetNode("GroundStations");
                ConfigNode[] stationNodes = stationNode.GetNodes();

                if (stationNodes.Length < 1) // missing ground-station list
                {
                    CNCLog.Error("The 'GroundStations' node is malformed! Reverted to the default list of ground stations.");
                    //do nothing since KSP provides this default list
                }
                else
                {
                    for (int i = 0; i < stationNodes.Length; i++)
                    {
                        CNCCommNetHomeData stationData = new CNCCommNetHomeData();
                        ConfigNode.LoadObjectFromConfig(stationData, stationNodes[i]);
                        if(!stationNodes[i].HasNode("Frequencies")) // empty list is not saved as empty node in persistent.sfs
                        {
                            stationData.Frequencies.Clear();// clear the default frequency list
                        }
                        persistentGroundStations.Add(stationData);
                    }
                }
            }
            else
            {
                CNCLog.Verbose("The 'GroundStations' node is not found. The default list of ground stations is loaded from KSP's data.");
                //do nothing since KSP provides this default list
            }
        }

        public override void OnSave(ConfigNode gameNode)
        {
            //Constellations
            if (gameNode.HasNode("Constellations"))
            {
                gameNode.RemoveNode("Constellations");
            }

            ConfigNode constellationNode = new ConfigNode("Constellations");
            for (int i=0; i<constellations.Count; i++)
            {
                ConfigNode newConstellationNode = new ConfigNode("Constellation");
                newConstellationNode = ConfigNode.CreateConfigFromObject(constellations[i], newConstellationNode);
                constellationNode.AddNode(newConstellationNode);
            }

            if (constellations.Count <= 0)
            {
                CNCLog.Error("No user-defined constellations to save!");
            }
            else
            {
                gameNode.AddNode(constellationNode);
            }

            //Ground stations
            if (gameNode.HasNode("GroundStations"))
            {
                gameNode.RemoveNode("GroundStations");
            }

            ConfigNode stationNode = new ConfigNode("GroundStations");
            for (int i = 0; i < groundStations.Count; i++)
            {
                ConfigNode newGroundStationNode = new ConfigNode("GroundStation");
                newGroundStationNode = ConfigNode.CreateConfigFromObject(groundStations[i], newGroundStationNode);
                stationNode.AddNode(newGroundStationNode);
            }

            if (groundStations.Count <= 0)
            {
                CNCLog.Error("No ground stations to save!");
            }
            else
            {
                gameNode.AddNode(stationNode);
            }

            CNCLog.Verbose("Scenario content to be saved:\n{0}", gameNode);
            base.OnSave(gameNode);
        }

        /// <summary>
        /// Obtain all communicable vessels that have the given frequency
        /// </summary>
        public List<CNCCommNetVessel> getCommNetVessels(short targetFrequency = -1)
        {
            cacheCommNetVessels();

            List<CNCCommNetVessel> newList = new List<CNCCommNetVessel>();
            for(int i=0; i<commVessels.Count; i++)
            {
                if (commVessels[i].getFrequencies().Contains(targetFrequency) || targetFrequency == -1)
                    newList.Add(commVessels[i]);
            }

            return newList;
        }

        /// <summary>
        /// Find the vessel that has the given comm node
        /// </summary>
        public Vessel findCorrespondingVessel(CommNode commNode)
        {
            cacheCommNetVessels();

            return commVessels.Find(x => CNCCommNetwork.AreSame(x.CommNetVessel.Comm, commNode)).Vessel; // more specific equal            //IEqualityComparer<CommNode> comparer = commNode.Comparer; // a combination of third-party mods somehow  affects CommNode's IEqualityComparer on two objects
            //return commVessels.Find(x => comparer.Equals(commNode, x.Comm)).Vessel;
        }

        /// <summary>
        /// Find the ground station that has the given comm node
        /// </summary>
        public CNCCommNetHome findCorrespondingGroundStation(CommNode commNode)
        {
            return groundStations.Find(x => CNCCommNetwork.AreSame(x.commNode, commNode));
        }

        /// <summary>
        /// Cache eligible vessels of the FlightGlobals
        /// </summary>
        private void cacheCommNetVessels()
        {
            if (!this.dirtyCommNetVesselList)
                return;

            CNCLog.Verbose("CommNetVessel cache - {0} entries deleted", this.commVessels.Count);
            this.commVessels.Clear();

            List<Vessel> allVessels = FlightGlobals.fetch.vessels;
            for (int i = 0; i < allVessels.Count; i++)
            {
                if (allVessels[i].connection != null && allVessels[i].vesselType != VesselType.Unknown)// && allVessels[i].vesselType != VesselType.Debris) // debris could be spent stage with functional probes and antennas
                {
                    CNCLog.Debug("Caching CommNetVessel '{0}'", allVessels[i].vesselName);
                    this.commVessels.Add(((IModularCommNetVessel)allVessels[i].connection).GetModuleOfType<CNCCommNetVessel>());
                }
            }

            CNCLog.Verbose("CommNetVessel cache - {0} entries added", this.commVessels.Count);

            this.dirtyCommNetVesselList = false;
        }

        /// <summary>
        /// GameEvent call for newly-created vessels (launch, staging, new asteriod etc)
        /// NOTE: Vessel v is fresh bread straight from the oven before any curation is done on this (i.e. debris.Connection is valid)
        /// </summary>
        private void onVesselCountChanged(Vessel v)
        {
            if (v.vesselType == VesselType.Base || v.vesselType == VesselType.Lander || v.vesselType == VesselType.Plane ||
               v.vesselType == VesselType.Probe || v.vesselType == VesselType.Relay || v.vesselType == VesselType.Rover ||
               v.vesselType == VesselType.Ship || v.vesselType == VesselType.Station)
            {
                CNCLog.Debug("Change in the vessel list detected. Cache refresh required.");
                this.dirtyCommNetVesselList = true;
            }
        }

        /// <summary>
        /// Convenient method to obtain a frequency list from a given CommNode
        /// </summary>
        public List<short> getFrequencies(CommNode a)
        {
            List<short> aFreqs = new List<short>();

            if (a.isHome && findCorrespondingGroundStation(a) != null)
            {
                aFreqs.AddRange(findCorrespondingGroundStation(a).Frequencies);
            }
            else
            {
                var cncvessel = ((IModularCommNetVessel)a.GetVessel().Connection).GetModuleOfType<CNCCommNetVessel>();
                aFreqs = cncvessel.getFrequencies();
            }

            return aFreqs;
        }

        /// <summary>
        /// Convenient method to obtain Comm Power of given frequency from a given CommNode
        /// </summary>
        public double getCommPower(CommNode a, short frequency)
        {
            double power = 0.0;

            if (a.isHome && findCorrespondingGroundStation(a) != null)
            {
                power = a.antennaRelay.power;
            }
            else
            {
                var cncvessel = ((IModularCommNetVessel)a.GetVessel().Connection).GetModuleOfType<CNCCommNetVessel>();
                power = cncvessel.getMaxComPower(frequency);
            }

            return power;
        }
    }
}
