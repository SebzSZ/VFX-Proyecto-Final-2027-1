using UnityEngine;

public class RotarSkybox : MonoBehaviour
{
    [SerializeField] private float _speed;

    private Material _skybox;
    private float _rotation;

    void Start()
    {
        if (RenderSettings.skybox == null)
        {
            enabled = false;
            return;
        }

        _skybox = new Material(RenderSettings.skybox);
        RenderSettings.skybox = _skybox;

        _rotation = _skybox.GetFloat("_Rotation");
    }

    void Update()
    {
        _rotation = Mathf.Repeat(
            _rotation + _speed * Time.deltaTime,
            360f
        );

        _skybox.SetFloat("_Rotation", _rotation);
    }
}