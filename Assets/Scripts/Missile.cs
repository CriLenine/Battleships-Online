using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Missile : MonoBehaviour
{
    [SerializeField] private Vector3 offSetCameraPosition;

    [Space(5)]

    [SerializeField] private Vector3 offSetCameraRotation;

    [Space(5)]

    [SerializeField] private float offSetyBezierPoint;

    [Space(5)]

    [SerializeField] private float travelTime;

    [System.NonSerialized] public Vector3 StartPosition;
    [System.NonSerialized] public Vector3 EndPosition;

    private Vector3 interpolatePoint;

    private new CinemachineVirtualCamera camera;

    private bool launchMissile = false;
    private float bezierProgression = 0f;

    private Vector3 nextPosition;

    private void Start()
    {
        transform.position = StartPosition;
        camera = CameraManager.CreateCamera(transform, offSetCameraPosition, offSetCameraRotation);
        camera.Follow = transform;

        CinemachineTransposer transposer = camera.GetCinemachineComponent<CinemachineTransposer>();
        transposer.m_FollowOffset = offSetCameraPosition;

        StartCoroutine(CameraMove());
        StartCoroutine(WaitTransition());

        interpolatePoint = (EndPosition - StartPosition)/2f;
        interpolatePoint.y += offSetyBezierPoint;
    }

    private void Update()
    {
        if(launchMissile && bezierProgression < 1)
        {
            transform.position = nextPosition;

            float coeff = 1 - bezierProgression;
            nextPosition = Mathf.Pow(coeff,2) * StartPosition + 2 * bezierProgression * coeff * interpolatePoint + Mathf.Pow(bezierProgression,2) * EndPosition;
            bezierProgression += Time.deltaTime/ travelTime;
            transform.LookAt(nextPosition);
            camera.transform.LookAt(nextPosition);

            if (bezierProgression >= 1)
                Explosion();
        }
    }

    

    private void OnDestroy()
    {
        CameraManager.DestroyCamera(camera, 8);
    }

    private void Explosion()
    {

    }

    private IEnumerator CameraMove()
    {
        yield return new WaitForFixedUpdate();
        camera.Priority = int.MaxValue;
    }

    private IEnumerator WaitTransition()
    {
        yield return new WaitForSeconds(CameraManager.transitionDelay);
        launchMissile = true;
        float coeff = 1 - bezierProgression;
        nextPosition = Mathf.Pow(coeff, 2) * StartPosition + 2 * bezierProgression * coeff * interpolatePoint + Mathf.Pow(bezierProgression, 2) * EndPosition;
        bezierProgression += Time.deltaTime / travelTime;
    }
}