using UnityEngine;
namespace CafeShopSimulator
{
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class WorldCameraFrame : MonoBehaviour
    {
        private void LateUpdate()
        {
            var camera=GetComponent<Camera>();
            float aspect=(float)Screen.width/Mathf.Max(1,Screen.height),target=16f/9;
            camera.rect=aspect>target?new Rect((1-target/aspect)/2,0,target/aspect,1):new Rect(0,(1-aspect/target)/2,1,aspect/target);
        }
    }
}
