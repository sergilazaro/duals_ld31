using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class Vec2i
{
	public int i = 0;
	public int j = 0;

	public Vec2i(int i, int j)
	{
		this.i = i;
		this.j = j;
	}

	public static bool operator ==(Vec2i p1, Vec2i p2)
	{
		return (p1.i == p2.i && p1.j == p2.j);
	}

	public static bool operator !=(Vec2i p1, Vec2i p2)
	{
		return (p1.i != p2.i || p1.j != p2.j);
	}

	public override bool Equals(System.Object obj)
	{
		// If parameter is null return false.
		if (obj == null)
		{
			return false;
		}

		// If parameter cannot be cast to Point return false.
		Vec2i p = obj as Vec2i;
		if ((System.Object)p == null)
		{
			return false;
		}

		// Return true if the fields match:
		return (p == this);
	}

	public override int GetHashCode()
	{
		int hash = 13;
		hash = (hash * 7) + i.GetHashCode();
		hash = (hash * 7) + j.GetHashCode();
		return hash;
	} 

	public override string ToString()
	{
		return "(" + i + ", " + j + ")";
	}
}

public class Board : MonoBehaviour
{
	public enum Color
	{
		Blue,
		Red
	}

	public enum State
	{
		Animating,
		Idle
	}

	public GameObject blueButtonPrefab;
	public GameObject redButtonPrefab;
	public GameObject blueWallPrefab;
	public GameObject redWallPrefab;
	public GameObject playerPrefab;
	public GameObject blackButtonPrefab;
	public GameObject solidWallPrefab;

	public AudioClip buttonClip;
	public AudioClip stepClip;
	public AudioClip selectClip;

	public RectTransform levelPanel;
	public Button startButton;
	public RectTransform creditPanel;
	public RectTransform titlePanel;

	public Vector2 startPos = new Vector2(3, 3);

	#region Constants

	public static readonly int BOARD_WIDTH = 6;
	public static readonly int BOARD_HEIGHT = BOARD_WIDTH;
	public static readonly float FLOOR_HEIGHT = 0.008f;
	public static readonly float PUSHED_BUTTON_SCALE_Y = 0.02f;
	public static readonly TimeSpan ANIM_TIME = TimeSpan.FromSeconds(.4);
	public static readonly TimeSpan SLOW_ANIM_TIME = TimeSpan.FromSeconds(1);
	public static readonly TimeSpan EXTRA_LOAD_WAIT = TimeSpan.FromSeconds(0.5);
	public static readonly float REALLY_SMALL_SCALE = 0.0005f;

	public static readonly float LEVEL_PANEL_X = -230;
	public static readonly float TITLE_PANEL_Y = -140;
	public static readonly float CREDIT_PANEL_Y = 190;

	#endregion

	public int currentLevelNum = 0;

	GameObject player;
	Vec2i playerPos;

	bool gameStarted = false;

	Dictionary<Color, List<GameObject>> buttons = new Dictionary<Color, List<GameObject>>();
	Dictionary<Color, List<GameObject>> walls = new Dictionary<Color, List<GameObject>>();
	List<GameObject> solidWalls = new List<GameObject>();
	GameObject[,] tiles = new GameObject[BOARD_WIDTH, BOARD_HEIGHT];
	GameObject exitButton;

	Color pressedColor = Color.Red;
	State state = State.Idle;
	bool animatingElements = false;
	bool animatingPlayer = false;

	public List<Color> pressedColors = new List<Color>()
	{
		Color.Red,
		Color.Red,
		Color.Red,
		Color.Red,
		Color.Red,
		Color.Red,
		Color.Red,
		Color.Red,
		Color.Red,
		Color.Red,
		Color.Red,
		Color.Red,
		Color.Red,
	};

	/*
	 * r,b: buttons
	 * R,B: walls
	 * x: black wall
	 *  : empty
	 * e: exit
	 */ 
	public List<List<string>> levels = new List<List<string>>()
	{
		/*
		new List<string>()
		{
			"      ",
			"      ",
			"      ",
			"      ",
			"      ",
			"      "
		},
		*/
		new List<string>() // good
		{
			"      ",
			"      ",
			"   e  ",
			"      ",
			"      ",
			"      "
		},
		new List<string>() // good
		{
			"    x ",
			" x  x ",
			" x  B ",
			"bxxxx ",
			"   B  ",
			"  xx e"
		},
		new List<string>() // good
		{
			"b  rxe",
			"  r B ",
			"xRxRxx",
			"    xb",
			"r   B ",
			"    x "
		},
		new List<string>() // good
		{
			"   r  ",
			" xxxx ",
			" B ex ",
			" xRxx ",
			"bx xx ",
			" x  b "
		},
		new List<string>() // good
		{
			"xBx x ",
			"ex x x",
			"xBx x ",
			"RxRxBx",
			"x xrxb",
			"bx x x"
		},
		new List<string>() // good
		{
			" xb  x",
			" Rxxx ",
			"xxr   ",
			" xxxBx",
			"bR rxx",
			"  r Be"
		},
		new List<string>() // good
		{
			"  e B ",
			"   B b",
			"xBxxxR",
			"   r  ",
			"xRxxBx",
			"  b   "
		},
		//new List<string>() // bad, first testbed
		//{
		//	"rbRB  ",
		//	"r   RR",
		//	"    BB",
		//	"xrrr  ",
		//	"xbbbBB",
		//	"xx  Be",
		//}
	};

	void Awake()
	{
		ExecuteDelayedAction(0.5f, () => { InitialFadeIn(); });
	}

	public void LoadFirstLevel()
	{
		this.audio.PlayOneShot(buttonClip);

		TweenManager.instance.ExecuteDelayedAction(0.5f,
			() =>
			{
				currentLevelNum = 0;
				LoadLevel(currentLevelNum);
				FadeLevelIn();

				GeneratePlayer();
				gameStarted = true;
			});

		InitialFadeOut();

		startButton.gameObject.SetActive(false);
	}

	private void InitialFadeIn()
	{
		currentLevelNum = 0;
		startButton.gameObject.SetActive(true);

		TweenManager.instance.AddTween(SLOW_ANIM_TIME, (t) => { titlePanel.anchoredPosition = new Vector2(titlePanel.anchoredPosition.x, t); }, 0, TITLE_PANEL_Y, TweenManager.EasingFunction.InvExp);
		TweenManager.instance.AddTween(SLOW_ANIM_TIME, (t) => { creditPanel.anchoredPosition = new Vector2(creditPanel.anchoredPosition.x, t); }, 0, CREDIT_PANEL_Y, TweenManager.EasingFunction.InvExp);
	}

	private void InitialFadeOut()
	{
		TweenManager.instance.AddTween(SLOW_ANIM_TIME, (t) => { titlePanel.anchoredPosition = new Vector2(titlePanel.anchoredPosition.x, t); }, TITLE_PANEL_Y, 0, TweenManager.EasingFunction.InvExp);
		TweenManager.instance.AddTween(SLOW_ANIM_TIME, (t) => { creditPanel.anchoredPosition = new Vector2(creditPanel.anchoredPosition.x, t); }, CREDIT_PANEL_Y, 0, TweenManager.EasingFunction.InvExp);
	}

	void GeneratePlayer()
	{
		if (player != null)
		{
			Destroy(player);
		}

		player = (GameObject) GameObject.Instantiate(playerPrefab, new Vector3(startPos.x, FLOOR_HEIGHT, -startPos.y), Quaternion.identity);
		playerPos = new Vec2i((int)startPos.x, (int)startPos.y);
	}

	char GetTileChar(int i, int j)
	{
		return levels[currentLevelNum][j][i];
	}

	bool IsTileWalkable(Vec2i tile)
	{
		return IsTileWalkable(tile.i, tile.j);
	}

	bool IsTileWalkable(int i, int j)
	{
		switch (GetTileChar(i, j))
		{
			case 'r':
			case 'b':
			case 'e':
			case ' ':
				return true;

			case 'R':
				return (pressedColor == Color.Red);
			case 'B':
				return (pressedColor == Color.Blue);

			case 'x':
				return false;

			default:
				throw new NotImplementedException();
		}
	}

	bool IsTileReachable(int i, int j)
	{
		return IsTileWalkable(i, j);
	}

	bool IsTileButton(int i, int j)
	{
		switch (GetTileChar(i, j))
		{
			case 'r':
			case 'b':
				return true;

			default:
				return false;
		}
	}

	bool IsTileExit(int i, int j)
	{
		switch (GetTileChar(i, j))
		{
			case 'e':
				return true;

			default:
				return false;
		}
	}

	Color CharToColor(char c)
	{
		switch (c)
		{
			case 'r':
			case 'R':
				return Color.Red;

			case 'b':
			case 'B':
				return Color.Blue;

			default:
				throw new NotImplementedException();
		}
	}

	void FadeOutObjectAndDestroy(GameObject g, TimeSpan duration)
	{
		TweenManager.instance.AddTween(
			duration,
			(t) => { g.transform.localScale = new Vector3(g.transform.localScale.x, t, g.transform.localScale.z); },
			g.transform.localScale.y, PUSHED_BUTTON_SCALE_Y, TweenManager.EasingFunction.EaseOutBounce,
			() => { Destroy(g); }
		);
	}

	void FadeInObject(GameObject g, float destScale, TimeSpan duration)
	{
		TweenManager.instance.AddTween(
			duration,
			(t) => { g.transform.localScale = new Vector3(g.transform.localScale.x, t, g.transform.localScale.z); },
			REALLY_SMALL_SCALE, destScale, TweenManager.EasingFunction.EaseOutBounce
		);
	}

	void LoadNewLevel()
	{
		state = State.Animating;

		// clear previous level
		{
			foreach (Color color in buttons.Keys)
			{
				foreach (GameObject g in buttons[color])
				{
					FadeOutObjectAndDestroy(g, SLOW_ANIM_TIME);
				}
			}

			foreach (Color color in walls.Keys)
			{
				foreach (GameObject g in walls[color])
				{
					FadeOutObjectAndDestroy(g, SLOW_ANIM_TIME);
				}
			}

			foreach (GameObject g in solidWalls)
			{
				FadeOutObjectAndDestroy(g, SLOW_ANIM_TIME);
			}

			FadeOutObjectAndDestroy(exitButton, SLOW_ANIM_TIME);

			TweenManager.instance.AddTween(SLOW_ANIM_TIME, (t) => { levelPanel.anchoredPosition = new Vector2(t, levelPanel.anchoredPosition.y); }, LEVEL_PANEL_X, 0, TweenManager.EasingFunction.InvExp);
		}

		TweenManager.instance.ExecuteDelayedAction(SLOW_ANIM_TIME + EXTRA_LOAD_WAIT,
		() =>
		{
			// load new level
			{
				currentLevelNum++;

				if (currentLevelNum >= levels.Count)
				{
					Debug.Log("All levels finished");

					gameStarted = false;
					Destroy(player);
					InitialFadeIn();

					return;
				}

				LoadLevel(currentLevelNum);
			}

			FadeLevelIn();
		});
	}

	void FadeLevelIn()
	{
		state = State.Animating;

		// fade new level in
		{
			foreach (Color color in buttons.Keys)
			{
				foreach (GameObject g in buttons[color])
				{
					FadeInObject(g, g.transform.localScale.y, SLOW_ANIM_TIME);
				}
			}

			foreach (Color color in walls.Keys)
			{
				foreach (GameObject g in walls[color])
				{
					FadeInObject(g, g.transform.localScale.y, SLOW_ANIM_TIME);
				}
			}

			foreach (GameObject g in solidWalls)
			{
				FadeInObject(g, g.transform.localScale.y, SLOW_ANIM_TIME);
			}

			FadeInObject(exitButton, exitButton.transform.localScale.y, SLOW_ANIM_TIME);

			TweenManager.instance.AddTween(SLOW_ANIM_TIME, (t) => { levelPanel.anchoredPosition = new Vector2(t, levelPanel.anchoredPosition.y); }, 0, LEVEL_PANEL_X, TweenManager.EasingFunction.InvExp);
		}

		TweenManager.instance.ExecuteDelayedAction(SLOW_ANIM_TIME + EXTRA_LOAD_WAIT,
		() =>
		{
			Debug.Log("FINISHED");
			state = State.Idle;
		});
	}

	void LoadLevel(int numLevel)
	{
		levelPanel.GetChild(0).GetComponent<Text>().text = "" + (currentLevelNum + 1);
		List<string> levelDef = levels[numLevel];
		pressedColor = pressedColors[numLevel];

		buttons.Clear();
		{
			buttons.Add(Color.Blue, new List<GameObject>());
			buttons.Add(Color.Red, new List<GameObject>());
		}
		walls.Clear();
		{
			walls.Add(Color.Blue, new List<GameObject>());
			walls.Add(Color.Red, new List<GameObject>());
		}
		solidWalls.Clear();

		Vector3 pos = new Vector3(0, FLOOR_HEIGHT, 0);
		for (int i = 0; i < BOARD_WIDTH; i++)
		{
			pos.x = i;

			for (int j = 0; j < BOARD_HEIGHT; j++)
			{
				pos.z = -j;

				GameObject obj;
				switch (levelDef[j][i])
				{
					case 'r':
						obj = (GameObject)GameObject.Instantiate(redButtonPrefab, pos, Quaternion.identity);

						if (pressedColor == Color.Red)
						{
							obj.transform.localScale = new Vector3(obj.transform.localScale.x, PUSHED_BUTTON_SCALE_Y, obj.transform.localScale.z);
						}

						buttons[Color.Red].Add(obj);

						break;

					case 'b':
						obj = (GameObject)GameObject.Instantiate(blueButtonPrefab, pos, Quaternion.identity);

						if (pressedColor == Color.Blue)
						{
							obj.transform.localScale = new Vector3(obj.transform.localScale.x, PUSHED_BUTTON_SCALE_Y, obj.transform.localScale.z);
						}

						buttons[Color.Blue].Add(obj);

						break;

					case 'R':
						obj = (GameObject)GameObject.Instantiate(redWallPrefab, pos, Quaternion.identity);

						if (pressedColor == Color.Red)
						{
							obj.transform.localScale = new Vector3(obj.transform.localScale.x, PUSHED_BUTTON_SCALE_Y, obj.transform.localScale.z);
						}

						walls[Color.Red].Add(obj);

						break;

					case 'B':
						obj = (GameObject)GameObject.Instantiate(blueWallPrefab, pos, Quaternion.identity);

						if (pressedColor == Color.Blue)
						{
							obj.transform.localScale = new Vector3(obj.transform.localScale.x, PUSHED_BUTTON_SCALE_Y, obj.transform.localScale.z);
						}

						walls[Color.Blue].Add(obj);

						break;

					case 'x':
						obj = (GameObject)GameObject.Instantiate(solidWallPrefab, pos, Quaternion.identity);

						solidWalls.Add(obj);
						break;

					case 'e':
						obj = (GameObject)GameObject.Instantiate(blackButtonPrefab, pos, Quaternion.identity);
						exitButton = obj;
						break;

					case ' ':
						obj = null;
						break;

					default:
						throw new NotImplementedException();
				}

				if (obj != null)
				{
					obj.GetComponent<Clickable>().tileX = i;
					obj.GetComponent<Clickable>().tileY = j;
					obj.GetComponent<Clickable>().board = this;

					obj.transform.parent = this.transform;
				}
			}
		}

		foreach (GameObject g in GameObject.FindGameObjectsWithTag("Tile"))
		{
			Clickable c = g.GetComponent<Clickable>();

			tiles[c.tileX, c.tileY] = g;
		}
	}

	bool MoveToTile(int tileX, int tileY)
	{
		Vec2i startPos = playerPos;
		Vec2i goalPos = new Vec2i(tileX, tileY);

		// A*
		{
			//Func<Vec2i, Vec2i, float> heuristic = (start, goal) => { return Math.Abs(goal.i - start.i) + Math.Abs(goal.j - start.j); };
			Func<Vec2i, Vec2i, float> heuristic = (start, goal) => { return Math.Max(Math.Abs(goal.i - start.i), Math.Abs(goal.j - start.j)); };
			Func<Vec2i, Vec2i, float> distance = (start, goal) => { return new Vector2(Math.Abs(goal.i - start.i), Math.Abs(goal.j - start.j)).magnitude; };
			Func<Vec2i, List<Vec2i>> getNeighbors = (pos) =>
			{
				List<Vec2i> neighbors = new List<Vec2i>();

				Action<Vec2i> addIfWalkable = (p) =>
				{
					if (IsTileWalkable(p.i, p.j))
					{
						neighbors.Add(p);
					}
				};

				if (pos.i > 0)
				{
					if (pos.j > 0)
					{
						addIfWalkable(new Vec2i(pos.i - 1, pos.j - 1));
					}

					if (pos.j < BOARD_HEIGHT - 1)
					{
						addIfWalkable(new Vec2i(pos.i - 1, pos.j + 1));
					}

					addIfWalkable(new Vec2i(pos.i - 1, pos.j));
				}

				if (pos.i < BOARD_WIDTH - 1)
				{
					if (pos.j > 0)
					{
						addIfWalkable(new Vec2i(pos.i + 1, pos.j - 1));
					}

					if (pos.j < BOARD_HEIGHT - 1)
					{
						addIfWalkable(new Vec2i(pos.i + 1, pos.j + 1));
					}

					addIfWalkable(new Vec2i(pos.i + 1, pos.j));
				}

				if (pos.j > 0)
				{
					addIfWalkable(new Vec2i(pos.i, pos.j - 1));
				}

				if (pos.j < BOARD_HEIGHT - 1)
				{
					addIfWalkable(new Vec2i(pos.i, pos.j + 1));
				}

				return neighbors;
			};

			HashSet<Vec2i> closed = new HashSet<Vec2i>();
			HashSet<Vec2i> open = new HashSet<Vec2i>();
			Dictionary<Vec2i, Vec2i> cameFrom = new Dictionary<Vec2i, Vec2i>();
			Dictionary<Vec2i, float> gScore = new Dictionary<Vec2i, float>();
			Dictionary<Vec2i, float> fScore = new Dictionary<Vec2i, float>();

			open.Add(startPos);
			gScore[startPos] = 0;
			fScore[startPos] = gScore[startPos] + heuristic(startPos, goalPos);

			bool found = false;
			while (open.Count > 0)
			{
				//Debug.Log(" ### NEW ITER ### ");
				//Debug.Log(" open: " + HashSetToString(open));
				//Debug.Log(" closed: " + HashSetToString(closed));
				//Debug.Log(" cameFrom: " + DictToString(cameFrom));
				//Debug.Log(" gScore: " + DictToString(gScore));
				//Debug.Log(" fScore: " + DictToString(fScore));

				Vec2i current = new Vec2i(-1, -1);
				{
					float lowestF = float.MaxValue;

					foreach (Vec2i pos in open)
					{
						if (fScore.ContainsKey(pos))
						{
							if (fScore[pos] < lowestF)
							{
								lowestF = fScore[pos];
								current = pos;
							}
						}
						else
						{
							throw new Exception("not in f?");
						}
					}
				}

				//Debug.Log(" chose " + current + ", fscore " + fScore[current]);

				if (current == goalPos)
				{
					// RECONSTRUCT PATH
					remainingJumps.Clear();
					remainingJumps.Add(goalPos);

					//Debug.Log("Reconstructing. camefrom " + (cameFrom.ContainsKey(goalPos) ? "contains" : "does NOT Contain") + " goalpos " + goalPos);
					//Debug.Log(" > cameFrom: " + DictToString(cameFrom));

					while (cameFrom.ContainsKey(goalPos))
					{
						goalPos = cameFrom[goalPos];
						remainingJumps.Add(goalPos);
					}

					//Debug.Log("#### FINISHED. Reconstructed Path: " + ListToString(path));

					for (int i = 1; i < remainingJumps.Count; i++)
					{
						Vec2i prev = remainingJumps[i - 1];
						Vec2i curr = remainingJumps[i];

						Debug.DrawLine(
							new Vector3(prev.i, 0.1f, -prev.j),
							new Vector3(curr.i, 0.1f, -curr.j),
							UnityEngine.Color.red,
							1.0f
						);
					}

					remainingJumps.Reverse();

					found = true;
					break;
				}

				open.Remove(current);
				closed.Add(current);

				foreach (Vec2i neighbor in getNeighbors(current))
				{
					if (closed.Contains(neighbor))
					{
						continue;
					}

					float tentativeG = gScore[current] + distance(current, neighbor);

					if (!open.Contains(neighbor) || tentativeG < gScore[neighbor])
					{
						cameFrom[neighbor] = current;
						gScore[neighbor] = tentativeG;
						fScore[neighbor] = gScore[neighbor] + heuristic(neighbor, goalPos);

						if (!open.Contains(neighbor))
						{
							open.Add(neighbor);
						}
					}
				}
			}

			if (!found)
			{
				return false;
			}
			else
			{
				remainingJumps.RemoveAt(0);
				StartNewPlayerJump();
				return true;
			}
		}
	}

	List<Vec2i> remainingJumps = new List<Vec2i>();

	void StartNewPlayerJump()
	{
		if (remainingJumps.Count == 0)
		{
			animatingPlayer = false;
			UpdateAnimatingState();

			return;
		}

		Vec2i startPos = playerPos;
		Vec2i newGoal = remainingJumps[0];
		remainingJumps.RemoveAt(0);

		if (!IsTileWalkable(newGoal))
		{
			animatingPlayer = false;
			UpdateAnimatingState();

			remainingJumps.Clear();
			return;
		}

		float initialY = player.transform.position.y;
		Quaternion initialRotation = Quaternion.identity;

		TweenManager.instance.AddTween(TimeSpan.FromSeconds(ANIM_TIME.TotalSeconds * 1.1f),
			(t) =>
			{
				float MAX_VERT = .5f;

				float vert = (1 - Mathf.Pow(2 * t - 1, 2)) * MAX_VERT;
				float horiz = t;
				float angle = 90 * t;

				player.transform.position = new Vector3(
					startPos.i + (newGoal.i - startPos.i) * horiz,
					initialY + vert,
					-(startPos.j + (newGoal.j - startPos.j) * horiz)
				);

				Quaternion newRotation = initialRotation;

				if (newGoal.i != startPos.i)
				{
					newRotation *= Quaternion.AngleAxis(angle * Mathf.Sign(newGoal.i - startPos.i), Vector3.back);
				}

				if (newGoal.j != startPos.j)
				{
					newRotation *= Quaternion.AngleAxis(angle * Mathf.Sign(newGoal.j - startPos.j), Vector3.left);
				}

				player.transform.GetChild(0).localRotation = newRotation;
			},
			0, 1, TweenManager.EasingFunction.Linear,
			() =>
			{
				playerPos = new Vec2i(newGoal.i, newGoal.j);

				if (IsTileButton(newGoal.i, newGoal.j))
				{
					this.audio.PlayOneShot(buttonClip);
					ChangeColorTo(CharToColor(GetTileChar(newGoal.i, newGoal.j)));
				}
				else if (IsTileExit(newGoal.i, newGoal.j))
				{
					this.audio.PlayOneShot(buttonClip);
					LoadNewLevel();
				}
				else
				{
					this.audio.PlayOneShot(stepClip);
				}

				StartNewPlayerJump();
			}
		);

		animatingPlayer = true;
		UpdateAnimatingState();
	}

	void ChangeColorTo(Color color)
	{
		if (color == pressedColor)
		{
			return;
		}

		// rise previously pressed color
		{
			foreach (GameObject obj in buttons[pressedColor])
			{
				GameObject o = obj;

				TweenManager.instance.AddTween(
					ANIM_TIME,
					(t) => { o.transform.localScale = new Vector3(o.transform.localScale.x, t, o.transform.localScale.z); },
					REALLY_SMALL_SCALE, 1, TweenManager.EasingFunction.EaseOutBounce
				);
			}

			foreach (GameObject obj in walls[pressedColor])
			{
				GameObject o = obj;

				TweenManager.instance.AddTween(
					ANIM_TIME,
					(t) => { o.transform.localScale = new Vector3(o.transform.localScale.x, t, o.transform.localScale.z); },
					REALLY_SMALL_SCALE, 1, TweenManager.EasingFunction.EaseOutBounce
				);
			}
		}

		// press current color
		{
			foreach (GameObject obj in buttons[color])
			{
				GameObject o = obj;

				TweenManager.instance.AddTween(
					ANIM_TIME,
					(t) => { o.transform.localScale = new Vector3(o.transform.localScale.x, t, o.transform.localScale.z); },
					1, PUSHED_BUTTON_SCALE_Y, TweenManager.EasingFunction.EaseOutBounce
				);
			}

			foreach (GameObject obj in walls[color])
			{
				GameObject o = obj;

				TweenManager.instance.AddTween(
					ANIM_TIME,
					(t) => { o.transform.localScale = new Vector3(o.transform.localScale.x, t, o.transform.localScale.z); },
					1, PUSHED_BUTTON_SCALE_Y, TweenManager.EasingFunction.EaseOutBounce
				);
			}
		}

		pressedColor = color;
		animatingElements = true;
		UpdateAnimatingState();

		TweenManager.instance.ExecuteDelayedAction(ANIM_TIME, () =>
		{
			animatingElements = false;
			UpdateAnimatingState();
		});
	}

	public void ClickedOnTile(int tileX, int tileY)
	{
		Debug.Log(Time.frameCount + " CLICK on tile (" + tileX + ", " + tileY + ")");

		if (state == State.Idle && gameStarted)
		{
			if (IsTileWalkable(tileX, tileY))
			{
				tiles[tileX, tileY].transform.GetChild(1).particleSystem.Play();
				this.audio.PlayOneShot(selectClip, 0.5f);

				bool moved = MoveToTile(tileX, tileY);
			}
		}
	}

	void UpdateAnimatingState()
	{
		if (animatingElements || animatingPlayer)
		{
			state = State.Animating;
		}
		else
		{
			state = State.Idle;
		}

		Debug.Log("New state: " + state);
	}

	public static string HashSetToString<T>(HashSet<T> set)
	{
		string s = "{";

		foreach (var v in set)
		{
			s += v + " ";
		}

		return s + "}";
	}

	public static string DictToString<K, V>(Dictionary<K, V> dict)
	{
		string s = "{";

		foreach (var kv in dict)
		{
			s += "(" + kv.Key + ": " + kv.Value + ") ";
		}

		return s + "}";
	}

	public static string ListToString<T>(List<T> list)
	{
		string s = "[";

		foreach (var e in list)
		{
			s += e + " ";
		}

		return s + "]";
	}


	public void ExecuteDelayedAction(TimeSpan delay, Action action)
	{
		ExecuteDelayedAction((float)delay.TotalSeconds, action);
	}

	public void ExecuteDelayedAction(float delay, Action action)
	{
		StartCoroutine(ExecuteDelayedAction_Coroutine(delay, action));
	}

	private IEnumerator ExecuteDelayedAction_Coroutine(float delay, Action action)
	{
		yield return new WaitForSeconds(delay);

		action.Invoke();
	}
}
