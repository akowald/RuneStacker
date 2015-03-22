using UnityEngine;
using System.Collections;

public class ParticleHelper : MonoBehaviour
{
	
	void Start ()
	{
		// Ensure that the particle effects show on top of the sprites
		SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
		particleSystem.renderer.sortingLayerID = spriteRenderer.sortingLayerID;
		particleSystem.renderer.sortingOrder = spriteRenderer.sortingOrder;
	}

	void Update ()
	{
		// Clean up the particle when it finishes running
		if(particleSystem != null)
		{
			if(!particleSystem.IsAlive())
			{
				Destroy (gameObject);
			}
		}
	}
}
