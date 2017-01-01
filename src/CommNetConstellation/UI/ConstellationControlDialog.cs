﻿using CommNetConstellation.CommNetLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

namespace CommNetConstellation.UI
{
    public class ConstellationControlDialog : AbstractDialog
    {
        private static readonly Texture2D colorTexture = UIUtils.loadImage("colorDisplay");
        private static readonly Texture2D focusTexture = UIUtils.loadImage("target");

        public ConstellationControlDialog(string title) : base(title, 
                                                            0.8f, //x
                                                            0.5f, //y
                                                            (int)(1920*0.3), //width
                                                            (int)(1200*0.5), //height
                                                            new DialogOptions[] { DialogOptions.ShowVersion, DialogOptions.HideCloseButton, DialogOptions.AllowBgInputs }) //arguments
        {
            
        }

        protected override List<DialogGUIBase> drawContentComponents()
        {
            List<DialogGUIBase> listComponments = new List<DialogGUIBase>();

            CNCLog.Debug("drawContentComponents()");
            setupConstellationList(listComponments);
            setupSatelliteList(listComponments);

            return listComponments;
        }

        private void setupConstellationList(List<DialogGUIBase> listComponments)
        {
            listComponments.Add(new DialogGUIHorizontalLayout(true, false, 0, new RectOffset(), TextAnchor.UpperCenter, new DialogGUIBase[] { new DialogGUILabel("\n<b>You can manage multiple constellations of vessels</b>", false, false) }));
            
            List<DialogGUIHorizontalLayout> eachRowGroupList = new List<DialogGUIHorizontalLayout>();

            DialogGUIButton createButton = new DialogGUIButton("New constellation", newConstellationClick, false);
            DialogGUIHorizontalLayout creationGroup = new DialogGUIHorizontalLayout(true, false, 4, new RectOffset(), TextAnchor.MiddleLeft, new DialogGUIBase[] { new DialogGUIFlexibleSpace(), createButton, new DialogGUIFlexibleSpace() });
            eachRowGroupList.Add(creationGroup);

            for (int i = 0; i < CNCCommNetScenario.Instance.constellations.Count; i++)
            {
                Constellation thisConstellation = CNCCommNetScenario.Instance.constellations.ElementAt<Constellation>(i);

                DialogGUIImage colorImage = new DialogGUIImage(new Vector2(32, 32), Vector2.one, thisConstellation.color, colorTexture);
                DialogGUILabel constNameLabel = new DialogGUILabel(thisConstellation.name, 130, 12);
                DialogGUILabel freqLabel = new DialogGUILabel(string.Format("Frequency: {0}", thisConstellation.frequency), 110, 12);
                DialogGUILabel numSatsLabel = new DialogGUILabel(string.Format("{0} vessels", Constellation.countVesselsOf(thisConstellation)),70, 12);
                DialogGUIButton updateButton = new DialogGUIButton("Edit", editConstellationClick, 50, 32, false);

                DialogGUIBase[] rowGUIBase = new DialogGUIBase[] { colorImage, constNameLabel, freqLabel, numSatsLabel, new DialogGUIFlexibleSpace(), updateButton, null};
                if (thisConstellation.frequency == CNCSettings.Instance.PublicRadioFrequency)
                    rowGUIBase[rowGUIBase.Length - 1] = new DialogGUIButton("Reset", resetConstellationClick, 60, 32, false);
                else
                    rowGUIBase[rowGUIBase.Length - 1] = new DialogGUIButton("Delete", deleteConstellationClick, 60, 32, false);

                DialogGUIHorizontalLayout lineGroup = new DialogGUIHorizontalLayout(true, false, 4, new RectOffset(), TextAnchor.MiddleCenter, rowGUIBase);
                eachRowGroupList.Add(lineGroup);
            }

            //Prepare a list container for the GUILayout rows
            DialogGUIBase[] rows = new DialogGUIBase[eachRowGroupList.Count + 1];
            rows[0] = new DialogGUIContentSizer(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize, true);
            for (int i = 0; i < eachRowGroupList.Count; i++)
                rows[i + 1] = eachRowGroupList[i];

            listComponments.Add(new DialogGUIScrollList(Vector2.one, false, true, new DialogGUIVerticalLayout(10, 100, 4, new RectOffset(5, 25, 5, 5), TextAnchor.UpperLeft, rows)));
        }

        private void setupSatelliteList(List<DialogGUIBase> listComponments)
        {
            listComponments.Add(new DialogGUIHorizontalLayout(true, false, 0, new RectOffset(), TextAnchor.UpperCenter, new DialogGUIBase[] { new DialogGUILabel("\n<b>You can edit the constellation configuration of an eligible vessel</b>", false, false) }));
            List<CNCCommNetVessel> allVessels = CNCUtils.getCommNetVessels();

            List<DialogGUIHorizontalLayout> eachRowGroupList = new List<DialogGUIHorizontalLayout>();
            for (int i = 0; i < allVessels.Count; i++)
            {
                CNCCommNetVessel thisVessel = allVessels.ElementAt<CNCCommNetVessel>(i);
                short radioFreq = thisVessel.getRadioFrequency();
                Color color = Constellation.getColor(CNCCommNetScenario.Instance.constellations, radioFreq);

                UIStyle focusStyle = UIUtils.createImageButtonStyle(focusTexture);
                DialogGUIButton focusButton = new DialogGUIButton("", delegate { vesselFocusClick(thisVessel.Vessel.vesselName); }, null, 32, 32, false, focusStyle);
                focusButton.image = focusStyle.normal.background;

                DialogGUILabel vesselLabel = new DialogGUILabel(thisVessel.Vessel.vesselName, 160, 12);
                DialogGUILabel freqLabel = new DialogGUILabel(string.Format("Frequency: <color={0}>{1}</color>", UIUtils.colorToHex(color), radioFreq), 120, 12);
                DialogGUILabel locationLabel = new DialogGUILabel(string.Format("Orbiting: {0}", thisVessel.Vessel.mainBody.name), 120, 12);
                DialogGUIButton setupButton = new DialogGUIButton("Setup", vesselSetupClick, 70, 32, false);

                DialogGUIHorizontalLayout rowGroup = new DialogGUIHorizontalLayout(true, false, 4, new RectOffset(), TextAnchor.MiddleCenter, new DialogGUIBase[] { focusButton, vesselLabel, freqLabel, locationLabel, new DialogGUIFlexibleSpace(), setupButton });
                eachRowGroupList.Add(rowGroup);
            }

            //Prepare a list container for the GUILayout rows
            DialogGUIBase[] rows = new DialogGUIBase[eachRowGroupList.Count + 1];
            rows[0] = new DialogGUIContentSizer(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize, true);
            for (int i = 0; i < eachRowGroupList.Count; i++)
                rows[i + 1] = eachRowGroupList[i];

            listComponments.Add(new DialogGUIScrollList(Vector2.one, false, true, new DialogGUIVerticalLayout(10, 100, 4, new RectOffset(5, 25, 5, 5), TextAnchor.UpperLeft, rows)));
        }

        private void resetConstellationClick()
        {
            MultiOptionDialog warningDialog = new MultiOptionDialog("Revert to the default constellation name and color?", "Constellation", HighLogic.UISkin, new DialogGUIBase[]
            {
                new DialogGUIButton("Reset", null),
                new DialogGUIButton("Cancel", null)
            });

            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), warningDialog, false, HighLogic.UISkin, true, string.Empty);
        }

        private void deleteConstellationClick()
        {
            MultiOptionDialog warningDialog = new MultiOptionDialog("Delete this constellation NAME?\n\nAll vessels in this constellation will be moved into the public constellation.", "Constellation", HighLogic.UISkin, new DialogGUIBase[]
            {
                new DialogGUIButton("Delete", null),
                new DialogGUIButton("Cancel", null)
            });

            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), warningDialog, false, HighLogic.UISkin, true, string.Empty);
        }

        private void vesselSetupClick()
        {
            new VesselSetupDialog("Vessel - <color=#00ff00>Setup</color>", null).launch(new System.Object[] {});
        }

        private void vesselFocusClick(string id)
        {
            CNCLog.Debug("Name: " + id);
        }

        private void newConstellationClick()
        {
            new ConstellationEditDialog("Constellation - <color=#00ff00>New</color>", null).launch(new System.Object[] { });
        }

        private void editConstellationClick()
        {
            new ConstellationEditDialog("Constellation - <color=#00ff00>Edit</color>", null).launch(new System.Object[] { });
        }
    }
}
