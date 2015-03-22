using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/***
 * This handles game tasks such as monitoring turns and keeping track of the player's score.
 * 
 */
public class Game : MonoBehaviour
{
	public static Game S;
	public Text textScore; // Score: 
	public Text textTargetScore; // Target: 
	public Text textMoves; // Moves: 
	public Text textGameOver; // You win!
	public int startingMoves = 10; // Number of moves the player has to reach a target score
	public int targetScore = 4000; // The target score that has to be reached before moves run out

	private int score; // Current game score
	private int moves; // Number of moves the player currently has
	
	void Start ()
	{
		S = this;
	}

	/***
	 * Called when a new game has begun!
	 * 
	 */
	public void OnGameStart()
	{
		score = 0;
		moves = startingMoves;
		textGameOver.enabled = false;

		UpdateScore();
		UpdateTargetScore();
		UpdateMovesLeft();
	}

	/**
	 * Called when the player makes a match.
	 * Use this function to calculate and display their new score.
	 * 
	 * @param numMatches The number of runes matched.
	 * @param multiplier If the runes cascade down and cause another match, this will be incremented.
	 * @param runeType The type of the rune.
	 * @param vertical True if this match is vertical, false for hotizontal.
	 * @param rowColumn The corresponding row or column that the match is at.
	 * @param matchStart The index that the match starts at.
	 */
	public void OnScoreEvent(int numMatches, int multiplier, RuneType runeType, bool vertical, int rowColumn, int matchStart)
	{
		int points = GetMatchPointValue(numMatches) * multiplier;

		IncrementScore(points);

		Debug.Log(string.Format("Match of {0} is worth: {1} * {2} = {3}!", numMatches, GetMatchPointValue(numMatches), multiplier, GetMatchPointValue(numMatches) * multiplier));

		/* An example of how to grab detailed information about the match
		if(vertical)
		{
			Debug.Log(string.Format("That caused a match of {0}: column {1} rows {2}-{3}!", numMatches, rowColumn, matchStart, matchStart+numMatches-1));
		}else{
			Debug.Log(string.Format("That caused a match of {0}: row {1} columns {2}-{3}!", numMatches, rowColumn, matchStart, matchStart+numMatches-1));
		}
		*/
	}

	/**
	 * Called when the player has used up a move.
	 * 
	 */
	public void OnMoveOver()
	{
		moves--;
		UpdateMovesLeft();

		// Check if the player has run out of moves. If so, end the game.
		if(moves <= 0)
		{
			textGameOver.enabled = true;
			Board.S.PlaySound(Board.SoundClips.GameOver);

			if(score >= targetScore)
			{
				// We won!
				textGameOver.text = "You win!";
			}else{
				// We lost :(
				textGameOver.text = "You lose.";
			}

			Board.S.PauseBoard(true); // Freeze the board so the user can't change it

			Invoke("RestartGame", 5.0f);
		}
	}

	void RestartGame()
	{
		Board.S.RestartBoard(true);
	}

	/***
	 * Increments the current score.
	 * 
	 * @param score The score to add.
	 */
	void IncrementScore(int score)
	{
		this.score += score;
		UpdateScore();
	}

	/***
	 * Sets the score.
	 * 
	 * @param score The new score.
	 */
	void SetScore(int score)
	{
		this.score = score;
		UpdateScore();
	}

	/***
	 * Updates the UI Text score with the current score.
	 * 
	 */
	void UpdateScore()
	{
		textScore.text = string.Format("Score: {0}", score);
	}

	/***
	 * Updates the UI Text target score.
	 * 
	 */
	void UpdateTargetScore()
	{
		textTargetScore.text = string.Format("Target: {0}", targetScore);
	}

	/***
	 * Updates the UI Text moves with the moves left.
	 * 
	 */
	void UpdateMovesLeft()
	{
		textMoves.text = string.Format("Moves: {0}", moves);
	}

	/**
	 * Returns the point value for the given match.
	 * 
	 * @param numMatch The match combination.
	 * @return The points the match is worth.
	 */
	int GetMatchPointValue(int numMatch)
	{
		if(numMatch < 3) return 0;

		if(numMatch == 3) return 60;
		if(numMatch == 4) return 120;
		if(numMatch == 5) return 200;

		return 300 + (numMatch - 6) * 100;
	}
}
