using System.Collections;
using System.Linq;
using KSP.UI.Screens;
using UnityEngine;

namespace BDArmory.UI
{
    /// <summary>
    /// Adds an "Armory" button to the editor's Filter by Function list that shows every BDArmory Lite weapon part.
    ///
    /// BDArmory Lite has no sub-tab bar of its own. Parts carry their subcategory for VAB Organizer
    /// (a VABORGANIZER { organizerSubcategory = bda... } block, written by the packaging script), so with
    /// VAB Organizer installed the parts are grouped under headers (Guns, Missiles, Bombs, ...) both here and
    /// in the stock Utility category. The packaging script also writes "bdacategory" on each part, which is
    /// what this filter uses.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class BDAEditorCategory : MonoBehaviour
    {
        public const string BDACategoryKey = "bdacategory";
        const string FilterName = "BDAParts";
        const string FilterDisplayName = "Armory";
        const string IconPath = "BDArmoryLite/Textures/icon";

        PartCategorizer.Category armoryFilter;

        void Awake()
        {
            if (PartLoader.LoadedPartsList.Any(IsArmoryPart))
                GameEvents.onGUIEditorToolbarReady.Add(OnToolbarReady);
        }

        void OnDestroy()
        {
            GameEvents.onGUIEditorToolbarReady.Remove(OnToolbarReady);
        }

        static bool IsArmoryPart(AvailablePart part) =>
            part != null && part.partConfig != null && !part.TechHidden && part.TechRequired != "Unresearchable" && part.partConfig.HasValue(BDACategoryKey);

        void OnToolbarReady() => StartCoroutine(AddArmoryFilter());

        IEnumerator AddArmoryFilter()
        {
            // Give filter extensions (and VAB Organizer) a few frames to set up first.
            yield return null;
            yield return null;
            yield return null;

            var filterByFunction = PartCategorizer.Instance.filters.Find(f => f.button.categorydisplayName == "#autoLOC_453547");
            if (filterByFunction == null)
            {
                Debug.LogWarning("[BDArmory.BDAEditorCategory]: Couldn't find the Filter by Function category; no Armory filter added.");
                yield break;
            }
            if (armoryFilter != null && filterByFunction.subcategories.Contains(armoryFilter)) yield break;

            var iconTex = GameDatabase.Instance.GetTexture(IconPath, false);
            var icon = new RUI.Icons.Selectable.Icon("BDArmory", iconTex, iconTex, false);
            armoryFilter = PartCategorizer.AddCustomSubcategoryFilter(filterByFunction, FilterName, FilterDisplayName, icon, IsArmoryPart);
            Debug.Log($"[BDArmory.BDAEditorCategory]: Added the Armory filter with {PartLoader.LoadedPartsList.Count(IsArmoryPart)} parts.");
        }
    }
}
