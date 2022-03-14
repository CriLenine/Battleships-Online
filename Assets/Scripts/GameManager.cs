﻿using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public enum State
    {
        PlacingShips,
        Playing
    }

    private static GameManager _instance;

    public static State CurrentState;
    public static bool PlacingShips => CurrentState == State.PlacingShips;
    public static bool Playing => CurrentState == State.Playing;

    public static ShipPlacement ShipPlacement;

    [Space(5)]
    [Header("Game")]

    private int _turn = -1;
    private Player[] _players;

    private static Player Me => _instance._players[MyID];
    private static Player CurrentPlayer => _instance._players[_instance._turn];

    public static int MyID { get; private set; }
    public static bool MyTurn => _instance._turn == MyID;

    [Space(5)]
    [Header("Missile")]

    [SerializeField]
    private Missile _missilePrefab;

    [SerializeField]
    private Vector3 _localSpawnPosition;

    private void Awake()
    {
        _instance = this;

        ShipPlacement = GetComponent<ShipPlacement>();
    }

    public void Boarded()
    {
        ClientManager.Boarded();
    }

    public static void Board(int id, int count)
    {
        MyID = id;
        _instance._players = Map.CreatePlayers(id, count);

        CurrentState = State.PlacingShips;
        UIManager.ShowMenu(UIManager.Menu.Board);
    }

    public static void KillPlayer(int id)
    {
        _instance._players[id].dead = true;
    }

    public static void Play()
    {
        _instance._turn = 0;

        CurrentState = State.Playing;

        UIManager.SetTurn(ClientManager.GetName(_instance._turn));
    }



    public static void Shoot(int id, int x, int y, bool touched, Action onTargetReach = null)
    {
        Player target = _instance._players[id];
        Transform player = CurrentPlayer.transform;

        Vector3 worldSpawnPosition = player.forward * _instance._localSpawnPosition.z + player.right * _instance._localSpawnPosition.x;
        Vector3 from = worldSpawnPosition + _instance._localSpawnPosition;
        Vector3 to = target.GetWorldPosition(x, y);

        ClientManager.Wait = true;

        Missile missile = Instantiate(_instance._missilePrefab, from, Quaternion.identity);
        missile.SetCallbacks(
            onTargetReach,
            delegate ()
            {
                ClientManager.Wait = false;

                UIManager.SetTurn(ClientManager.GetName(CurrentPlayer.Id));
            }
        );

        missile.Shoot(from, to, touched);

        UIManager.ShowShoot(ClientManager.GetName(CurrentPlayer.Id), ClientManager.GetName(target.Id));

        target.SetCellType(x, y, touched ? Cell.Type.ShipHit : Cell.Type.EmptyHit);

        do
        {
            _instance._turn = (_instance._turn + 1) % _instance._players.Length;
        } while (CurrentPlayer.dead);
    }

    public static void Shoot(int id, int x, int y, int shipId, int shipX, int shipY, int shipDir)
    {
        Player target = _instance._players[id];
        GameObject shipPrefab = ShipPlacement.GetShipDataByID(shipId).prefab;

        Shoot(id, x, y, true, delegate ()
        {
            Transform ship;
            if (!target.You)
            {
                ship = Instantiate(shipPrefab, target.GetWorldPosition(shipX, shipY),
                    target.transform.rotation * Quaternion.Euler(90 * shipDir * -Vector3.up), target.transform).transform;
            }
            else
                ship = target.GetShip(x, y).transform;
            ship.localRotation *= Quaternion.Euler(Vector3.right * 1000f);
        });

    }

    public static void RemoveShip(int x, int y)
    {
        Me.RemoveShip(x, y);
    }
}
