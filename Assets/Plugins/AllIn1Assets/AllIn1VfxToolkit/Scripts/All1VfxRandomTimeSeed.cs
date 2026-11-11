using UnityEngine;

namespace AllIn1Assets.AllIn1VfxToolkit
{
    public class All1VfxRandomTimeSeed : MonoBehaviour
    {
        [SerializeField] private float minSeedValue = 0;
        [SerializeField] private float maxSeedValue = 100f;

        private void Start()
        {
            if(!TryGetComponent(out Renderer targetRenderer))
            {
                Debug.LogError("This GameObject: " + gameObject.name + " doesn't have a Renderer and it should, please double check what you are doing");
                return;
            }

            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetFloat("_TimingSeed", Random.Range(minSeedValue, maxSeedValue));
            targetRenderer.SetPropertyBlock(properties);
        }
    }
}