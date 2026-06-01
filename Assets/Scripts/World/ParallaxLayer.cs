  using UnityEngine;

  public class ParallaxLayer : MonoBehaviour
  {
      [SerializeField] private Transform cameraTransform;
      [SerializeField, Range(0f, 1f)] private float parallaxFactor = 0.2f;

      private Vector3 lastCameraPosition;

      void Start()
      {
          if (cameraTransform == null)
              cameraTransform = Camera.main.transform;

          lastCameraPosition = cameraTransform.position;
      }

      void LateUpdate()
      {
          Vector3 delta = cameraTransform.position - lastCameraPosition;
          transform.position += new Vector3(delta.x * parallaxFactor, delta.y * parallaxFactor, 0f);
          lastCameraPosition = cameraTransform.position;
      }
  }