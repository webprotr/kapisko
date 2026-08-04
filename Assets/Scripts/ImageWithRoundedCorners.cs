using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class ImageWithRoundedCorners : MonoBehaviour
{
    private static readonly int PropsRadius = Shader.PropertyToID("_WidthHeightRadius");
    private static readonly int PropsAspect = Shader.PropertyToID("_AspectRatio");

    [SerializeField] private float radius = 15f;
    
    private Image image;
    private Material material;

    public float Radius
    {
        get => radius;
        set { radius = value; Refresh(); }
    }

    private void OnEnable()
    {
        image = GetComponent<Image>();
        Refresh();
    }

    private void OnValidate()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (image == null) image = GetComponent<Image>();
        
        // URP Shader İsmini Aratıyoruz
        Shader shader = Shader.Find("UI/RoundedCornersURP");
        if (shader == null) 
        {
            Debug.LogWarning("[ImageWithRoundedCorners] 'UI/RoundedCornersURP' shader'ı bulunamadı!");
            return;
        }

        if (material == null || material.shader != shader)
        {
            material = new Material(shader);
            image.material = material;
        }

        Rect rect = image.rectTransform.rect;
        float width = rect.width;
        float height = rect.height;

        if (width <= 0 || height <= 0) return;

        material.SetVector(PropsRadius, new Vector4(width, height, radius, 0));
        material.SetVector(PropsAspect, new Vector4(width / height, 1, 0, 0));
    }
}