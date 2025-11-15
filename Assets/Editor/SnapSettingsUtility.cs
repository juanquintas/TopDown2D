using UnityEditor;
using UnityEngine;

namespace SmallScaleInc.TopDownPixelCharactersPack1.EditorTools
{
    public static class SnapSettingsUtility
    {
        [MenuItem("Tools/Isometric/Set Snap 1x1")]
        public static void SetSnapOneUnit()
        {
            EditorSnapSettings.move = new Vector3(1f, 1f, 0f);
            EditorSnapSettings.rotate = 0f;
            EditorSnapSettings.scale = 1f;
            Debug.Log("Snap settings updated: Move (1,1,0)");
        }
    }
}
