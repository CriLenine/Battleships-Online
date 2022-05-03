using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    public GameObject canvasSelection;
    public Button submitButton;
    public enum PlayerState
    {
        PlacingChips,
        Waiting,
        Aiming
    }

    public static Main instance;
    public static int currentId = -1;
    public static GameObject currentInstanciatedChip;
    public static float lastRotation;
    public static int nShipsToPlace = 1;
    public static Dictionary<int, Button> chipsButtons = new Dictionary<int, Button>();
    public static Dictionary<int, int> chipsLengths = new Dictionary<int, int>();
    public static Dictionary<PlayerCell.CellType, Material> cellMaterials = new Dictionary<PlayerCell.CellType, Material>();
    public static PlayerState currentState;
    public static bool connected = false;
    public static bool boarded = false;

    [Serializable]
    private struct ShipsData
    {
        public Button button;
        public int shipId;
        public int length;
    }
    [Serializable]
    private struct CellMaterial
    {
        public PlayerCell.CellType cellType;
        public Material material;
    }
    [SerializeField] private ShipsData[] _shipsDatas;
    [SerializeField] private CellMaterial[] _cellsMaterials;
    [SerializeField] private LayerMask _cellsLayer;

    private PlayerCell _currentCell;

    private void Awake()
    {
        instance = this;
        foreach (ShipsData button in _shipsDatas)
        {
            chipsButtons.Add(button.shipId, button.button);
            chipsLengths.Add(button.shipId, button.length);
        }
        foreach (CellMaterial cellMat in _cellsMaterials)
        {
            cellMaterials.Add(cellMat.cellType, cellMat.material);
        }
    }

    private void Update()
    {
        if ((currentState != PlayerState.PlacingChips && !ClientManager.MyTurn) || currentState == PlayerState.Waiting || !connected)
        {
            _currentCell?.MouseExit();
            return;
        }
        //placingShips or myTurn
        PlayerCell tmp;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, _cellsLayer))
        {
            tmp = hitInfo.transform.GetComponent<PlayerCell>();
            if (Input.GetMouseButtonDown(0))
                tmp.PointerClick();
            if (_currentCell != tmp)
            {
                if (_currentCell != null)
                    _currentCell.MouseExit();
                _currentCell = tmp;
                _currentCell.MouseEnter();
            }
        }
        else
            _currentCell?.MouseExit();
        if (currentState == PlayerState.PlacingChips)
        {
            if (Input.GetMouseButtonDown(1))
                RotateChip();

            submitButton.interactable = boarded ? false : nShipsToPlace == 0;
        }
    }

    public void RotateChip()
    {
        if (currentId == -1)
            return;
        lastRotation += 90;
        lastRotation %= 360;
        currentInstanciatedChip.transform.Rotate(Vector3.up * 90);
    }

    public void ChangeCurrentChip(int id)
    {
        Destroy(currentInstanciatedChip);
        if (currentId != -1)
            chipsButtons[currentId].interactable = true;
        chipsButtons[id].interactable = false;
        currentId = id;
    }
}
