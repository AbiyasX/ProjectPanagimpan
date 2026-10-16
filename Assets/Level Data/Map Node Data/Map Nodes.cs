using UnityEngine;

[CreateAssetMenu(fileName = "MapNodes", menuName = "Scriptable Objects/MapNodes")]
public class MapNodes : ScriptableObject
{
    public string Name;
    public GameObject Model;
    bool isEnd;
}
