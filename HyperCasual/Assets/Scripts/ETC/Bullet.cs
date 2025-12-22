using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float Speed = 5;
    public ParticleSystem OnHitEffect;
    public AudioClip bulletClip;
    public AudioClip onHitClip;

    public bool isTargeting;
    public Transform target;
    public float rotSpeed = 0;
    [Tooltip("평사 여부, true일 경우 y축 방향에서 탄알의 속도는 0이며 상하로 회전할 수 없습니다")]
    public bool isFlatShoot = false;

    private float initialYPosition;

    private void Start()
    {
        // 초기 y 좌표 저장 (평사 모드일 때 y 좌표를 유지하기 위함)
        initialYPosition = transform.position.y;

        if (bulletClip != null)
        {
            var audio = gameObject.AddComponent<AudioSource>();
            audio.clip = bulletClip;
            audio.Play();
        }
    }
    private void Update()
    {
        if (isTargeting == true && target != null)
        {
            Vector3 targetDirection = target.position - transform.position;

            // 평사 모드인 경우 회전을 XZ 평면으로 제한 (상하 회전 금지)
            if (isFlatShoot)
            {
                targetDirection.y = 0;
                targetDirection = targetDirection.normalized;
            }

            transform.forward = Vector3.RotateTowards(transform.forward, targetDirection, rotSpeed * Time.deltaTime, 0.0f);

            // 평사 모드인 경우 transform.forward의 y 성분이 0이 되도록 보장 (XZ 평면에 제한)
            if (isFlatShoot)
            {
                Vector3 flatForward = transform.forward;
                flatForward.y = 0;
                if (flatForward.sqrMagnitude > 0.001f)
                {
                    flatForward = flatForward.normalized;
                    transform.forward = flatForward;
                }
            }
        }
        else
        {
            // 타겟을 추적하지 않아도, 평사 모드라면 forward의 y 성분이 0이 되도록 보장
            if (isFlatShoot)
            {
                Vector3 flatForward = transform.forward;
                flatForward.y = 0;
                if (flatForward.sqrMagnitude > 0.001f)
                {
                    flatForward = flatForward.normalized;
                    transform.forward = flatForward;
                }
            }
        }

        // 이동 (Space.Self 사용, 객체의 forward 방향으로 이동)
        Vector3 forward = Vector3.forward;
        transform.Translate(forward * Speed * Time.deltaTime, Space.Self);

        // 평사 모드인 경우 y 좌표를 강제로 유지 (y 축 속도가 0이 되도록)
        if (isFlatShoot)
        {
            Vector3 pos = transform.position;
            transform.position = new Vector3(pos.x, initialYPosition, pos.z);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        // 충돌한 오브젝트가 'Enemy' 태그일 때만 히트 이펙트 재생 및 탄환 파괴 처리
        if (other == null || !other.gameObject.CompareTag("Enemy"))
        {
            return;
        }

        if (OnHitEffect != null)
        {
            var onHitObj = Instantiate(OnHitEffect, transform.position, Quaternion.identity);
            var onHit = onHitObj.gameObject.AddComponent<BulletAudioTrigger>();
            if (onHitClip != null)
            {
                onHit.onClip = onHitClip;
            }

        }
        Destroy(gameObject);
    }
}