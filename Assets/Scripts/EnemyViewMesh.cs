using UnityEngine;
//적의 시야 범위를 부채꼴 메쉬로 표시
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EnemyViewMesh : MonoBehaviour
{
    [Header("시야 정보")]
    [SerializeField] EnemyVision _vision;

    [Header("부채꼴 정밀도")]
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
    {//시야를 표시할 메쉬 생성
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
        //분할 수가 바뀌면 메쉬 구성도 다시 생성
        if(_vertices.Length != _segmentCount + 2)
        {
            CreateArrays();
        }
        UpdateViewColor();
        DrawView(_vision.EyePosition, _vision.GetFlatForward());
    }
   

    void CreateArrays()
    {
        //중심점과 부채꼴 가장자리 점 준비
        _vertices = new Vector3[_segmentCount + 2];
        _triangles = new int[_segmentCount * 3];

        for(int i=0; i< _segmentCount; i++)
        {
            int index = i * 3;

            //중심점과 가장자리 점 2개를 연결해 삼각형 구성
            _triangles[index] = 0;
            _triangles[index + 1] = i + 1;
            _triangles[index + 2] = i + 2;
        }

        _mesh.Clear();
        _mesh.vertices = _vertices;
        _mesh.triangles = _triangles;
    }

   

    void UpdateViewColor()
    {
        //기존 설정을 유지하면서 탐지 여부에 따라 색상 변경
        _meshRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor("_BaseColor", _vision.CanSeePlayer ? _detectedColor : _normalColor);
        _meshRenderer.SetPropertyBlock(_propertyBlock); 
    }
    void DrawView(Vector3 eyePosition,Vector3 forward)
    {
        //눈의 수평 위치와 시야 메쉬 오브젝트의 높이를 사용
        Vector3 center = new Vector3(eyePosition.x,transform.position.y,eyePosition.z); 

        //월드 위치를 메쉬에서 사용하는 로컬 위치로 변환
        _vertices[0] = transform.InverseTransformPoint(center);

        float radius = _vision.ViewRadius;
        float startAngle = -_vision.ViewAngle * 0.5f;
        float angleStep = _vision.ViewAngle / _segmentCount;

        for(int i = 0; i <= _segmentCount; i++)
        {
            //시야 왼쪽부터 오른쪽까지 방향을 나누어 검사
            float angle = startAngle + angleStep * i;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            float distance = radius;
            //장애물이 있으면 그 앞까지만 시야 표시
            if (_vision.CheckObstacle(eyePosition,direction,radius,out RaycastHit hit))
            {
                distance = hit.distance;    
            }

            Vector3 worldPoint = center + direction *distance;
            _vertices[i+1] = transform.InverseTransformPoint(worldPoint);   
        }

        //계산한 위치로 메쉬와 표시 범위 갱신
        _mesh.vertices = _vertices;
        _mesh.RecalculateBounds();
    }

    void OnDestroy()
    {
        //직접 생성한 메쉬 정리
        if(_mesh != null)
        {
            Destroy(_mesh);
        }
    }
}
