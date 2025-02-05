using Drawing;
using UnityEngine;

namespace VLib.Testers
{
    [ExecuteAlways]
    public class RayCapsuleTester : MonoBehaviour
    {
        [SerializeField] CapsuleNative capsule;
        [SerializeField] Transform rayMasta;

        Ray ray;
        bool hit;

        void Update()
        {
            ray = new Ray(rayMasta.position, rayMasta.forward);
            hit = capsule.IntersectsRay(ray);
            
            Draw.ingame.PushColor(Color.white);
            capsule.DrawAline(ref Draw.ingame);
            Draw.ingame.PopColor();
            Draw.ingame.PushColor(hit ? Color.red : Color.green);
            Draw.ingame.Ray(ray, 100f);
            Draw.ingame.PopColor();
        }
    }
}