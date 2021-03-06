using Cinemachine;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private static CameraManager instance;

    public static float transitionDelay;


    [SerializeField] private GameObject cameraPrefab;

    [Space(5)]

    [SerializeField] private Vector3 offSetPosition;

    [Space(5)]

    [SerializeField] private Vector3 offSetRotation;

    [Space(20)]

    [SerializeField] private TextMeshProUGUI playerText;

    private CinemachineClearShot cinemachineClearShot;
    private int index = 0;
    

    private void Awake()
    {
        instance = this;

        cinemachineClearShot = GetComponent<CinemachineClearShot>();
    }

    private void Start()
    {
        transitionDelay = cinemachineClearShot.m_DefaultBlend.m_Time;
    }


    private void ChangeCamera(int index)
    {
        cinemachineClearShot.LiveChild.Priority = 0;
        cinemachineClearShot.ChildCameras[index].Priority = 1;
        playerText.text = Map.GetPlayerById(index).nickName;
    }

    public void Next()
    {
        index = (index + 1) % cinemachineClearShot.ChildCameras.Length;
        ChangeCamera(index);
    }

    public void Previous()
    {
        index = (index - 1);
        if (index < 0)
            index = cinemachineClearShot.ChildCameras.Length - 1;

        ChangeCamera(index);
    }

    public static CinemachineVirtualCamera CreateCamera(Transform objectTransform, Vector3 offSetPosition, Vector3 offSetRotation)
    {
        GameObject cameraObject = Instantiate(instance.cameraPrefab, instance.cameraPrefab.transform.position, objectTransform.rotation);
        cameraObject.transform.position = objectTransform.position + instance.offSetPosition;
        cameraObject.transform.Rotate(instance.offSetRotation);
        cameraObject.transform.SetParent(instance.transform, true);

        CinemachineVirtualCamera cinemachineVirtualCamera = cameraObject.GetComponent<CinemachineVirtualCamera>();

        return cinemachineVirtualCamera;
    }
    public static void InitCamera(List<Transform> playersTransform)
    {
        foreach (Transform playerTransform in playersTransform)
        {
            CreateCamera(playerTransform, instance.offSetPosition, instance.offSetRotation);
        }

        instance.cinemachineClearShot.ChildCameras[Map.myId].Priority = 1;
        instance.playerText.text = Map.GetPlayerById(Map.myId).nickName;
    }

    public static void DestroyCamera(CinemachineVirtualCamera cinemachineVirtualCamera, int nextCamera)
    {

        if (nextCamera < instance.cinemachineClearShot.ChildCameras.Length)
            instance.ChangeCamera(nextCamera);
        else
            instance.ChangeCamera(instance.index);

        Destroy(cinemachineVirtualCamera.gameObject, transitionDelay);

    }

}
