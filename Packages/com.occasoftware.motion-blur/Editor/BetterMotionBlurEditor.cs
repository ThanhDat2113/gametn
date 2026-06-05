using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using OccaSoftware.MotionBlur.Runtime;

namespace OccaSoftware.MotionBlur.Editor
{
    [CustomEditor(typeof(BetterMotionBlur))]
    public class BetterMotionBlurEditor : VolumeComponentEditor
    {
        SerializedDataParameter mode;
        SerializedDataParameter qualityLevel;
        SerializedDataParameter shutterSpeed;

        SerializedDataParameter depthSeparationMode;

        SerializedDataParameter depthSeparationDistance;

        SerializedDataParameter maskMode;
        SerializedDataParameter preferStrictMasks;
        SerializedDataParameter invertMask;
        SerializedDataParameter layerMask;

        public override void OnEnable()
        {
            PropertyFetcher<BetterMotionBlur> o = new PropertyFetcher<BetterMotionBlur>(serializedObject);

            mode = Unpack(o.Find(x => x.mode));
            qualityLevel = Unpack(o.Find(x => x.qualityLevel));
            shutterSpeed = Unpack(o.Find(x => x.shutterSpeed));
            depthSeparationMode = Unpack(o.Find(x => x.depthSeparationMode));
            depthSeparationDistance = Unpack(o.Find(x => x.depthSeparationDistance));

            maskMode = Unpack(o.Find(x => x.maskMode));
            preferStrictMasks = Unpack(o.Find(x => x.preferStrictMasks));
            invertMask = Unpack(o.Find(x => x.invertMask));
            layerMask = Unpack(o.Find(x => x.layerMask));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(mode);

            BeginIndent();
            PropertyField(qualityLevel, new GUIContent("Quality Level"));
            PropertyField(shutterSpeed, new GUIContent("Shutter Speed"));
            PropertyField(depthSeparationMode, new GUIContent("Depth Separation Mode"));

            BeginIndent();
            PropertyField(depthSeparationDistance, new GUIContent("Depth Separation Distance"));
            EndIndent();

            PropertyField(maskMode);

            BeginIndent();
            PropertyField(preferStrictMasks);
            PropertyField(invertMask);
            PropertyField(layerMask);
            EndIndent();
            EndIndent();
        }

        private static void BeginIndent()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(16);
            GUILayout.BeginVertical();
        }

        private static void EndIndent()
        {
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
}
