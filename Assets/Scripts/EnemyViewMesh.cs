using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EnemyViewMesh : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    float _viewRadius = 8f;

    [SerializeField, Range(1f, 360f)]
    float _viewAngle = 90f;

    [SerializeField, Range(3, 360)]
    int _segmentCount = 120;

    [SerializeField]
    LayerMask _obstacleMask;

    [SerializeField, Min(0f)]
    float _rayHeight = 1.7f;

    [Header("플레이어 탐지 테스트")]
    [SerializeField] Transform _playerTarget;
    [SerializeField] Color _normalColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] Color _detectedColor = new Color(1f, 0f, 0f, 0.3f);

    MeshRenderer _meshRenderer;
    MaterialPropertyBlock _propertyBlock;

    bool _canSeePlayer;
    public bool CanSeePlayer => _canSeePlayer;
    
    Mesh _mesh;
    Vector3[] _vertices;
    int[] _triangles;

     void Awake()
    {
        _mesh = new Mesh();
        _mesh.name = "Enemy View Mesh";
        _mesh.MarkDynamic();

        GetComponent<MeshFilter>().sharedMesh = _mesh;
        CreateArrays();

        _meshRenderer = GetComponent<MeshRenderer>();
        _propertyBlock = new MaterialPropertyBlock();
    }

     void LateUpdate()
    {
        if(_vertices.Length != _segmentCount + 2)
        {
            CreateArrays();
        }

        Vector3 eyePosition = GetEyePosition();
        Vector3 forward = GetFlatForward();

        CheckPlayer(eyePosition,forward);
        UpdateViewColor();
        DrawView(eyePosition,forward);
    }
    
    Vector3 GetEyePosition()
    {
        return transform.position + Vector3.up * _rayHeight;
    }

    Vector3 GetFlatForward()
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if(forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }
        return forward.normalized;
    }

    bool CheckObstacle(Vector3 origin,Vector3 direction, float distance, out RaycastHit hit)
    {
        return Physics.Raycast(origin, direction, out hit, distance, _obstacleMask, QueryTriggerInteraction.Ignore);
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

    void CheckPlayer(Vector3 eyePosition, Vector3 forward)
    {
        bool detected =IsPlayerVisible(eyePosition, forward);   
        
        if (detected != _canSeePlayer)
        {
                Debug.Log(detected ? "플레이어 발견!" : "플레이어 놓침!", this);
               
         }
        _canSeePlayer = detected;
    }

    bool IsPlayerVisible(Vector3 eyePosition, Vector3 forward)
    {
        if(_playerTarget == null) return false;
        Vector3 toPlayer = _playerTarget.position - eyePosition;
        float distance = toPlayer.magnitude;

        if(distance > _viewRadius) return false;
        if (distance <= 0.001f) return true;
        Vector3 flatToPlayer = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
        float angle = Vector3.Angle(forward,flatToPlayer);

        if (angle > _viewAngle * 0.5f) return false;

        return !CheckObstacle(eyePosition, toPlayer / distance, distance, out _);
    }

    void UpdateViewColor()
    {
        _meshRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor("_BaseColor", _canSeePlayer ? _detectedColor : _normalColor);
        _meshRenderer.SetPropertyBlock(_propertyBlock); 
    }
    void DrawView(Vector3 eyePosition,Vector3 forward)
    {
        Vector3 center = transform.position;

        _vertices[0] = Vector3.zero;

        float startAngle = -_viewAngle * 0.5f;
        float angleStep = _viewAngle / _segmentCount;

        for(int i = 0; i <= _segmentCount; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            float distance = _viewRadius;
            if (CheckObstacle(eyePosition,direction,_viewRadius,out RaycastHit hit))
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
