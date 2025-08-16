using UnityEngine;

public class CameraController : MonoBehaviour
{

    [SerializeField] private Transform _player;
    [SerializeField] private Vector3 _camOffset = new Vector3(0, 5, -10);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    void LateUpdate() {
        if (_player != null) {
            transform.position = _player.position + _camOffset;
        }
    }
}
