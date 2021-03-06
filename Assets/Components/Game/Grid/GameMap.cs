﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;


public class GameMap : MonoBehaviour {

	private GameController game;
	private GameGrid grid;
	private Hud hud;
	private Entity selectedEntity;


	public void Init (GameController game, GameGrid grid, Hud hud) {
		this.game = game;
		this.grid = grid;
		this.hud = hud;

		SelectGameTool(GameTools.TILE);
	}


	// ===============================================================
	// Tools
	// ===============================================================

	public void SelectGameToolButton (string str) {
		if (System.String.IsNullOrEmpty(str)) {
			Debug.LogError("GameTool string is empty");
			return;
		}

		GameTools tool = (GameTools)(System.Enum.Parse(typeof(GameTools), str));
		SelectGameTool(tool);
	}


	private void SelectGameTool (GameTools tool) {
		game.tool = tool;

		if (tool == GameTools.NONE) {
			return;
		}

		DeselectSelectedEntity();

		hud.SelectGameTool(tool);

		if (tool == GameTools.PLAY) {
			game.ResetGame();
		}
	}


	private void EnableGameTool (GameTools tool, bool value) {
		hud.EnableGameTool(tool, value);
	}


	// ===============================================================
	// Entities
	// ===============================================================

	private void SelectEntity (Entity entity) {
		Entity[] entities = FindObjectsOfType<Entity>();
		foreach(Entity ent in entities) {
			ent.Deselect();
		}
		
		entity.Select();
		selectedEntity = entity;

		//SelectGameTool(GameTools.NONE);
	}


	private void DeselectSelectedEntity () {
		if (selectedEntity) {
			selectedEntity.Deselect();
			selectedEntity = null;
		}
	}


	private void DeleteEntity (Entity entity) {
		if (selectedEntity == entity) {
			selectedEntity = null;
		}

		if (entity is Player) {
			EnableGameTool(GameTools.PLAYER, true);
		}

		Destroy(entity.gameObject);
	}


	private Obstacle CreateObstacle (int x, int y) {
		Transform parent = grid.container.Find("Obstacles");

		GameObject obj = GameObject.Instantiate(game.prefabs.obstaclePrefab);
		obj.transform.SetParent(parent, false);
		obj.name = "Obstacle";
		Obstacle obstacle = obj.GetComponent<Obstacle>();
		obstacle.Init(grid, x, y);
		return obstacle;
	}

	private Item CreateItem (int x, int y) {
		Transform parent = grid.container.Find("Items");

		GameObject obj = GameObject.Instantiate(game.prefabs.itemPrefab);
		obj.transform.SetParent(parent, false);
		obj.name = "Item";
		Item item = obj.GetComponent<Item>();
		item.Init(grid, x, y);
		return item;
	}

	private Star CreateStar (int x, int y) {
		Transform parent = grid.container.Find("Stars").transform;

		GameObject obj = GameObject.Instantiate(game.prefabs.starPrefab);
		obj.transform.SetParent(parent, false);
		obj.name = "Star";
		Star star = obj.GetComponent<Star>();
		star.Init(grid, x, y);
		return star;
	}

	private Player CreatePlayer (int x, int y) {
		Transform parent = grid.container;

		GameObject obj = GameObject.Instantiate(game.prefabs.playerPrefab);
		obj.transform.SetParent(parent, false);
		obj.name = "Player";
		Player player = obj.GetComponent<Player>();
		player.Init(grid, x, y);

		hud.EnableGameTool(GameTools.PLAYER, false);
		SelectGameTool(GameTools.NONE);

		return player;
	}


	// ===============================================================
	// User Interaction
	// ===============================================================

	private bool isMouseDown = false;
	private TileTypes newTileType;
	private int lastX = -1;
	private int lastY = -1;
	

	void Update () {
		// if we are in play mode, interaction will be handled by GameGrid
		if (game.tool == GameTools.PLAY || game.IsPaused()) {
			return;
		}

		OnMouseInteraction();
	}


	public void OnMouseInteraction() {
		if (EventSystem.current.IsPointerOverGameObject()) {
			if (EventSystem.current.currentSelectedGameObject != null &&
				EventSystem.current.currentSelectedGameObject.GetComponent<Button>()) {
				return;
			}	
		}

		if (Input.GetMouseButtonDown(0)) {
			isMouseDown = true;

			// define new tile type
			if (game.tool == GameTools.TILE) {
				RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
				if (hit.transform !=null && hit.transform.tag == "Tile") {
					Tile tile = hit.transform.GetComponent<Tile>();
					newTileType = tile.type == TileTypes.WATER ? TileTypes.GROUND : TileTypes.WATER;
				}
			}
		}

		if (Input.GetMouseButtonUp(0)) {
			isMouseDown = false;
			lastX = -1;
			lastY = -1;
			DeselectSelectedEntity();
		}

		if (isMouseDown) {
			RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

			if (hit.transform != null) {
				switch (hit.transform.tag) {
				
				case "Tile":
					// interact with tiles
					ClickOnTile(hit.transform.GetComponent<Tile>());
					break;

				case "Entity":
					if (selectedEntity) { return; }
					
					// interact with entities
					Entity entity = hit.transform.GetComponent<Entity>();
					if (Input.GetKey(KeyCode.LeftShift)) {
						DeleteEntity(entity);
					} else {
						SelectEntity(entity);
					}
					break;
				}				
			}
		}
	}


	private void ClickOnTile (Tile tile) {
		// escape if mouse isn't on a different tile
		if (tile.x == lastX && tile.y == lastY) { return; }
		lastX = tile.x;
		lastY = tile.y;

		// if an entity is selected, locate it on selected tile
		if (selectedEntity) {
			if (tile.IsWalkable()) {
				selectedEntity.LocateAtCoords(tile.x, tile.y);
				return;
			}
		}


		// execute different acions depending on selected game tool
		switch(game.tool) {
		case GameTools.TILE:
			grid.ChangeTile(tile, newTileType);
			break;
		case GameTools.OBSTACLE:
			if (tile.IsWalkable()) { CreateObstacle(tile.x, tile.y); }
			break;
		case GameTools.ITEM:
			if (tile.IsWalkable()) { CreateItem(tile.x, tile.y); }
			break;
		case GameTools.STAR:
			if (tile.IsWalkable()) { CreateStar(tile.x, tile.y); }
			break;
		case GameTools.PLAYER:
			if (tile.IsWalkable()) { 
				game.player = CreatePlayer(tile.x, tile.y); 
				game.SetPlayerListeners();
			}
			break;
		}
	}

}
