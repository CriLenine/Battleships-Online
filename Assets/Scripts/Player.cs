using UnityEngine;

public class Player : MonoBehaviour
{
    public GameObject prefabCell;
    public string nickName;
    public int id;
    public bool you;
    public bool dead = false;
    private const int WIDTH = 10, HEIGHT = 10;
    private float _cellSize;
    private Vector3 _gridStart;
    private PlayerCell[,] _grid;

    private bool _displayShipMenu = false;
    private int _iRemove;
    private int _jRemove;



    public void Initialize()
    {
        _cellSize = prefabCell.transform.localScale.x;
        _grid = new PlayerCell[HEIGHT, WIDTH];
        _gridStart = transform.position +
                    transform.forward * (WIDTH - 1) / 2f * _cellSize -
                    transform.right * (HEIGHT - 1) / 2f * _cellSize
                    +Vector3.up * 10;


        for (int x = 0; x < HEIGHT; x++)
        {
            for (int y = 0; y < WIDTH; y++)
            {
                GameObject cellGo = Instantiate(prefabCell, transform);
                cellGo.transform.position = _gridStart + 
                                            x * _cellSize * transform.right -
                                            _cellSize * y * transform.forward;
                cellGo.transform.localRotation = Quaternion.identity;
                PlayerCell cell = cellGo.GetComponent<PlayerCell>();
                cell.position = new Vector2Int(x, y);
                cell.type = PlayerCell.CellType.None;
                cell.onClick += OnCellClicked;
                _grid[x, y] = cell;
            }
        }
    }
    void OnCellClicked(PlayerCell cell)
    {
        if (_displayShipMenu || dead)
            return;
        if (Main.currentState == Main.PlayerState.Waiting)
            Debug.Log("Ce n'est pas ton tour");
        else if (Main.currentState == Main.PlayerState.Aiming && !you)
        {
            ClientManager.Shoot(id, cell.position.x, cell.position.y);
        }
        else if (Main.currentState == Main.PlayerState.PlacingChips)
        {
            if (Main.currentId != -1)
            {
                if (PlaceChip(cell.position))
                {
                    Main.currentInstanciatedChip = null;
                    Main.currentId = -1;
                }
            }
            else
            {
                if (cell.ship != null)
                {
                    Main.currentInstanciatedChip = cell.ship;
                    _iRemove = cell.position.x;
                    _jRemove = cell.position.y;
                    _displayShipMenu = true;
                }
            }
        }
        Debug.Log(cell.position.ToString() + cell.type.ToString());
    }

    private bool PlaceChip(Vector2Int cellPosition)
    {
        int dir = (int)Main.lastRotation / 90;//sens horaire
        Vector2Int vect = Vector2Int.zero;
        if (dir == 0)
            vect = Vector2Int.right;
        else if (dir == 1)
            vect = Vector2Int.up;
        else if (dir == 2)
            vect = Vector2Int.left;
        else if (dir == 3)
            vect = Vector2Int.down;
        int i = cellPosition.x, j = cellPosition.y;
        int length = Main.chipsLengths[Main.currentId];
        for (int k = 0; k < length; k++)
        {
            if (i < 0 || i >= _grid.GetLength(0) || j < 0 || j >= _grid.GetLength(1))
                return false;
            if (_grid[i, j].ship != null)
                return false;
            i += vect.x;
            j += vect.y;
        }

        i = cellPosition.x;
        j = cellPosition.y;
        Main.currentInstanciatedChip.GetComponentInChildren<Chip>().direction = vect;
        int trigDir = dir % 2 == 1 ? dir + 2 % 4 : dir;
        ClientManager.AddShip(Main.currentId, i, j, trigDir, length);
        --Main.nShipsToPlace;

        for (int k = 0; k < Main.chipsLengths[Main.currentId]; k++)
        {
            _grid[i, j].ship = Main.currentInstanciatedChip;
            i += vect.x;
            j += vect.y;
        }
        return true;
    }

    private void RemoveShip()
    {
        Chip chip = _grid[_iRemove, _jRemove].ship.GetComponentInChildren<Chip>();
        Vector2Int shipDir = chip.direction;
        Vector2Int browseDir = new Vector2Int(shipDir.x, -shipDir.y);
        while ((_iRemove < 0 || _iRemove >= _grid.GetLength(0) || _jRemove < 0 || _jRemove >= _grid.GetLength(1))
            && _grid[_iRemove + browseDir.x, _jRemove + browseDir.y].ship == _grid[_iRemove, _jRemove].ship)
        {
            _iRemove += browseDir.x;
            _jRemove += browseDir.y;
        }
    }

    public Vector3 GetWorldPosition(int i, int j)
    {
        return _gridStart + i * _cellSize * transform.right - j * _cellSize * transform.forward;
    }

    public void EmptyCellHit(int i, int j)
    {
        _grid[i, j].type = PlayerCell.CellType.EmptyHit;
        _grid[i, j].GetComponent<MeshRenderer>().material = Main.cellMaterials[PlayerCell.CellType.EmptyHit];
    }
    public void ShipCellHit(int i, int j)
    {
        _grid[i, j].type = PlayerCell.CellType.ShipHit;
        _grid[i, j].GetComponent<MeshRenderer>().material = Main.cellMaterials[PlayerCell.CellType.ShipHit];
    }

    public GameObject GetShip(int i, int j)
    {
        return _grid[i, j].ship;
    }

    void OnGUI()
    {
        if (_displayShipMenu && !Main.boarded)
        {
            Vector2 position = Camera.main.WorldToScreenPoint(Main.currentInstanciatedChip.transform.position);
            position.y = Screen.height - position.y;
            GUILayout.BeginArea(new Rect(position.x, position.y, 300, 400), GUI.skin.box);

            GUIStyle labelStyle = new GUIStyle("Label");
            labelStyle.fontSize = 32;
            GUILayout.Label("Remove this ship ?", labelStyle);

            GUIStyle buttonStyle = new GUIStyle("Button");
            buttonStyle.fontSize = 32;
            if (GUILayout.Button("Remove it", buttonStyle))
            {
                RemoveShip();
                ClientManager.RemoveShip(_iRemove, _jRemove);
                Main.nShipsToPlace++;
                Main.chipsButtons[Main.currentInstanciatedChip.GetComponentInChildren<Chip>().id].interactable = true;
                Destroy(Main.currentInstanciatedChip);
                Main.currentId = -1;
                Main.currentInstanciatedChip = null;
                _displayShipMenu = false;
            }

            if (GUILayout.Button("Cancel", buttonStyle))
            {
                Main.currentId = -1;
                Main.currentInstanciatedChip = null;
                _displayShipMenu = false;
            }
            GUILayout.EndArea();
        }
    }
}
