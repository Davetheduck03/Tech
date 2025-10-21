using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TowerManager))]
public class I_TowerManager : Editor
{
    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
        TowerManager script = (TowerManager)target;

        script.Damage = EditorGUILayout.FloatField("Damage", script.Damage);
        script.IsAOE = EditorGUILayout.Toggle("IsAOE", script.IsAOE);
        if (script.IsAOE)
        {
            script.AOE = EditorGUILayout.FloatField("AOE", script.AOE);
        }
    }
}
