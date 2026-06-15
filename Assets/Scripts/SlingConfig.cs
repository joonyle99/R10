using UnityEngine;

[CreateAssetMenu(fileName = "SlingConfig", menuName = "Rising Hook/SlingConfig")]
public class SlingConfig : ScriptableObject
{
    [System.Serializable]
    public struct KickConfig
    {
        [Range(0f, 90f)] public float angle; // 벽 킥 각도: 0 = 벽 반대편 수평, 90 = 수직 위
        public float speed;
    }

    public float slingGravity = 30f;
    public float slingSpeed = 20f;
    public float slingTime = 0.3f;
    public float bonusTime = 0.2f;
    [Range(0f, 90f)] public float wallAngleThreshold = 10f; // 법선이 수평에서 이 각도 이내면 벽
    [Range(-90f, 90f)] public float groundShotMaxAngle = 10f; // 조준 고도각(수평 기준)이 이 값 이하면 땅샷 (포물선 대신 직선 발사)
    public float groundShotAimLineLength = 3f; // 땅샷 조준선 길이
    public float aimTimeScale = 0.15f;
    public float noChargePauseDuration = 0.3f; // 차지 없을 때 피드백용 타임스케일 정지 시간
    public float circleCastRadius = 0.4f;
    public float minDistance = 2f;
    public int maxBounces = 4;
    public int bonusKickCount = 1; // 예산 소진 후에도 벽에 닿으면 주어지는 추가 킥 횟수 (조준선엔 미표시)
    public float wallRepelSpeed = 5f; // 킥 불가 상태로 벽에 닿았을 때 밀려나는 속도 (벽 타고 떨어지는 그림 방지)
    public int baseCharges = 1; // 시작 차지 — 지상 발사도 차지 소모하므로 기본 1개 보장
    public KickConfig kick = new KickConfig { angle = 45f, speed = 50f };
}
