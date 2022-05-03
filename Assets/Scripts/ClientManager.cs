using System.Collections.Generic;
using PlayerIOClient;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class ClientManager : MonoBehaviour
{
	private Queue<Message> messages;

	private Connection server;

	private static ClientManager instance;

	public static int nCurrentPlayer = -1;

	public static bool MyTurn => nCurrentPlayer == Map.myId;

	[SerializeField] private GameObject _gamePanel;
	[SerializeField] private TextMeshProUGUI _currentPlayerText;
	[SerializeField] private GameObject _prefabMissile;
	[SerializeField] private Transform _exampleFiringPoint;
	[SerializeField] private Transform _examplePlayer;
	private Vector3 _offsetMissileSpawn;

	public static bool wait = false;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
			Destroy(gameObject);
		
	}

	private void Start()
	{
		_offsetMissileSpawn = _exampleFiringPoint.position - _examplePlayer.position;
		messages = new Queue<Message>();

		if (PlayerPrefs.HasKey("Username"))
		{
			username.text = PlayerPrefs.GetString("Username");
			roomname.text = PlayerPrefs.GetString("Roomname");
		}
	}

	public void Connect()
	{
		if (username.text.Length < 3 || roomname.text.Length < 3)
			return;

		b_connect.interactable = false;

		PlayerIO.Authenticate(
			"battlechips-tmwm0lz8memju96zesetw",
			"public",
			new Dictionary<string, string> {
				{ "userId", username.text },
			},
			null,
			delegate (Client client) {
				Debug.Log("Successfully connected to Player.IO");

				//client.Multiplayer.DevelopmentServer = new ServerEndpoint("25.59.158.42", 8184);
				client.Multiplayer.DevelopmentServer = new ServerEndpoint("localhost", 8184);

				client.Multiplayer.CreateJoinRoom(
					roomname.text,
					"BattleChip",
					true,
					null,
					null,
					delegate (Connection connection) {
						Debug.Log("Joined Room.");

						PlayerPrefs.SetString("Username", username.text);
						PlayerPrefs.SetString("Roomname", roomname.text);

						server = connection;
						server.OnMessage += OnMessage;
						server.OnDisconnect += OnDisconnect;

						server.Send("Count");
						ShowMenu(Menu.Ready);
					},
					delegate (PlayerIOError error) {
						Debug.Log("Error Joining Room: " + error.ToString());

						ShowMenu(Menu.Connect);
						b_connect.interactable = true;
					}
				);
			},
			delegate (PlayerIOError error) {
				Debug.Log("Error connecting: " + error.ToString());

				ShowMenu(Menu.Connect);
				b_connect.interactable = true;
			}
		);
	}

	public void Ready(bool state)
	{
		server.Send("Ready", state);
	}

	public static void AddShip(int id, int x, int y, int dir, int length)
	{
		Debug.Log("Add");
		instance.server.Send("Add", id, x, y, dir, length);
	}

	public static void RemoveShip(int x, int y)
	{
		Debug.Log("Remove");
		instance.server.Send("Remove", x, y);
	}

	public static void Boarded()
	{
		Debug.Log("Boarded");
		Main.boarded = true;
		instance.server.Send("Boarded");
	}
	public static void Shoot(int id, int x, int y)
	{
		Debug.Log("Shoot");
		instance.server.Send("Shoot", id, x, y);
	}

	private void FixedUpdate()
	{
		while (messages.Count > 0 && !wait)
		{
			Message message = messages.Dequeue();

			switch (message.Type)
			{
				case "Board":
				{
					int id = message.GetInt(0);
					int count = message.GetInt(1);

					string[] playersName = ExtractMessage<string>(message, 2);

					ShowMenu(Menu.None);
					Map.nPlayers = count;
					Map.myId = id;
					Map.Init(playersName);
					Main.connected = true;
					Main.instance.canvasSelection.SetActive(true);

					break;
				}

				case "Count":
				{
					int ready = message.GetInt(0);
					int total = message.GetInt(1);

					playerCount.text = ready + "/" + total + " pret" + (ready > 1 ? "s" : "");

					break;
				}

				case "Shoot":
				{
					int id = message.GetInt(0);
					int x = message.GetInt(1);
					int y = message.GetInt(2);

					bool touched = message.GetBoolean(3);
					bool destroyed = message.GetBoolean(4);

					int idShip = 0, xShip = 0, yShip = 0, dirShip = 0;

					if (destroyed)
					{
						idShip = message.GetInt(5);
						xShip = message.GetInt(6);
						yShip = message.GetInt(7);
						dirShip = message.GetInt(8);
					}

					Transform currentPlayerT = Map.GetPlayerById(nCurrentPlayer).transform;
					Player targetedPlayer = Map.GetPlayerById(id);

					wait = true;
					Debug.Log("Shoot");
					Missile missile = Instantiate(_prefabMissile).GetComponent<Missile>();
					Vector3 relativeOffset = currentPlayerT.forward * _offsetMissileSpawn.z + currentPlayerT.right * _offsetMissileSpawn.x;

					missile.TextGameObject = shootTextGameObject;
					missile.GameUiPrefab = _gamePanel;
					missile.Init(currentPlayerT.position + relativeOffset, targetedPlayer.GetWorldPosition(x, y), touched, id);
					missile.SetText(Map.GetPlayerById(nCurrentPlayer).nickName + " tire sur " + Map.GetPlayerById(id).nickName);

					if (touched)
					{
						// Lancer le missile avec la variable destroyed pour indiquer s'il faut afficher le bateau coul� pendant l'animation
						targetedPlayer.ShipCellHit(x, y);
						if (destroyed)
						{
							Transform shipT;
							if (!targetedPlayer.you)
							{
								Vector3 yRotation = new Vector3(0, -dirShip * 90, 0);
								shipT = Instantiate(Main.chipsButtons[idShip].transform.GetChild(0), targetedPlayer.GetWorldPosition(xShip, yShip),
									targetedPlayer.transform.rotation * Quaternion.Euler(yRotation), targetedPlayer.transform);
							}
							else
								shipT = targetedPlayer.GetShip(x, y).transform;
						missile.SetDestroyedShip(shipT);
						}
					}
					else
					{
						// Case devient rouge
						targetedPlayer.EmptyCellHit(x, y);
					}
					do
					{
						++nCurrentPlayer;
						nCurrentPlayer %= Map.nPlayers;

					} while (Map.GetPlayerById(nCurrentPlayer).dead);
					Player currentPlayer = Map.GetPlayerById(nCurrentPlayer);
					_currentPlayerText.text = $"Tour de {currentPlayer.nickName}";
					Main.currentState = currentPlayer.id == nCurrentPlayer ? Main.PlayerState.Aiming : Main.PlayerState.Waiting;
					break;
				}

				case "Play":
				{
					nCurrentPlayer = 0;
					Player currentPlayer = Map.GetPlayerById(nCurrentPlayer);
					_currentPlayerText.text = $"Tour de {currentPlayer.nickName}";
					Main.currentState = currentPlayer.id == nCurrentPlayer ? Main.PlayerState.Aiming : Main.PlayerState.Waiting;

					Main.instance.canvasSelection.SetActive(false);
					_gamePanel.SetActive(true);
					// La partie vient de commencer tous les joueurs ont r�pondu "Boarded"

					break;
				}

				case "Dead":
				{
					int id = message.GetInt(0);

					Map.GetPlayerById(id).dead = true;
					// Le joueur id vient de perdre son dernier vaisseau

					break;
				}

				case "End":
				{
					int winner = message.GetInt(0);

					ShowMenu(Menu.End);
					SetBoard(winner);

					break;
				}
			}
		}
	}

	private void OnMessage(object sender, Message message)
	{
		messages.Enqueue(message);
	}

	private T[] ExtractMessage<T>(Message message, uint startIndex = 0)
	{
		T[] items = new T[message.Count - startIndex];

		for (uint i = startIndex; i < message.Count; ++i)
			items[i - startIndex] = (T)message[i];

		return items;
	}

	private void OnDisconnect(object sender, string reason)
	{
		Disconnect();
	}

	public static void Disconnect()
	{
		if (instance.server != null)
		{
			instance.server.Disconnect();
			instance.server = null;
		}

		instance.ShowMenu(Menu.Connect);
	}

	private void OnDestroy()
	{
		if (server != null)
			Disconnect();
	}

	#region UI

	[SerializeField]
	private TMP_InputField username;

	[SerializeField]
	private TMP_InputField roomname;

	[SerializeField]
	private TextMeshProUGUI playerCount;

	[SerializeField]
	private GameObject background;

	[SerializeField]
	private GameObject connect;

	[SerializeField]
	private GameObject end;

	[SerializeField]
	private TextMeshProUGUI winnerText;

	[SerializeField]
	private TextMeshProUGUI othersText;

	[SerializeField]
	private Button b_connect;

	[SerializeField]
	private GameObject ready;

	[SerializeField]
	private GameObject shootTextGameObject;

	private enum Menu
	{
		None,
		Play,
		Connect,
		Ready,
		End
	}

	private void ShowMenu(Menu menu)
	{
		background?.SetActive(menu != Menu.None && menu != Menu.Play);
		connect?.SetActive(menu == Menu.Connect);
		ready?.SetActive(menu == Menu.Ready);
		end?.SetActive(menu == Menu.End);
		_gamePanel?.SetActive(menu == Menu.Play);
	}

	private void SetBoard(int winnerID)
    {
		string winner = "Vainqueur :\nZalphug";
		string others = "";

		for (int i = 0; i < Map.playersTransform.Count; ++i)
        {
			Player component = Map.playersTransform[i].GetComponent<Player>();
			if (component.id != winnerID)
				others += component.nickName;
			if (i < Map.playersTransform.Count - 1)
				others += "\n";
		}

		winnerText.text = winner;
		othersText.text = others;
    }

	#endregion
}
