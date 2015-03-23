using UnityEngine; 
using System.Collections.Generic;
using System.Collections;

public class BoardCoords
{
	public int x,y;
	public BoardCoords(int x, int y)
	{
		this.x = x;
		this.y = y;
	}

	public static bool areCoordsAdjacent(BoardCoords c1, BoardCoords c2)
	{
		// Are they on the same row and adjacent?
		if(c1.x == c2.x && (c2.y == c1.y-1 || c2.y == c1.y+1)) return true;
		// Are they on the same column and adjacent?
		if(c1.y == c2.y && (c2.x == c1.x-1 || c2.x == c1.x+1)) return true;

		return false;
	}
}

/***
 * This handles board tasks such as creating the board, finding matches, and animating runes.
 * 
 */
public class Board : MonoBehaviour
{
	static public Board S; // Singleton of Board since there will be only 

	public int boardSize = 5; // Default square size of the board
	public GameObject[] runes; // A list of the rune prefabs that can be used
	private GameObject[,] board; // The array representation of the board [row,col]
	private Rune clickedRune; // The Rune that is currently selected, waiting to be paired
	private Rune swappedRune; // The Rune that will be swapped with the clicked rune
	public GameObject particleExplode; // Explosion particle effect when the runes explode
	private float timeNextAction; // The time the next action should start
	private int multiplier; // The current multiplier for points. Based on how many times the board has cascaded
	private bool paused; // If the board is paused, the player can no longer select runs

	public enum SoundClips{Select=0, Swap, Smash_Common, Smash_Rare, Smash_UltraRare, GameOver};
	public AudioClip[] audioClips; // A list of audio clips that can be played

	public enum GameState{Empty=0, Ready, Swapping, Swapping_Back, Deleting, Cascading, Paused};
	private GameState gameState;

	// Use this for initialization
	void Start ()
	{
		S = this;
		board = new GameObject[boardSize, boardSize];

		RestartBoard();
	}

	/***
	 * Preps for a new game and populates a new board.
	 * 
	 * @param effects True for animation effects, false otherwise.
	 */
	public void RestartBoard(bool effects=false)
	{
		paused = true;
		if(effects)
		{
			// Animate the runes instead of having them disappear instantly
			for(int row=0; row<boardSize; row++)
			{
				for(int col=0; col<boardSize; col++)
				{
					if(board[row,col] != null)
					{
						iTween.FadeTo(board[row,col], iTween.Hash("alpha", 0.0f, "time", 1.5f, "easetype", "linear"));
						//iTween.ScaleTo(board[row,col], iTween.Hash("x", 0.05f, "y", 0.04f, "time", 2.0f, "easetype", "linear"));
					}
				}
			}
			Invoke("OnShrinkCompleted", 3.0f);
			return;
		}

		clickedRune = null;
		swappedRune = null;
		gameState = GameState.Empty;
		paused = false;

		// Populute the board with runes
		InitializeBoard();
		
		// Tell the Game object we are ready
		if(gameState == GameState.Ready)
		{
			Game.S.OnGameStart();
		}
	}

	/***
	 * Ensures that all runes in the board are destroyed.
	 * 
	 */
	public void ClearBoard()
	{
		for(int row=0;row<boardSize;row++)
		{
			for(int col=0;col<boardSize;col++)
			{
				if(board[row, col] != null)
				{
					Destroy(board[row, col]);
					board[row,col] = null;
				}
			}
		}

		gameState = GameState.Empty;
	}

	/***
	 * Clears and populates the board instantly, such as you would want at round start.
	 * Note: The board will not have any matches after this function is called.
	 * 
	 */
	public void InitializeBoard()
	{
		ClearBoard ();

		if(runes.Length == 0)
		{
			Debug.LogWarning("Missing rune prefabs in BoardCreator! The board will be empty.");
			return;
		}

		for(int row=0;row<boardSize;row++)
		{
			for(int col=0;col<boardSize;col++)
			{
				if(board[row,col] == null)
				{
					int randomNumber = GetRandomRune(row, col); // Gets a random rune that won't cause a match
					if(randomNumber == -1) continue;

					// Instantiate the rune and place it directly in its final position on the board
					board[row,col] = Instantiate(runes[randomNumber], new Vector3(row,col,0), Quaternion.identity) as GameObject; 
					SaveRuneCoords(board[row, col], row, col);
				}
				
			}
		}

		gameState = GameState.Ready;
	}

	/***
	 * Saves a reference of where the Rune is on the Rune object.
	 * 
	 */
	private void SaveRuneCoords(GameObject obj, int row, int column)
	{
		Rune rune = board[row,column].GetComponent<Rune>();
		rune.coords = new BoardCoords(row, column);
	}

	/***
	 * Finds a random rune that can be placed without making a match of 3 of more.
	 * 
	 * @param row The row the rune would exist at.
	 * @param column The column the rune would exist at.
	 * @return int The index of a random rune that will not cause a match. -1 for no match found.
	 */
	private int GetRandomRune(int row, int column)
	{
		// Create a list of all the runes that won't cause a match
		List<int> results = new List<int>();
		foreach(GameObject rune in runes)
		{
			int index = (int)rune.GetComponent<Rune>().type;

			// See if placing this rune would cause a match of 3 or more

			// Check for a match in the rune's row
			bool inMatch = false;
			int numMatches = 0;
			for(int i=0; i<boardSize; i++)
			{
				// Start counting when we come across a match
				// When we come across the rune's position, count it as a match
				if(i == column || (board[row,i] != null && (int)board[row,i].GetComponent<Rune>().type == index))
				{
					// We found a match
					numMatches++;

					if(i == column) inMatch = true; // We came across where we want to place the rune
				}else{
					// We did not find a match
					if(inMatch == true) break; // We've gone past where the rune is placed and we did not find a match so there is no point in checking further

					numMatches = 0;
				}
			}
			if(inMatch && numMatches >= 3)
			{
				// We can't place this rune here
				continue;
			}

			// Check for a match in the rune's column
			inMatch = false;
			numMatches = 0;
			for(int i=0; i<boardSize; i++)
			{
				// Start counting when we come across a match
				// When we come across the rune's position, also count it as a match
				if(i == row || (board[i,column] != null && (int)board[i,column].GetComponent<Rune>().type == index))
				{
					// We found a match
					numMatches++;
					
					if(i == row) inMatch = true; // We came across where we want to place the rune
				}else{
					// We did not find a match
					if(inMatch == true) break; // We've gone past where the rune is placed and we did not find a match so there is no point in checking further
					
					numMatches = 0;
				}
			}
			if(inMatch && numMatches >= 3)
			{
				// We can't place this rune here
				continue;
			}

			// We won't have a problem placing this rune here
			results.Add(index);
		}

		if(results.Count == 0)
		{
			Debug.LogError("Failed to find any suitable runes to place on the board!");
			return -1;
		}

		return results[Random.Range (0, results.Count)];
	}

	/***
	 * Called whenever the player clicks a rune.
	 * 
	 * @param rune The rune that was clicked.
	 * 
	 */
	public void OnRuneClick(Rune rune)
	{
		if(gameState != GameState.Ready) return;

		if(clickedRune == null)
		{
			// A rune has not been clicked yet
			rune.ToggleGlow(true);

			clickedRune = rune;
			swappedRune = null;
			PlaySound (SoundClips.Select);
		}else{
			// A rune has already been clicked

			// Did the player click the same rune?
			if(clickedRune == rune)
			{
				return;
			}

			// Check if the runes are adjacent to each other
			if(BoardCoords.areCoordsAdjacent(clickedRune.coords, rune.coords))
			{
				// A valid pair was selected, swap em'
				gameState = GameState.Swapping;
				swappedRune = rune;

				AnimateRuneSwap(clickedRune, swappedRune);
			}else{
				// An invalid pair was selected
				// De-select the current rune
				clickedRune.ToggleGlow(false);
				// Select the rune just clicked
				rune.ToggleGlow(true);

				clickedRune = rune;
				swappedRune = null;
				PlaySound (SoundClips.Select);
			}
		}

	}

	/***
	 * Animates the swapping process for two runes.
	 * 
	 * @param r1 The first rune to be swapped. This one will be on top.
	 * @param r2 The second rune to be swapped.
	 */
	void AnimateRuneSwap(Rune r1, Rune r2)
	{
		// Set the sorting layer on both runes to make sure they do not clip and the swap looks clean
		r1.ToggleGlow(false);
		r1.BringToFront();
		r2.BringToBack();

		Vector3 posRune1 = new Vector3(r1.coords.x, r1.coords.y, 0.0f);
		Vector3 posRune2 = new Vector3(r2.coords.x, r2.coords.y, 0.0f);

		iTween.MoveTo(r1.gameObject, iTween.Hash("position", posRune2, "time", 0.3f, "easeType", "linear"));
		iTween.MoveTo(r2.gameObject, iTween.Hash("position", posRune1, "time", 0.3f, "easeType", "linear", "oncomplete", "OnSwapCompleted", "oncompletetarget", this.gameObject, "oncompleteparams", r2));
		
		PlaySound(SoundClips.Swap);
	}

	/***
	 * Sets up and plays an audio clip.
	 * 
	 * @param sound The sound to play.
	 */
	public void PlaySound(SoundClips sound)
	{
		audio.volume = 1.0f;
		audio.pitch = 1.0f;
		if(sound == SoundClips.Smash_Common || sound == SoundClips.Smash_Rare || sound == SoundClips.Smash_UltraRare)
		{
			audio.volume = 0.7f;
			if(sound == SoundClips.Smash_Common) audio.pitch += 0.25f * multiplier;
		}

		audio.clip = audioClips[(int)sound];
		audio.Play();
	}

	/***
	 * Called when the rune swap animation has completed.
	 * 
	 * @param rune The rune that was just swapped.
	 */
	void OnSwapCompleted(Rune rune)
	{
		if(clickedRune == null || swappedRune == null) return;
		if(gameState != GameState.Swapping && gameState != GameState.Swapping_Back) return;

		SwapRunes(clickedRune.coords.x, clickedRune.coords.y, swappedRune.coords.x, swappedRune.coords.y);

		switch(gameState)
		{
			case GameState.Swapping:
				multiplier = 1;
				if(CheckForMatch() == 0)
				{
					// No matches were found after swapping so swap the two runes back to their original position
					AnimateRuneSwap (clickedRune, swappedRune);

					gameState = GameState.Swapping_Back;
				}else{
					// One or more matches was just found and deleted
					// CheckForMatch() will set the new state
				}
				
				break;
			case GameState.Swapping_Back:
				// The runes have finished swapping back to their original position
				// Allow the user to interact with the board again
				clickedRune = null;
				swappedRune = null;
				
				gameState = GameState.Ready;
				
				break;
		}
	}

	/***
	 * Swaps two runes in the board array.
	 * 
	 * @param oldRow The row to swap from.
	 * @param oldColumn The column to swap from.
	 * @param newRow The row to swap to.
	 * @param newColumn The column to swap to.
	 */
	void SwapRunes(int oldRow, int oldColumn, int newRow, int newColumn)
	{
		GameObject tempObj = board[oldRow,oldColumn];
		board[oldRow,oldColumn] = board[newRow,newColumn];
		board[newRow,newColumn] = tempObj;

		if(board[oldRow,oldColumn] != null) SaveRuneCoords(board[oldRow,oldColumn], oldRow, oldColumn);
		if(board[newRow,newColumn] != null) SaveRuneCoords(board[newRow,newColumn], newRow, newColumn);
	}

	/***
	 * Checks the board for a match of 3 or more.
	 * 
	 * @return The number of matches found.
	 */
	int CheckForMatch()
	{
		int result = 0;
		bool bigMatch = false;
		List<GameObject> explode = new List<GameObject>();
		// Check for a match in all the rows and columns
		for(int row=0; row<boardSize; row++)
		{
			// Check for matches in a column (vertically)
			int numMatches = 0;
			int matchStart = -1;
			int runeType = -1;
			for(int col=0; col<boardSize; col++)
			{
				if(SearchForMatch(row, col, true, ref runeType, ref numMatches, ref matchStart))
				{
					// We found a match in a column (vertical)
					OnFoundMatch(numMatches, true, row, matchStart, runeType, ref explode);
					if(numMatches >= 5) bigMatch = true;
					result++;

					numMatches = 0;
					runeType = -1;
				}
			}
			if(numMatches >= 3)
			{
				// We found a match in a column (vertical)
				OnFoundMatch(numMatches, true, row, matchStart, runeType, ref explode);
				if(numMatches >= 5) bigMatch = true;
				result++;
			}

			// Check for matches in a row (horizontally)
			numMatches = 0;
			matchStart = -1;
			runeType = -1;
			for(int col=0; col<boardSize; col++)
			{
				if(SearchForMatch(col, row, false, ref runeType, ref numMatches, ref matchStart))
				{
					// We found a match in a row (horiztonally)
					OnFoundMatch(numMatches, false, row, matchStart, runeType, ref explode);
					if(numMatches >= 5) bigMatch = true;
					result++;

					numMatches = 0;
					runeType = -1;

				}
			}
			if(numMatches >= 3)
			{
				// We found a match in a row (horizontal)
				OnFoundMatch(numMatches, false, row, matchStart, runeType, ref explode);
				if(numMatches >= 5) bigMatch = true;
				result++;
			}
		}

		if(result > 0)
		{
			// Once all the matches have been removed, move all the runes down and spawn in new ones
			if(bigMatch)
			{
				PlaySound (SoundClips.Smash_UltraRare);
			}else if(multiplier >= 4)
			{
				PlaySound(SoundClips.Smash_Rare);
			}else{
				PlaySound(SoundClips.Smash_Common);
			}

			gameState = GameState.Deleting;
			timeNextAction = Time.time + 0.2f; // A little buffer time between when the runes explode and the new ones fall down
		}

		// Delete all the runes that belong to a match of 3 or more
		foreach(GameObject obj in explode)
		{
			ExplodeRune(obj);
		}

		return result;
	}

	/***
	 * Called when a match is found.
	 * 
	 * @param numMatches The number of matches found.
	 * @param vertical True if the match is vertical, false for horizontal.
	 * @param rowColumn The corresponding row or column that the match is at.
	 * @param matchStart The index that the match starts at.
	 * @param runeType The rune type of the match.
	 */
	void OnFoundMatch(int numMatches, bool vertical, int rowColumn, int matchStart, int runeType, ref List<GameObject> explode)
	{
		Game.S.OnScoreEvent(numMatches, multiplier, (RuneType)runeType, vertical, rowColumn, matchStart);

		for(int i=matchStart; i<matchStart+numMatches; i++)
		{
			if(vertical)
			{
				explode.Add(board[rowColumn,i]);
			}else{
				explode.Add(board[i,rowColumn]);
			}
		}
	}

	/***
	 * This is mainly a helper function for CheckForMatch so I don't have to duplicate code
	 * to search for matches in rows as well as columns.
	 * 
	 */
	bool SearchForMatch(int row, int column, bool vertical, ref int matchType, ref int numMatches, ref int matchStart)
	{
		// Process the given rune. If it is a match, increment the matches, otherwise, reset to zero.
		if(board[row,column] != null)
		{
			int runeType = (int)board[row,column].GetComponent<Rune>().type;
			if(runeType != matchType) // We encountered a new rune type so start counting over
			{
				if(numMatches >= 3) return true;
				numMatches = 0; 
			}
			
			if(numMatches == 0) if(vertical) matchStart = column; else matchStart = row;
			numMatches++;
			matchType = runeType;
		}else{
			if(numMatches >= 3) return true;
			
			numMatches = 0;
			matchType = -1;
		}

		return false;
	}

	/**
	 * Removes the rune and spawns a particle effect.
	 * 
	 * @param obj The rune to remove.
	 */
	void ExplodeRune(GameObject obj)
	{
		Rune rune = obj.GetComponent<Rune>();
		if(rune == null) return;

		// Spawn the explosion
		Instantiate(particleExplode, new Vector3(rune.coords.x, rune.coords.y, 0.0f), Quaternion.identity);
		
		Destroy(obj);
		board[rune.coords.x, rune.coords.y] = null;
	}

	/***
	 * Regenerates any destroyed runes and causes all the runes to cascade into place.
	 * 
	 */
	void CascadeBoard()
	{
		/*
		 * 1. Cascade all the runes down.
		 *    - Start at the bottom and iterate up through a column of runes.
		 *    - For each rune, try and go down until another rune is encountered.
		 *    - If we moved down at least one space, we know that the rune has even space to move down.
		 *    - Call SwawpRunes() to move the rune down to an empty space. (do not physically move it yet)
		 * 2. Spawn new runes in the game at the top (row 10)
		 * 3. Visually move all runes down.
		 *    - Move each rune from its current position to its .coords!
		 */
		// 1. Cascade all the runes down.
		List<GameObject> needToBeMoved = new List<GameObject>();
		for(int column=0; column<boardSize; column++)
		{
			for(int row=0; row<boardSize; row++)
			{
				if(board[column,row] != null)
				{
					// Go down until we encounter another rune
					int under = -1;
					for(int i=row-1; i>=0; i--)
					{
						if(board[column,i] == null)
						{
							under = i;
						}else{
							break;
						}
					}
					if(under != -1)
					{
						// We found an empty spot for this rune
						SwapRunes(column, row, column, under); // Moves the rune down virtually

						needToBeMoved.Add(board[column,under]);
					}
				}
			}
		}

		// 2. Spawn new runes in the top row
		for(int column=0; column<boardSize; column++)
		{
			int topRow = boardSize;
			for(int row=0; row<boardSize;row++)
			{
				if(board[column,row] == null)
				{
					// We found an empty rune so spawn one at the top and have it fall down into this position
					board[column,row] = Instantiate(runes[Random.Range(0, runes.Length)], new Vector3(column, topRow++, 0.0f), Quaternion.identity) as GameObject; 
					SaveRuneCoords(board[column,row], column, row);

					needToBeMoved.Add(board[column,row]);
				}
			}
		}

		// 3. Visually move the runes down
		foreach(GameObject obj in needToBeMoved)
		{
			Rune rune = obj.GetComponent<Rune>();

			Vector3 desiredPos = new Vector3(rune.coords.x, rune.coords.y, 0.0f);

			iTween.MoveTo(obj, iTween.Hash("position", desiredPos, "time", 1.0f, "easeType", "easeOutBounce"));
		}

		gameState = GameState.Cascading;
	}

	/***
	 * Instantly teleports a rune to its correct space.
	 *
	 * @param obj The rune to teleport.
	 */
	void TeleportRune(GameObject obj)
	{
		if(obj == null) return;

		Rune rune = obj.GetComponent<Rune>();

		obj.transform.position = new Vector3(rune.coords.x, rune.coords.y, 0.0f);
	}

	/***
	 * Called on every frame.
	 * 
	 */
	void Update()
	{
		switch(gameState)
		{
			case GameState.Deleting:
				if(Time.time > timeNextAction)
				{
					multiplier++;
					// Move onto the cascade action
					CascadeBoard();
				}
				break;

			case GameState.Cascading:
				// Check that all runes have stopped cascading
				int count = 0;
				for(int i=0; i<boardSize; i++)
				{
					for(int j=0; j<boardSize; j++)
					{
						if(board[i,j] != null) count += iTween.Count(board[i,j], "move");
					}
				}
	            if(count == 0)
				{
					// All runes have stopped
					// Check and delete matches once again
					if(CheckForMatch() == 0)
					{
						Game.S.OnMoveOver();
						
						// No matches were found, let the player interact with the board again
						gameState = GameState.Ready;
					}
				}
				
				break;
		}

		// Restart the board when the R key is pressed
		if(Input.GetKeyUp(KeyCode.R))
		{
			if(gameState == GameState.Ready && !paused)
			{
				RestartBoard(true);
			}
		}
	}

	/***
	 * Pause the board so the player can no longer select any runes.
	 * 
	 * @param pause True to pause board, false otherwise.
	 */
	public void PauseBoard(bool pause)
	{
		paused = pause;
	}

	/***
	 * Restarts the game when the Clear animation has been completed.
	 * 
	 */
	void OnShrinkCompleted()
	{
		RestartBoard();
	}
}
	
/*
 * Flaws:
 * 1. Game will get stuck if there are no possible moves. (hasn't happened yet)
 */

