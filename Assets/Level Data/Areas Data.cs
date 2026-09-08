using UnityEngine;

[CreateAssetMenu(fileName = "AreasData", menuName = "Scriptable Objects/AreasData")]
public class AreasData : ScriptableObject
{
    public string AreaTitle;
    public string WelcomeText;
    [TextArea(10,10)]
    public string areaSypnosis;
    public GameObject background;
    public ScenarioData[] possbileScenarios;
}
