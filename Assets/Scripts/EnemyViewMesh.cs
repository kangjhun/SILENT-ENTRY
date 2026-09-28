using UnityEngine;

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
        //실행 중 분할 수를 변경하면 배열과 삼각형을 다시 생성
        if(_vertices.Length != _segmentCount + 2)
        {
            CreateArrays();
        }
        UpdateViewColor();
        DrawView(_vision.EyePosition, _vision.GetFlatForward());
    }
   

    void CreateArrays()
    {
        //중심 정점 1개 + 부채꼴 가장자리 정점(segmentCount + 1개)
        _vertices = new Vector3[_segmentCount + 2];
        //삼각형 하나를 구성하는 정점 인덱스는 3개
        _triangles = new int[_segmentCount * 3];

        for(int i=0; i< _segmentCount; i++)
        {
            int index = i * 3;

            //중심과 이웃한 가장자리 정점 2개를 연결
            _triangles[index] = 0;
            _triangles[index + 1] = i + 1;
            _triangles[index + 2] = i + 2;
        }

        _mesh.Clear();
        //정점 배열을 먼저 등록한 다음 삼각형 인덱스 등록
        _mesh.vertices = _vertices;
        _mesh.triangles = _triangles;
    }

   

    void UpdateViewColor()
    {
        //다른 코드가 지정한 프로퍼티를 유지하고 색상만 변경
        _meshRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor("_BaseColor", _vision.CanSeePlayer ? _detectedColor : _normalColor);
        _meshRenderer.SetPropertyBlock(_propertyBlock); 
    }
    void DrawView(Vector3 eyePosition,Vector3 forward)
    {
        //메쉬는 ViewMesh 오브젝트의 Y 높이에 표시
        Vector3 center = new Vector3(eyePosition.x,transform.position.y,eyePosition.z); 

        //Mesh 정점은 로컬 좌표이므로 월드 좌표를 변환
        _vertices[0] = transform.InverseTransformPoint(center);

        float radius = _vision.ViewRadius;
        //정면을 기준으로 시야각의 절반만큼 왼쪽에서 시작
        float startAngle = -_vision.ViewAngle * 0.5f;
        //각 탐색 방향 사이의 각도
        float angleStep = _vision.ViewAngle / _segmentCount;

        for(int i = 0; i <= _segmentCount; i++)
        {
            float angle = startAngle + angleStep * i;
            //수평 정면 방향을 Y축 기준으로 회전
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            float distance = radius;
            //눈높이에서 장애물을 확인하고, 부딪힌 경우 해당 거리까지만 메쉬를 표시
            if (_vision.CheckObstacle(eyePosition,direction,radius,out RaycastHit hit))
            {
                distance = hit.distance;    
            }

            Vector3 worldPoint = center + direction *distance;
            _vertices[i+1] = transform.InverseTransformPoint(worldPoint);   
        }

        //삼각형 연결은 그대로 두고 정점 위치만 갱신
        _mesh.vertices = _vertices;
        //변경된 정점에 맞춰 렌더링 범위를 다시 계산
        _mesh.RecalculateBounds();
    }

    void OnDestroy()
    {
        //런타임에 직접 생성한 메쉬 정리
        if(_mesh != null)
        {
            Destroy(_mesh);
        }
    }
}
