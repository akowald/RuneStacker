using UnityEngine;
using System.Collections;

public class ParticleHelper : MonoBehaviour
{
	
	void Start ()
	{
		// Ensure that the particle effects show on top of the sprites
		SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
		GetComponent<ParticleSystem>().GetComponent<Renderer>().sortingLayerID = spriteRenderer.sortingLayerID;
		GetComponent<ParticleSystem>().GetComponent<Renderer>().sortingOrder = spriteRenderer.sortingOrder;
	}

	void Update ()
	{
		// Clean up the particle when it finishes running
		if(GetComponent<ParticleSystem>() != null)
		{
			if(!GetComponent<ParticleSystem>().IsAlive())
			{
				Destroy (gameObject);
			}
		}
	}
}
