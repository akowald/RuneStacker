using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum RuneType{Blue=0, Red, Orange, Purple, Yellow, Green};

public class Rune : MonoBehaviour
{
	public RuneType type;

	private GameObject backGlow; // A reference to the rune's glow sprite
	public BoardCoords coords; // A container for the x and y board coordinates

	void Start()
	{
		backGlow = null;

		// Find the back glowly part of the rune
		foreach(Transform child in gameObject.transform)
		{
			if(child.tag == "Glow")
			{
				backGlow = child.gameObject;
			}
		}
	}

	/***
	 * OnMouseUpAsButton is only called when the mouse is released over the same GUIElement or Collider as it was pressed.
	 */
	void OnMouseUpAsButton()
	{
		// For now, commence the fading animation on the back prefab of the rube
		// Find the back part of the rune
		Board.S.OnRuneClick(this);
	}

	/***
	 * Toggle glowing effects on the rune.
	 * 
	 */
	public void ToggleGlow()
	{
		if(backGlow == null) return;

		bool glowing = !(iTween.Count (backGlow, "colo") > 0);
		ToggleGlow(glowing);
	}

	/***
	 * Set whether the rune should be glowing or not.
	 *
	 * @param glowing True to glow, false otherwise.
	 */
	public void ToggleGlow(bool glowing)
	{
		if(backGlow == null) return;

		backGlow.GetComponent<SpriteRenderer>().GetComponent<Renderer>().material.color = new Color(1.0f, 1.0f, 1.0f, 1.0f); // Restore the default alpha on the rune

		if(glowing)
		{
			// Make it glow
			iTween.Stop(backGlow, "colo"); // Stop all color tweens on the glow prefab
			iTween.FadeTo(backGlow, iTween.Hash("alpha", 0.0f, "time", 0.5f, "looptype", "pingPong", "easetype", "easeInOutQuad")); // Start glowing again!
		}else{
			// Stop the glow
			iTween.Stop(backGlow, "colo");
		}
	}

	/***
	 * Causes the rune to shake in the x direction. Intended to indicate an invalid pair but currently unused.
	 * 
	 */
	public void Shake()
	{
		iTween.ShakePosition(this.gameObject, iTween.Hash("x", 0.1f, "time", 0.5f));
	}

	/***
	 * When the sprites pass each other while being swapped, they clip since they have the same sprite renderer layer order.
	 * This method will ensure that the rune doesn't overlap with others.
	 * 
	 */
	public void BringToFront()
	{
		this.GetComponent<SpriteRenderer>().sortingOrder = 10;
		// Set the sorting order on the child sprites
		foreach(Transform child in gameObject.transform)
		{
			if(child.tag == "Glow")
			{
				child.GetComponent<SpriteRenderer>().sortingOrder = 12; // the glow sprite
			}else{
				child.GetComponent<SpriteRenderer>().sortingOrder = 13; // the symbol sprite
			}
		}
	}

	/***
	 * Performs the opposite of BringToFront.
	 * 
	 */
	public void BringToBack()
	{
		this.GetComponent<SpriteRenderer>().sortingOrder = 0;
		// Set the sorting order on the child sprites
		foreach(Transform child in gameObject.transform)
		{
			if(child.tag == "Glow")
			{
				child.GetComponent<SpriteRenderer>().sortingOrder = 2; // the glow sprite
			}else{
				child.GetComponent<SpriteRenderer>().sortingOrder = 3; // the symbol sprite
			}
		}
	}

}
