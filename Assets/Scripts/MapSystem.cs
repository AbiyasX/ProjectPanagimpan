using Sirenix.OdinInspector;
using UnityEngine;

public class MapSystem : MonoBehaviour
{
    public MapNodes[] m_MapNodes;
    public GameObject Parent;
    int mapIndex;
    [Button]
    public void addNode()
    {
        for (int i = 0; i < m_MapNodes.Length; i++) 
        {
            if (m_MapNodes.Length < 0) return;
            Vector3 offset = new Vector3(0, 100, 0);
            GameObject node = Instantiate(m_MapNodes[mapIndex].Model, transform.position, transform.rotation);

            node.transform.SetParent(Parent.transform);
            node.transform.position += offset;
        }
    }
}
