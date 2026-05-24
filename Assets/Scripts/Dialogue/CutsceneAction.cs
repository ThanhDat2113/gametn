using System.Collections;
using UnityEngine;

public abstract class CutsceneAction : ScriptableObject
{
    public abstract IEnumerator Execute();
}