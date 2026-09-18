using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EnemyViewMesh : MonoBehaviour
{
    [SerializeField] EnemyVision _vision;

    [SerializeField, Range(3, 360)]
    int _segmentCount = 120;


    [Header("플레이어 탐지 테스트")]
    [SerializeField] Color _normalColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] Color _detectedColor = new Color(1f, 0f, 0f, 0.3f);

    MeshRenderer _meshRenderer;
    MaterialPropertyBlock _propertyBlock;

    Mesh _mesh;
    Vector3[] _vertices;
    int[] _triangles;

     void Awake()
    {
        _mesh = new Mesh();
        _mesh.name = "Enemy View Mesh";
        _mesh.MarkDynamic();

        GetComponent<MeshFilter>().sharedMesh = _mesh;

        _meshRenderer = GetComponent<MeshRenderer>();
        _propertyBlock = new MaterialPropertyBlock(); 
        CreateArrays();
    }

     void LateUpdate()
    {
        if(_vision == null) return;
        if(_vertices.Length != _segmentCount + 2)
        {
            CreateArrays();
        }
        UpdateViewColor();
        DrawView(_vision.EyePosition, _vision.GetFlatForward());
    }
   

    void CreateArrays()
    {
        _vertices = new Vector3[_segmentCount + 2];
        _triangles = new int[_segmentCount * 3];

        for(int i=0; i< _segmentCount; i++)
        {
            int index = i * 3;
            _triangles[index] = 0;
            _triangles[index + 1] = i + 1;
            _triangles[index + 2] = i + 2;
        }

        _mesh.Clear();
    }

   

    void UpdateViewColor()
    {
        _meshRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor("_BaseColor", _vision.CanSeePlayer ? _detectedColor : _normalColor);
        _meshRenderer.SetPropertyBlock(_propertyBlock); 
    }
    void DrawView(Vector3 eyePosition,Vector3 forward)
    {
        Vector3 center = new Vector3(eyePosition.x,transform.position.y,eyePosition.z); 

        _vertices[0] = transform.InverseTransformPoint(center);

        float radius = _vision.ViewRadius;
        float startAngle = -_vision.ViewAngle * 0.5f;
        float angleStep = _vision.ViewAngle / _segmentCount;

        for(int i = 0; i <= _segmentCount; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            float distance = radius;
            if (_vision.CheckObstacle(eyePosition,direction,radius,out RaycastHit hit))
            {
                distance = hit.distance;    
            }

            Vector3 worldPoint = center + direction *distance;
            _vertices[i+1] = transform.InverseTransformPoint(worldPoint);   
        }

        _mesh.vertices = _vertices;
        _mesh.triangles = _triangles;
        _mesh.RecalculateBounds();
    }

    void OnDestroy()
    {
        if(_mesh != null)
        {
            Destroy(_mesh);
        }
    }
}
