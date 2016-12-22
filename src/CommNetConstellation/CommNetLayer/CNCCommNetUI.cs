﻿using CommNet;
using KSP.UI.Screens.Mapview;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Vectrosity;

namespace CommNetConstellation.CommNetLayer
{
    public class CNCCommNetUI : CommNetUI
    {
        public CNCCommNetUI()
        {
            //base.colorHigh = new Color(0.43f, 0.81f, 0.96f, 1f); // blue
        }

        public static new CNCCommNetUI Instance
        {
            get;
            protected set;
        }

        protected override void Start()
        {
            base.Start();
        }

        /*
        protected override void Awake()
        {
            if (CNCCommNetUI.Instance != null && CNCCommNetUI.Instance != this)
            {
                UnityEngine.Object.Destroy(CNCCommNetUI.Instance);
            }
            CNCCommNetUI.Instance = this;
            this.lineMaterial = CNCCommNetUI.TelemetryMaterial;
            CNCCommNetUI.LowColorBrightnessFactor = GameSettings.COMMNET_LOWCOLOR_BRIGHTNESSFACTOR;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (CNCCommNetUI.Instance == this)
            {
                CNCCommNetUI.Instance = null;
            }
        }
        */

        protected override void UpdateDisplay()
        {
            //base.UpdateDisplay();
            updateCodes();
            coloriseEachConstellation(CNCSettings.Instance.PublicRadioFrequency, new Color(0.65f, 0.65f, 0.65f, 1f));
            coloriseEachConstellation(1, new Color(0.43f, 0.81f, 0.96f, 1f));
            coloriseEachConstellation(2, new Color(0.95f, 0.43f, 0.49f, 1f));

            VectorLine l = Line;
        }

        private void coloriseEachConstellation(int radioFrequency, Color newColor)
        {
            List<CNCCommNetVessel> commnetVessels = CNCUtils.getCommNetVessels(radioFrequency);

            for (int i = 0; i < commnetVessels.Count; i++)
            {
                MapObject mapObj = commnetVessels.ElementAt(i).Vessel.mapObject;

                if (mapObj.type == MapObject.ObjectType.Vessel)
                {
                    Image thisImageIcon = mapObj.uiNode.GetComponentInChildren<Image>();
                    thisImageIcon.color = newColor;
                }
            }

            //TODO: color links
            //line.SetColor(newColor);
            //line.Draw();

            /*
            CommNetwork commNet = CommNetNetwork.Instance.CommNet;
            CommNetVessel commNetVessel = null;
            CommNode commNode = null;
            CommPath commPath = null;

            if (this.vessel != null && this.vessel.connection != null && this.vessel.connection.Comm.Net != null)
            {
                commNetVessel = this.vessel.connection;
                commNode = commNetVessel.Comm;
                commPath = commNetVessel.ControlPath;
            }

            switch (CommNetUI.Mode)
            {
                case CommNetUI.DisplayMode.Network:
                {
                    int index2 = num;
                    while (index2-- > 0)
                    {
                        CommLink commLink = commNet.Links[index2];
                        float f = (float)commNet.Links[index2].GetBestSignal();
                        float t = Mathf.Pow(f, this.colorLerpPower);
                        if (this.swapHighLow)
                        {
                            this.line.SetColor(Color.Lerp(this.colorHigh, this.colorLow, t), index2);
                        }
                        else
                        {
                            this.line.SetColor(Color.Lerp(this.colorLow, this.colorHigh, t), index2);
                        }
                    }
                    break;
                }
            }
            */
        }

        private void updateCodes()
        {
            if (FlightGlobals.ActiveVessel == null)
            {
                this.useTSBehavior = true;
            }
            else
            {
                this.useTSBehavior = false;
                this.vessel = FlightGlobals.ActiveVessel;
            }
            if (this.vessel == null || this.vessel.connection == null || this.vessel.connection.Comm.Net == null)
            {
                this.useTSBehavior = true;
                if (CommNetUI.ModeTrackingStation != CommNetUI.DisplayMode.None)
                {
                    if (CommNetUI.ModeTrackingStation != CommNetUI.DisplayMode.Network)
                    {
                        ScreenMessages.PostScreenMessage("CommNet Visualization set to " + CommNetUI.DisplayMode.Network.ToString(), 5f);
                    }
                    CommNetUI.ModeTrackingStation = CommNetUI.DisplayMode.Network;
                }
            }
            if (CNCCommNetNetwork.Instance == null)
            {
                return;
            }
            if (this.useTSBehavior)
            {
                CommNetUI.Mode = CommNetUI.ModeTrackingStation;
            }
            else
            {
                CommNetUI.Mode = CommNetUI.ModeFlightMap;
            }
            CommNetwork commNet = CNCCommNetNetwork.Instance.CommNet;
            CommNetVessel commNetVessel = null;
            CommNode commNode = null;
            CommPath commPath = null;
            if (this.vessel != null && this.vessel.connection != null && this.vessel.connection.Comm.Net != null)
            {
                commNetVessel = this.vessel.connection;
                commNode = commNetVessel.Comm;
                commPath = commNetVessel.ControlPath;
            }
            int count = this.points.Count;
            int num = 0;
            switch (CommNetUI.Mode)
            {
                case CommNetUI.DisplayMode.None:
                    num = 0;
                    break;
                case CommNetUI.DisplayMode.FirstHop:
                    if (commNetVessel.ControlState == VesselControlState.Probe || commNetVessel.ControlState == VesselControlState.Kerbal || commPath == null || commPath.Count == 0)
                    {
                        num = 0;
                    }
                    else
                    {
                        commPath.First.GetPoints(this.points);
                        num = 1;
                    }
                    break;
                case CommNetUI.DisplayMode.Path:
                    if (commNetVessel.ControlState == VesselControlState.Probe || commNetVessel.ControlState == VesselControlState.Kerbal || commPath == null || commPath.Count == 0)
                    {
                        num = 0;
                    }
                    else
                    {
                        commPath.GetPoints(this.points, true);
                        num = commPath.Count;
                    }
                    break;
                case CommNetUI.DisplayMode.VesselLinks:
                    num = commNode.Count;
                    commNode.GetLinkPoints(this.points);
                    break;
                case CommNetUI.DisplayMode.Network:
                    if (this.vessel == null)
                    {
                    }
                    if (commNet.Links.Count == 0)
                    {
                        num = 0;
                    }
                    else
                    {
                        commNet.GetLinkPoints(this.points);
                        num = commNet.Links.Count;
                    }
                    break;
            }
            if (num == 0)
            {
                if (this.line != null)
                {
                    this.line.active = false;
                }
                this.points.Clear();
                return;
            }
            if (this.line != null)
            {
                this.line.active = true;
            }
            else
            {
                this.refreshLines = true;
            }
            ScaledSpace.LocalToScaledSpace(this.points);
            if (this.refreshLines || MapView.Draw3DLines != this.draw3dLines || count != this.points.Count || this.line == null)
            {
                this.CreateLine(ref this.line, this.points);
                this.draw3dLines = MapView.Draw3DLines;
                this.refreshLines = false;
            }
            switch (CommNetUI.Mode)
            {
                case CommNetUI.DisplayMode.FirstHop:
                    {
                        float f = (float)commPath.First.signalStrength;
                        float t = Mathf.Pow(f, this.colorLerpPower);
                        if (this.swapHighLow)
                        {
                            this.line.SetColor(Color.Lerp(this.colorHigh, this.colorLow, t), 0);
                        }
                        else
                        {
                            this.line.SetColor(Color.Lerp(this.colorLow, this.colorHigh, t), 0);
                        }
                        break;
                    }
                case CommNetUI.DisplayMode.Path:
                    {
                        int index = num;
                        while (index-- > 0)
                        {
                            float f = (float)commPath[index].signalStrength;
                            float t = Mathf.Pow(f, this.colorLerpPower);
                            if (this.swapHighLow)
                            {
                                this.line.SetColor(Color.Lerp(this.colorHigh, this.colorLow, t), index);
                            }
                            else
                            {
                                this.line.SetColor(Color.Lerp(this.colorLow, this.colorHigh, t), index);
                            }
                        }
                        break;
                    }
                case CommNetUI.DisplayMode.VesselLinks:
                    {
                        Dictionary<CommNode, CommLink>.ValueCollection.Enumerator enumerator = commNode.Values.GetEnumerator();
                        int num2 = 0;
                        while (enumerator.MoveNext())
                        {
                            CommLink commLink = enumerator.Current;
                            float f = (float)commLink.GetSignalStrength(commLink.a != commNode, commLink.b != commNode);
                            float t = Mathf.Pow(f, this.colorLerpPower);
                            if (this.swapHighLow)
                            {
                                this.line.SetColor(Color.Lerp(this.colorHigh, this.colorLow, t), num2);
                            }
                            else
                            {
                                this.line.SetColor(Color.Lerp(this.colorLow, this.colorHigh, t), num2);
                            }
                            num2++;
                        }
                        break;
                    }
                case CommNetUI.DisplayMode.Network:
                    {
                        int index2 = num;
                        while (index2-- > 0)
                        {
                            CommLink commLink = commNet.Links[index2];
                            float f = (float)commNet.Links[index2].GetBestSignal();
                            float t = Mathf.Pow(f, this.colorLerpPower);
                            if (this.swapHighLow)
                            {
                                this.line.SetColor(Color.Lerp(this.colorHigh, this.colorLow, t), index2);
                            }
                            else
                            {
                                this.line.SetColor(Color.Lerp(this.colorLow, this.colorHigh, t), index2);
                            }
                        }
                        break;
                    }
            }
            if (this.draw3dLines)
            {
                this.line.SetWidth(this.lineWidth3D);
                this.line.Draw3D();
            }
            else
            {
                this.line.SetWidth(this.lineWidth2D);
                this.line.Draw();
            }
        }
    }
}
