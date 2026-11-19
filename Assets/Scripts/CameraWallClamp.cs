using UnityEngine;
using Unity.Cinemachine;

[ExecuteAlways]
[SaveDuringPlay]
public class CameraWallClamp : CinemachineExtension
{
    [Header("기준 대상")]
    [SerializeField] Transform _playerRoot;
    [SerializeField] PlayerCover _playerCover;

    [Header("벽 충돌 차단")]
    [SerializeField] LayerMask _obstacleMask;
    [SerializeField] float _pivotHeight = 1.45f;
    [SerializeField, Min(0.02f)] float _probeRadius = 0.12f;
    [SerializeField, Min(0f)] float _wallMargin = 0.22f;
    [SerializeField, Min(0.1f)] float _minDistanceFromPivot = 0.42f;

    [Header("튕김 방지 및 제자리 복귀")]
    [SerializeField] bool _preventSnapBounce = true;
    [SerializeField, Min(0.1f)] float _maxStepDistance = 0.45f;
    [SerializeField, Min(1f)] float _returnSpeed = 24f;

    [Header("높이 치솟음 제한 (일반 이동 시)")]
    [SerializeField] bool _clampHeight = true;
    [SerializeField] float _maxHeightAbovePlayer = 2.55f;

    Vector3 _smoothedCamPos;
    bool _hasSmoothedPos;

    protected override void Awake()
    {
        base.Awake();
        ResolveReferences();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _hasSmoothedPos = false;
    }

    void OnValidate()
    {
        ResolveReferences();
    }

    void ResolveReferences()
    {
        if (_playerRoot == null)
        {
            var vcam = GetComponent<CinemachineCamera>();
            if (vcam != null && vcam.Follow != null)
                _playerRoot = vcam.Follow;
        }

        if (_playerCover == null && _playerRoot != null)
            _playerCover = _playerRoot.GetComponent<PlayerCover>();
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize) return;
        if (!Application.isPlaying || Time.timeScale <= 0f) return;

        ResolveReferences();
        if (_playerRoot == null) return;

        if (_playerCover != null && _playerCover.IsInCover)
        {
            _hasSmoothedPos = false;
            return;
        }

        Vector3 pivot = _playerRoot.position + Vector3.up * _pivotHeight;
        Vector3 desiredPos = state.RawPosition;

        // 1. 높이 치솟음 제한
        if (_clampHeight)
        {
            float maxCamY = _playerRoot.position.y + _maxHeightAbovePlayer;
            if (desiredPos.y > maxCamY)
                desiredPos.y = maxCamY;
        }

        // 2. 플레이어 기준점에서 목표 카메라 위치까지 벽 관통 여부 검사
        Vector3 toCam = desiredPos - pivot;
        float dist = toCam.magnitude;

        if (dist > 0.001f)
        {
            Vector3 dir = toCam / dist;
            if (Physics.SphereCast(pivot, _probeRadius, dir, out RaycastHit hit, dist, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                float safeDist = Mathf.Max(_minDistanceFromPivot, hit.distance - _wallMargin);
                desiredPos = pivot + dir * safeDist;
            }
        }

        // 3. 튕겨 나가는 급격한 좌표 점프 억제 및 제자리 복귀
        if (_preventSnapBounce)
        {
            float safeDt = Mathf.Max(deltaTime, 0.0001f);
            if (!_hasSmoothedPos)
            {
                _smoothedCamPos = desiredPos;
                _hasSmoothedPos = true;
            }
            else
            {
                float jumpDist = Vector3.Distance(_smoothedCamPos, desiredPos);
                if (jumpDist > _maxStepDistance)
                {
                    // 한 번에 크게 튀는 경우 상한 거리만큼만 이동시킨 뒤 빠르게 제자리로 수렴
                    Vector3 stepPos = Vector3.MoveTowards(_smoothedCamPos, desiredPos, _maxStepDistance);
                    _smoothedCamPos = Vector3.Lerp(stepPos, desiredPos, 1f - Mathf.Exp(-_returnSpeed * safeDt));
                }
                else
                {
                    _smoothedCamPos = Vector3.Lerp(_smoothedCamPos, desiredPos, 1f - Mathf.Exp(-_returnSpeed * safeDt));
                }
            }

            // 복귀 과정에서도 벽을 뚫지 않도록 한 번 더 클램프
            Vector3 smoothToCam = _smoothedCamPos - pivot;
            float smoothDist = smoothToCam.magnitude;
            if (smoothDist > 0.001f)
            {
                Vector3 smoothDir = smoothToCam / smoothDist;
                if (Physics.SphereCast(pivot, _probeRadius, smoothDir, out RaycastHit smoothHit, smoothDist, _obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    float safeSmoothDist = Mathf.Max(_minDistanceFromPivot, smoothHit.distance - _wallMargin);
                    _smoothedCamPos = pivot + smoothDir * safeSmoothDist;
                }
            }

            state.RawPosition = _smoothedCamPos;
        }
        else
        {
            state.RawPosition = desiredPos;
        }
    }
}