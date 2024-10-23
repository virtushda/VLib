using UnityEngine;

namespace VLib.TestMovement
{
    [ExecuteAlways]
    public class TestSpinner : MonoBehaviour
    {
        [SerializeField] float distance = 5f;
        [SerializeField] float speed = 1f;
        [SerializeField] bool removeOnPlay = true;

        float currentSpin;

        void OnEnable()
        {
            currentSpin = 0;
            
            if (Application.isPlaying)
            {
                if (removeOnPlay)
                    Destroy(this);
            }
        }

        void Update()
        {
            currentSpin += Time.deltaTime * speed;
            var positionDir = new Vector3(Mathf.Sin(currentSpin), 0, Mathf.Cos(currentSpin));
            transform.localPosition = positionDir * distance;
            
            // Face transform along velocity
            var alongCircle = Vector3.Cross(positionDir, Vector3.up);
            transform.rotation = Quaternion.LookRotation(alongCircle, Vector3.up);
        }
    }
}