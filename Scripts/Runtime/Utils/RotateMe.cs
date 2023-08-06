using UnityEngine;

namespace HSR.Utils
{
    [DisallowMultipleComponent]
    public class RotateMe : MonoBehaviour
    {
        public float Speed = 20;

        private void Update()
        {
            transform.Rotate(Vector3.up, -Speed * Time.deltaTime, Space.Self);
        }
    }
}
